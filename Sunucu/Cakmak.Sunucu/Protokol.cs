using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Cakmak.Kurallar;

namespace Cakmak.Sunucu
{
    /// <summary>Gelen mesaj kurallara uymuyor. Yönlendirici GECERSIZ_MESAJ'a çevirir.</summary>
    sealed class GecersizMesaj : Exception { }

    /// <summary>JSON ile kural motoru tipleri arasındaki çeviri. Biçim: docs/Protokol.md</summary>
    static class Protokol
    {
        // Türkçe karakterler ç diye kaçmasın, olduğu gibi gitsin. HTML'e gömülmediği için güvenli.
        static readonly JsonSerializerOptions Yazim = new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

        public static string Yaz(JsonObject nesne) => nesne.ToJsonString(Yazim);

        // ------------------------------------------------------------ Adlar

        /// <summary>SoruSorma → soruSorma</summary>
        public static string Ad<T>(T deger) where T : Enum
        {
            var s = deger.ToString();
            return char.ToLowerInvariant(s[0]) + s.Substring(1);
        }

        /// <summary>SiraSendeDegil → SIRA_SENDE_DEGIL</summary>
        public static string HataKodu(Sonuc sonuc) =>
            Regex.Replace(sonuc.ToString(), "(?<!^)([A-Z])", "_$1").ToUpperInvariant();

        public static T Coz<T>(string ad) where T : struct, Enum
        {
            foreach (T deger in Enum.GetValues(typeof(T)))
                if (Ad(deger) == ad) return deger;
            throw new GecersizMesaj();
        }

        public static string Id(OyuncuId id) => id.ToString();
        static string Id(OyuncuId? id) => id?.ToString();

        public static OyuncuId IdCoz(string metin)
        {
            if (metin != null && metin.Length > 1 && metin[0] == 'p'
                && int.TryParse(metin.Substring(1), NumberStyles.None, CultureInfo.InvariantCulture, out var n) && n > 0)
                return new OyuncuId(n);
            throw new GecersizMesaj();
        }

        // ------------------------------------------------------------ Okuma yardımcıları

        public static JsonObject Nesne(string metin)
        {
            if (metin == null) throw new GecersizMesaj();
            try
            {
                var kok = JsonNode.Parse(metin) as JsonObject ?? throw new GecersizMesaj();
                Dolas(kok);
                return kok;
            }
            catch (JsonException) { throw new GecersizMesaj(); }
            // JsonObject alanlarını tembel açar: aynı alan iki kez yazılmışsa ancak açılırken ArgumentException atar.
            catch (ArgumentException) { throw new GecersizMesaj(); }
        }

        /// <summary>Bütün ağacı bir kez açar ki bozukluk burada, oda kilidine girmeden yakalansın.</summary>
        static void Dolas(JsonNode n)
        {
            if (n is JsonObject o) foreach (var alan in o) Dolas(alan.Value);
            else if (n is JsonArray a) foreach (var x in a) Dolas(x);
        }

        public static string Metin(JsonObject n, string alan, bool zorunlu = true)
        {
            var d = n[alan];
            if (d == null) return zorunlu ? throw new GecersizMesaj() : null;
            if (d is JsonValue v && v.TryGetValue(out string s)) return s;
            throw new GecersizMesaj();
        }

        public static int? Sayi(JsonObject n, string alan, bool zorunlu = true)
        {
            var d = n[alan];
            if (d == null) return zorunlu ? throw new GecersizMesaj() : (int?)null;
            if (d is JsonValue v && v.TryGetValue(out int i)) return i;
            throw new GecersizMesaj();
        }

        static bool Mantik(JsonObject n, string alan)
        {
            var d = n[alan];
            if (d == null) return false;
            if (d is JsonValue v && v.TryGetValue(out bool b)) return b;
            throw new GecersizMesaj();
        }

        // ------------------------------------------------------------ Hamle

        public static Hamle HamleCoz(JsonObject n)
        {
            if (n == null) throw new GecersizMesaj();
            switch (Coz<MiniOyunTuru>(Metin(n, "oyun")))
            {
                case MiniOyunTuru.Tkm: return Hamle.TasKagitMakas(Coz<TkmSecim>(Metin(n, "secim")));
                case MiniOyunTuru.TekCift: return Hamle.TekCift(Sayi(n, "parmak").Value, Mantik(n, "tek"));
                case MiniOyunTuru.Zar: return Hamle.ZarAt();
                case MiniOyunTuru.Reaksiyon: return Hamle.Reaksiyon(Sayi(n, "ms").Value);
                default: throw new GecersizMesaj();
            }
        }

        static JsonObject HamleYaz(Hamle? hamle)
        {
            if (!hamle.HasValue) return null;
            var h = hamle.Value;
            var o = new JsonObject { ["oyun"] = Ad(h.Tur) };
            switch (h.Tur)
            {
                case MiniOyunTuru.Tkm: o["secim"] = Ad(h.Tkm); break;
                case MiniOyunTuru.TekCift: o["parmak"] = h.Sayi; o["tek"] = h.TekDiyor; break;
                case MiniOyunTuru.Reaksiyon: o["ms"] = h.Sayi; break;
            }
            return o;
        }

        // ------------------------------------------------------------ Giden mesajlar

        public static string Hata(string kod) => Yaz(new JsonObject { ["t"] = "hata", ["kod"] = kod });

