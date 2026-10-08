using System.Collections.Generic;
using Cakmak.Kurallar;
using UnityEngine.UIElements;
using static TheLighter.Istemci.Arayuz;

namespace TheLighter.Istemci
{
    /// <summary>Elden ele: 4–10 isim, mini oyun ve tur sayısı.</summary>
    public sealed class KurulumEkrani : IEkran
    {
        // Oyuncu sınırları kural motorundan. İsim uzunluğu sadece ekrana sığsın diye.
        const int EnAz = Oyun.EnAzOyuncu;
        const int EnFazla = Oyun.EnFazlaOyuncu;
        const int IsimUzunlugu = 14;

        static readonly int[] TurSecenekleri = { 10, 15, 20, 0 };
        static readonly MiniOyunTuru[] MiniSecenekleri =
            { MiniOyunTuru.Tkm, MiniOyunTuru.TekCift, MiniOyunTuru.Zar, MiniOyunTuru.Reaksiyon, MiniOyunTuru.Karisik };

        readonly Uygulama uygulama;
        readonly Ayarlar ayarlar;
        readonly List<TextField> kutular = new List<TextField>();
        readonly List<VisualElement> satirlar = new List<VisualElement>();
        readonly List<Button> silButonlari = new List<Button>();
        readonly List<Button> miniButonlari = new List<Button>();
        readonly List<Button> turButonlari = new List<Button>();
        readonly VisualElement liste;
        readonly Button ekle;
        readonly Label hata;
        bool baslaDenendi;

        public VisualElement Kok { get; }

        public KurulumEkrani(Uygulama uygulama)
        {
            this.uygulama = uygulama;
            var son = uygulama.SonAyarlar;
            ayarlar = new Ayarlar { MiniOyun = son.MiniOyun, TurSayisi = son.TurSayisi };

            Kok = Kutu("ekran", "kurulum");

            var ust = Kutu("satir");
            ust.Add(Buton(Metin.Al("genel.geri"), uygulama.AnaMenu, "buton-kucuk"));
            ust.Add(Esnek());
            Kok.Add(ust);

            var kaydirma = new ScrollView(ScrollViewMode.Vertical);
            kaydirma.AddToClassList("kaydirma");
            Kok.Add(kaydirma);

            kaydirma.Add(Yazi(Metin.Al("kurulum.baslik"), "baslik"));
            kaydirma.Add(Yazi(Metin.Al("kurulum.aciklama", EnAz, EnFazla), "aciklama"));

            liste = Kutu("isim-listesi");
            kaydirma.Add(liste);
            ekle = Buton(Metin.Al("kurulum.ekle"), () => { SatirEkle(""); Durum(); }, "buton-ikincil");
            kaydirma.Add(ekle);

            kaydirma.Add(Yazi(Metin.Al("kurulum.mini_oyun"), "alt-baslik"));
            var miniSatiri = Kutu("secenekler");
            foreach (var m in MiniSecenekleri)
            {
                var secim = m;
                var b = Buton(Metin.Al("mini.ad." + m), () => { ayarlar.MiniOyun = secim; Durum(); }, "secenek");
                miniButonlari.Add(b);
                miniSatiri.Add(b);
            }
            kaydirma.Add(miniSatiri);

            kaydirma.Add(Yazi(Metin.Al("kurulum.tur"), "alt-baslik"));
            var turSatiri = Kutu("secenekler");
            foreach (var t in TurSecenekleri)
            {
                var secim = t;
                var b = Buton(t == 0 ? Metin.Al("kurulum.tur_sinirsiz") : Metin.Al("kurulum.tur_sayi", t),
                    () => { ayarlar.TurSayisi = secim; Durum(); }, "secenek");
                turButonlari.Add(b);
                turSatiri.Add(b);
            }
            kaydirma.Add(turSatiri);

            hata = Yazi("", "hata");
            Kok.Add(hata);
            Kok.Add(Buton(Metin.Al("kurulum.basla"), Basla, "buton-birincil", "buton-buyuk"));

            var isimler = uygulama.SonIsimler;
            for (int i = 0; i < isimler.Count && i < EnFazla; i++) SatirEkle(isimler[i]);
            while (kutular.Count < EnAz) SatirEkle("");
            Durum();
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

        void Durum()
        {
            for (int i = 0; i < satirlar.Count; i++)
            {
                satirlar[i].Q<Label>(className: "isim-no").text = (i + 1) + ".";
                silButonlari[i].SetEnabled(kutular.Count > EnAz);
            }
            ekle.SetEnabled(kutular.Count < EnFazla);

            for (int i = 0; i < MiniSecenekleri.Length; i++)
                miniButonlari[i].EnableInClassList("secili", MiniSecenekleri[i] == ayarlar.MiniOyun);
            for (int i = 0; i < TurSecenekleri.Length; i++)
                turButonlari[i].EnableInClassList("secili", TurSecenekleri[i] == ayarlar.TurSayisi);

            List<string> isimler;
            hata.text = baslaDenendi ? (Dogrula(out isimler) ?? "") : "";
        }

        /// <summary>Hata varsa metnini, yoksa null döner.</summary>
        string Dogrula(out List<string> isimler)
        {
            isimler = new List<string>();
            var gorulen = new HashSet<string>();
            for (int i = 0; i < kutular.Count; i++)
            {
                string isim = (kutular[i].value ?? "").Trim();
                if (isim.Length == 0) return Metin.Al("kurulum.hata_bos", i + 1);
                if (!gorulen.Add(Katla(isim))) return Metin.Al("kurulum.hata_ayni", isim);
                isimler.Add(isim);
            }
            if (isimler.Count < EnAz) return Metin.Al("kurulum.hata_az", EnAz);
            return null;
        }

        void Basla()
        {
            baslaDenendi = true;
            List<string> isimler;
            string h = Dogrula(out isimler);
            hata.text = h ?? "";
            if (h != null) return;
            var s = uygulama.OyunuBaslat(isimler, ayarlar);
            if (s != Sonuc.Tamam) hata.text = HataMetni.Al(s);
        }

        /// <summary>"Ali" ile "ali" aynı kişi. Türkçe I/İ doğru küçülsün diye kültürden bağımsız katlama.</summary>
        static string Katla(string s)
        {
            return s.Replace('I', 'ı').Replace('İ', 'i').ToLowerInvariant();
        }

        public void Guncelle(float gecenSn) { }

        public void ArkaPlanaGecti() { }
    }
}
