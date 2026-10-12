using System;
using System.Collections.Generic;
using System.Text;
using Cakmak.Kurallar;

namespace TheLighter.Ag
{
    public enum BaglantiDurumu
    {
        /// <summary>Odada değil (ana menü).</summary>
        Kapali,
        /// <summary>Oda kuruluyor / odaya giriliyor, hosgeldin bekleniyor.</summary>
        Giriyor,
        Odada,
        /// <summary>Bağlantı koptu, kendiliğinden tekrar deneniyor ("Bağlanıyor…").</summary>
        YenidenBaglaniyor,
    }

    /// <summary>Cihazda kalıcı küçük ayarlar (Unity'de PlayerPrefs). Testte sözlük.</summary>
    public interface IKalici
    {
        string Oku(string ad);
        void Yaz(string ad, string deger);
        void Sil(string ad);
    }

    /// <summary>
    /// Online oturum: bağlan, odaya gir, mesajları çöz, koparsa kendiliğinden geri dön.
    /// Kural hesaplamaz; ekran <see cref="SonDurum"/>'a bakar. UnityEngine'siz: her karede <see cref="Guncelle"/> çağrılır
    /// (gelen mesajlar ve bütün olaylar orada, ana iş parçacığında işlenir).
    /// Biçim ve kurallar: docs/Protokol.md ("Bağlantı", "Bağlantı koparsa", "Sınırlar").
    /// </summary>
    public sealed class OnlineIstemci
    {
        public const string KayitOda = "online.oda";
        public const string KayitIsim = "online.isim";
        public const string KayitAnahtar = "online.anahtar";

        /// <summary>Odadan çıkış nedenleri (metin anahtarı: "online.cikis.&lt;neden&gt;"; sunucu kodları "hata.&lt;KOD&gt;").</summary>
        public const string NedenBaglanamadi = "baglanamadi";
        public const string NedenAyrildin = "ayrildin";

        /// <summary>Yeniden bağlanma beklemesi: 1, 2, 4, 8 sn, sonra hep bu kadar.</summary>
        static readonly double[] Beklemeler = { 1, 2, 4, 8, 10 };

        /// <summary>Sunucu saniyede 10 mesaj (arka arkaya 20) kabul ediyor; biraz altında kalınır.</summary>
        const double SaniyedeMesaj = 8;
        const double EnFazlaHak = 10;
        /// <summary>Gönderilemeyip bekleyen mesaj sınırı. Dolarsa en eskisi atılır (zaten bayatlamıştır).</summary>
        const int EnFazlaSira = 40;

        readonly Func<ITasima> tasimaKur;
        readonly IKalici kalici;
        readonly Action<string> gunluk;
        readonly Queue<string> sira = new Queue<string>();

        Uri adres;
        ITasima tasima;
        string bekleyenIlkMesaj;   // bağlantı açılınca gidecek odaKur/katil
        double sonrakiDeneme;      // YenidenBaglaniyor: kalan sn
        int denemeSayisi;
        double hak = EnFazlaHak;

        public BaglantiDurumu Durum { get; private set; } = BaglantiDurumu.Kapali;
        public OyuncuId? Sen { get; private set; }
        public string Oda { get; private set; }
        public string Isim { get; private set; }
        /// <summary>Hiçbir yere yazılmaz, sadece <see cref="IKalici"/>'ye (bu oda için).</summary>
        string anahtar;

        /// <summary>Son kabul edilen durum. Odadan çıkınca null.</summary>
        public DurumMesaji SonDurum { get; private set; }

        public event Action<DurumMesaji> DurumGeldi;
        public event Action<OlayMesaji> OlayGeldi;
        /// <summary>Odadayken gelen hata (ESKI_TUR, SIRA_SENDE_DEGIL...): ekran kısa bir uyarı gösterir.</summary>
        public event Action<HataMesaji> HataGeldi;
        /// <summary>Odadan çıkıldı ya da girilemedi. Neden: sunucu kodu ("ODA_YOK", "ATILDIN", "ISIM_GECERSIZ"...) ya da Neden* sabitleri.</summary>
        public event Action<string> Ayrildi;

        /// <param name="tasimaKur">Her bağlantı denemesi için yeni taşıma (gerçekte <see cref="WebSocketTasima"/>).</param>
        /// <param name="gunluk">Hata ayıklama günlüğü. Anahtar hiçbir zaman buraya yazılmaz.</param>
        public OnlineIstemci(Func<ITasima> tasimaKur, IKalici kalici, Action<string> gunluk = null)
        {
            this.tasimaKur = tasimaKur ?? throw new ArgumentNullException(nameof(tasimaKur));
            this.kalici = kalici;
            this.gunluk = gunluk;
        }

        public bool Kurucuyum => Sen.HasValue && SonDurum != null && SonDurum.Kurucu == Sen;

