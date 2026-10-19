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

        /// <summary>Sunucunun kendi hata kodları (docs/Protokol.md, "hata"). Kural motorununkiler <see cref="Sonuc"/>.</summary>
        public static readonly string[] SunucuKodlari =
        {
            "GECERSIZ_MESAJ", "ODA_YOK", "ODA_DOLU", "OYUN_BASLADI", "ISIM_GECERSIZ", "ODADA_DEGILSIN", "ZATEN_ODADASIN",
            "KURUCU_DEGILSIN", "OYUN_YOK", "ESKI_TUR", "COK_HIZLI", "SUNUCU_DOLU", "ATILDIN", "OYUNDA_DEGILSIN", "GUNCELLE",
            "RAPOR_YAZILAMADI",
        };

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
                case "sohbet": return SohbetCoz(m);
                case "raporAlindi": return RaporAlindiCoz(m);
                case "engeller": return EngellerCoz(m);
                default: return null;
            }
        }

        static GelenMesaj RaporAlindiCoz(JsonDeger m)
        {
            var kim = Id(m["kim"]);
            return kim.HasValue ? new RaporAlindi { Kim = kim.Value } : null;
        }

        /// <summary>Liste zorunlu (boş olabilir). Bozuk kimlik atlanır, liste düşmez.</summary>
        static GelenMesaj EngellerCoz(JsonDeger m)
        {
            var kimler = m["kimler"];
            if (kimler.Tur != JsonTuru.Dizi) return null;
            if (kimler.Elemanlar.Count == 0) return null;
            var e = new EngellerMesaji();
            foreach (var k in kimler.Elemanlar)
            {
                var id = Id(k);
                if (id.HasValue && !e.Kimler.Contains(id.Value)) e.Kimler.Add(id.Value);
            }
            return e;
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
            return new HataMesaji
            {
                Kod = kod,
                Sonuc = SonucKodu(kod),
                Ihlal = m["ihlal"].TamsayiYa(0),
                Sinir = m["sinir"].TamsayiYa(0),
                Neden = m["neden"].Metin,
            };
        }

        static GelenMesaj SohbetCoz(JsonDeger m)
        {
            var kimden = Id(m["kimden"]);
            string metin = m["metin"].Metin;
            if (!kimden.HasValue || string.IsNullOrEmpty(metin)) return null;
            var kime = Id(m["kime"]);
            var kanal = Ad<SohbetKanali>(m["kime"].Metin);
            // "kime" var ama ne kişi ne kanal: DM'i genele düşürme, at.
            if (!kime.HasValue && !kanal.HasValue && m["kime"].Tur == JsonTuru.Metin) return null;
            return new SohbetGelen { Id = m["id"].Metin, Kimden = kimden.Value, Kime = kime, Kanal = kanal, Metin = metin };
        }

        static GelenMesaj OlayCoz(JsonDeger m)
        {
            string ad = m["olay"].Metin;
            var kim = Id(m["kim"]);
            if (ad == "atildi") return new OlayMesaji { Atildi = true, Kim = kim, Neden = m["neden"].Metin };
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
                Hedef = Id(m["hedef"]),
                Takim = m["takim"].Tamsayi,
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
                d.GrupPaketi = Math.Max(0, m["grupPaketi"].TamsayiYa(0));
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
            s.Oyun = Ad<OyunTuru>(a["oyun"].Metin) ?? s.Oyun;
            s.Can = a["can"].TamsayiYa(s.Can);
            s.Casus = Ad<CasusTuru>(a["casus"].Metin) ?? s.Casus;
            s.RolAcilir = a["rolAcilir"].MantikYa(s.RolAcilir);
            s.GecelikKurban = a["gecelikKurban"].TamsayiYa(s.GecelikKurban);
            foreach (var r in a["kapaliRoller"].Elemanlar)
            {
                var rol = Ad<Rol>(r.Metin);
                if (rol.HasValue && !s.KapaliRoller.Contains(rol.Value)) s.KapaliRoller.Add(rol.Value);
            }
            foreach (var t in a["takimlar"].Alanlar)
            {
                var id = Id(t.Key);
                var no = t.Value.Tamsayi;
                if (id.HasValue && no.HasValue) s.Takimlar[id.Value] = no.Value;
            }
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

                // Yeni oyunlar (docs/Oyunlar.md). Tanınmayan oyun adı Çakmak sayılır.
                Oyun = Ad<OyunTuru>(m["oyun"].Metin) ?? OyunTuru.Cakmak,
                Supheliler = IdListesi(m["supheliler"]),
                SonSuclama = m["sonSuclama"].MantikYa(false),
                Rolun = Ad<Rol>(sana["rol"].Metin) ?? Rol.Yok,
                SuclamaHakkin = sana["suclamaHakki"].MantikYa(false),
                TetikCeken = Id(m["tetikCeken"]),
                Final = m["final"].MantikYa(false),
                TakimPuanlari = SayiListesi(m["takimPuanlari"]),

                // Casus: sır ve mekândaki rol sadece casus olmayanda, liste ve kategori herkeste.
                Sirrin = sana["sir"].Metin,
                MekanRolun = sana["mekanRolu"].Metin,
                CasusListesi = MetinListesi(m["casusListesi"]),
                CasusKategori = m["casusKategori"].Metin,
                Yakalanan = Id(m["yakalanan"]),

                // Çakmak Gecesi: gün herkese; katiller, bulgular, haklar, Cellat'ın hedefi, bombalar, Kulak'ın sorusu sadece sana.
                Gun = m["gun"].TamsayiYa(0),
                KalanGunduzTuru = m["kalanGunduzTuru"].TamsayiYa(0),
                IkinciSecilebilir = IdListesi(m["ikinciSecilebilir"]),
                Katiller = RolListesi(sana["katiller"]),
                KatilSecimleri = KatilSecimleri(sana["katilSecimleri"]),
                Bulgularin = Bulgular(sana["bulgularin"]),
                KalanHakkin = sana["kalanHakkin"].Tamsayi,
                CellatHedefin = Id(sana["cellatHedefin"]),
                Bombalilar = IdListesi(sana["bombalilar"]),
                KulakSorusu = sana["kulakSorusu"].Metin,
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
                    Can = o["can"].TamsayiYa(0),
                    Elendi = o["elendi"].MantikYa(false),
                    DevretmeHakki = o["devretmeHakki"].MantikYa(false),
                    Takim = o["takim"].Tamsayi,
                    AcikRol = Ad<Rol>(o["acikRol"].Metin),
                    Susturuldu = o["susturuldu"].MantikYa(false),
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

        /// <summary><c>[ { "kim", "rol" } ]</c>: katiller (sana) ve oyun sonunda herkesin rolü. Bozuk öğe atlanır.</summary>
        static List<OyuncuRolu> RolListesi(JsonDeger dizi)
        {
            var l = new List<OyuncuRolu>();
            foreach (var x in dizi.Elemanlar)
            {
                var kim = Id(x["kim"]);
                var rol = Ad<Rol>(x["rol"].Metin);
                if (kim.HasValue && rol.HasValue) l.Add(new OyuncuRolu { Kim = kim.Value, Rol = rol.Value });
            }
            return l;
        }

        static List<KatilSecimi> KatilSecimleri(JsonDeger dizi)
        {
            var l = new List<KatilSecimi>();
            foreach (var x in dizi.Elemanlar)
            {
                var kim = Id(x["kim"]);
                var hedef = Id(x["hedef"]);
                if (kim.HasValue && hedef.HasValue) l.Add(new KatilSecimi { Kim = kim.Value, Hedef = hedef.Value });
            }
            return l;
        }

        /// <summary><c>sana.bulgularin</c>: gece bakışlarının sonuçları; sadece rolün doldurduğu alanlar dolu.</summary>
        static List<Bulgu> Bulgular(JsonDeger dizi)
        {
            var l = new List<Bulgu>();
            foreach (var x in dizi.Elemanlar)
            {
                var kim = Id(x["kim"]);
                if (!kim.HasValue) continue;
                l.Add(new Bulgu
                {
                    Gun = x["gun"].TamsayiYa(0),
                    Kim = kim.Value,
                    Supheli = x["supheli"].Tur == JsonTuru.Mantik ? x["supheli"].MantikYa(false) : (bool?)null,
                    Rol = Ad<Rol>(x["rol"].Metin),
                    Ziyaretciler = IdListesi(x["ziyaretciler"]),
                    Grup = IdListesi(x["grup"]),
                    KotuVar = x["kotuVar"].Tur == JsonTuru.Mantik ? x["kotuVar"].MantikYa(false) : (bool?)null,
                });
            }
            return l;
        }

        static List<string> MetinListesi(JsonDeger dizi)
        {
            var l = new List<string>();
            foreach (var x in dizi.Elemanlar)
                if (x.Metin != null) l.Add(x.Metin);
            return l;
        }

        static List<int> SayiListesi(JsonDeger dizi)
        {
            var l = new List<int>();
            foreach (var x in dizi.Elemanlar)
                if (x.Tamsayi.HasValue) l.Add(x.Tamsayi.Value);
            return l;
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
                Kostebek = KostebekCoz(o["kostebek"]),
                RusRuleti = RusRuletiCoz(o["rusRuleti"]),
                Takim = o["takim"].Tur == JsonTuru.Nesne
                    ? new TakimSonucu { Kazanan = o["takim"]["kazanan"].Tamsayi, Puanlar = SayiListesi(o["takim"]["puanlar"]) }
                    : null,
                Casus = CasusCoz(o["casus"]),
                Gece = o["gece"].Tur == JsonTuru.Nesne
                    ? new CakmakGecesiSonucu
                    {
                        Kazanan = Ad<GeceKazanan>(o["gece"]["kazanan"].Metin),
                        Kazananlar = IdListesi(o["gece"]["kazananlar"]),
                        Roller = RolListesi(o["gece"]["roller"]),
                    }
                    : null,
                OynananTur = o["oynananTur"].TamsayiYa(0),
                Oyuncular = oyuncular,
                Unvanlar = unvanlar,
                Gorevler = gorevler,
                IfsaOlanlar = ifsalar,
                CurcunaSorulari = curcuna,
            };
        }

        /// <summary><c>ozet.casus</c>: sadece Casus oyununda. Kim, kazanan ya da neden bozuksa null.</summary>
        static CasusSonucu CasusCoz(JsonDeger c)
        {
            if (c.Tur != JsonTuru.Nesne) return null;
            var kim = Id(c["kim"]);
            var kazanan = Ad<CasusKazanan>(c["kazanan"].Metin);
            var neden = Ad<CasusNedeni>(c["neden"].Metin);
            if (!kim.HasValue || !kazanan.HasValue || !neden.HasValue) return null;
            return new CasusSonucu { Casus = kim.Value, Kazanan = kazanan.Value, Neden = neden.Value, Sir = c["sir"].Metin, Tahmin = c["tahmin"].Metin };
        }

        /// <summary><c>ozet.kostebek</c>: sadece Köstebek oyununda. Kim ya da kazanan bozuksa null.</summary>
        static KostebekSonucu KostebekCoz(JsonDeger k)
        {
            if (k.Tur != JsonTuru.Nesne) return null;
            var kim = Id(k["kim"]);
            var kazanan = Ad<KostebekKazanan>(k["kazanan"].Metin);
            if (!kim.HasValue || !kazanan.HasValue) return null;
            return new KostebekSonucu
            {
                Kostebek = kim.Value,
                Kazanan = kazanan.Value,
                Neden = Ad<KostebekNedeni>(k["neden"].Metin) ?? KostebekNedeni.Yakalanmadi,
            };
        }

        /// <summary><c>ozet.rusRuleti</c>: kazanan (yoksa null) ve elenme sırası.</summary>
        static RusRuletiSonucu RusRuletiCoz(JsonDeger r)
        {
            if (r.Tur != JsonTuru.Nesne) return null;
            return new RusRuletiSonucu { Kazanan = Id(r["kazanan"]), ElenmeSirasi = IdListesi(r["elenmeSirasi"]) };
        }

        // ------------------------------------------------------------ Giden

        /// <summary>Sunucu istemciden gelen mesajı en fazla bu kadar bayt kabul ediyor (fazlası bağlantıyı kapatır).</summary>
        public const int EnFazlaBayt = 4096;

        static string Yaz(JsonNesne n) => n.ToString();

        /// <summary>
        /// Bu istemcinin protokol sürümü: <c>odaKur</c> ve <c>katil</c>'de gider (docs/Protokol.md, "Sürüm"). Sunucunun kabul
        /// ettiği en eskiden küçükse <c>GUNCELLE</c> gelir. Uyumsuz bir protokol değişikliğinde artar.
        /// </summary>
        public const int ProtokolSurumu = 1;

        public static string OdaKur(string isim) =>
            Yaz(new JsonNesne().Ekle("t", "odaKur").Ekle("isim", isim).Ekle("surum", ProtokolSurumu));

        /// <param name="anahtar">İlk girişte null. Geri dönüşte saklanan anahtar + aynı isim.</param>
        public static string Katil(string oda, string isim, string anahtar) =>
            Yaz(new JsonNesne().Ekle("t", "katil").Ekle("oda", oda).Ekle("isim", isim).Ekle("anahtar", anahtar)
                .Ekle("surum", ProtokolSurumu));

        /// <summary>Sadece değişen alanlar gönderilir; null olanlar yazılmaz.</summary>
        public static string AyarDegistir(OyunModu? mod = null, MiniOyunTuru? miniOyun = null, int? turSayisi = null,
            int? soruSn = null, int? cevapSn = null, int? hamleSn = null, CezaSeviyesi? ceza = null,
            bool? gorevler = null, bool? kaos = null, IEnumerable<string> paketler = null, OyunTuru? oyun = null, int? can = null,
            IReadOnlyDictionary<OyuncuId, int> takimlar = null, CasusTuru? casus = null,
            bool? rolAcilir = null, int? gecelikKurban = null, IEnumerable<Rol> kapaliRoller = null)
        {
            var n = new JsonNesne().Ekle("t", "ayarlar");
            if (oyun.HasValue) n.Ekle("oyun", AdYaz(oyun.Value));
            if (can.HasValue) n.Ekle("can", can.Value);
            if (casus.HasValue) n.Ekle("casus", AdYaz(casus.Value));
            if (rolAcilir.HasValue) n.Ekle("rolAcilir", rolAcilir.Value);
            if (gecelikKurban.HasValue) n.Ekle("gecelikKurban", gecelikKurban.Value);
            if (kapaliRoller != null)
            {
                var d = new List<string>();
                foreach (var r in kapaliRoller) d.Add(AdYaz(r));
                n.Ekle("kapaliRoller", d);
            }
            if (takimlar != null)
            {
                // Boş nesne: rastgele ve dengeli.
                var t = new JsonNesne();
                foreach (var kv in takimlar) t.Ekle(kv.Key.ToString(), kv.Value);
                n.Ekle("takimlar", t);
            }
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

        /// <summary>Raporla (docs/Protokol.md, "Rapor ve engelleme"). Cevap <c>raporAlindi</c> ya da <c>RAPOR_YAZILAMADI</c>.</summary>
        public static string Rapor(OyuncuId kim) => Yaz(new JsonNesne().Ekle("t", "rapor").Ekle("kim", kim.ToString()));

        /// <summary>Engelle ya da (<paramref name="engel"/> false) engeli kaldır. Cevap <c>engeller</c>: tam liste.</summary>
        public static string Engelle(OyuncuId kim, bool engel = true)
        {
            var n = new JsonNesne().Ekle("t", "engelle").Ekle("kim", kim.ToString());
            if (!engel) n.Ekle("engel", false);
            return Yaz(n);
        }

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
        /// <summary>Köstebek: erken suçlama (herkesin oyun boyu 1 hakkı, sadece soruSorma'da).</summary>
        public static string Sucla(int tur) => Yaz(Komut(tur, "sucla"));
        /// <summary>Köstebek, Casus: "kim?" oylamasında oy.</summary>
        public static string SuclamaOyu(int tur, OyuncuId hedef) => Yaz(Komut(tur, "suclamaOyu").Ekle("hedef", hedef.ToString()));
        /// <summary>Rus Ruleti: tetiği çek (sadece tetikCeken).</summary>
        public static string TetikCek(int tur) => Yaz(Komut(tur, "tetikCek"));
        /// <summary>Rus Ruleti: tetiği devret (oyun boyu 1 kez).</summary>
        public static string TetikDevret(int tur, OyuncuId hedef) => Yaz(Komut(tur, "tetikDevret").Ekle("hedef", hedef.ToString()));
        /// <summary>Çakmak Gecesi: linç oyu.</summary>
        public static string LincOyu(int tur, OyuncuId hedef) => Yaz(Komut(tur, "lincOyu").Ekle("hedef", hedef.ToString()));

        /// <summary>
        /// Çakmak Gecesi: gece seçimi. <paramref name="hedef"/> null = pas; <paramref name="ikinci"/> katillerin yeteneği;
        /// <paramref name="ifsa"/> Hakem'in kararı. Boş alanlar gitmez.
        /// </summary>
        public static string GeceSecimi(int tur, OyuncuId? hedef, OyuncuId? ikinci = null, bool? ifsa = null)
        {
            var n = Komut(tur, "geceSecimi");
            if (hedef.HasValue) n.Ekle("hedef", hedef.Value.ToString());
            if (ikinci.HasValue) n.Ekle("ikinci", ikinci.Value.ToString());
            if (ifsa.HasValue) n.Ekle("ifsa", ifsa.Value);
            return Yaz(n);
        }

        /// <summary>Çakmak Gecesi: gündüz yeteneği (Sorgucu, Sınayıcı, Kuklacı: hedef = C, Kışkırtıcı).</summary>
        public static string Yetenek(int tur, OyuncuId? hedef = null)
        {
            var n = Komut(tur, "yetenek");
            if (hedef.HasValue) n.Ekle("hedef", hedef.Value.ToString());
            return Yaz(n);
        }

        /// <summary>Çakmak Gecesi: Suikastçı linçte birinin rolünü tahmin eder.</summary>
        public static string Suikast(int tur, OyuncuId hedef, Rol rol) =>
            Yaz(Komut(tur, "suikast").Ekle("hedef", hedef.ToString()).Ekle("rol", AdYaz(rol)));

        /// <summary>Casus: mekânı (ya da kelimeyi) tahmin et; listeden biri. Yakalanmadan bir kez ya da son şansta.</summary>
        public static string CasusTahmini(int tur, string tahmin) => Yaz(Komut(tur, "casusTahmini").Ekle("tahmin", tahmin));
        public static string BedelOde(int tur) => Yaz(Komut(tur, "bedelOde"));
        public static string KarariGec(int tur) => Yaz(Komut(tur, "karariGec"));

        /// <param name="kime">null: genel sohbet. Doluysa o kişiye DM.</param>
        public static string Sohbet(OyuncuId? kime, string metin) => Sohbet(SohbetYeri.Kimden(kime), metin);

        /// <summary>Genel (<c>kime</c> yok), DM (<c>"p3"</c>) ya da adlı kanal (<c>"takim"</c>, <c>"katil"</c>, <c>"oluler"</c>).</summary>
        public static string Sohbet(SohbetYeri yer, string metin)
        {
            var n = new JsonNesne().Ekle("t", "sohbet");
            if (!yer.GenelMi) n.Ekle("kime", yer.Anahtar);
            return Yaz(n.Ekle("metin", metin));
        }

        /// <summary>Bir grup paketi parçası. <paramref name="ilk"/>: sunucudaki eski paketi siler.</summary>
        public static string GrupPaketi(IEnumerable<string> sorular, bool ilk)
        {
            var n = new JsonNesne().Ekle("t", "grupPaketi");
            if (ilk) n.Ekle("ilk", true);
            return Yaz(n.Ekle("sorular", new List<string>(sorular)));
        }

        /// <summary>Grup paketinin bir parçası en fazla bu kadar bayt (sınır 4096, pay bırakılır).</summary>
        public const int GrupPaketiParcaBayt = 3800;
        /// <summary>Sunucunun grup paketi sınırları (docs/Protokol.md, "Grup paketi").</summary>
        public const int GrupPaketiEnFazla = 500;
        public const int GrupPaketiSoruUzunlugu = 140;

        /// <summary>
        /// Grup paketini sunucuya gidecek mesajlara böler: her biri <paramref name="sinir"/> baytı geçmez (JSON kaçışları dahil),
        /// sıra korunur, sadece ilki <c>ilk: true</c>. Boş, 140'tan uzun ve tekrar eden (büyük/küçük harf farkıyla) sorular atlanır,
        /// en fazla 500. Hiç soru kalmazsa boş liste.
        /// </summary>
        public static List<string> GrupPaketiParcalari(IEnumerable<string> sorular, int sinir = GrupPaketiParcaBayt)
        {
            var temiz = new List<string>();
            var gorulen = new HashSet<string>();
            if (sorular != null)
                foreach (var s in sorular)
                {
                    string t = (s ?? "").Trim();
                    if (t.Length == 0 || t.Length > GrupPaketiSoruUzunlugu) continue;
                    if (!gorulen.Add(t.ToLowerInvariant())) continue;
                    temiz.Add(t);
                    if (temiz.Count >= GrupPaketiEnFazla) break;
                }

            var parcalar = new List<string>();
            var parca = new List<string>();
            foreach (var s in temiz)
            {
                parca.Add(s);
                if (Encoding.UTF8.GetByteCount(GrupPaketi(parca, parcalar.Count == 0)) <= sinir) continue;
                parca.RemoveAt(parca.Count - 1);
                if (parca.Count == 0) continue; // tek başına sığmayan soru (140 karakterle olmaz) atlanır
                parcalar.Add(GrupPaketi(parca, parcalar.Count == 0));
                parca = new List<string> { s };
                if (Encoding.UTF8.GetByteCount(GrupPaketi(parca, false)) > sinir) parca.Clear();
            }
            if (parca.Count > 0) parcalar.Add(GrupPaketi(parca, parcalar.Count == 0));
            return parcalar;
        }

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
