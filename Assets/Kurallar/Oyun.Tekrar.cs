using System;
using System.Collections.Generic;

namespace Cakmak.Kurallar
{
    // S3 — Tekrar oynatma kuralları (Ana Plan 8): cezalar, Bedel, Kızgın Çakmak, kaos turları,
    // Grup Kararı oylaması, İkiye Katla, gizli görevler, unvanlar, oyun özeti.
    // Hepsi ayarlardan açılır; kapalıyken çekirdek döngü eskisi gibi çalışır ve rastgelelik tüketmez.
    public sealed partial class Oyun
    {
        public const int BedelHakkiBaslangic = 2;
        public const int KaosAraligi = 5;
        public const int EsikEnAz = 8;
        public const int EsikEnFazla = 15;

        static readonly CezaTuru[] Kisitlamalar =
            { CezaTuru.Palyaco, CezaTuru.HavuzdanSor, CezaTuru.BeraberlikRakibe, CezaTuru.SorunIfsa };

        /// <summary>Oyuncunun dışarıya hiç verilmeyen ya da sadece özette çıkan izleri.</summary>
        sealed class Iz
        {
            public readonly Dictionary<OyuncuId, int> Sordugu = new Dictionary<OyuncuId, int>();
            public readonly Dictionary<OyuncuId, int> Gosterdigi = new Dictionary<OyuncuId, int>();
            public readonly Dictionary<OyuncuId, int> GosterenKisi = new Dictionary<OyuncuId, int>();
            public readonly List<Ceza> Kisitlamalar = new List<Ceza>();
            public int Intikam;
            public int BIkenIfsa;
            public Gorev Gorev;
        }

        readonly Dictionary<OyuncuId, Iz> izler = new Dictionary<OyuncuId, Iz>();
        readonly List<IfsaKaydi> ifsaKayitlari = new List<IfsaKaydi>();
        readonly List<string> curcunaYazilan = new List<string>();
        Deste sesliDeste;

        // Kızgın Çakmak. Eşik hiçbir görünüme konmaz.
        int devir;
        int esik;
        /// <summary>Isı göstergesine her devirde -1/0/+1 gürültü: eşik ısı değişiminden hesaplanamasın.</summary>
        int isiRuhu;

        // Soran Gizli: A'nın sayacı ve harcanan kartı tur ortasında değişirse A belli olur. Tur sonuna ertelenir.
        OyuncuId? bekleyenSoruSorma;
        readonly List<ErtelenenKart> ertelenenKartlar = new List<ErtelenenKart>();

        struct ErtelenenKart
        {
            public OyuncuId Sahibi;
            public Ceza Kart;
        }

        // Tur durumu
        KaosKurali kaos;
        bool korSoru;
        bool zorlaIfsa;
        bool katlandi;
        OyuncuId? ilkKaybeden;
        OyuncuId? kararVeren;
        bool bekleyenCKazandi;
        OyuncuId? bekleyenKaybeden;
        bool bedelOdendi;
        readonly Dictionary<OyuncuId, bool> oylar = new Dictionary<OyuncuId, bool>();
        readonly List<OyuncuId> oylayanlar = new List<OyuncuId>();
        OylamaSonucu sonOylama;
        readonly List<CezaCekimi> turCezalari = new List<CezaCekimi>();
        OyuncuId? oncekiB;
        OyuncuId? oncekiC;

        bool SoranGizli => kaos == KaosKurali.SoranGizli;
        bool KizginCakmak => ayar.CezaSeviyesi == CezaSeviyesi.Cesur;
        OyuncuId? GorunenA() => SoranGizli ? (OyuncuId?)null : a;
        Iz IzAl(OyuncuId id) => izler[id];

        // ------------------------------------------------------------ Komutlar

        /// <summary>Grup Kararı: A ve B dışındakiler ifşa mı güme mi diye oy verir. Herkes oy verince sonuç çıkar.</summary>
        public Sonuc Oy(OyuncuId kim, bool ifsa)
        {
            if (asama != Asama.Oylama) return Sonuc.YanlisAsama;
            if (!oylayanlar.Contains(kim)) return Sonuc.SiraSendeDegil;
            if (oylar.ContainsKey(kim)) return Sonuc.HakKalmadi;

            oylar[kim] = ifsa;
            Yay(new Olay { Tur = OlayTuru.OyVerildi, Kim = kim });
            if (oylar.Count == oylayanlar.Count) OylamayiBitir();
            return Sonuc.Tamam;
        }