        /// <summary>Uygulama kapanıp açıldıysa kaldığı oda (yoksa null). "Kaldığın odaya dön" için.</summary>
        public string KayitliOda => kalici?.Oku(KayitOda);
        public string KayitliIsim => kalici?.Oku(KayitIsim);

        // ------------------------------------------------------------ Odaya girme / çıkma

        /// <summary>Bağlantıyı açar, açılınca odaKur yollar. Sonuç: DurumGeldi ya da Ayrildi.</summary>
        public void OdaKur(Uri sunucu, string isim)
        {
            Basla(sunucu, isim, null, null);
            bekleyenIlkMesaj = Cevirici.OdaKur(isim);
        }

        /// <summary>Koda katılır. Bu oda için saklı anahtar varsa onunla (aynı kimlikle geri dönüş).</summary>
        public void Katil(Uri sunucu, string oda, string isim)
        {
            oda = (oda ?? "").Trim().ToUpperInvariant();
            string eskiAnahtar = kalici != null && kalici.Oku(KayitOda) == oda ? kalici.Oku(KayitAnahtar) : null;
            Basla(sunucu, isim, oda, eskiAnahtar);
            bekleyenIlkMesaj = Cevirici.Katil(oda, isim, eskiAnahtar);
        }

        void Basla(Uri sunucu, string isim, string oda, string eskiAnahtar)
        {
            TasimayiBirak();
            adres = sunucu ?? throw new ArgumentNullException(nameof(sunucu));
            Isim = isim;
            Oda = oda;
            anahtar = eskiAnahtar;
            Sen = null;
            SonDurum = null;
            sira.Clear();
            denemeSayisi = 0;
            Durum = BaglantiDurumu.Giriyor;
            TasimaAc();
        }

        /// <summary>Kendi isteğiyle çıkış: bağlantıyı kapatır, saklı anahtarı unutur.</summary>
        public void Ayril() => Cik(NedenAyrildin, unut: true);

        void Cik(string neden, bool unut)
        {
            TasimayiBirak();
            if (unut) Unut();
            var onceki = Durum;
            Durum = BaglantiDurumu.Kapali;
            Sen = null;
            SonDurum = null;
            sira.Clear();
            bekleyenIlkMesaj = null;
            Yaz("çıkış: " + neden);
            if (onceki != BaglantiDurumu.Kapali) Ayrildi?.Invoke(neden);
        }

        void Unut()
        {
            if (kalici == null) return;
            kalici.Sil(KayitOda);
            kalici.Sil(KayitAnahtar);
        }

        // ------------------------------------------------------------ Gönderme

        /// <summary>Odadayken mesaj yollar. Mesaj sınırı aşılmasın diye gerekirse sıraya koyar. Koptuysa false.</summary>
        public bool Gonder(string json)
        {
            if (Durum != BaglantiDurumu.Odada || json == null) return false;
            if (Encoding.UTF8.GetByteCount(json) > Cevirici.EnFazlaBayt) return false;
            if (sira.Count >= EnFazlaSira) sira.Dequeue();
            sira.Enqueue(json);
            SiraBosalt();
            return true;
        }

        /// <summary>Son durumun turu: her komut bununla gider (eskiyse sunucu ESKI_TUR der).</summary>
        public int Tur => SonDurum?.Gorunum?.Tur ?? 0;

        void SiraBosalt()
        {
            while (sira.Count > 0 && hak >= 1 && tasima != null)
            {
                if (!tasima.Gonder(sira.Peek())) return;
                sira.Dequeue();
                hak -= 1;
            }
        }

        // ------------------------------------------------------------ Her kare

        /// <summary>Her karede ana iş parçacığından çağrılır.</summary>
        public void Guncelle(TimeSpan gecen)
        {
            double sn = Math.Max(0, gecen.TotalSeconds);
            hak = Math.Min(EnFazlaHak, hak + sn * SaniyedeMesaj);

            // Bir karede çok mesaj gelebilir; olay işleyicisi Ayril() derse taşıma değişir, döngü durur.
            var bu = tasima;
            while (bu != null && bu == tasima && bu.Al(out var o)) Isle(o);

            if (Durum == BaglantiDurumu.YenidenBaglaniyor && tasima == null)
            {
                sonrakiDeneme -= sn;
                if (sonrakiDeneme <= 0) Dene();
            }
            if (Durum == BaglantiDurumu.Odada) SiraBosalt();
        }

        /// <summary>
        /// Uygulama arka plandan döndü (Android OnApplicationPause(false)). Bağlantı sessizce ölmüş olabilir:
        /// hemen yeniden bağlan. Aynı anahtarla dönülünce sunucu eski bağlantıyı kendisi kapatır.
        /// </summary>
        public void UygulamaDondu()
        {
            if (Durum != BaglantiDurumu.Odada && Durum != BaglantiDurumu.YenidenBaglaniyor) return;
            TasimayiBirak();
            Durum = BaglantiDurumu.YenidenBaglaniyor;
            denemeSayisi = 0;
            Dene();
        }

