using System;
using System.Collections.Generic;
using Cakmak.Kurallar;

namespace TheLighter.Akis
{
    /// <summary>Bölünmüş ekranda kimin hamle yapacağı. Sırayı ekran belirler, sonucu kural motoru.</summary>
    public enum MiniOyunAdimi
    {
        /// <summary>Gizli seçim (TKM, Tek-Çift): önce B seçer, C bakmaz.</summary>
        BSeciyor,

        /// <summary>Gizli seçim: sonra C seçer, B bakmaz.</summary>
        CSeciyor,

        /// <summary>Zar: gizli bir şey yok, ikisi de kendi zarını atar.</summary>
        IkisiDeAtiyor,

        /// <summary>Reaksiyon: ikisi aynı anda, yeşil olunca basar.</summary>
        Reaksiyon,

        /// <summary>İkisi de oynadı, sonuç bekleniyor.</summary>
        Bekliyor,
    }

    /// <summary>
    /// Elden ele (tek cihaz) modunun akışı. Kural hesaplamaz: neyin seçilebileceği, sıranın kimde
    /// olduğu, kimin kazandığı <see cref="Oyun"/>'dan gelir. Bu sınıfın işi telefonun kimin elinde
    /// olduğunu takip etmek, perdeyi göstermek ve hangi ekranın kimin görünümüyle çizileceğine karar vermek.
    /// Perde açıkken ve oyun duraklatıldığında <see cref="Oyun.Ilerle"/> çağrılmaz: telefon el değiştirirken süre akmaz.
    /// </summary>
    public sealed class EldenEleAkisi
    {
        readonly Oyun oyun;
        readonly List<Oyuncu> oyuncular;
        readonly List<IfsaKaydi> ifsaOlanlar = new List<IfsaKaydi>();
        string kaldirilanPerde;
        OyuncuId? suresiDolan;
        bool suresiDolanBydi;
        Hamle? bBuEl;
        Hamle? cBuEl;
        BerabereEl sonBerabere;

        public EldenEleAkisi(Oyun oyun, IReadOnlyList<Oyuncu> oyuncular, Ayarlar ayarlar)
        {
            if (oyun == null) throw new ArgumentNullException(nameof(oyun));
            if (oyuncular == null || oyuncular.Count == 0) throw new ArgumentException("Oyuncu yok.", nameof(oyuncular));
            this.oyun = oyun;
            this.oyuncular = new List<Oyuncu>(oyuncular);
            Ayarlar = ayarlar ?? new Ayarlar();
            oyun.OlayOldu += OlayGeldi;
            Yenile();
        }

        /// <summary>Kural motorunu kurar ve akışı başlatır. Kur hata verirse akış null döner.</summary>
        public static Sonuc Kur(Ayarlar ayarlar, IReadOnlyList<Oyuncu> oyuncular, int tohum, out EldenEleAkisi akis)
        {
            akis = null;
            var s = Oyun.Kur(ayarlar, oyuncular, tohum, out var oyun);
            if (s != Sonuc.Tamam) return s;
            akis = new EldenEleAkisi(oyun, oyuncular, ayarlar);
            return Sonuc.Tamam;
        }

        public IReadOnlyList<Oyuncu> Oyuncular => oyuncular;
        public Ayarlar Ayarlar { get; }
        public EkranDurumu Ekran { get; private set; }

        /// <summary>Menü açıkken true. Saat ilerlemez.</summary>
        public bool Duraklatildi { get; set; }

        public bool Bitti => Ekran.Tur == EkranTuru.OyunSonu;

        // ---------- Zaman ----------

        public void Ilerle(TimeSpan gecen)
        {
            if (!Duraklatildi && !Ekran.SaatDurur) oyun.Ilerle(gecen);
            Yenile();
        }

        // ---------- Komutlar ----------

        /// <summary>Perdede "hazırım" denince.</summary>
        public void PerdeyiKaldir()
        {
            if (!Ekran.SaatDurur) return;
            kaldirilanPerde = PerdeAnahtari(Acik());
            suresiDolan = null;
            suresiDolanBydi = false;
            Yenile();
        }

        public Sonuc SoruGonder(OyuncuId b, string metin)
        {
            if (Ekran.Tur != EkranTuru.SoruSorma) return Sonuc.YanlisAsama;
            return Sonra(oyun.SoruSor(Ekran.Kime.Value, b, metin ?? ""));
        }

        /// <summary>Soru kulağa söylendi. Uygulama metni bilmez.</summary>
        public Sonuc Fisildadim(OyuncuId b)
        {
            if (Ekran.Tur != EkranTuru.SoruSorma) return Sonuc.YanlisAsama;
            return Sonra(oyun.SoruSor(Ekran.Kime.Value, b, null));
        }