        /// <summary>İkiye Katla turunda kaybeden "bir el daha" der. Yine kaybederse cezası ikiye katlanır.</summary>
        public Sonuc IkiyeKatla(OyuncuId kim)
        {
            if (asama != Asama.IkiyeKatla) return Sonuc.YanlisAsama;
            if (kim != kararVeren) return Sonuc.SiraSendeDegil;

            katlandi = true;
            ilkKaybeden = kim;
            kararVeren = null;
            bHamle = null;
            cHamle = null;
            beraberlik = 0;
            Yay(new Olay { Tur = OlayTuru.IkiyeKatlandi, Kim = kim });
            AsamayaGec(Asama.MiniOyun, ayar.HamleSuresiSn);
            return Sonuc.Tamam;
        }

        /// <summary>Soru ifşa olmak üzere: B bedel öder, soru gizli kalır, B iki ceza çeker.</summary>
        public Sonuc BedelOde(OyuncuId kim)
        {
            if (asama != Asama.Bedel) return Sonuc.YanlisAsama;
            if (kim != kararVeren) return Sonuc.SiraSendeDegil;
            TuruBitir(ifsa: false, kaybeden: bekleyenKaybeden, bedel: true);
            return Sonuc.Tamam;
        }

        /// <summary>İkiye Katla ya da Bedel kararını beklemeden "hayır" der.</summary>
        public Sonuc KarariGec(OyuncuId kim)
        {
            if (asama != Asama.IkiyeKatla && asama != Asama.Bedel) return Sonuc.YanlisAsama;
            if (kim != kararVeren) return Sonuc.SiraSendeDegil;
            KarardanVazgecildi();
            return Sonuc.Tamam;
        }

        // ------------------------------------------------------------ Kurulum ve tur başı

        void TekrarKur()
        {
            sesliDeste = new Deste(ayar.SesliCezalar ?? Array.Empty<string>());
            foreach (var o in oyuncular) izler[o.Id] = new Iz();

            if (ayar.CezaSeviyesi != CezaSeviyesi.Kapali)
                foreach (var o in oyuncular) o.BedelHakki = BedelHakkiBaslangic;

            if (KizginCakmak) esik = YeniEsik();

            if (ayar.GizliGorevler)
            {
                // Herkese aynı zorlukta görev (Ana Plan 8.2).
                var zorluk = (Zorluk)rastgele.Next(3);
                foreach (var o in oyuncular) IzAl(o.Id).Gorev = GorevUret(o.Id, zorluk);
            }
        }

        void TekrarTurBasi()
        {
            turCezalari.Clear();
            kaos = KaosKurali.Yok;
            korSoru = false;
            zorlaIfsa = false;
            katlandi = false;
            ilkKaybeden = null;
            kararVeren = null;
            bedelOdendi = false;
            oylar.Clear();
            oylayanlar.Clear();
            sonOylama = null;

            // Palyaço çekildiği turdan sonraki turlarda sayılır: "2 tur" = sonraki iki tur.
            foreach (var iz in izler.Values)
                for (int i = iz.Kisitlamalar.Count - 1; i >= 0; i--)
                {
                    var k = iz.Kisitlamalar[i];
                    if (k.Tur != CezaTuru.Palyaco) continue;
                    if (k.Yeni) { k.Yeni = false; continue; }
                    if (--k.Kalan <= 0) iz.Kisitlamalar.RemoveAt(i);
                }

            if (ayar.KaosTurlari && tur % KaosAraligi == 0) kaos = KaosSec();

            // Soran Gizli: A önceki turun C'si olursa (herkes onu biliyor) gizlilik bir işe yaramaz. A rastgele.
            if (SoranGizli) a = RastgeleOyuncu();

            if (kaos == KaosKurali.KorSoru)
            {
                // Curcuna'da A'ya zaten havuzdan soru atandıysa o kullanılır, ikincisi boşa çekilmez.
                atananSoru = atananSoru ?? HavuzdanCek();
                atananZorunlu = true;
                degistirmeHakki = false;
                korSoru = true;
            }
            else if (!atananZorunlu && KisitlamaBul(a, CezaTuru.HavuzdanSor) != null)
            {
                var s = HavuzdanCek();
                if (s != null)
                {
                    atananSoru = s;
                    atananZorunlu = true;
                }
            }
        }

