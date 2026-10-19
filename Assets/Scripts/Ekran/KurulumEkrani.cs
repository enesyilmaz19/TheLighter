using System.Collections.Generic;
using Cakmak.Kurallar;
using TheLighter.Akis;
using UnityEngine.UIElements;
using static TheLighter.Istemci.Arayuz;

namespace TheLighter.Istemci
{
    /// <summary>
    /// Elden ele lobisi (Ana Plan 8.8): isimler, mod, mini oyun, tur, paketler, ceza seviyesi,
    /// gizli görevler, kaos turları. En üstte "Hızlı başla". Bugünkü oyuncular kayıtlı bir grupsa
    /// grubun hafızası ve "Bizim paket" görünür. Temiz modda sadece Aile Dostu ve en fazla Hafif ceza.
    /// Takım Düellosu'nda isimlerin altında takım dağıtma (dokun: takım değiştir, boşsa rastgele).
    /// </summary>
    public sealed class KurulumEkrani : IEkran, IGeriTusu
    {
        // Oyuncu sınırları kural motorundan. İsim uzunluğu sadece ekrana sığsın diye.
        // Çakmak Gecesi 6–18, diğer oyunlar 4–10 (kural motorunun sabitleri).
        int EnAz => OyuncuSayisi.EnAz(ayarlar.Oyun);
        int EnFazla => OyuncuSayisi.EnFazla(ayarlar.Oyun);
        const int IsimUzunlugu = 14;
        const int GosterilenSeri = 3;

        static readonly int[] TurSecenekleri = { 10, 15, 20, 0 };
        static readonly MiniOyunTuru[] MiniSecenekleri =
            { MiniOyunTuru.Tkm, MiniOyunTuru.TekCift, MiniOyunTuru.Zar, MiniOyunTuru.Reaksiyon, MiniOyunTuru.Karisik };
        static readonly OyunModu[] ModSecenekleri = { OyunModu.Normal, OyunModu.Temiz, OyunModu.Curcuna };
        static readonly CezaSeviyesi[] CezaSecenekleri = { CezaSeviyesi.Kapali, CezaSeviyesi.Hafif, CezaSeviyesi.Cesur };

        readonly Uygulama uygulama;
        readonly Ayarlar ayarlar;
        readonly HashSet<string> paketler;
        readonly List<GrupDosyasi> gruplar;
        readonly List<TextField> kutular = new List<TextField>();
        readonly List<VisualElement> satirlar = new List<VisualElement>();
        readonly List<Button> silButonlari = new List<Button>();
        /// <summary>Her satırın kalıcı kimliği (satır silinince diğerlerinin takımı kaymasın). Oyunda p1 = 1. satır.</summary>
        readonly List<OyuncuId> satirIdleri = new List<OyuncuId>();
        int sonSatirNo;
        readonly TakimDagitimi dagitim = new TakimDagitimi();
        readonly TakimDagitmaParcasi takimParcasi;
        readonly List<Button> oyunButonlari = new List<Button>();
        readonly List<Button> canButonlari = new List<Button>();
        readonly VisualElement canBolumu;
        readonly List<Button> casusButonlari = new List<Button>();
        readonly VisualElement casusBolumu;
        readonly Label casusAciklama;
        readonly VisualElement turBolumu;
        readonly Label turYok;
        readonly List<Button> modButonlari = new List<Button>();
        readonly List<Button> miniButonlari = new List<Button>();
        readonly List<Button> turButonlari = new List<Button>();
        readonly List<Button> cezaButonlari = new List<Button>();
        readonly List<KeyValuePair<string, Button>> paketButonlari = new List<KeyValuePair<string, Button>>();
        readonly VisualElement liste;
        readonly VisualElement hafizaKutusu;
        readonly VisualElement paketKutusu;
        readonly VisualElement efsanePerdesi;
        readonly Label oyunAciklama;
        readonly Label modAciklama;
        readonly Label paketNotu;
        readonly Label kisiAciklama;
        readonly VisualElement paketBolumu;
        readonly Label geceSoruNotu;
        readonly GeceAyarlari geceAyarlari;
        readonly Label cezaAciklama;
        readonly Button gorevButonu;
        readonly Button kaosButonu;
        readonly Button ekle;
        readonly Label hata;
        GrupDosyasi eslesen;
        string eslesenYol;
        bool baslaDenendi;

        public VisualElement Kok { get; }

