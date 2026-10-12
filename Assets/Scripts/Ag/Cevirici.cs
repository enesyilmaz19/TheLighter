using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Cakmak.Kurallar;

namespace TheLighter.Ag
{
    /// <summary>
    /// Sunucu mesajlarını kural motorunun tiplerine çevirir (ve tersi). Biçim: docs/Protokol.md.
    /// Kural hesaplamaz: gelen ne ise o. Bozuk ya da tanınmayan mesajda null döner, hiç atmaz.
    /// Tanınmayan bir alan değeri (yeni bir kaos, yeni bir ceza) mesajı düşürmez, o alan varsayılanında kalır:
    /// sunucu istemciden önce güncellenirse oyun yine akar.
    /// </summary>
    public static class Cevirici
    {
        // ------------------------------------------------------------ Adlar

        /// <summary>"soruSorma" → Asama.SoruSorma. Tanınmazsa null.</summary>
        public static T? Ad<T>(string ad) where T : struct, Enum
        {
            if (string.IsNullOrEmpty(ad)) return null;
            foreach (T deger in Enum.GetValues(typeof(T)))
                if (AdYaz(deger) == ad) return deger;
            return null;
        }

        /// <summary>Asama.SoruSorma → "soruSorma"</summary>
        public static string AdYaz<T>(T deger) where T : struct, Enum
        {
            var s = deger.ToString();
            return char.ToLowerInvariant(s[0]) + s.Substring(1);
        }

        /// <summary>"p3" → OyuncuId(3). Bozuksa null.</summary>
        public static OyuncuId? Id(string metin)
        {
            if (metin != null && metin.Length > 1 && metin[0] == 'p'
                && int.TryParse(metin.Substring(1), NumberStyles.None, CultureInfo.InvariantCulture, out int n) && n > 0)
                return new OyuncuId(n);
            return null;
        }

        static OyuncuId? Id(JsonDeger d) => Id(d.Metin);

        /// <summary>"SIRA_SENDE_DEGIL" → Sonuc.SiraSendeDegil. Sunucunun kendi kodlarında null.</summary>
        public static Sonuc? SonucKodu(string kod)
        {
            if (string.IsNullOrEmpty(kod)) return null;
            foreach (Sonuc s in Enum.GetValues(typeof(Sonuc)))
                if (HataKodu(s) == kod) return s;
            return null;
        }

        /// <summary>Sonuc.SiraSendeDegil → "SIRA_SENDE_DEGIL" (sunucunun yazdığı biçim).</summary>
        public static string HataKodu(Sonuc s)
        {
            var ad = s.ToString();
            var sb = new StringBuilder();
            for (int i = 0; i < ad.Length; i++)
            {
                if (i > 0 && char.IsUpper(ad[i])) sb.Append('_');
                sb.Append(char.ToUpperInvariant(ad[i]));
            }
            return sb.ToString();
        }

        // ------------------------------------------------------------ Gelen

        /// <param name="sen">Bu cihazın oyuncusu (hosgeldin'den). Oyun durumunda <see cref="Gorunum.Sen"/> olur.</param>
        /// <returns>Bozuk, tanınmayan ya da zorunlu alanı eksik mesajda null.</returns>
        public static GelenMesaj Coz(string json, OyuncuId? sen = null)
        {
            if (!JsonDeger.Dene(json, out var m) || m.Tur != JsonTuru.Nesne) return null;
            switch (m["t"].Metin)
            {
                case "hosgeldin": return HosgeldinCoz(m);
                case "hata": return HataCoz(m);
                case "olay": return OlayCoz(m);
                case "durum": return DurumCoz(m, sen);
                default: return null;
            }
        }

        static GelenMesaj HosgeldinCoz(JsonDeger m)
        {
            var sen = Id(m["sen"]);
            string anahtar = m["anahtar"].Metin;
            string oda = m["oda"].Metin;
            if (!sen.HasValue || string.IsNullOrEmpty(anahtar) || string.IsNullOrEmpty(oda)) return null;
            return new Hosgeldin { Sen = sen.Value, Anahtar = anahtar, Oda = oda };
        }