        KaosKurali KaosSec()
        {
            var adaylar = new List<KaosKurali>();
            foreach (KaosKurali k in Enum.GetValues(typeof(KaosKurali)))
            {
                if (k == KaosKurali.Yok) continue;
                if (k == KaosKurali.KorSoru && HavuzBosMu()) continue;
                if (k == KaosKurali.SoranGizli && ayar.TekCihaz) continue;
                // Ani Ölüm sadece berabere bitebilen oyunlarda anlamlı (Zar ve Tek-Çift berabere bitmez).
                if (k == KaosKurali.AniOlum && (buTurMiniOyun == MiniOyunTuru.Zar || buTurMiniOyun == MiniOyunTuru.TekCift)) continue;
                adaylar.Add(k);
            }
            return adaylar[rastgele.Next(adaylar.Count)];
        }

        bool HavuzBosMu() => (havuz == null || havuz.Bos) && oneriDestesi.Bos;

        string HavuzdanCek()
        {
            if (havuz != null && !havuz.Bos) return havuz.Cek(rastgele);
            if (!oneriDestesi.Bos) return oneriDestesi.Cek(rastgele);
            return null;
        }

        // ------------------------------------------------------------ Tur içi kancalar

        void SoruSoruldu(OyuncuId hedef)
        {
            var iz = IzAl(a);
            Artir(iz.Sordugu, hedef);
            if (a == oncekiC && hedef == oncekiB) iz.Intikam++;

            if (SoranGizli)
            {
                // Tur sonuna kadar görünen hiçbir şey A'da değişmesin.
                bekleyenSoruSorma = a;
                Ertele(KisitlamaBul(a, CezaTuru.HavuzdanSor));
                var sorunIfsa = KisitlamaBul(a, CezaTuru.SorunIfsa);
                if (sorunIfsa != null) { zorlaIfsa = true; Ertele(sorunIfsa); }
                return;
            }
            Bul(a).SoruSorma++;
            KisitlamaKullan(a, CezaTuru.HavuzdanSor);
            if (KisitlamaKullan(a, CezaTuru.SorunIfsa)) zorlaIfsa = true;
        }

        void Ertele(Ceza kart)
        {
            if (kart != null) ertelenenKartlar.Add(new ErtelenenKart { Sahibi = a, Kart = kart });
        }

        /// <summary>Tur bitince (yeni tur başında ya da oyun sonunda) ertelenen sayaç ve kartlar işlenir.</summary>
        void TurSonuIsleri()
        {
            if (bekleyenSoruSorma.HasValue) Bul(bekleyenSoruSorma.Value).SoruSorma++;
            bekleyenSoruSorma = null;
            foreach (var e in ertelenenKartlar) IzAl(e.Sahibi).Kisitlamalar.Remove(e.Kart);
            ertelenenKartlar.Clear();
        }

        /// <summary>Soru güme gitti: Curcuna listesinden de silinir, bellekte tutulmaz (Ana Plan 8.5: kesin kural).</summary>
        void GumeyeGitti()
        {
            if (soru != null) curcunaYazilan.Remove(soru);
            soru = null;
        }

        /// <summary>Süre dolup tur yarıda kaldıysa "kendisini gösterene hemen soru" (İntikamcı) zinciri kopar.</summary>
        void OncekiTuruUnut()
        {
            oncekiB = null;
            oncekiC = null;
        }

        static void Artir(Dictionary<OyuncuId, int> sayac, OyuncuId kim) =>
            sayac[kim] = (sayac.TryGetValue(kim, out var n) ? n : 0) + 1;