        void Isle(TasimaOlayi o)
        {
            switch (o.Tur)
            {
                case TasimaOlayTuru.Acildi:
                    if (bekleyenIlkMesaj != null) tasima.Gonder(bekleyenIlkMesaj);
                    break;
                case TasimaOlayTuru.Mesaj:
                    MesajIsle(o.Metin);
                    break;
                case TasimaOlayTuru.Kapandi:
                    Koptu(o.Metin);
                    break;
            }
        }

        void Koptu(string neden)
        {
            Yaz("bağlantı koptu: " + neden);
            TasimayiBirak();
            if (Durum == BaglantiDurumu.Giriyor)
            {
                // Hiç girilemedi: sunucu kapalı, adres yanlış ya da ağ yok.
                Cik(NedenBaglanamadi, unut: false);
                return;
            }
            if (Durum == BaglantiDurumu.Kapali) return;
            Durum = BaglantiDurumu.YenidenBaglaniyor;
            sonrakiDeneme = Beklemeler[Math.Min(denemeSayisi, Beklemeler.Length - 1)];
            denemeSayisi++;
        }

        void Dene()
        {
            // Oda henüz yoksa (odaKur COK_HIZLI aldı) oda kurmayı yeniden dene.
            // Varsa anahtar + aynı isim: oyundaysa yer tutuluyor; lobide yer kaybolduysa aynı isimle yeni giriş olur.
            bekleyenIlkMesaj = string.IsNullOrEmpty(Oda) ? Cevirici.OdaKur(Isim) : Cevirici.Katil(Oda, Isim, anahtar);
            TasimaAc();
        }

        void TasimaAc()
        {
            tasima = tasimaKur();
            tasima.Ac(adres);
        }

        void TasimayiBirak()
        {
            var t = tasima;
            tasima = null;
            t?.Kapat();
        }

        void MesajIsle(string json)
        {
            var m = Cevirici.Coz(json, Sen);
            if (m == null)
            {
                Yaz("tanınmayan mesaj atlandı");
                return;
            }

            switch (m)
            {
                case Hosgeldin h:
                    Sen = h.Sen;
                    Oda = h.Oda;
                    anahtar = h.Anahtar;
                    bekleyenIlkMesaj = null;
                    denemeSayisi = 0;
                    Durum = BaglantiDurumu.Odada;
                    if (kalici != null)
                    {
                        kalici.Yaz(KayitOda, h.Oda);
                        kalici.Yaz(KayitIsim, Isim ?? "");
                        kalici.Yaz(KayitAnahtar, h.Anahtar);
                    }
                    Yaz(h.ToString());
                    SiraBosalt();
                    break;

                case DurumMesaji d:
                    if (Durum != BaglantiDurumu.Odada) return;
                    // Eski sürüm (geç gelen) atılır. Sayaç odanın: geri dönen de kaldığı yerden devam eder.
                    if (SonDurum != null && d.Surum < SonDurum.Surum) return;
                    SonDurum = d;
                    DurumGeldi?.Invoke(d);
                    break;

                case OlayMesaji olay:
                    if (Durum != BaglantiDurumu.Odada) return;
                    OlayGeldi?.Invoke(olay);
                    break;

                case HataMesaji hata:
                    HataIsle(hata);
                    break;
            }
        }

        void HataIsle(HataMesaji h)
        {
            Yaz(h.ToString());
            switch (h.Kod)
            {
                case "ATILDIN":
                    // Atıldın: bir daha deneme, anahtarı unut.
                    Cik(h.Kod, unut: true);
                    return;
                case "ODA_YOK":
                    // Oda kapanmış (ya da kod yanlış). Saklı anahtar artık işe yaramaz.
                    Cik(h.Kod, unut: true);
                    return;
                case "COK_HIZLI":
                    // Mesaj hiç işlenmedi. Giriş sırasındaysa biraz sonra yeniden denenir.
                    if (Durum == BaglantiDurumu.Giriyor || Durum == BaglantiDurumu.YenidenBaglaniyor)
                    {
                        TasimayiBirak();
                        Durum = BaglantiDurumu.YenidenBaglaniyor;
                        sonrakiDeneme = Beklemeler[Math.Min(denemeSayisi, Beklemeler.Length - 1)];
                        denemeSayisi++;
                        return;
                    }
                    break;
            }

            if (Durum == BaglantiDurumu.Giriyor || Durum == BaglantiDurumu.YenidenBaglaniyor)
            {
                // Girilemedi (ISIM_GECERSIZ, ODA_DOLU, SUNUCU_DOLU...): bağlantı odasız kalmasın.
                Cik(h.Kod, unut: Durum == BaglantiDurumu.YenidenBaglaniyor);
                return;
            }
            HataGeldi?.Invoke(h);
        }

        void Yaz(string s) => gunluk?.Invoke("[Online] " + s);
    }
}