        static GelenMesaj HataCoz(JsonDeger m)
        {
            string kod = m["kod"].Metin;
            if (string.IsNullOrEmpty(kod)) return null;
            return new HataMesaji { Kod = kod, Sonuc = SonucKodu(kod) };
        }

        static GelenMesaj OlayCoz(JsonDeger m)
        {
            string ad = m["olay"].Metin;
            var kim = Id(m["kim"]);
            if (ad == "atildi") return new OlayMesaji { Atildi = true, Kim = kim };
            var tur = Ad<OlayTuru>(ad);
            if (!tur.HasValue) return null;
            var olay = new Olay
            {
                Tur = tur.Value,
                A = Id(m["a"]),
                B = Id(m["b"]),
                C = Id(m["c"]),
                Kim = kim,
                Soru = m["soru"].Metin,
                Ceza = CezaCoz(m["ceza"]),
                Kaos = Ad<KaosKurali>(m["kaos"].Metin) ?? KaosKurali.Yok,
            };
            return new OlayMesaji { Olay = olay, Kim = kim };
        }

        static GelenMesaj DurumCoz(JsonDeger m, OyuncuId? sen)
        {
            var surum = m["surum"].Tamsayi;
            string asama = m["asama"].Metin;
            if (!surum.HasValue || asama == null) return null;

            var d = new DurumMesaji
            {
                Surum = surum.Value,
                Oda = m["oda"].Metin,
                Kurucu = Id(m["kurucu"]),
                Oyuncular = OdaListesi(m["oyuncular"]),
                Bekleyenler = OdaListesi(m["bekleyenler"]),
            };

            if (asama == "lobi")
            {
                d.Yer = OdaYeri.Lobi;
                d.Ayarlar = AyarlarCoz(m["ayarlar"]);
                return d;
            }
            if (asama == "beklemeOdasi")
            {
                d.Yer = OdaYeri.BeklemeOdasi;
                d.Tur = m["tur"].TamsayiYa(0);
                d.ToplamTur = m["toplamTur"].TamsayiYa(0);
                d.OyunBitti = m["oyunBitti"].MantikYa(false);
                return d;
            }

            var a = Ad<Asama>(asama);
            if (!a.HasValue) return null;
            d.Yer = OdaYeri.Oyun;
            d.Gorunum = GorunumCoz(m, a.Value, sen);
            d.Tur = d.Gorunum.Tur;
            d.ToplamTur = d.Gorunum.ToplamTur;
            d.BeklemeKalanMs = m["bekleme"]["kalanMs"].Tamsayi;
            d.Ozet = OzetCoz(m["ozet"], d.Gorunum.Oyuncular);
            return d;
        }

        static List<OdaOyuncusu> OdaListesi(JsonDeger dizi)
        {
            var liste = new List<OdaOyuncusu>();
            foreach (var o in dizi.Elemanlar)
            {
                var id = Id(o["id"]);
                if (!id.HasValue) continue;
                liste.Add(new OdaOyuncusu
                {
                    Id = id.Value,
                    Isim = o["isim"].Metin ?? "",
                    Renk = o["renk"].TamsayiYa(id.Value.Deger - 1),
                    Bagli = o["bagli"].MantikYa(true),
                });
            }
            return liste;
        }