        void CakmakDevredildi(OyuncuId veren, OyuncuId alan)
        {
            Artir(IzAl(veren).Gosterdigi, alan);
            Artir(IzAl(alan).GosterenKisi, veren);

            if (!KizginCakmak) return;
            devir++;
            if (devir < esik)
            {
                isiRuhu = rastgele.Next(-1, 2);
                return;
            }

            // Eşiği geçiren devirde çakmağı alan yanar: büyük ceza = iki kart.
            Bul(alan).Yanma++;
            Yay(new Olay { Tur = OlayTuru.Yandi, Kim = alan });
            CezaCek(alan, CezaSeviyesi.Cesur, CekimNedeni.Yandi);
            CezaCek(alan, CezaSeviyesi.Cesur, CekimNedeni.Yandi);
            devir = 0;
            isiRuhu = 0;
            esik = YeniEsik();
        }

        void OylamayiBaslat()
        {
            // "Sorun ifşa olur" cezası varsa oylamanın anlamı yok: doğrudan ifşa, kimse kaybetmedi.
            if (zorlaIfsa) { SonucaGec(true, kaybedenVar: false); return; }
            oylayanlar.Clear();
            foreach (var o in oyuncular)
                if (o.Id != a && o.Id != b) oylayanlar.Add(o.Id);
            AsamayaGec(Asama.Oylama, ayar.HamleSuresiSn);
        }

        void OylamayiBitir()
        {
            int ifsa = 0, gume = 0;
            foreach (var oy in oylar.Values) { if (oy) ifsa++; else gume++; }
            sonOylama = new OylamaSonucu { Ifsa = ifsa, Gume = gume };
            // Çoğunluk ifşa derse ifşa. Eşitlik ya da hiç oy yoksa güme (B'nin lehine, süre dolma kuralı gibi).
            // Çoğunluk yoksa kimse kaybetmedi: AFK oylayıcılar kimseyi cezalandıramaz.
            SonucaGec(ifsa > gume, kaybedenVar: ifsa != gume);
        }

        /// <summary>Beraberlikte "beraberlik rakibinin" cezası varsa beraberliği bozar.</summary>
        bool BeraberligiCezaBozar(out bool cKazandi)
        {
            cKazandi = false;
            var bK = KisitlamaBul(b.Value, CezaTuru.BeraberlikRakibe);
            var cK = KisitlamaBul(c.Value, CezaTuru.BeraberlikRakibe);
            if (bK == null && cK == null) return false;
            if (bK != null) IzAl(b.Value).Kisitlamalar.Remove(bK);
            if (cK != null) IzAl(c.Value).Kisitlamalar.Remove(cK);
            if (bK != null && cK != null) return false; // ikisinde de var: cezalar birbirini götürür, normal beraberlik
            cKazandi = bK != null;                      // B'nin cezası → beraberlik C'nin
            return true;
        }

        void MiniOyunSonrasi(bool cKazandi)
        {
            if (kaos == KaosKurali.IkiyeKatla && !katlandi)
            {
                bekleyenCKazandi = cKazandi;
                kararVeren = cKazandi ? b : c;
                AsamayaGec(Asama.IkiyeKatla, ayar.KararSuresiSn);
                return;
            }
            SonucaGec(cKazandi);
        }

        void KarardanVazgecildi()
        {
            var nerede = asama;
            kararVeren = null;
            if (nerede == Asama.IkiyeKatla)
            {
                MiniOyunuSay(bekleyenCKazandi); // katlanmadı: ilk el sayılır
                SonucaGec(bekleyenCKazandi);
            }
            else TuruBitir(ifsa: true, kaybeden: bekleyenKaybeden, bedel: false);
        }

        /// <summary>Kazanan belli: soru ifşa mı güme mi? İfşaysa B'ye bedel fırsatı.</summary>
        void SonucaGec(bool cKazandi, bool kaybedenVar = true)
        {
            OyuncuId? kaybeden = kaybedenVar ? (cKazandi ? b.Value : c.Value) : (OyuncuId?)null;
            bool ifsa = kaos == KaosKurali.TersDunya ? !cKazandi : cKazandi;
            if (zorlaIfsa) ifsa = true;

            if (ifsa && !zorlaIfsa && ayar.CezaSeviyesi != CezaSeviyesi.Kapali && Bul(b.Value).BedelHakki > 0)
            {
                bekleyenKaybeden = kaybeden;
                kararVeren = b;
                AsamayaGec(Asama.Bedel, ayar.KararSuresiSn);
                return;
            }
            TuruBitir(ifsa, kaybeden, bedel: false);
        }