        public static string Hosgeldin(OyuncuId sen, string anahtar, string oda) =>
            Yaz(new JsonObject { ["t"] = "hosgeldin", ["sen"] = Id(sen), ["anahtar"] = anahtar, ["oda"] = oda });

        public static string OlayYaz(Olay o) => Yaz(new JsonObject
        {
            ["t"] = "olay",
            ["olay"] = Ad(o.Tur),
            ["a"] = Id(o.A),
            ["b"] = Id(o.B),
            ["c"] = Id(o.C),
            ["kim"] = Id(o.Kim),
            ["soru"] = o.Soru,
        });

        public static JsonObject AyarlarYaz(Ayarlar a) => new JsonObject
        {
            ["mod"] = Ad(a.Mod),
            ["miniOyun"] = Ad(a.MiniOyun),
            ["turSayisi"] = a.TurSayisi,
            ["soruSn"] = a.SoruSuresiSn,
            ["cevapSn"] = a.CevapSuresiSn,
            ["hamleSn"] = a.HamleSuresiSn,
        };

        /// <summary>Odadaki oyuncunun kural motorunda olmayan bilgileri.</summary>
        public readonly struct OyuncuEki
        {
            public readonly OyuncuId Id;
            public readonly string Isim;
            public readonly int Renk;
            public readonly bool Bagli;

            public OyuncuEki(OyuncuId id, string isim, int renk, bool bagli)
            {
                Id = id; Isim = isim; Renk = renk; Bagli = bagli;
            }
        }

        public static string LobiDurumu(int surum, string oda, OyuncuId kurucu, IEnumerable<OyuncuEki> oyuncular, Ayarlar ayarlar)
        {
            var liste = new JsonArray();
            foreach (var o in oyuncular)
                liste.Add(new JsonObject { ["id"] = Id(o.Id), ["isim"] = o.Isim, ["renk"] = o.Renk, ["bagli"] = o.Bagli });

            return Yaz(new JsonObject
            {
                ["t"] = "durum",
                ["surum"] = surum,
                ["oda"] = oda,
                ["asama"] = "lobi",
                ["kurucu"] = Id(kurucu),
                ["oyuncular"] = liste,
                ["ayarlar"] = AyarlarYaz(ayarlar),
            });
        }

        public static string OyunDurumu(int surum, string oda, OyuncuId kurucu, Func<OyuncuId, OyuncuEki> ek, Gorunum g)
        {
            var liste = new JsonArray();
            foreach (var o in g.Oyuncular)
            {
                var e = ek(o.Id);
                liste.Add(new JsonObject
                {
                    ["id"] = Id(o.Id),
                    ["isim"] = o.Isim,
                    ["renk"] = e.Renk,
                    ["bagli"] = e.Bagli,
                    ["havuzaEkledigi"] = o.HavuzaEkledigi,
                    ["gosterilme"] = o.Gosterilme,
                    ["soruAlma"] = o.SoruAlma,
                    ["soruSorma"] = o.SoruSorma,
                    ["ifsaEttirme"] = o.IfsaEttirme,
                    ["saklama"] = o.Saklama,
                    ["miniOyunKazanma"] = o.MiniOyunKazanma,
                });
            }

            var secilebilir = new JsonArray();
            foreach (var id in g.Secilebilir) secilebilir.Add(Id(id));

            JsonObject son = null;
            if (g.SonMiniOyun != null)
            {
                var s = g.SonMiniOyun;
                son = new JsonObject
                {
                    ["oyun"] = Ad(s.Tur),
                    ["bHamle"] = HamleYaz(s.BHamle),
                    ["cHamle"] = HamleYaz(s.CHamle),
                    ["bZar"] = s.BZar,
                    ["cZar"] = s.CZar,
                    ["zarlaKarar"] = s.ZarlaKarar,
                    ["sureDoldu"] = s.SureDoldu,
                    ["cKazandi"] = s.CKazandi,
                };
            }

            return Yaz(new JsonObject
            {
                ["t"] = "durum",
                ["surum"] = surum,
                ["oda"] = oda,
                ["asama"] = Ad(g.Asama),
                ["tur"] = g.Tur,
                ["toplamTur"] = g.ToplamTur,
                ["kalanMs"] = g.KalanMs,
                ["kurucu"] = Id(kurucu),
                ["a"] = Id(g.A),
                ["b"] = Id(g.B),
                ["c"] = Id(g.C),
                ["oyuncular"] = liste,
                ["secilebilir"] = secilebilir,
                ["miniOyun"] = Ad(g.MiniOyun),
                ["bHamleYapti"] = g.BHamleYapti,
                ["cHamleYapti"] = g.CHamleYapti,
                ["beraberlik"] = g.Beraberlik,
                ["sonMiniOyun"] = son,
                ["sana"] = new JsonObject
                {
                    ["soru"] = g.Soru,
                    ["fisilti"] = g.Fisilti,
                    ["atananSoru"] = g.AtananSoru,
                    ["degistirmeHakki"] = g.DegistirmeHakki,
                    ["oneri"] = g.Oneri,
                    ["hamlen"] = HamleYaz(g.Hamlen),
                },
                ["ifsa"] = g.Asama == Asama.Ifsa ? new JsonObject { ["soru"] = g.IfsaSoru, ["fisilti"] = g.IfsaFisilti } : null,
            });
        }
    }
}