        public KurulumEkrani(Uygulama uygulama)
        {
            this.uygulama = uygulama;
            var son = uygulama.SonAyarlar;
            ayarlar = new Ayarlar
            {
                Oyun = son.Oyun,
                Can = son.Can,
                Casus = son.Casus,
                RolAcilir = son.RolAcilir,
                GecelikKurban = son.GecelikKurban,
                KapaliRoller = new List<Rol>(son.KapaliRoller),
                Mod = son.Mod,
                MiniOyun = son.MiniOyun,
                TurSayisi = son.TurSayisi,
                CezaSeviyesi = son.CezaSeviyesi,
                GizliGorevler = son.GizliGorevler,
                KaosTurlari = son.KaosTurlari,
            };
            paketler = new HashSet<string>(uygulama.SonPaketler);
            gruplar = GrupDeposu.Hepsi();

            Kok = Kutu("ekran", "kurulum");

            var ust = Kutu("satir");
            ust.Add(Buton(Metin.Al("genel.geri"), uygulama.AnaMenu, "buton-kucuk"));
            ust.Add(Esnek());
            Kok.Add(ust);

            var kaydirma = new ScrollView(ScrollViewMode.Vertical);
            kaydirma.AddToClassList("kaydirma");
            Kok.Add(kaydirma);

            // Çok ayar yeni oyuncuyu korkutur: tek dokunuşla önerilen oyun (Ana Plan 8.8).
            kaydirma.Add(Buton(Metin.Al("kurulum.hizli"), HizliBasla, "buton-birincil", "buton-buyuk"));
            kaydirma.Add(Yazi(Metin.Al("kurulum.hizli_ipucu"), "ipucu"));

            // Oyun ve mod iki ayrı eksen (docs/Oyunlar.md 0): oyun kendi amacı ve kazananıyla, mod soruların nereden geldiği.
            kaydirma.Add(Yazi(Metin.Al("kurulum.oyun"), "alt-baslik"));
            kaydirma.Add(Secenekler(OyunSecimi.Secenekler, o => Metin.Al("oyun.ad." + o), o => ayarlar.Oyun = o, oyunButonlari));
            oyunAciklama = Yazi("", "ipucu");
            kaydirma.Add(oyunAciklama);
            // Rus Ruleti: herkesin canı (3 = Son Çakmak).
            canBolumu = Kutu("ayar-bolumu");
            canBolumu.Add(Yazi(Metin.Al("rus.can_baslik"), "alt-baslik"));
            canBolumu.Add(Secenekler(OyunSecimi.CanSecenekleri, CanYazisi, c => ayarlar.Can = c, canButonlari));
            kaydirma.Add(canBolumu);
            // Casus: mekân ya da kelime (Imposter).
            casusBolumu = Kutu("ayar-bolumu");
            casusBolumu.Add(Yazi(Metin.Al("casus.tur_baslik"), "alt-baslik"));
            casusBolumu.Add(Secenekler(OyunSecimi.CasusSecenekleri, c => Metin.Al("casus.tur." + c), c => ayarlar.Casus = c, casusButonlari));
            casusAciklama = Yazi("", "ipucu");
            casusBolumu.Add(casusAciklama);
            kaydirma.Add(casusBolumu);

            kaydirma.Add(Yazi(Metin.Al("kurulum.baslik"), "baslik"));
            kisiAciklama = Yazi("", "aciklama");
            kaydirma.Add(kisiAciklama);
            liste = Kutu("isim-listesi");
            kaydirma.Add(liste);
            ekle = Buton(Metin.Al("kurulum.ekle"), () => { SatirEkle(""); Durum(); }, "buton-ikincil");
            kaydirma.Add(ekle);
            // Takım Düellosu: isimlerin hemen altında takımlar.
            takimParcasi = new TakimDagitmaParcasi();
            kaydirma.Add(takimParcasi.Kok);

            hafizaKutusu = Kutu("hafiza-karti", "gizli");
            kaydirma.Add(hafizaKutusu);

            kaydirma.Add(Yazi(Metin.Al("kurulum.mod"), "alt-baslik"));
            kaydirma.Add(Secenekler(ModSecenekleri, m => Metin.Al("mod.ad." + m), m => ayarlar.Mod = m, modButonlari));
            modAciklama = Yazi("", "ipucu");
            kaydirma.Add(modAciklama);

            kaydirma.Add(Yazi(Metin.Al("kurulum.mini_oyun"), "alt-baslik"));
            kaydirma.Add(Secenekler(MiniSecenekleri, m => Metin.Al("mini.ad." + m), m => ayarlar.MiniOyun = m, miniButonlari));

            turBolumu = Kutu("ayar-bolumu");
            turBolumu.Add(Yazi(Metin.Al("kurulum.tur"), "alt-baslik"));
            turBolumu.Add(Secenekler(TurSecenekleri,
                t => t == 0 ? Metin.Al("kurulum.tur_sinirsiz") : Metin.Al("kurulum.tur_sayi", t),
                t => ayarlar.TurSayisi = t, turButonlari));
            kaydirma.Add(turBolumu);
            // Rus Ruleti'nde tur sayısı yok: son kalan kazanır.
            turYok = Yazi("", "ipucu");
            kaydirma.Add(turYok);

            // Çakmak Gecesi: rol açılması, gecelik kurban, roller. Paket seçimi bu oyunda yok (öneriler suçlama sorularından).
            geceAyarlari = new GeceAyarlari(
                a => { ayarlar.RolAcilir = a; Durum(); },
                k => { ayarlar.GecelikKurban = k; Durum(); },
                r =>
                {
                    var yeni = GeceAyarlari.Degistir(ayarlar.KapaliRoller, r);
                    if (OyunSecimi.Uyumlu(ayarlar, x => x.KapaliRoller = yeni)) ayarlar.KapaliRoller = yeni;
                    Durum();
                });
            kaydirma.Add(geceAyarlari.Kok);
            geceSoruNotu = Yazi(Metin.Al("gece.paket_yok"), "ipucu");
            kaydirma.Add(geceSoruNotu);

            paketBolumu = Kutu("ayar-bolumu");
            paketBolumu.Add(Yazi(Metin.Al("kurulum.paketler"), "alt-baslik"));
            paketKutusu = Kutu("secenekler");
            paketBolumu.Add(paketKutusu);
            paketNotu = Yazi("", "ipucu");
            paketBolumu.Add(paketNotu);
            kaydirma.Add(paketBolumu);

            kaydirma.Add(Yazi(Metin.Al("kurulum.ceza"), "alt-baslik"));
            kaydirma.Add(Secenekler(CezaSecenekleri, c => Metin.Al("ceza.seviye." + c), c => ayarlar.CezaSeviyesi = c, cezaButonlari));
            cezaAciklama = Yazi("", "ipucu");
            kaydirma.Add(cezaAciklama);

            kaydirma.Add(Yazi(Metin.Al("kurulum.ekstra"), "alt-baslik"));
            var ekstra = Kutu("secenekler");
            gorevButonu = Buton("", () =>
            {
                if (OyunSecimi.GorevlerHerkeste(ayarlar.Oyun)) return;
                ayarlar.GizliGorevler = !ayarlar.GizliGorevler;
                Durum();
            }, "secenek");
            kaosButonu = Buton("", () => { ayarlar.KaosTurlari = !ayarlar.KaosTurlari; Durum(); }, "secenek");
            ekstra.Add(gorevButonu);
            ekstra.Add(kaosButonu);
            kaydirma.Add(ekstra);

            hata = Yazi("", "hata");
            Kok.Add(hata);
            Kok.Add(Buton(Metin.Al("kurulum.basla"), Basla, "buton-birincil", "buton-buyuk"));

            efsanePerdesi = Kutu("menu-perde", "gizli");
            Kok.Add(efsanePerdesi);

            var isimler = uygulama.SonIsimler;
            for (int i = 0; i < isimler.Count && i < EnFazla; i++) SatirEkle(isimler[i]);
            while (kutular.Count < EnAz) SatirEkle("");
            // Lobiye dönüldüyse son oyunun takımları (isimler aynı sırada geri geldi).
            if (son.Oyun == OyunTuru.Takim && son.TakimDagilimi != null)
                for (int i = 0; i < satirIdleri.Count; i++)
                    if (son.TakimDagilimi.TryGetValue(new OyuncuId(i + 1), out var takimNo)) dagitim.Koy(satirIdleri[i], takimNo);
            Durum();
        }