        static OdaAyarlari AyarlarCoz(JsonDeger a)
        {
            var s = new OdaAyarlari();
            if (!a.Var) return s;
            s.Mod = Ad<OyunModu>(a["mod"].Metin) ?? s.Mod;
            s.MiniOyun = Ad<MiniOyunTuru>(a["miniOyun"].Metin) ?? s.MiniOyun;
            s.TurSayisi = a["turSayisi"].TamsayiYa(s.TurSayisi);
            s.SoruSn = a["soruSn"].TamsayiYa(s.SoruSn);
            s.CevapSn = a["cevapSn"].TamsayiYa(s.CevapSn);
            s.HamleSn = a["hamleSn"].TamsayiYa(s.HamleSn);
            s.Ceza = Ad<CezaSeviyesi>(a["ceza"].Metin) ?? s.Ceza;
            s.Gorevler = a["gorevler"].MantikYa(false);
            s.Kaos = a["kaos"].MantikYa(false);
            foreach (var p in a["paketler"].Elemanlar)
                if (p.Metin != null) s.Paketler.Add(p.Metin);
            return s;
        }

        static Gorunum GorunumCoz(JsonDeger m, Asama asama, OyuncuId? sen)
        {
            var sana = m["sana"];
            var ifsa = m["ifsa"];
            var g = new Gorunum
            {
                Sen = sen ?? default,
                Asama = asama,
                Tur = m["tur"].TamsayiYa(0),
                ToplamTur = m["toplamTur"].TamsayiYa(0),
                KalanMs = Math.Max(0, m["kalanMs"].TamsayiYa(0)),
                Oyuncular = OyuncuDurumlari(m["oyuncular"]),
                A = Id(m["a"]),
                B = Id(m["b"]),
                C = Id(m["c"]),
                Secilebilir = IdListesi(m["secilebilir"]),
                MiniOyun = Ad<MiniOyunTuru>(m["miniOyun"].Metin) ?? MiniOyunTuru.Tkm,
                BHamleYapti = m["bHamleYapti"].MantikYa(false),
                CHamleYapti = m["cHamleYapti"].MantikYa(false),
                Beraberlik = m["beraberlik"].TamsayiYa(0),
                SonMiniOyun = SonMiniOyunCoz(m["sonMiniOyun"]),

                Soru = sana["soru"].Metin,
                Fisilti = sana["fisilti"].MantikYa(false),
                AtananSoru = sana["atananSoru"].Metin,
                DegistirmeHakki = sana["degistirmeHakki"].MantikYa(false),
                Oneri = sana["oneri"].Metin,
                Hamlen = HamleCoz(sana["hamlen"]),
                Gorevin = GorevCoz(sana["gorevin"]),
                OyVerdin = sana["oyVerdin"].MantikYa(false),

                IfsaSoru = ifsa["soru"].Metin,
                IfsaFisilti = ifsa["fisilti"].MantikYa(false),

                Kaos = Ad<KaosKurali>(m["kaos"].Metin) ?? KaosKurali.Yok,
                Isi = Ad<IsiSeviyesi>(m["isi"].Metin),
                KararVeren = Id(m["kararVeren"]),
                Oylayanlar = IdListesi(m["oylayanlar"]),
                OyKullanan = m["oyKullanan"].TamsayiYa(0),
                SonOylama = m["sonOylama"].Tur == JsonTuru.Nesne
                    ? new OylamaSonucu { Ifsa = m["sonOylama"]["ifsa"].TamsayiYa(0), Gume = m["sonOylama"]["gume"].TamsayiYa(0) }
                    : null,
                BedelOdendi = m["bedelOdendi"].MantikYa(false),
                TurCezalari = TurCezalari(m["turCezalari"]),
            };
            return g;
        }