        /// <summary>Öneri A'nın görünümüne düşer: <c>Ekran.Gorunum.Oneri</c>.</summary>
        public Sonuc OneriCek()
        {
            if (Ekran.Tur != EkranTuru.SoruSorma) return Sonuc.YanlisAsama;
            return Sonra(oyun.OneriCek(Ekran.Kime.Value));
        }

        public Sonuc CevapSec(OyuncuId c)
        {
            if (Ekran.Tur != EkranTuru.CevapSecme) return Sonuc.YanlisAsama;
            return Sonra(oyun.CevapSec(Ekran.Kime.Value, c));
        }

        public Sonuc Hamle(OyuncuId kim, Hamle hamle)
        {
            if (Ekran.Tur != EkranTuru.MiniOyun) return Sonuc.YanlisAsama;
            bool bMi = Ekran.BGorunumu != null && kim == Ekran.BGorunumu.Sen;

            // Hamle motora gitmeden kaydedilir: ikinci hamlede "Berabere" olayı bu çağrının içinde gelir.
            var eskiB = bBuEl;
            var eskiC = cBuEl;
            if (bMi) bBuEl = hamle; else cBuEl = hamle;
            sonBerabere = null;

            var s = oyun.MiniOyunHamlesi(kim, hamle);
            if (s != Sonuc.Tamam)
            {
                bBuEl = eskiB;
                cBuEl = eskiC;
            }
            return Sonra(s);
        }

        public Sonuc OyunuBitir()
        {
            Duraklatildi = false;
            return Sonra(oyun.OyunuBitir());
        }

        Sonuc Sonra(Sonuc s)
        {
            Yenile();
            return s;
        }

        // ---------- Yardımcılar ----------

        public Oyuncu OyuncuBul(OyuncuId id)
        {
            foreach (var o in oyuncular)
                if (o.Id == id) return o;
            return null;
        }

        /// <summary>Renk ve sıra numarası için oyuncunun listedeki yeri.</summary>
        public int Sira(OyuncuId id)
        {
            for (int i = 0; i < oyuncular.Count; i++)
                if (oyuncular[i].Id == id) return i;
            return 0;
        }

        public string Isim(OyuncuId? id)
        {
            if (id == null) return "";
            var o = OyuncuBul(id.Value);
            return o == null ? id.Value.ToString() : o.Isim;
        }

        /// <summary>Bölünmüş ekranda sıradaki adım. Kural değil, elden ele sıralaması.</summary>
        public static MiniOyunAdimi SiradakiAdim(Gorunum g)
        {
            if (g == null || g.Asama != Asama.MiniOyun) return MiniOyunAdimi.Bekliyor;
            if (g.BHamleYapti && g.CHamleYapti) return MiniOyunAdimi.Bekliyor;
            switch (g.MiniOyun)
            {
                case MiniOyunTuru.Zar: return MiniOyunAdimi.IkisiDeAtiyor;
                case MiniOyunTuru.Reaksiyon: return MiniOyunAdimi.Reaksiyon;
                default: return g.BHamleYapti ? MiniOyunAdimi.CSeciyor : MiniOyunAdimi.BSeciyor;
            }
        }

        // ---------- Olaylar ----------

        void OlayGeldi(Olay o)
        {
            switch (o.Tur)
            {
                case OlayTuru.Ifsa:
                    ifsaOlanlar.Add(new IfsaKaydi { Soru = o.Soru, Soran = o.A, Cevap = o.C });
                    break;

                case OlayTuru.SureDoldu:
                    // Olay yeni tur başlamadan yayılıyor: aşama hâlâ süresi dolan aşama.
                    suresiDolan = o.Kim;
                    suresiDolanBydi = Acik().Asama == Asama.CevapSecme;
                    break;

                case OlayTuru.Berabere:
                    if (bBuEl.HasValue && cBuEl.HasValue)
                        sonBerabere = new BerabereEl { Tur = bBuEl.Value.Tur, BHamle = bBuEl.Value, CHamle = cBuEl.Value };
                    bBuEl = null;
                    cBuEl = null;
                    break;

                case OlayTuru.CevapSecildi:
                case OlayTuru.TurBasladi:
                    bBuEl = null;
                    cBuEl = null;
                    sonBerabere = null;
                    break;
            }
        }

        // ---------- Ekranı hesapla ----------

        Gorunum Acik() => oyun.GorunumAl(oyuncular[0].Id);