        VisualElement Secenekler<T>(T[] secenekler, System.Func<T, string> yazi, System.Action<T> sec, List<Button> butonlar)
        {
            var satir = Kutu("secenekler");
            foreach (var s in secenekler)
            {
                var secim = s;
                var b = Buton(yazi(s), () => { sec(secim); Durum(); }, "secenek");
                butonlar.Add(b);
                satir.Add(b);
            }
            return satir;
        }

        void SatirEkle(string isim)
        {
            if (kutular.Count >= EnFazla) return;

            var satir = Kutu("isim-satiri");
            var no = Yazi("", "isim-no");
            var kutu = new TextField { maxLength = IsimUzunlugu, value = isim ?? "" };
            kutu.AddToClassList("isim-kutusu");
            kutu.RegisterValueChangedCallback(_ => Durum());
            var sil = Buton(Metin.Al("kurulum.sil"), () => SatirSil(satir), "buton-kucuk", "sil");

            satir.Add(no);
            satir.Add(kutu);
            satir.Add(sil);
            liste.Add(satir);
            satirlar.Add(satir);
            kutular.Add(kutu);
            silButonlari.Add(sil);
            satirIdleri.Add(new OyuncuId(++sonSatirNo));
        }

        void SatirSil(VisualElement satir)
        {
            int i = satirlar.IndexOf(satir);
            if (i < 0 || kutular.Count <= EnAz) return;
            satir.RemoveFromHierarchy();
            satirlar.RemoveAt(i);
            kutular.RemoveAt(i);
            silButonlari.RemoveAt(i);
            satirIdleri.RemoveAt(i);
            dagitim.Esitle(satirIdleri);
            Durum();
        }