        void TuruBitir(bool ifsa, OyuncuId? kaybeden, bool bedel)
        {
            kararVeren = null;
            var bo = Bul(b.Value);
            var co = Bul(c.Value);

            if (bedel)
            {
                // Bedel, kaybedenin normal cezasının yerine geçer: B iki kart çeker.
                bo.BedelOdeme++;
                bo.BedelHakki--;
                Yay(new Olay { Tur = OlayTuru.BedelOdendi, Kim = b });
                var seviye = ayar.CezaSeviyesi;
                CezaCek(b.Value, seviye, CekimNedeni.Bedel);
                CezaCek(b.Value, seviye, CekimNedeni.Bedel);
                // Ters Dünya'da mini oyunu C kaybetmiş olabilir: B'nin bedeli C'yi cezadan kurtarmaz.
                if (kaybeden != b) KaybedenCeza(kaybeden);
            }
            else
            {
                KaybedenCeza(kaybeden);
            }

            bedelOdendi = bedel;
            oncekiB = b;
            oncekiC = c;

            if (ifsa)
            {
                co.IfsaEttirme++;
                IzAl(b.Value).BIkenIfsa++;
                if (soru != null) ifsaKayitlari.Add(new IfsaKaydi { Soru = soru, A = a, B = b.Value, C = c.Value });
                AsamayaGec(Asama.Ifsa, ayar.SonucSuresiSn);
                Yay(new Olay { Tur = OlayTuru.Ifsa, A = GorunenA(), B = b, C = c, Soru = soru });
            }
            else
            {
                bo.Saklama++;
                GumeyeGitti(); // güme giden soru hiçbir yerde kalmaz
                AsamayaGec(Asama.Gume, ayar.SonucSuresiSn);
                Yay(new Olay { Tur = OlayTuru.Gume, A = GorunenA(), B = b, C = c });
            }
        }

        void KaybedenCeza(OyuncuId? kaybeden)
        {
            var seviye = EtkinCezaSeviyesi();
            if (!kaybeden.HasValue || seviye == CezaSeviyesi.Kapali) return;
            int adet = katlandi && kaybeden == ilkKaybeden ? 2 : 1;
            for (int i = 0; i < adet; i++) CezaCek(kaybeden.Value, seviye, CekimNedeni.Kaybetti);
        }

        /// <summary>Ceza Turu kaosu seviye Kapalı olsa da ceza getirir: Temiz modda Hafif, değilse Cesur (Ana Plan 8.3).</summary>
        CezaSeviyesi EtkinCezaSeviyesi()
        {
            if (kaos != KaosKurali.CezaTuru || ayar.CezaSeviyesi != CezaSeviyesi.Kapali) return ayar.CezaSeviyesi;
            return ayar.Mod == OyunModu.Temiz ? CezaSeviyesi.Hafif : CezaSeviyesi.Cesur;
        }

        // ------------------------------------------------------------ Cezalar

        void CezaCek(OyuncuId kim, CezaSeviyesi seviye, CekimNedeni neden)
        {
            Ceza ceza;
            if (seviye == CezaSeviyesi.Cesur && !sesliDeste.Bos && rastgele.Next(2) == 0)
            {
                ceza = new Ceza { Tur = CezaTuru.Sesli, Metin = sesliDeste.Cek(rastgele) };
            }
            else
            {
                // "Havuzdan sor" Curcuna'da (zaten havuzdan) ve havuz boşken hiçbir şey yapmaz: çekilmez.
                var adaylar = new List<CezaTuru>(Kisitlamalar);
                if (ayar.Mod == OyunModu.Curcuna || oneriDestesi.Bos) adaylar.Remove(CezaTuru.HavuzdanSor);
                var tur = adaylar[rastgele.Next(adaylar.Count)];
                ceza = new Ceza { Tur = tur, Kalan = tur == CezaTuru.Palyaco ? 2 : 1, Yeni = true };
                IzAl(kim).Kisitlamalar.Add(ceza);
            }

            Bul(kim).CezaCekme++;
            turCezalari.Add(new CezaCekimi { Kim = kim, Ceza = ceza.Kopya(), Neden = neden });
            Yay(new Olay { Tur = OlayTuru.CezaCekildi, Kim = kim, Ceza = ceza.Kopya() });
        }

