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

        public static bool? Mantik(JsonObject n, string alan, bool zorunlu = true)
        {
            var d = n[alan];
            if (d == null) return zorunlu ? throw new GecersizMesaj() : (bool?)null;
            if (d is JsonValue v && v.TryGetValue(out bool b)) return b;
            throw new GecersizMesaj();
        }

        /// <summary>Metin dizisi. Alan yoksa null.</summary>
        public static List<string> MetinListesi(JsonObject n, string alan)
        {
            var d = n[alan];
            if (d == null) return null;
            if (!(d is JsonArray dizi)) throw new GecersizMesaj();
            var liste = new List<string>(dizi.Count);
            foreach (var x in dizi)
                liste.Add(x is JsonValue v && v.TryGetValue(out string s) ? s : throw new GecersizMesaj());
            return liste;
        }

        // ------------------------------------------------------------ Hamle

        public static Hamle HamleCoz(JsonObject n)
        {
            if (n == null) throw new GecersizMesaj();
            switch (Coz<MiniOyunTuru>(Metin(n, "oyun")))
            {
                case MiniOyunTuru.Tkm: return Hamle.TasKagitMakas(Coz<TkmSecim>(Metin(n, "secim")));
                case MiniOyunTuru.TekCift: return Hamle.TekCift(Sayi(n, "parmak").Value, Mantik(n, "tek", false) ?? false);
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

        /// <summary>Temiz mod: metin gönderilmedi. <c>ihlal</c>: bu odada kaçıncı, <c>sinir</c>'e varınca atılır.</summary>
        public static string Uygunsuz(int ihlal, int sinir) =>
            Yaz(new JsonObject { ["t"] = "hata", ["kod"] = "UYGUNSUZ", ["ihlal"] = ihlal, ["sinir"] = sinir });

        /// <summary>Atılana son mesaj. <c>neden</c>: "kurucu" (kurucu attı) ya da "temiz" (Temiz modda son ihlal).</summary>
        public static string Atildin(string neden) => Yaz(new JsonObject { ["t"] = "hata", ["kod"] = "ATILDIN", ["neden"] = neden });

        /// <summary>Sohbet mesajı. <c>kime</c> null ise genel, doluysa DM (sadece alana ve yazana gider).</summary>
        public static string Sohbet(int id, OyuncuId kimden, OyuncuId? kime, string metin) => Yaz(new JsonObject
        {
            ["t"] = "sohbet",
            ["id"] = "m" + id,
            ["kimden"] = Id(kimden),
            ["kime"] = Id(kime),
            ["metin"] = metin,
        });

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
            ["ceza"] = CezaYaz(o.Ceza),
            ["kaos"] = o.Tur == OlayTuru.KaosBasladi ? Ad(o.Kaos) : null,
        });

        /// <summary>Sunucunun kendi olayı (kural motorunda yok): biri atıldı. <c>neden</c>: <see cref="Atildin"/>.</summary>
        public static string AtildiOlayi(OyuncuId kim, string neden) => Yaz(new JsonObject
        {
            ["t"] = "olay",
            ["olay"] = "atildi",
            ["a"] = null,
            ["b"] = null,
            ["c"] = null,
            ["kim"] = Id(kim),
            ["soru"] = null,
            ["ceza"] = null,
            ["kaos"] = null,
            ["neden"] = neden,
        });

        public static JsonObject AyarlarYaz(Ayarlar a, IEnumerable<string> paketler) => new JsonObject
        {
            ["mod"] = Ad(a.Mod),
            ["miniOyun"] = Ad(a.MiniOyun),
            ["turSayisi"] = a.TurSayisi,
            ["soruSn"] = a.SoruSuresiSn,
            ["cevapSn"] = a.CevapSuresiSn,
            ["hamleSn"] = a.HamleSuresiSn,
            ["ceza"] = Ad(a.CezaSeviyesi),
            ["gorevler"] = a.GizliGorevler,
            ["kaos"] = a.KaosTurlari,
            ["paketler"] = Dizi(paketler, p => p),
        };

        static JsonArray Dizi<T>(IEnumerable<T> liste, Func<T, JsonNode> yaz)
        {
            var dizi = new JsonArray();
            foreach (var x in liste) dizi.Add(yaz(x));
            return dizi;
        }

        static JsonObject CezaYaz(Ceza c) =>
            c == null ? null : new JsonObject { ["tur"] = Ad(c.Tur), ["metin"] = c.Metin, ["kalan"] = c.Kalan };

        static JsonObject GorevYaz(Gorev g) => g == null ? null : new JsonObject
        {
            ["tur"] = Ad(g.Tur),
            ["hedef"] = Id(g.Hedef),
            ["adet"] = g.Adet,
            ["zorluk"] = Ad(g.Zorluk),
        };

        /// <summary>Oyun sonu özeti. Sayaçlar zaten <c>oyuncular</c>'da, burada tekrar edilmez.</summary>
        static JsonObject OzetYaz(OyunOzeti o) => o == null ? null : new JsonObject
        {
            ["oynananTur"] = o.OynananTur,
            ["unvanlar"] = Dizi(o.Unvanlar, u => new JsonObject { ["tur"] = Ad(u.Tur), ["kim"] = Id(u.Kim), ["deger"] = u.Deger }),
            ["gorevler"] = Dizi(o.Gorevler, g => new JsonObject { ["kim"] = Id(g.Kim), ["gorev"] = GorevYaz(g.Gorev), ["basarili"] = g.Basarili }),
            ["ifsaOlanlar"] = Dizi(o.IfsaOlanlar, k => new JsonObject { ["soru"] = k.Soru, ["a"] = Id(k.A), ["b"] = Id(k.B), ["c"] = Id(k.C) }),
            ["curcunaSorulari"] = Dizi(o.CurcunaSorulari, s => s),
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

        static JsonArray EkListesi(IEnumerable<OyuncuEki> oyuncular) =>
            Dizi(oyuncular, o => new JsonObject { ["id"] = Id(o.Id), ["isim"] = o.Isim, ["renk"] = o.Renk, ["bagli"] = o.Bagli });

        /// <summary>Bekleme odasındakine giden durum: oyunda değil, sadece herkese açık bilgi.</summary>
        public static string BeklemeOdasiDurumu(int surum, string oda, OyuncuId kurucu, IEnumerable<OyuncuEki> oyuncular,
            IEnumerable<OyuncuEki> bekleyenler, int tur, int toplamTur, bool oyunBitti) => Yaz(new JsonObject
        {
            ["t"] = "durum",
            ["surum"] = surum,
            ["oda"] = oda,
            ["asama"] = "beklemeOdasi",
            ["kurucu"] = Id(kurucu),
            ["oyuncular"] = EkListesi(oyuncular),
            ["bekleyenler"] = EkListesi(bekleyenler),
            ["tur"] = tur,
            ["toplamTur"] = toplamTur,
            ["oyunBitti"] = oyunBitti,
        });

        public static string LobiDurumu(int surum, string oda, OyuncuId kurucu, IEnumerable<OyuncuEki> oyuncular, Ayarlar ayarlar, IEnumerable<string> paketler)
        {
            var liste = EkListesi(oyuncular);

            return Yaz(new JsonObject
            {
                ["t"] = "durum",
                ["surum"] = surum,
                ["oda"] = oda,
                ["asama"] = "lobi",
                ["kurucu"] = Id(kurucu),
                ["oyuncular"] = liste,
                ["ayarlar"] = AyarlarYaz(ayarlar, paketler),
            });
        }

        /// <param name="ozet">Sadece oyunSonu'nda dolu.</param>
        /// <param name="beklemeKalanMs">Oyun az oyuncu yüzünden duruyorsa bitmesine kalan süre, yoksa null.</param>
        /// <param name="bekleyenler">Bekleme odasındakiler (sonraki oyunda oynayacak).</param>
        public static string OyunDurumu(int surum, string oda, OyuncuId kurucu, Func<OyuncuId, OyuncuEki> ek, Gorunum g, OyunOzeti ozet,
            int? beklemeKalanMs, IEnumerable<OyuncuEki> bekleyenler)
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
                    ["oynadigiMiniOyun"] = o.OynadigiMiniOyun,
                    ["cezaCekme"] = o.CezaCekme,
                    ["yanma"] = o.Yanma,
                    ["bedelOdeme"] = o.BedelOdeme,
                    ["bedelHakki"] = o.BedelHakki,
                    ["kaosKazanma"] = o.KaosKazanma,
                    ["kisitlamalar"] = Dizi(o.Kisitlamalar, CezaYaz),
                    ["afk"] = o.Afk,
                    ["cikti"] = o.Cikti,
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
                    ["katlandi"] = s.Katlandi,
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
                ["kaos"] = Ad(g.Kaos),
                ["isi"] = g.Isi.HasValue ? Ad(g.Isi.Value) : null,
                ["kararVeren"] = Id(g.KararVeren),
                ["oylayanlar"] = Dizi(g.Oylayanlar, x => Id(x)),
                ["oyKullanan"] = g.OyKullanan,
                ["sonOylama"] = g.SonOylama == null ? null : new JsonObject { ["ifsa"] = g.SonOylama.Ifsa, ["gume"] = g.SonOylama.Gume },
                ["bedelOdendi"] = g.BedelOdendi,
                ["turCezalari"] = Dizi(g.TurCezalari, x => new JsonObject { ["kim"] = Id(x.Kim), ["ceza"] = CezaYaz(x.Ceza), ["neden"] = Ad(x.Neden) }),
                ["sana"] = new JsonObject
                {
                    ["soru"] = g.Soru,
                    ["fisilti"] = g.Fisilti,
                    ["atananSoru"] = g.AtananSoru,
                    ["degistirmeHakki"] = g.DegistirmeHakki,
                    ["oneri"] = g.Oneri,
                    ["hamlen"] = HamleYaz(g.Hamlen),
                    ["gorevin"] = GorevYaz(g.Gorevin),
                    ["oyVerdin"] = g.OyVerdin,
                },
                ["ifsa"] = g.Asama == Asama.Ifsa ? new JsonObject { ["soru"] = g.IfsaSoru, ["fisilti"] = g.IfsaFisilti } : null,
                ["ozet"] = OzetYaz(ozet),
                ["bekleme"] = beklemeKalanMs.HasValue ? new JsonObject { ["kalanMs"] = beklemeKalanMs.Value } : null,
                ["bekleyenler"] = EkListesi(bekleyenler),
            });
        }
    }
}