        List<string> YazilanIsimler()
        {
            var l = new List<string>();
            foreach (var k in kutular)
            {
                string s = (k.value ?? "").Trim();
                if (s.Length > 0) l.Add(s);
            }
            return l;
        }

        void Durum()
        {
            for (int i = 0; i < satirlar.Count; i++)
            {
                satirlar[i].Q<Label>(className: "isim-no").text = (i + 1) + ".";
                silButonlari[i].SetEnabled(kutular.Count > EnAz);
            }
            ekle.SetEnabled(kutular.Count < EnFazla);
            kisiAciklama.text = Metin.Al("kurulum.aciklama", EnAz, EnFazla);
            bool gece = ayarlar.Oyun == OyunTuru.CakmakGecesi;
            geceAyarlari.Kok.EnableInClassList("gizli", !gece);
            geceAyarlari.Ciz(ayarlar.RolAcilir, ayarlar.GecelikKurban, ayarlar.KapaliRoller);
            geceSoruNotu.EnableInClassList("gizli", !gece);
            paketBolumu.EnableInClassList("gizli", gece);

            bool temiz = ayarlar.Mod == OyunModu.Temiz;
            if (temiz && ayarlar.CezaSeviyesi == CezaSeviyesi.Cesur) ayarlar.CezaSeviyesi = CezaSeviyesi.Hafif;
            // Oyun değişince uymayan ayarlar kapanır (Köstebek'te kaos yok); kural motoruna sorulur.
            OyunSecimi.Uyumla(ayarlar);

            for (int i = 0; i < OyunSecimi.Secenekler.Length; i++)
                oyunButonlari[i].EnableInClassList("secili", OyunSecimi.Secenekler[i] == ayarlar.Oyun);
            oyunAciklama.text = Metin.Al("oyun.aciklama." + ayarlar.Oyun);
            canBolumu.EnableInClassList("gizli", ayarlar.Oyun != OyunTuru.RusRuleti);
            for (int i = 0; i < OyunSecimi.CanSecenekleri.Length; i++)
                canButonlari[i].EnableInClassList("secili", OyunSecimi.CanSecenekleri[i] == ayarlar.Can);
            casusBolumu.EnableInClassList("gizli", ayarlar.Oyun != OyunTuru.Casus);
            for (int i = 0; i < OyunSecimi.CasusSecenekleri.Length; i++)
                casusButonlari[i].EnableInClassList("secili", OyunSecimi.CasusSecenekleri[i] == ayarlar.Casus);
            casusAciklama.text = Metin.Al("casus.tur_aciklama." + ayarlar.Casus);
            bool turYokMu = OyunSecimi.TurSayisiYok(ayarlar.Oyun);
            turBolumu.EnableInClassList("gizli", turYokMu);
            turYok.EnableInClassList("gizli", !turYokMu);
            turYok.text = turYokMu ? Metin.Al("kurulum.tur_yok." + ayarlar.Oyun) : "";
            TakimlariCiz();

            for (int i = 0; i < ModSecenekleri.Length; i++)
            {
                var m = ModSecenekleri[i];
                modButonlari[i].EnableInClassList("secili", m == ayarlar.Mod);
                // Oyuna uymayan mod (Casus'ta Curcuna) kapalı: kural motoruna sorulur.
                modButonlari[i].SetEnabled(m == ayarlar.Mod || OyunSecimi.Uyumlu(ayarlar, a => a.Mod = m));
            }
            modAciklama.text = Metin.Al("mod.aciklama." + ayarlar.Mod);
            for (int i = 0; i < MiniSecenekleri.Length; i++)
                miniButonlari[i].EnableInClassList("secili", MiniSecenekleri[i] == ayarlar.MiniOyun);
            for (int i = 0; i < TurSecenekleri.Length; i++)
                turButonlari[i].EnableInClassList("secili", TurSecenekleri[i] == ayarlar.TurSayisi);
            for (int i = 0; i < CezaSecenekleri.Length; i++)
            {
                cezaButonlari[i].EnableInClassList("secili", CezaSecenekleri[i] == ayarlar.CezaSeviyesi);
                var c = CezaSecenekleri[i];
                // Uymayan ceza seviyesi (Temiz'de Cesur, Rus Ruleti'nde hepsi) kural motoruna sorulur.
                cezaButonlari[i].SetEnabled(c == ayarlar.CezaSeviyesi || OyunSecimi.Uyumlu(ayarlar, a => a.CezaSeviyesi = c));
            }
            cezaAciklama.text = Metin.Al("ceza.seviye_aciklama." + ayarlar.CezaSeviyesi)
                + (temiz ? "  " + Metin.Al("kurulum.ceza_temiz") : "");

            bool herkeste = OyunSecimi.GorevlerHerkeste(ayarlar.Oyun);
            bool gorevAcik = herkeste || ayarlar.GizliGorevler;
            gorevButonu.text = Metin.Al("kurulum.gorevler") + ": " + Metin.Al(herkeste ? "kurulum.herkeste" : gorevAcik ? "kurulum.acik" : "kurulum.kapali");
            gorevButonu.EnableInClassList("secili", gorevAcik);
            // Kilitli ama açık (Köstebek'te herkeste) soluklaşmasın, okunabilir kalsın: tıklama yok sayılır.
            gorevButonu.SetEnabled(herkeste || ayarlar.GizliGorevler || OyunSecimi.Uyumlu(ayarlar, a => a.GizliGorevler = true));
            kaosButonu.text = Metin.Al("kurulum.kaos") + ": " + Metin.Al(ayarlar.KaosTurlari ? "kurulum.acik" : "kurulum.kapali");
            kaosButonu.EnableInClassList("secili", ayarlar.KaosTurlari);
            kaosButonu.SetEnabled(ayarlar.KaosTurlari || OyunSecimi.Uyumlu(ayarlar, a => a.KaosTurlari = true));

            // Grup: isimler değiştikçe yeniden eşlenir. Grup değişirse hafıza kartı ve "Bizim paket" yeniden kurulur.
            eslesen = GrupDeposu.Eslesen(YazilanIsimler(), gruplar);
            string yol = eslesen != null ? eslesen.Yol : null;
            if (yol != eslesenYol || paketButonlari.Count == 0)
            {
                eslesenYol = yol;
                HafizayiKur();
                PaketleriKur();
            }
            PaketDurumu(temiz);

            List<string> isimler;
            hata.text = baslaDenendi ? (Dogrula(out isimler) ?? "") : "";
        }