        Ceza KisitlamaBul(OyuncuId kim, CezaTuru tur) => IzAl(kim).Kisitlamalar.Find(k => k.Tur == tur);

        bool KisitlamaKullan(OyuncuId kim, CezaTuru tur)
        {
            var k = KisitlamaBul(kim, tur);
            if (k == null) return false;
            IzAl(kim).Kisitlamalar.Remove(k);
            return true;
        }

        int YeniEsik() => rastgele.Next(EsikEnAz, EsikEnFazla + 1);

        /// <summary>
        /// Kesin sayı vermeden ipucu. Yanmadan önce her zaman "kızgın" görünür, ama sonraki devirde mi
        /// ondan sonrakinde mi yakacağı belli olmaz: B'nin "buna verirsem yanar mı" kumarı bu.
        /// </summary>
        IsiSeviyesi IsiHesapla()
        {
            int kalan = Math.Max(1, esik - devir + isiRuhu);
            if (devir < 3) return IsiSeviyesi.Soguk;
            if (kalan <= 2) return IsiSeviyesi.Kizgin;
            if (kalan <= 5) return IsiSeviyesi.Sicak;
            return IsiSeviyesi.Ilik;
        }

        // ------------------------------------------------------------ Görünüm

        void TekrarGorunumu(Gorunum g, OyuncuId kim)
        {
            bool turda = asama != Asama.SoruToplama && asama != Asama.OyunSonu;
            g.Kaos = turda ? kaos : KaosKurali.Yok;
            if (KizginCakmak) g.Isi = IsiHesapla();
            g.Gorevin = IzAl(kim).Gorev?.Kopya();

            if (asama == Asama.IkiyeKatla || asama == Asama.Bedel) g.KararVeren = kararVeren;

            if (asama == Asama.Oylama)
            {
                g.Oylayanlar = new List<OyuncuId>(oylayanlar);
                g.OyVerdin = oylar.ContainsKey(kim);
                g.OyKullanan = oylar.Count;
            }

            if ((asama == Asama.Ifsa || asama == Asama.Gume) && sonOylama != null)
                g.SonOylama = new OylamaSonucu { Ifsa = sonOylama.Ifsa, Gume = sonOylama.Gume };
            g.BedelOdendi = asama == Asama.Gume && bedelOdendi;

            if (turda)
            {
                var liste = new List<CezaCekimi>(turCezalari.Count);
                foreach (var cek in turCezalari) liste.Add(new CezaCekimi { Kim = cek.Kim, Ceza = cek.Ceza.Kopya(), Neden = cek.Neden });
                g.TurCezalari = liste;
            }
        }

        List<OyuncuDurumu> OyuncuKopyasi()
        {
            var liste = new List<OyuncuDurumu>(oyuncular.Count);
            foreach (var o in oyuncular)
            {
                var kisitlamalar = new List<Ceza>();
                foreach (var k in IzAl(o.Id).Kisitlamalar) kisitlamalar.Add(k.Kopya());
                liste.Add(new OyuncuDurumu
                {
                    Id = o.Id,
                    Isim = o.Isim,
                    HavuzaEkledigi = o.HavuzaEkledigi,
                    Gosterilme = o.Gosterilme,
                    SoruAlma = o.SoruAlma,
                    SoruSorma = o.SoruSorma,
                    IfsaEttirme = o.IfsaEttirme,
                    Saklama = o.Saklama,
                    MiniOyunKazanma = o.MiniOyunKazanma,
                    OynadigiMiniOyun = o.OynadigiMiniOyun,
                    CezaCekme = o.CezaCekme,
                    Yanma = o.Yanma,
                    BedelOdeme = o.BedelOdeme,
                    BedelHakki = o.BedelHakki,
                    KaosKazanma = o.KaosKazanma,
                    Kisitlamalar = kisitlamalar,
                });
            }
            return liste;
        }

        // ------------------------------------------------------------ Görevler

