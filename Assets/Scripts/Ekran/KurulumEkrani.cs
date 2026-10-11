using System.Collections.Generic;
using Cakmak.Kurallar;
using UnityEngine.UIElements;
using static TheLighter.Istemci.Arayuz;

namespace TheLighter.Istemci
{
    /// <summary>
    /// Elden ele lobisi (Ana Plan 8.8): isimler, mod, mini oyun, tur, paketler, ceza seviyesi,
    /// gizli görevler, kaos turları. En üstte "Hızlı başla". Bugünkü oyuncular kayıtlı bir grupsa
    /// grubun hafızası ve "Bizim paket" görünür. Temiz modda sadece Aile Dostu ve en fazla Hafif ceza.
    /// </summary>
    public sealed class KurulumEkrani : IEkran
    {
        // Oyuncu sınırları kural motorundan. İsim uzunluğu sadece ekrana sığsın diye.
        const int EnAz = Oyun.EnAzOyuncu;
        const int EnFazla = Oyun.EnFazlaOyuncu;
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
        readonly List<Button> modButonlari = new List<Button>();
        readonly List<Button> miniButonlari = new List<Button>();
        readonly List<Button> turButonlari = new List<Button>();
        readonly List<Button> cezaButonlari = new List<Button>();
        readonly List<KeyValuePair<string, Button>> paketButonlari = new List<KeyValuePair<string, Button>>();
        readonly VisualElement liste;
        readonly VisualElement hafizaKutusu;
        readonly VisualElement paketKutusu;
        readonly VisualElement efsanePerdesi;
        readonly Label modAciklama;
        readonly Label paketNotu;
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

            kaydirma.Add(Yazi(Metin.Al("kurulum.baslik"), "baslik"));
            kaydirma.Add(Yazi(Metin.Al("kurulum.aciklama", EnAz, EnFazla), "aciklama"));
            liste = Kutu("isim-listesi");
            kaydirma.Add(liste);
            ekle = Buton(Metin.Al("kurulum.ekle"), () => { SatirEkle(""); Durum(); }, "buton-ikincil");
            kaydirma.Add(ekle);

            hafizaKutusu = Kutu("hafiza-karti", "gizli");
            kaydirma.Add(hafizaKutusu);

            kaydirma.Add(Yazi(Metin.Al("kurulum.mod"), "alt-baslik"));
            kaydirma.Add(Secenekler(ModSecenekleri, m => Metin.Al("mod.ad." + m), m => ayarlar.Mod = m, modButonlari));
            modAciklama = Yazi("", "ipucu");
            kaydirma.Add(modAciklama);

            kaydirma.Add(Yazi(Metin.Al("kurulum.mini_oyun"), "alt-baslik"));
            kaydirma.Add(Secenekler(MiniSecenekleri, m => Metin.Al("mini.ad." + m), m => ayarlar.MiniOyun = m, miniButonlari));

            kaydirma.Add(Yazi(Metin.Al("kurulum.tur"), "alt-baslik"));
            kaydirma.Add(Secenekler(TurSecenekleri,
                t => t == 0 ? Metin.Al("kurulum.tur_sinirsiz") : Metin.Al("kurulum.tur_sayi", t),
                t => ayarlar.TurSayisi = t, turButonlari));

            kaydirma.Add(Yazi(Metin.Al("kurulum.paketler"), "alt-baslik"));
            paketKutusu = Kutu("secenekler");
            kaydirma.Add(paketKutusu);
            paketNotu = Yazi("", "ipucu");
            kaydirma.Add(paketNotu);

            kaydirma.Add(Yazi(Metin.Al("kurulum.ceza"), "alt-baslik"));
            kaydirma.Add(Secenekler(CezaSecenekleri, c => Metin.Al("ceza.seviye." + c), c => ayarlar.CezaSeviyesi = c, cezaButonlari));
            cezaAciklama = Yazi("", "ipucu");
            kaydirma.Add(cezaAciklama);

            kaydirma.Add(Yazi(Metin.Al("kurulum.ekstra"), "alt-baslik"));
            var ekstra = Kutu("secenekler");
            gorevButonu = Buton("", () => { ayarlar.GizliGorevler = !ayarlar.GizliGorevler; Durum(); }, "secenek");
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
        }

        void SatirSil(VisualElement satir)
        {
            int i = satirlar.IndexOf(satir);
            if (i < 0 || kutular.Count <= EnAz) return;
            satir.RemoveFromHierarchy();
            satirlar.RemoveAt(i);
            kutular.RemoveAt(i);
            silButonlari.RemoveAt(i);
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

            bool temiz = ayarlar.Mod == OyunModu.Temiz;
            if (temiz && ayarlar.CezaSeviyesi == CezaSeviyesi.Cesur) ayarlar.CezaSeviyesi = CezaSeviyesi.Hafif;

            for (int i = 0; i < ModSecenekleri.Length; i++)
                modButonlari[i].EnableInClassList("secili", ModSecenekleri[i] == ayarlar.Mod);
            modAciklama.text = Metin.Al("mod.aciklama." + ayarlar.Mod);
            for (int i = 0; i < MiniSecenekleri.Length; i++)
                miniButonlari[i].EnableInClassList("secili", MiniSecenekleri[i] == ayarlar.MiniOyun);
            for (int i = 0; i < TurSecenekleri.Length; i++)
                turButonlari[i].EnableInClassList("secili", TurSecenekleri[i] == ayarlar.TurSayisi);
            for (int i = 0; i < CezaSecenekleri.Length; i++)
            {
                cezaButonlari[i].EnableInClassList("secili", CezaSecenekleri[i] == ayarlar.CezaSeviyesi);
                cezaButonlari[i].SetEnabled(!temiz || CezaSecenekleri[i] != CezaSeviyesi.Cesur);
            }
            cezaAciklama.text = Metin.Al("ceza.seviye_aciklama." + ayarlar.CezaSeviyesi)
                + (temiz ? "  " + Metin.Al("kurulum.ceza_temiz") : "");

            gorevButonu.text = Metin.Al("kurulum.gorevler") + ": " + Metin.Al(ayarlar.GizliGorevler ? "kurulum.acik" : "kurulum.kapali");
            gorevButonu.EnableInClassList("secili", ayarlar.GizliGorevler);
            kaosButonu.text = Metin.Al("kurulum.kaos") + ": " + Metin.Al(ayarlar.KaosTurlari ? "kurulum.acik" : "kurulum.kapali");
            kaosButonu.EnableInClassList("secili", ayarlar.KaosTurlari);

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
            if (ayarlar.Mod != OyunModu.Temiz && SeciliPaketler().Count == 0) return Metin.Al("kurulum.hata_paket");
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
            var s = uygulama.OyunuBaslat(isimler, ayarlar, SeciliPaketler());
            if (s != Sonuc.Tamam) hata.text = HataMetni.Al(s);
        }

        public void Guncelle(float gecenSn) { }

        public void ArkaPlanaGecti() { }
    }
}