        // ---------- Takımlar (Takım Düellosu) ----------

        void TakimlariCiz()
        {
            bool takimli = ayarlar.Oyun == OyunTuru.Takim;
            takimParcasi.Kok.EnableInClassList("gizli", !takimli);
            if (!takimli) return;
            takimParcasi.Ciz(dagitim, satirIdleri, SatirIsmi, SatirRengi,
                id => { dagitim.Dokun(id); Durum(); },
                () => { dagitim.Temizle(); Durum(); });
        }

        string SatirIsmi(OyuncuId id)
        {
            int i = satirIdleri.IndexOf(id);
            string isim = i >= 0 ? (kutular[i].value ?? "").Trim() : "";
            return isim.Length > 0 ? isim : Metin.Al("kurulum.isimsiz", i + 1);
        }

        /// <summary>Oyundaki rengi: elden elede renk sırası satır sırası.</summary>
        int SatirRengi(OyuncuId id) => System.Math.Max(0, satirIdleri.IndexOf(id));

        /// <summary>Kurulacak oyunun dağılımı: satır sırasıyla p1, p2... Rastgeleyse null.</summary>
        Dictionary<OyuncuId, int> TakimHaritasi()
        {
            if (ayarlar.Oyun != OyunTuru.Takim || dagitim.Rastgele) return null;
            var m = new Dictionary<OyuncuId, int>();
            for (int i = 0; i < satirIdleri.Count; i++)
            {
                var t = dagitim.Takimi(satirIdleri[i]);
                if (t.HasValue) m[new OyuncuId(i + 1)] = t.Value;
            }
            return m;
        }