        static readonly GorevTuru[] KolayGorevler = { GorevTuru.KisiyiGoster, GorevTuru.FarkliKisiGoster };
        static readonly GorevTuru[] OrtaGorevler = { GorevTuru.MiniOyunKazan, GorevTuru.HicIfsaOlma, GorevTuru.AyniKisiyeSor };
        static readonly GorevTuru[] ZorGorevler = { GorevTuru.SeniGostersin, GorevTuru.HicGosterilme, GorevTuru.KaosTurundaKazan };

        Gorev GorevUret(OyuncuId sahibi, Zorluk zorluk)
        {
            var turler = new List<GorevTuru>(zorluk == Zorluk.Kolay ? KolayGorevler : zorluk == Zorluk.Orta ? OrtaGorevler : ZorGorevler);
            // Kaos kapalıysa ya da oyun ilk kaos turuna (5. tur) varmadan bitecekse kaos görevi verilmez.
            if (!ayar.KaosTurlari || (ayar.TurSayisi > 0 && ayar.TurSayisi < KaosAraligi)) turler.Remove(GorevTuru.KaosTurundaKazan);
            var tur = turler[rastgele.Next(turler.Count)];

            var gorev = new Gorev { Tur = tur, Zorluk = zorluk };
            switch (tur)
            {
                case GorevTuru.KisiyiGoster: gorev.Adet = 2; gorev.Hedef = RastgeleOyuncu(sahibi); break;
                case GorevTuru.FarkliKisiGoster: gorev.Adet = 3; break;
                case GorevTuru.MiniOyunKazan: gorev.Adet = 3; break;
                case GorevTuru.HicIfsaOlma: gorev.Adet = 1; break;
                case GorevTuru.AyniKisiyeSor: gorev.Adet = 2; break;
                case GorevTuru.SeniGostersin: gorev.Adet = 1; gorev.Hedef = RastgeleOyuncu(sahibi); break;
                case GorevTuru.KaosTurundaKazan: gorev.Adet = 1; break;
            }
            return gorev;
        }

        bool GorevBasarili(OyuncuDurumu o, Iz iz)
        {
            var g = iz.Gorev;
            switch (g.Tur)
            {
                case GorevTuru.KisiyiGoster: return Sayi(iz.Gosterdigi, g.Hedef.Value) >= g.Adet;
                case GorevTuru.FarkliKisiGoster: return iz.Gosterdigi.Count >= g.Adet;
                case GorevTuru.MiniOyunKazan: return o.MiniOyunKazanma >= g.Adet;
                case GorevTuru.HicIfsaOlma: return o.SoruAlma >= g.Adet && iz.BIkenIfsa == 0;
                case GorevTuru.AyniKisiyeSor: return EnBuyuk(iz.Sordugu) >= g.Adet;
                case GorevTuru.SeniGostersin: return Sayi(iz.GosterenKisi, g.Hedef.Value) >= g.Adet;
                case GorevTuru.HicGosterilme: return o.Gosterilme == 0;
                case GorevTuru.KaosTurundaKazan: return o.KaosKazanma >= g.Adet;
                default: return false;
            }
        }

        static int Sayi(Dictionary<OyuncuId, int> d, OyuncuId k) => d.TryGetValue(k, out var n) ? n : 0;

        static int EnBuyuk(Dictionary<OyuncuId, int> d)
        {
            int enBuyuk = 0;
            foreach (var n in d.Values) if (n > enBuyuk) enBuyuk = n;
            return enBuyuk;
        }

        // ------------------------------------------------------------ Özet ve unvanlar

        /// <summary>
        /// Oyunun özeti, oyun bitince çağrılır. Güme giden soru içinde yoktur.
        /// Oyun bitmeden çağrılırsa gizli parçalar (görevler, ifşa listesi) boş gelir: herkesin görevi ve
        /// Soran Gizli turlarının A'sı oyun sırasında kimseye gitmesin.
        /// </summary>
        public OyunOzeti Ozet()
        {
            bool bitti = asama == Asama.OyunSonu;
            var gorevler = new List<GorevSonucu>();
            if (ayar.GizliGorevler && bitti)
                foreach (var o in oyuncular)
                {
                    var iz = IzAl(o.Id);
                    gorevler.Add(new GorevSonucu { Kim = o.Id, Gorev = iz.Gorev.Kopya(), Basarili = GorevBasarili(o, iz) });
                }

            var ifsalar = new List<IfsaKaydi>(ifsaKayitlari.Count);
            if (bitti) foreach (var k in ifsaKayitlari) ifsalar.Add(new IfsaKaydi { Soru = k.Soru, A = k.A, B = k.B, C = k.C });

            return new OyunOzeti
            {
                OynananTur = tur,
                Oyuncular = OyuncuKopyasi(),
                Unvanlar = UnvanlariSec(),
                Gorevler = gorevler,
                IfsaOlanlar = ifsalar,
                CurcunaSorulari = new List<string>(curcunaYazilan),
            };
        }