        static List<OyuncuDurumu> OyuncuDurumlari(JsonDeger dizi)
        {
            var liste = new List<OyuncuDurumu>();
            foreach (var o in dizi.Elemanlar)
            {
                var id = Id(o["id"]);
                if (!id.HasValue) continue;
                var kisit = new List<Ceza>();
                foreach (var k in o["kisitlamalar"].Elemanlar)
                {
                    var c = CezaCoz(k);
                    if (c != null) kisit.Add(c);
                }
                liste.Add(new OyuncuDurumu
                {
                    Id = id.Value,
                    Isim = o["isim"].Metin ?? "",
                    HavuzaEkledigi = o["havuzaEkledigi"].TamsayiYa(0),
                    Gosterilme = o["gosterilme"].TamsayiYa(0),
                    SoruAlma = o["soruAlma"].TamsayiYa(0),
                    SoruSorma = o["soruSorma"].TamsayiYa(0),
                    IfsaEttirme = o["ifsaEttirme"].TamsayiYa(0),
                    Saklama = o["saklama"].TamsayiYa(0),
                    MiniOyunKazanma = o["miniOyunKazanma"].TamsayiYa(0),
                    OynadigiMiniOyun = o["oynadigiMiniOyun"].TamsayiYa(0),
                    CezaCekme = o["cezaCekme"].TamsayiYa(0),
                    Yanma = o["yanma"].TamsayiYa(0),
                    BedelOdeme = o["bedelOdeme"].TamsayiYa(0),
                    BedelHakki = o["bedelHakki"].TamsayiYa(0),
                    KaosKazanma = o["kaosKazanma"].TamsayiYa(0),
                    Kisitlamalar = kisit,
                    Afk = o["afk"].MantikYa(false),
                    Cikti = o["cikti"].MantikYa(false),
                });
            }
            return liste;
        }

        static List<OyuncuId> IdListesi(JsonDeger dizi)
        {
            var liste = new List<OyuncuId>();
            foreach (var x in dizi.Elemanlar)
            {
                var id = Id(x);
                if (id.HasValue) liste.Add(id.Value);
            }
            return liste;
        }

        static Ceza CezaCoz(JsonDeger c)
        {
            if (c.Tur != JsonTuru.Nesne) return null;
            var tur = Ad<CezaTuru>(c["tur"].Metin);
            if (!tur.HasValue) return null;
            return new Ceza { Tur = tur.Value, Metin = c["metin"].Metin, Kalan = c["kalan"].TamsayiYa(0) };
        }

        static List<CezaCekimi> TurCezalari(JsonDeger dizi)
        {
            var liste = new List<CezaCekimi>();
            foreach (var x in dizi.Elemanlar)
            {
                var kim = Id(x["kim"]);
                var ceza = CezaCoz(x["ceza"]);
                if (!kim.HasValue || ceza == null) continue;
                liste.Add(new CezaCekimi { Kim = kim.Value, Ceza = ceza, Neden = Ad<CekimNedeni>(x["neden"].Metin) ?? CekimNedeni.Kaybetti });
            }
            return liste;
        }

        static Gorev GorevCoz(JsonDeger g)
        {
            if (g.Tur != JsonTuru.Nesne) return null;
            var tur = Ad<GorevTuru>(g["tur"].Metin);
            if (!tur.HasValue) return null;
            return new Gorev
            {
                Tur = tur.Value,
                Hedef = Id(g["hedef"]),
                Adet = g["adet"].TamsayiYa(1),
                Zorluk = Ad<Zorluk>(g["zorluk"].Metin) ?? Zorluk.Orta,
            };
        }

        public static Hamle? HamleCoz(JsonDeger h)
        {
            if (h.Tur != JsonTuru.Nesne) return null;
            switch (Ad<MiniOyunTuru>(h["oyun"].Metin))
            {
                case MiniOyunTuru.Tkm:
                    var secim = Ad<TkmSecim>(h["secim"].Metin);
                    return secim.HasValue ? Hamle.TasKagitMakas(secim.Value) : (Hamle?)null;
                case MiniOyunTuru.TekCift:
                    var parmak = h["parmak"].Tamsayi;
                    return parmak.HasValue ? Hamle.TekCift(parmak.Value, h["tek"].MantikYa(false)) : (Hamle?)null;
                case MiniOyunTuru.Zar:
                    return Hamle.ZarAt();
                case MiniOyunTuru.Reaksiyon:
                    var ms = h["ms"].Tamsayi;
                    return ms.HasValue ? Hamle.Reaksiyon(ms.Value) : (Hamle?)null;
                default:
                    return null;
            }
        }