        // ---------- Paketler ----------

        void PaketleriKur()
        {
            paketKutusu.Clear();
            paketButonlari.Clear();
            foreach (var p in uygulama.Paketler)
                PaketButonu(p.Ad, Metin.Al("kurulum.paket_sayi", p.GorunenAd, p.Sorular.Count));
            if (eslesen != null && eslesen.Kayit.GrupPaketi.Count > 0)
                PaketButonu(PaketBilgisi.Bizim, Metin.Al("kurulum.bizim_paket", eslesen.Kayit.GrupPaketi.Count));
        }

        void PaketButonu(string ad, string yazi)
        {
            var b = Buton(yazi, () =>
            {
                if (ayarlar.Mod == OyunModu.Temiz) return; // Temiz modda paket kilitli.
                if (!paketler.Remove(ad)) paketler.Add(ad);
                Durum();
            }, "secenek");
            paketButonlari.Add(new KeyValuePair<string, Button>(ad, b));
            paketKutusu.Add(b);
        }

        void PaketDurumu(bool temiz)
        {
            foreach (var pb in paketButonlari)
            {
                bool secili = temiz ? pb.Key == PaketBilgisi.AileDostu : paketler.Contains(pb.Key);
                pb.Value.EnableInClassList("secili", secili);
                // Kilitli ama seçili olan (Aile Dostu) soluklaşmasın, okunabilir kalsın.
                pb.Value.SetEnabled(!temiz || secili);
            }
            paketNotu.text = temiz ? Metin.Al("kurulum.paket_temiz") : "";
        }

        // ---------- Grup hafızası (Ana Plan 8.5) ----------

        void HafizayiKur()
        {
            hafizaKutusu.Clear();
            hafizaKutusu.EnableInClassList("gizli", eslesen == null);
            if (eslesen == null) return;
            var k = eslesen.Kayit;

            hafizaKutusu.Add(Yazi(k.Ad, "alt-baslik"));
            hafizaKutusu.Add(Yazi(Metin.Al("hafiza.oyun", k.OyunSayisi + 1), "aciklama"));

            GrupUyesi rekor = null;
            foreach (var u in k.Uyeler)
                if (u.RekorGosterilme > 0 && (rekor == null || u.RekorGosterilme > rekor.RekorGosterilme)) rekor = u;
            if (rekor != null) hafizaKutusu.Add(Yazi(Metin.Al("hafiza.rekor", rekor.Isim, rekor.RekorGosterilme), "ipucu"));

            int gosterilen = 0;
            foreach (var u in k.Uyeler)
                foreach (var s in u.Seriler)
                {
                    if (s.Seri < 2 || gosterilen >= GosterilenSeri) continue;
                    hafizaKutusu.Add(Yazi(Metin.Al("hafiza.seri", u.Isim, s.Seri, Metin.Al("unvan.ad." + s.Tur)), "ipucu"));
                    gosterilen++;
                }

            if (k.Efsaneler.Count > 0)
                hafizaKutusu.Add(Buton(Metin.Al("hafiza.efsaneler", k.Efsaneler.Count), EfsaneleriAc, "buton-kucuk"));
        }