        /// <summary>Her unvanın tek bir sahibi olmalı (eşitlikte kimse almaz). En uç 3 unvan seçilir.</summary>
        List<Unvan> UnvanlariSec()
        {
            var adaylar = new List<KeyValuePair<Unvan, double>>();

            void Aday(UnvanTuru tur, Func<OyuncuDurumu, Iz, int> deger, int enAz)
            {
                int enBuyuk = -1;
                OyuncuId? sahibi = null;
                bool esit = false;
                double toplam = 0;
                foreach (var o in oyuncular)
                {
                    int d = deger(o, IzAl(o.Id));
                    toplam += d;
                    if (d > enBuyuk) { enBuyuk = d; sahibi = o.Id; esit = false; }
                    else if (d == enBuyuk) esit = true;
                }
                if (esit || enBuyuk < enAz) return;
                double ortalama = toplam / oyuncular.Count;
                adaylar.Add(new KeyValuePair<Unvan, double>(
                    new Unvan { Tur = tur, Kim = sahibi.Value, Deger = enBuyuk }, enBuyuk / Math.Max(ortalama, 0.5)));
            }

            Aday(UnvanTuru.Miknatis, (o, iz) => o.Gosterilme, 2);
            Aday(UnvanTuru.Gammaz, (o, iz) => o.IfsaEttirme, 2);
            Aday(UnvanTuru.SirKupu, (o, iz) => o.Saklama, 2);
            Aday(UnvanTuru.HedefTahtasi, (o, iz) => o.SoruAlma, 2);
            Aday(UnvanTuru.SoruMakinesi, (o, iz) => o.SoruSorma, 2);
            Aday(UnvanTuru.Sansli, (o, iz) => o.MiniOyunKazanma, 2);
            Aday(UnvanTuru.Takintili, (o, iz) => EnBuyuk(iz.Sordugu), 2);
            Aday(UnvanTuru.Intikamci, (o, iz) => iz.Intikam, 1);
            Aday(UnvanTuru.AtesleOynayan, (o, iz) => o.Yanma, 1);
            Aday(UnvanTuru.Bedelci, (o, iz) => o.BedelOdeme, 1);
            Aday(UnvanTuru.Cezakes, (o, iz) => o.CezaCekme, 2);
            Aday(UnvanTuru.KaosCanavari, (o, iz) => o.KaosKazanma, 1);
            Aday(UnvanTuru.Kale, (o, iz) => iz.BIkenIfsa == 0 ? o.SoruAlma : 0, 2);
            Aday(UnvanTuru.Talihsiz, (o, iz) => o.MiniOyunKazanma == 0 ? o.OynadigiMiniOyun : 0, 2);

            // Görünmez: tek bir kişi hiç gösterilmediyse, ve herkese en az bir sıra gelecek kadar tur oynandıysa.
            var gosterilmeyen = oyuncular.FindAll(o => o.Gosterilme == 0);
            if (gosterilmeyen.Count == 1 && tur >= oyuncular.Count)
                adaylar.Add(new KeyValuePair<Unvan, double>(new Unvan { Tur = UnvanTuru.Gorunmez, Kim = gosterilmeyen[0].Id }, 1.5));

            adaylar.Sort((x, y) => y.Value != x.Value ? y.Value.CompareTo(x.Value) : x.Key.Tur.CompareTo(y.Key.Tur));
            var secilen = new List<Unvan>();
            for (int i = 0; i < adaylar.Count && i < 3; i++) secilen.Add(adaylar[i].Key);
            return secilen;
        }
    }
}