        static MiniOyunSonucu SonMiniOyunCoz(JsonDeger s)
        {
            if (s.Tur != JsonTuru.Nesne) return null;
            return new MiniOyunSonucu
            {
                Tur = Ad<MiniOyunTuru>(s["oyun"].Metin) ?? MiniOyunTuru.Tkm,
                BHamle = HamleCoz(s["bHamle"]),
                CHamle = HamleCoz(s["cHamle"]),
                BZar = s["bZar"].TamsayiYa(0),
                CZar = s["cZar"].TamsayiYa(0),
                ZarlaKarar = s["zarlaKarar"].MantikYa(false),
                SureDoldu = s["sureDoldu"].MantikYa(false),
                CKazandi = s["cKazandi"].MantikYa(false),
                Katlandi = s["katlandi"].MantikYa(false),
            };
        }

        /// <param name="oyuncular">Sayaçlar özette tekrar edilmez, durum'un oyuncu listesinden gelir.</param>
        static OyunOzeti OzetCoz(JsonDeger o, IReadOnlyList<OyuncuDurumu> oyuncular)
        {
            if (o.Tur != JsonTuru.Nesne) return null;

            var unvanlar = new List<Unvan>();
            foreach (var u in o["unvanlar"].Elemanlar)
            {
                var tur = Ad<UnvanTuru>(u["tur"].Metin);
                var kim = Id(u["kim"]);
                if (tur.HasValue && kim.HasValue) unvanlar.Add(new Unvan { Tur = tur.Value, Kim = kim.Value, Deger = u["deger"].TamsayiYa(0) });
            }

            var gorevler = new List<GorevSonucu>();
            foreach (var g in o["gorevler"].Elemanlar)
            {
                var kim = Id(g["kim"]);
                var gorev = GorevCoz(g["gorev"]);
                if (kim.HasValue && gorev != null) gorevler.Add(new GorevSonucu { Kim = kim.Value, Gorev = gorev, Basarili = g["basarili"].MantikYa(false) });
            }

            var ifsalar = new List<IfsaKaydi>();
            foreach (var k in o["ifsaOlanlar"].Elemanlar)
            {
                var a = Id(k["a"]);
                var b = Id(k["b"]);
                var c = Id(k["c"]);
                if (a.HasValue && b.HasValue && c.HasValue) ifsalar.Add(new IfsaKaydi { Soru = k["soru"].Metin, A = a.Value, B = b.Value, C = c.Value });
            }

            var curcuna = new List<string>();
            foreach (var s in o["curcunaSorulari"].Elemanlar)
                if (s.Metin != null) curcuna.Add(s.Metin);

            return new OyunOzeti
            {
                OynananTur = o["oynananTur"].TamsayiYa(0),
                Oyuncular = oyuncular,
                Unvanlar = unvanlar,
                Gorevler = gorevler,
                IfsaOlanlar = ifsalar,
                CurcunaSorulari = curcuna,
            };
        }

        // ------------------------------------------------------------ Giden

        /// <summary>Sunucu istemciden gelen mesajı en fazla bu kadar bayt kabul ediyor (fazlası bağlantıyı kapatır).</summary>
        public const int EnFazlaBayt = 4096;

        static string Yaz(JsonNesne n) => n.ToString();

        public static string OdaKur(string isim) => Yaz(new JsonNesne().Ekle("t", "odaKur").Ekle("isim", isim));

        /// <param name="anahtar">İlk girişte null. Geri dönüşte saklanan anahtar + aynı isim.</param>
        public static string Katil(string oda, string isim, string anahtar) =>
            Yaz(new JsonNesne().Ekle("t", "katil").Ekle("oda", oda).Ekle("isim", isim).Ekle("anahtar", anahtar));