        void EfsaneleriAc()
        {
            if (eslesen == null) return;
            efsanePerdesi.Clear();
            var kutu = Kutu("menu-kutu");
            kutu.Add(Yazi(Metin.Al("hafiza.efsaneler_baslik"), "baslik"));
            kutu.Add(Yazi(Metin.Al("hafiza.efsaneler_ipucu"), "ipucu"));
            var kaydirma = new ScrollView(ScrollViewMode.Vertical);
            kaydirma.AddToClassList("kaydirma");
            var efsaneler = eslesen.Kayit.Efsaneler;
            for (int i = efsaneler.Count - 1; i >= 0; i--)
            {
                var kart = Kutu("son-kart");
                kart.Add(Yazi(Metin.Al("sonuc.ifsa_soru", efsaneler[i]), "son-soru"));
                kaydirma.Add(kart);
            }
            kutu.Add(kaydirma);
            kutu.Add(Buton(Metin.Al("hafiza.kapat"), () => efsanePerdesi.AddToClassList("gizli"), "buton-birincil"));
            efsanePerdesi.Add(kutu);
            efsanePerdesi.RemoveFromClassList("gizli");
        }

        // ---------- Başlat ----------

        /// <summary>Hata varsa metnini, yoksa null döner.</summary>
        string Dogrula(out List<string> isimler)
        {
            isimler = new List<string>();
            var gorulen = new HashSet<string>();
            for (int i = 0; i < kutular.Count; i++)
            {
                string isim = (kutular[i].value ?? "").Trim();
                if (isim.Length == 0) return Metin.Al("kurulum.hata_bos", i + 1);
                if (!gorulen.Add(Oyuncu.IsimAnahtari(isim))) return Metin.Al("kurulum.hata_ayni", isim);
                isimler.Add(isim);
            }
            if (isimler.Count < EnAz) return Metin.Al("kurulum.hata_az", EnAz);
            // Başka oyundan çok kişiyle gelindiyse (Çakmak Gecesi 18, diğerleri 10).
            if (isimler.Count > EnFazla) return Metin.Al("kurulum.hata_cok", EnFazla);
            if (ayarlar.Oyun != OyunTuru.CakmakGecesi && ayarlar.Mod != OyunModu.Temiz && SeciliPaketler().Count == 0)
                return Metin.Al("kurulum.hata_paket");
            // Takımlar kural motorunun kurulumuna sorulur (herkes bir takımda, en fazla 1 kişi fark).
            if (ayarlar.Oyun == OyunTuru.Takim) return TakimDagitmaParcasi.UyariMetni(dagitim, satirIdleri);
            return null;
        }

        List<string> SeciliPaketler()
        {
            // Listede olmayan (silinmiş ya da başka grubun) paket adları gitmesin.
            var l = new List<string>();
            foreach (var pb in paketButonlari)
                if (paketler.Contains(pb.Key)) l.Add(pb.Key);
            return l;
        }

        /// <summary>Normal, TKM, 15 tur, Genel paket, Cesur, görevler açık, kaos açık (Ana Plan 8.8).</summary>
        void HizliBasla()
        {
            ayarlar.Oyun = OyunTuru.Cakmak;
            ayarlar.Mod = OyunModu.Normal;
            ayarlar.MiniOyun = MiniOyunTuru.Tkm;
            ayarlar.TurSayisi = 15;
            ayarlar.CezaSeviyesi = CezaSeviyesi.Cesur;
            ayarlar.GizliGorevler = true;
            ayarlar.KaosTurlari = true;
            paketler.Clear();
            paketler.Add(uygulama.Paketler.Exists(p => p.Ad == PaketBilgisi.Genel) || uygulama.Paketler.Count == 0
                ? PaketBilgisi.Genel
                : uygulama.Paketler[0].Ad);
            Durum();
            Basla();
        }

        void Basla()
        {
            baslaDenendi = true;
            List<string> isimler;
            string h = Dogrula(out isimler);
            hata.text = h ?? "";
            if (h != null) return;
            ayarlar.TakimDagilimi = TakimHaritasi();
            var s = uygulama.OyunuBaslat(isimler, ayarlar, SeciliPaketler());
            if (s != Sonuc.Tamam) hata.text = HataMetni.Al(s);
        }

        /// <summary>"1 can", "2 can", "3 can (Son Çakmak)".</summary>
        public static string CanYazisi(int can) =>
            can == Oyun.EnFazlaCan ? Metin.Al("rus.can_son_cakmak", can) : Metin.Al("rus.can_sayi", can);

        public void Guncelle(float gecenSn) { }

        public void ArkaPlanaGecti() { }

        public GeriEkrani GeriTuru => GeriEkrani.Kurulum;

        public void CikisSor() { }
    }
}
