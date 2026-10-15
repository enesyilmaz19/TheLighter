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
    /// Telefonun sırayla herkese dolaştığı anlarda da (görevler, Curcuna soru yazma, Grup Kararı oyu) saat durur:
    /// herkes masada, AFK yok; 10 kişinin sırayla oy vermesine motorun süresi yetmez.
    /// </summary>
    public sealed class EldenEleAkisi : IOyunAkisi
    {
        readonly Oyun oyun;
        readonly List<Oyuncu> oyuncular;
        readonly List<IfsaSatiri> ifsaOlanlar = new List<IfsaSatiri>();
        readonly HashSet<OyuncuId> goreviGorenler = new HashSet<OyuncuId>();
        string kaldirilanPerde;
        string onaylananCeza;
        OyuncuId? suresiDolan;
        bool suresiDolanBydi;
        Hamle? bBuEl;
        Hamle? cBuEl;
        BerabereEl sonBerabere;
        OyunOzeti ozet;

        // Köstebek: "kim?" oylaması. Tartışma istemcinin sayacıyla, kural motorunun saati oylama boyunca durur.
        int suclamaNo;
        OyuncuId? suclamaBaslatan;
        double tartismaKalanMs;
        bool tartismaBitti = true;
        SuclamaSonucu suclamaSonucu;

        // Rus Ruleti: son tetiğin sonucu ve final duyurusu, bir sonraki perdede.
        TetikSonucu tetikSonucu;
        bool finalDuyurusu;

        // Takım Düellosu: bu turun puanları, sonuç ekranında.
        readonly List<PuanAni> puanAnlari = new List<PuanAni>();

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
        public bool Online => false;
        /// <summary>Elden elede telefon herkesin: tek bir "ben" yok.</summary>
        public OyuncuId? Ben => null;
        /// <summary>Elden elede oyunu herkes bitirebilir (menüden).</summary>
        public bool Kurucuyum => true;
        public Ayarlar Ayarlar { get; }
        public EkranDurumu Ekran { get; private set; }

        /// <summary>Menü açıkken true. Saat ilerlemez.</summary>
        public bool Duraklatildi { get; set; }

        public bool Bitti => Ekran.Tur == EkranTuru.OyunSonu;

        // ---------- Zaman ----------

        public void Ilerle(TimeSpan gecen)
        {
            if (!Duraklatildi && !Ekran.SaatDurur) oyun.Ilerle(gecen);
            if (!Duraklatildi && Ekran.Tur == EkranTuru.SuclamaTartisma)
            {
                tartismaKalanMs -= Math.Max(0, gecen.TotalMilliseconds);
                if (tartismaKalanMs <= 0) tartismaBitti = true;
            }
            Yenile();
        }

        // ---------- Komutlar ----------

        /// <summary>Perdede "hazırım" denince.</summary>
        public void PerdeyiKaldir()
        {
            if (Ekran.Tur != EkranTuru.Perde && Ekran.Tur != EkranTuru.MiniOyunPerdesi) return;
            kaldirilanPerde = Ekran.PerdeAnahtari;
            suresiDolan = null;
            suresiDolanBydi = false;
            suclamaSonucu = null;
            tetikSonucu = null;
            finalDuyurusu = false;
            Yenile();
        }

        /// <summary>Görev ekranında "gördüm": telefon sıradakine geçer.</summary>
        public void GorevGordum()
        {
            if (Ekran.Tur != EkranTuru.GorevGoster || !Ekran.Kime.HasValue) return;
            goreviGorenler.Add(Ekran.Kime.Value);
            Yenile();
        }

        /// <summary>Curcuna: telefonu tutan kişi havuza bir soru ekler (kişi başı 2).</summary>
        public Sonuc HavuzaSoruEkle(string metin)
        {
            if (Ekran.Tur != EkranTuru.SoruToplama) return Sonuc.YanlisAsama;
            return Sonra(oyun.HavuzaSoruEkle(Ekran.Kime.Value, metin ?? ""));
        }

        /// <summary>
        /// A soruyu gönderir. Soru A'ya atandıysa (Curcuna, Kör Soru, "havuzdan sor" cezası) metin
        /// kural motorunda yok sayılır, atanan soru gider.
        /// </summary>
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

        /// <summary>Curcuna: A'ya atanan soruyu bir kez değiştirir. Yenisi <c>Ekran.Gorunum.AtananSoru</c>.</summary>
        public Sonuc SoruyuDegistir()
        {
            if (Ekran.Tur != EkranTuru.SoruSorma) return Sonuc.YanlisAsama;
            return Sonra(oyun.SoruyuDegistir(Ekran.Kime.Value));
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

        /// <summary>Grup Kararı: telefonu tutan kişinin oyu.</summary>
        public Sonuc Oy(bool ifsa)
        {
            if (Ekran.Tur != EkranTuru.Oylama) return Sonuc.YanlisAsama;
            return Sonra(oyun.Oy(Ekran.Kime.Value, ifsa));
        }

        /// <summary>İkiye Katla: kaybeden "bir el daha" der.</summary>
        public Sonuc IkiyeKatla()
        {
            if (Ekran.Tur != EkranTuru.Karar || !Ekran.Kime.HasValue) return Sonuc.YanlisAsama;
            return Sonra(oyun.IkiyeKatla(Ekran.Kime.Value));
        }

        /// <summary>Bedel: B soruyu kurtarır, iki ceza çeker.</summary>
        public Sonuc BedelOde()
        {
            if (Ekran.Tur != EkranTuru.Karar || !Ekran.Kime.HasValue) return Sonuc.YanlisAsama;
            return Sonra(oyun.BedelOde(Ekran.Kime.Value));
        }

        /// <summary>İkiye Katla ya da Bedel'e "hayır".</summary>
        public Sonuc KarariGec()
        {
            if (Ekran.Tur != EkranTuru.Karar || !Ekran.Kime.HasValue) return Sonuc.YanlisAsama;
            return Sonra(oyun.KarariGec(Ekran.Kime.Value));
        }

        /// <summary>Ceza kartları okundu: saat yeniden işler.</summary>
        public void CezalariOnayla()
        {
            if (!Ekran.CezaOnayBekliyor) return;
            onaylananCeza = CezaAnahtari(Ekran.Gorunum);
            Yenile();
        }

        /// <summary>Köstebek'te önce son oylamayı açar (kural motoru); oylama sürerken YanlisAsama.</summary>
        public Sonuc OyunuBitir()
        {
            Duraklatildi = false;
            return Sonra(oyun.OyunuBitir());
        }

        // ---------- Köstebek ----------

        public IReadOnlyList<OyuncuId> SuclamaHakkiOlanlar
        {
            get
            {
                var l = new List<OyuncuId>();
                foreach (var o in oyuncular)
                    if (oyun.GorunumAl(o.Id).SuclamaHakkin) l.Add(o.Id);
                return l;
            }
        }

        /// <summary>Telefon A'dayken: "Kim suçluyor?" sorusunda seçilen kişi hakkını kullanır.</summary>
        public Sonuc Sucla(OyuncuId kim)
        {
            if (Ekran.Tur != EkranTuru.SoruSorma) return Sonuc.YanlisAsama;
            return Sonra(oyun.Sucla(kim));
        }

        public Sonuc SuclamaOyu(OyuncuId hedef)
        {
            if (Ekran.Tur != EkranTuru.Suclama || !Ekran.Kime.HasValue) return Sonuc.YanlisAsama;
            return Sonra(oyun.SuclamaOyu(Ekran.Kime.Value, hedef));
        }

        // ---------- Rus Ruleti ----------

        public Sonuc TetikCek()
        {
            if (Ekran.Tur != EkranTuru.Tetik || !Ekran.Kime.HasValue) return Sonuc.YanlisAsama;
            return Sonra(oyun.TetikCek(Ekran.Kime.Value));
        }

        public Sonuc TetikDevret(OyuncuId hedef)
        {
            if (Ekran.Tur != EkranTuru.Tetik || !Ekran.Kime.HasValue) return Sonuc.YanlisAsama;
            return Sonra(oyun.TetikDevret(Ekran.Kime.Value, hedef));
        }

        public void TartismayiBitir()
        {
            if (Ekran.Tur != EkranTuru.SuclamaTartisma) return;
            tartismaBitti = true;
            Yenile();
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
                    ifsaOlanlar.Add(new IfsaSatiri { Soru = o.Soru, Soran = o.A, Cevap = o.C });
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

                case OlayTuru.SuclamaBasladi:
                    suclamaNo++;
                    suclamaBaslatan = o.Kim;
                    tartismaKalanMs = Ayarlar.SuclamaSuresiSn * 1000.0;
                    tartismaBitti = false;
                    suclamaSonucu = null;
                    break;

                case OlayTuru.SuclamaBitti:
                    suclamaSonucu = new SuclamaSonucu { Suclanan = o.Kim };
                    break;

                case OlayTuru.TetikBos:
                    if (o.Kim.HasValue) tetikSonucu = new TetikSonucu { Kim = o.Kim.Value };
                    break;

                case OlayTuru.TetikPatladi:
                    if (o.Kim.HasValue) tetikSonucu = new TetikSonucu { Kim = o.Kim.Value, Patladi = true };
                    break;

                case OlayTuru.Elendi:
                    if (tetikSonucu != null && o.Kim == tetikSonucu.Kim) tetikSonucu.Elendi = true;
                    break;

                case OlayTuru.FinalBasladi:
                    finalDuyurusu = true;
                    break;

                case OlayTuru.DuelloPuani:
                case OlayTuru.IfsaBonusu:
                    if (o.Takim.HasValue)
                        puanAnlari.Add(new PuanAni { IfsaBonusu = o.Tur == OlayTuru.IfsaBonusu, Kim = o.Kim, Takim = o.Takim.Value });
                    break;

                case OlayTuru.TurBasladi:
                    puanAnlari.Clear();
                    bBuEl = null;
                    cBuEl = null;
                    sonBerabere = null;
                    break;

                case OlayTuru.CevapSecildi:
                case OlayTuru.IkiyeKatlandi:
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

            // Gizli görevi (Köstebek'te rolü de) olan herkese oyun başında telefon sırayla gider (Ana Plan 8.2).
            // Kimin görevi olduğu kural motorundan: Köstebek'te ayar kapalı olsa da herkesin görevi var.
            if (acik.Asama != Asama.OyunSonu)
            {
                foreach (var o in oyuncular)
                {
                    if (goreviGorenler.Contains(o.Id) || oyun.GorunumAl(o.Id).Gorevin == null) continue;
                    e.Kime = o.Id;
                    e.PerdeNedeni = PerdeNedeni.Gorev;
                    string gp = "G|" + o.Id;
                    if (gp != kaldirilanPerde) PerdeKur(e, EkranTuru.Perde, gp);
                    else Kur(e, EkranTuru.GorevGoster, oyun.GorunumAl(o.Id), "|" + o.Id);
                    Ekran = e;
                    return;
                }
            }

            switch (acik.Asama)
            {
                case Asama.SoruToplama:
                {
                    OyuncuDurumu yazan = null;
                    foreach (var od in acik.Oyuncular)
                        if (od.HavuzaEkledigi < Oyun.KisiBasiHavuzSorusu) { yazan = od; break; }
                    if (yazan == null)
                    {
                        Kur(e, EkranTuru.Bekleniyor, Seyirci(acik), "");
                        break;
                    }
                    e.Kime = yazan.Id;
                    e.PerdeNedeni = PerdeNedeni.SoruYazma;
                    string tp = "T|" + yazan.Id;
                    if (tp != kaldirilanPerde) PerdeKur(e, EkranTuru.Perde, tp);
                    else Kur(e, EkranTuru.SoruToplama, oyun.GorunumAl(yazan.Id), "|" + yazan.Id + "|" + yazan.HavuzaEkledigi);
                    break;
                }

                case Asama.SoruSorma:
                {
                    e.Kime = acik.A;
                    e.PerdeNedeni = PerdeNedeni.SoruSirasi;
                    e.SuresiDolan = suresiDolan;
                    e.SuresiDolanBydi = suresiDolanBydi;
                    e.SuclamaSonucu = suclamaSonucu;
                    e.TetikSonucu = tetikSonucu;
                    // Oylamadan sonra telefon A'ya geri döner: perde yeniden (anahtarda oylamanın numarası).
                    string sp = "S|" + acik.Tur + "|" + acik.A + "|" + suclamaNo;
                    if (sp != kaldirilanPerde) PerdeKur(e, EkranTuru.Perde, sp);
                    else Kur(e, EkranTuru.SoruSorma, oyun.GorunumAl(acik.A.Value), "");
                    break;
                }

                case Asama.CevapSecme:
                {
                    e.Kime = acik.B;
                    e.PerdeNedeni = PerdeNedeni.CevapSirasi;
                    string cp = "C|" + acik.Tur + "|" + acik.B;
                    if (cp != kaldirilanPerde) PerdeKur(e, EkranTuru.Perde, cp);
                    else Kur(e, EkranTuru.CevapSecme, oyun.GorunumAl(acik.B.Value), "");
                    break;
                }

                case Asama.MiniOyun:
                {
                    string mp = "M|" + acik.Tur + "|" + acik.B + "|" + acik.C;
                    if (mp != kaldirilanPerde)
                    {
                        PerdeKur(e, EkranTuru.MiniOyunPerdesi, mp);
                        // Rus Ruleti finalinde soru yok: tetiğin sonucu ve "FİNAL!" bu perdede.
                        e.TetikSonucu = tetikSonucu;
                        e.FinalDuyurusu = finalDuyurusu;
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
                }

                case Asama.Oylama:
                {
                    // Telefon oy verecekler arasında sırayla dolaşır. Oy gizli: kimse kimin ne dediğini görmez.
                    OyuncuId? oylayan = null;
                    foreach (var id in acik.Oylayanlar)
                        if (!oyun.GorunumAl(id).OyVerdin) { oylayan = id; break; }
                    if (!oylayan.HasValue)
                    {
                        Kur(e, EkranTuru.Bekleniyor, Seyirci(acik), "");
                        break;
                    }
                    e.Kime = oylayan;
                    e.PerdeNedeni = PerdeNedeni.Oy;
                    string op = "O|" + acik.Tur + "|" + oylayan.Value;
                    if (op != kaldirilanPerde) PerdeKur(e, EkranTuru.Perde, op);
                    else Kur(e, EkranTuru.Oylama, GorevsizGorunum(oylayan.Value), "|" + oylayan.Value);
                    break;
                }

                case Asama.Suclama:
                {
                    // Önce telefon ortada tartışma (istemcinin sayacı), sonra telefon oy verecekler arasında dolaşır.
                    if (!tartismaBitti)
                    {
                        Kur(e, EkranTuru.SuclamaTartisma, Seyirci(acik), "|" + suclamaNo);
                        e.SuclamaBaslatan = suclamaBaslatan;
                        e.TartismaKalanMs = (int)Math.Max(0, tartismaKalanMs);
                        break;
                    }
                    OyuncuId? oylayan = null;
                    foreach (var id in acik.Oylayanlar)
                        if (!oyun.GorunumAl(id).OyVerdin) { oylayan = id; break; }
                    if (!oylayan.HasValue)
                    {
                        Kur(e, EkranTuru.Bekleniyor, Seyirci(acik), "");
                        break;
                    }
                    e.Kime = oylayan;
                    e.PerdeNedeni = PerdeNedeni.Suclama;
                    string yp = "Y|" + suclamaNo + "|" + oylayan.Value;
                    if (yp != kaldirilanPerde) PerdeKur(e, EkranTuru.Perde, yp);
                    else Kur(e, EkranTuru.Suclama, GorevsizGorunum(oylayan.Value), "|" + suclamaNo + "|" + oylayan.Value);
                    break;
                }

                case Asama.Tetik:
                {
                    // Açık bilgi, perde yok: telefon kaybedende, herkes ekrana bakar. Saat işler (süre dolunca çekilir).
                    var cekecek = acik.TetikCeken;
                    if (!cekecek.HasValue)
                    {
                        Kur(e, EkranTuru.Bekleniyor, Seyirci(acik), "");
                        break;
                    }
                    Kur(e, EkranTuru.Tetik, GorevsizGorunum(cekecek.Value), "|" + cekecek.Value);
                    e.Kime = cekecek;
                    break;
                }

                case Asama.IkiyeKatla:
                case Asama.Bedel:
                {
                    var g = Seyirci(acik);
                    Kur(e, EkranTuru.Karar, g, "|" + acik.Asama);
                    e.Kime = g.KararVeren;
                    break;
                }

                case Asama.Ifsa:
                case Asama.Gume:
                {
                    var g = Seyirci(acik);
                    bool bekliyor = g.TurCezalari.Count > 0 && CezaAnahtari(g) != onaylananCeza;
                    Kur(e, acik.Asama == Asama.Ifsa ? EkranTuru.Ifsa : EkranTuru.Gume, g, bekliyor ? "|ceza" : "");
                    e.CezaOnayBekliyor = bekliyor;
                    e.PuanAnlari = new List<PuanAni>(puanAnlari);
                    break;
                }

                case Asama.OyunSonu:
                    Kur(e, EkranTuru.OyunSonu, Seyirci(acik), "");
                    e.IfsaOlanlar = ifsaOlanlar.AsReadOnly();
                    if (ozet == null) ozet = oyun.Ozet();
                    e.Ozet = ozet;
                    break;

                default:
                    Kur(e, EkranTuru.Bekleniyor, Seyirci(acik), "");
                    break;
            }

            Ekran = e;
        }

        static string CezaAnahtari(Gorunum g) => "K|" + g.Tur + "|" + g.Asama;

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
            e.PerdeAnahtari = perdeAnahtari;
            e.Anahtar = tur + "|" + perdeAnahtari;
        }

        /// <summary>
        /// Herkesin baktığı ekranlar için gizli bilgisi olmayan birinin görünümü: A, B ve C dışından biri.
        /// En az 4 oyuncu olduğu için turda her zaman vardır. Görevi de temizlenir: ekranda kimsenin görevi durmasın.
        /// </summary>
        Gorunum Seyirci(Gorunum acik)
        {
            foreach (var o in oyuncular)
                if (o.Id != acik.A && o.Id != acik.B && o.Id != acik.C) return GorevsizGorunum(o.Id);
            foreach (var o in oyuncular)
                if (o.Id != acik.B) return GorevsizGorunum(o.Id);
            acik.Gorevin = null;
            acik.Rolun = Rol.Yok;
            acik.SuclamaHakkin = false;
            return acik;
        }

        /// <summary>
        /// Herkesin baktığı ekran için: kişiye özel olup ekranda durmaması gerekenler (görev, Köstebek'te rol ve suçlama hakkı)
        /// temizlenir. Seyirci köstebek olsa bile ortadaki ekranda bu görünmez.
        /// </summary>
        Gorunum GorevsizGorunum(OyuncuId id)
        {
            var g = oyun.GorunumAl(id);
            g.Gorevin = null;
            g.Rolun = Rol.Yok;
            g.SuclamaHakkin = false;
            return g;
        }

        /// <summary>Telefon ikisinin arasında duruyor: ekranda kullanılmayan gizli alanlar boşaltılır.</summary>
        static Gorunum Temizle(Gorunum g)
        {
            g.Soru = null;
            g.Fisilti = false;
            g.Hamlen = null;
            g.Oneri = null;
            g.AtananSoru = null;
            g.Gorevin = null;
            g.Rolun = Rol.Yok;
            g.SuclamaHakkin = false;
            return g;
        }
    }
}