        void Yenile()
        {
            // Herkese açık alanlar (aşama, A, B, C, tur) için herhangi birinin görünümü yeter.
            var acik = Acik();
            var e = new EkranDurumu();
            string perde = PerdeAnahtari(acik);
            bool perdeAcik = perde != null && perde != kaldirilanPerde;

            switch (acik.Asama)
            {
                case Asama.SoruSorma:
                    e.Kime = acik.A;
                    e.PerdeNedeni = PerdeNedeni.SoruSirasi;
                    e.SuresiDolan = suresiDolan;
                    e.SuresiDolanBydi = suresiDolanBydi;
                    if (perdeAcik) PerdeKur(e, EkranTuru.Perde, perde);
                    else Kur(e, EkranTuru.SoruSorma, oyun.GorunumAl(acik.A.Value), "");
                    break;

                case Asama.CevapSecme:
                    e.Kime = acik.B;
                    e.PerdeNedeni = PerdeNedeni.CevapSirasi;
                    if (perdeAcik) PerdeKur(e, EkranTuru.Perde, perde);
                    else Kur(e, EkranTuru.CevapSecme, oyun.GorunumAl(acik.B.Value), "");
                    break;

                case Asama.MiniOyun:
                    if (perdeAcik)
                    {
                        PerdeKur(e, EkranTuru.MiniOyunPerdesi, perde);
                    }
                    else
                    {
                        // Reaksiyon ekranı kendi zamanlamasını tutar: biri basınca baştan çizilmesin.
                        string ek = acik.MiniOyun == MiniOyunTuru.Reaksiyon
                            ? "|" + acik.Beraberlik
                            : "|" + acik.BHamleYapti + "|" + acik.CHamleYapti + "|" + acik.Beraberlik;
                        Kur(e, EkranTuru.MiniOyun, Seyirci(acik), ek);
                        e.BGorunumu = Temizle(oyun.GorunumAl(acik.B.Value));
                        e.CGorunumu = Temizle(oyun.GorunumAl(acik.C.Value));
                        if (!acik.BHamleYapti && !acik.CHamleYapti) e.SonBerabere = sonBerabere;
                    }
                    break;

                case Asama.Ifsa:
                    Kur(e, EkranTuru.Ifsa, Seyirci(acik), "");
                    break;

                case Asama.Gume:
                    Kur(e, EkranTuru.Gume, Seyirci(acik), "");
                    break;

                case Asama.OyunSonu:
                    Kur(e, EkranTuru.OyunSonu, Seyirci(acik), "");
                    e.IfsaOlanlar = ifsaOlanlar.AsReadOnly();
                    break;

                default:
                    Kur(e, EkranTuru.Bekleniyor, Seyirci(acik), "");
                    break;
            }

            Ekran = e;
        }

        static void Kur(EkranDurumu e, EkranTuru tur, Gorunum g, string ek)
        {
            e.Tur = tur;
            e.Gorunum = g;
            e.Anahtar = tur + "|" + g.Tur + "|" + g.A + "|" + g.B + "|" + g.C + ek;
        }

        void PerdeKur(EkranDurumu e, EkranTuru tur, string perdeAnahtari)
        {
            e.Tur = tur;
            e.Gorunum = Seyirci(Acik());
            e.Anahtar = tur + "|" + perdeAnahtari;
        }

        /// <summary>Telefonun el değiştirmesi gereken anlar. Aynı anahtar için perde bir kez gösterilir.</summary>
        static string PerdeAnahtari(Gorunum g)
        {
            switch (g.Asama)
            {
                case Asama.SoruSorma: return "S|" + g.Tur + "|" + g.A;
                case Asama.CevapSecme: return "C|" + g.Tur + "|" + g.B;
                case Asama.MiniOyun: return "M|" + g.Tur + "|" + g.B + "|" + g.C;
                default: return null;
            }
        }

        /// <summary>
        /// Herkesin baktığı ekranlar için gizli bilgisi olmayan birinin görünümü: A, B ve C dışından biri.
        /// En az 4 oyuncu olduğu için turda her zaman vardır.
        /// </summary>
        Gorunum Seyirci(Gorunum acik)
        {
            foreach (var o in oyuncular)
                if (o.Id != acik.A && o.Id != acik.B && o.Id != acik.C) return oyun.GorunumAl(o.Id);
            foreach (var o in oyuncular)
                if (o.Id != acik.B) return oyun.GorunumAl(o.Id);
            return acik;
        }

        /// <summary>Telefon ikisinin arasında duruyor: ekranda kullanılmayan gizli alanlar boşaltılır.</summary>
        static Gorunum Temizle(Gorunum g)
        {
            g.Soru = null;
            g.Fisilti = false;
            g.Hamlen = null;
            g.Oneri = null;
            g.AtananSoru = null;
            return g;
        }
    }
}
