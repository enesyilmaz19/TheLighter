using TheLighter.Ag;
using TheLighter.Akis;
using UnityEngine.UIElements;
using static TheLighter.Istemci.Arayuz;

namespace TheLighter.Istemci
{
    /// <summary>
    /// Online giriş: isim, "Oda kur" ya da 5 harflik kodla "Katıl". Uygulama kapanıp açıldıysa "Kaldığın odaya dön".
    /// Bağlantı burada değil, oda kurarken/katılırken açılır (sunucu odasız bağlantıyı 120 sn'de keser).
    /// Sunucu adresi yayında sabit (<see cref="SunucuAdresi"/>); "Sunucu" alanı sadece editörde ve geliştirme build'inde.
    /// </summary>
    public sealed class OnlineMenuEkrani : IEkran, IGeriTusu
    {
        const int IsimUzunlugu = 20;   // sunucunun sınırı
        const int KodUzunlugu = 5;

        readonly OnlineYonetici online;
        readonly TextField isim;
        readonly TextField kod;
        readonly TextField adres;
        readonly VisualElement sunucuKutusu;
        readonly Button kur;
        readonly Button katil;
        readonly Button don;
        readonly Label durum;
        readonly Label hata;

        public VisualElement Kok { get; }

        public OnlineMenuEkrani(Uygulama uygulama, OnlineYonetici online, string neden)
        {
            this.online = online;
            Kok = Kutu("ekran", "online-menu");

            var ust = Kutu("satir");
            ust.Add(Buton(Metin.Al("genel.geri"), () => { online.Ayril(); uygulama.AnaMenu(); }, "buton-kucuk"));
            ust.Add(Esnek());
            if (SunucuAdresi.AlanGorunur(OnlineYonetici.Gelistirme))
                ust.Add(Buton(Metin.Al("online.sunucu"), () => sunucuKutusu.ToggleInClassList("gizli"), "buton-kucuk"));
            Kok.Add(ust);

            var kaydirma = new ScrollView(ScrollViewMode.Vertical);
            kaydirma.AddToClassList("kaydirma");
            Kok.Add(kaydirma);

            sunucuKutusu = Kutu("hafiza-karti", "gizli");
            sunucuKutusu.Add(Yazi(Metin.Al("online.sunucu_baslik"), "alt-baslik"));
            adres = new TextField { maxLength = 200, value = online.Adres };
            adres.AddToClassList("isim-kutusu");
            sunucuKutusu.Add(adres);
            sunucuKutusu.Add(Yazi(Metin.Al("online.sunucu_ipucu"), "ipucu"));
            sunucuKutusu.Add(Buton(Metin.Al("online.sunucu_kaydet"), AdresKaydet, "buton-ikincil"));
            kaydirma.Add(sunucuKutusu);

            kaydirma.Add(Yazi(Metin.Al("online.baslik"), "baslik"));
            kaydirma.Add(Yazi(Metin.Al("online.isim"), "alt-baslik"));
            isim = new TextField { maxLength = IsimUzunlugu, value = online.SonIsim };
            isim.AddToClassList("isim-kutusu");
            isim.RegisterValueChangedCallback(_ => Durum());
            kaydirma.Add(isim);

            kur = Buton(Metin.Al("online.oda_kur"), OdaKur, "buton-birincil", "buton-buyuk");
            kaydirma.Add(kur);
            kaydirma.Add(Yazi(Metin.Al("online.oda_kur_ipucu"), "ipucu"));

            kaydirma.Add(Yazi(Metin.Al("online.koda_katil"), "alt-baslik"));
            var satir = Kutu("satir");
            kod = new TextField { maxLength = KodUzunlugu };
            kod.AddToClassList("isim-kutusu");
            kod.AddToClassList("kod-kutusu");
            kod.RegisterValueChangedCallback(ev =>
            {
                // Kod büyük harf; I ve O hiç yok (sunucu kullanmıyor), elle düzeltmeye gerek yok, sunucu ODA_YOK der.
                string buyuk = (ev.newValue ?? "").ToUpperInvariant();
                if (buyuk != ev.newValue) kod.SetValueWithoutNotify(buyuk);
                Durum();
            });
            satir.Add(kod);
            katil = Buton(Metin.Al("online.katil"), Katil, "buton-birincil");
            satir.Add(katil);
            kaydirma.Add(satir);

            string kayitli = online.Istemci.KayitliOda;
            don = Buton(Metin.Al("online.geri_don", kayitli ?? ""), Don, "buton-ikincil");
            don.EnableInClassList("gizli", string.IsNullOrEmpty(kayitli));
            kaydirma.Add(don);

            durum = Yazi("", "aciklama", "orta");
            Kok.Add(durum);
            // Yayın build'inde sunucu adresi henüz yoksa (VPS gelmeden): online kapalı, neden yazılı.
            hata = Yazi(online.SunucuVar ? OnlineYonetici.NedenMetni(neden) : Metin.Al("online.sunucu_yok"), "hata");
            Kok.Add(hata);
            Durum();
        }

        string Isim => (isim.value ?? "").Trim();
        string Kod => (kod.value ?? "").Trim().ToUpperInvariant();

        void Durum()
        {
            bool giriyor = online.Istemci.Durum != BaglantiDurumu.Kapali;
            bool sunucu = online.SunucuVar;
            kur.SetEnabled(sunucu && !giriyor && Isim.Length > 0);
            katil.SetEnabled(sunucu && !giriyor && Isim.Length > 0 && Kod.Length == KodUzunlugu);
            don.SetEnabled(sunucu && !giriyor);
            durum.text = giriyor ? Metin.Al("online.baglaniyor") : "";
        }

        void OdaKur()
        {
            hata.text = "";
            online.OdaKur(Isim);
            Durum();
        }

        void Katil()
        {
            hata.text = "";
            online.Katil(Kod, Isim);
            Durum();
        }

        /// <summary>Saklı oda + isim + anahtar: oyundaysa yeri tutuluyor, aynı kimlikle döner.</summary>
        void Don()
        {
            hata.text = "";
            string i = online.Istemci.KayitliIsim;
            online.Katil(online.Istemci.KayitliOda, string.IsNullOrEmpty(i) ? Isim : i);
            Durum();
        }

        void AdresKaydet()
        {
            if (online.AdresKaydet(adres.value))
            {
                hata.text = "";
                sunucuKutusu.AddToClassList("gizli");
            }
            else hata.text = Metin.Al("online.sunucu_gecersiz");
        }

        public void Guncelle(float gecenSn) => Durum();

        public void ArkaPlanaGecti() { }

        public GeriEkrani GeriTuru => GeriEkrani.OnlineMenu;

        public void CikisSor() { }
    }
}