        /// <summary>Sadece değişen alanlar gönderilir; null olanlar yazılmaz.</summary>
        public static string AyarDegistir(OyunModu? mod = null, MiniOyunTuru? miniOyun = null, int? turSayisi = null,
            int? soruSn = null, int? cevapSn = null, int? hamleSn = null, CezaSeviyesi? ceza = null,
            bool? gorevler = null, bool? kaos = null, IEnumerable<string> paketler = null)
        {
            var n = new JsonNesne().Ekle("t", "ayarlar");
            if (mod.HasValue) n.Ekle("mod", AdYaz(mod.Value));
            if (miniOyun.HasValue) n.Ekle("miniOyun", AdYaz(miniOyun.Value));
            if (turSayisi.HasValue) n.Ekle("turSayisi", turSayisi.Value);
            if (soruSn.HasValue) n.Ekle("soruSn", soruSn.Value);
            if (cevapSn.HasValue) n.Ekle("cevapSn", cevapSn.Value);
            if (hamleSn.HasValue) n.Ekle("hamleSn", hamleSn.Value);
            if (ceza.HasValue) n.Ekle("ceza", AdYaz(ceza.Value));
            if (gorevler.HasValue) n.Ekle("gorevler", gorevler.Value);
            if (kaos.HasValue) n.Ekle("kaos", kaos.Value);
            if (paketler != null) n.Ekle("paketler", new List<string>(paketler));
            return Yaz(n);
        }

        public static string Baslat() => Tip("baslat");
        public static string Bitir() => Tip("bitir");
        public static string Buradayim() => Tip("buradayim");
        public static string TekrarOyna() => Tip("tekrarOyna");
        public static string At(OyuncuId kim) => Yaz(new JsonNesne().Ekle("t", "at").Ekle("kim", kim.ToString()));

        static string Tip(string t) => Yaz(new JsonNesne().Ekle("t", t));

        static JsonNesne Komut(int tur, string komut) => new JsonNesne().Ekle("t", "komut").Ekle("tur", tur).Ekle("komut", komut);

        public static string HavuzaEkle(int tur, string metin) => Yaz(Komut(tur, "havuzaEkle").Ekle("metin", metin));
        public static string OneriCek(int tur) => Yaz(Komut(tur, "oneriCek"));
        public static string SoruyuDegistir(int tur) => Yaz(Komut(tur, "soruyuDegistir"));
        /// <param name="metin">Curcuna'da yok sayılır (atanan soru gider). Online'da fısıltı yok.</param>
        public static string SoruSor(int tur, OyuncuId hedef, string metin) =>
            Yaz(Komut(tur, "soruSor").Ekle("hedef", hedef.ToString()).Ekle("metin", metin));
        public static string CevapSec(int tur, OyuncuId hedef) => Yaz(Komut(tur, "cevapSec").Ekle("hedef", hedef.ToString()));
        public static string Oy(int tur, bool ifsa) => Yaz(Komut(tur, "oy").Ekle("ifsa", ifsa));
        public static string IkiyeKatla(int tur) => Yaz(Komut(tur, "ikiyeKatla"));
        public static string BedelOde(int tur) => Yaz(Komut(tur, "bedelOde"));
        public static string KarariGec(int tur) => Yaz(Komut(tur, "karariGec"));

        public static string HamleYap(int tur, Hamle h)
        {
            var o = new JsonNesne().Ekle("oyun", AdYaz(h.Tur));
            switch (h.Tur)
            {
                case MiniOyunTuru.Tkm: o.Ekle("secim", AdYaz(h.Tkm)); break;
                case MiniOyunTuru.TekCift: o.Ekle("parmak", h.Sayi).Ekle("tek", h.TekDiyor); break;
                case MiniOyunTuru.Reaksiyon: o.Ekle("ms", h.Sayi); break; // online'da yok, sunucu reddeder
            }
            return Yaz(Komut(tur, "hamle").Ekle("hamle", o));
        }
    }
}
