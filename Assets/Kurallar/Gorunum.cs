using System;
using System.Collections.Generic;

namespace Cakmak.Kurallar
{
    /// <summary>
    /// Bir oyuncunun görmesi gereken her şey. <see cref="Oyun.GorunumAl"/> her çağrıda yenisini üretir.
    /// "Sadece sana" alanları başka oyuncunun görünümünde boş gelir. Gizlilik burada sağlanır.
    /// </summary>
    public sealed class Gorunum
    {
        public OyuncuId Sen;
        public Asama Asama;
        public int Tur;
        /// <summary>0 = sınırsız.</summary>
        public int ToplamTur;
        public int KalanMs;
        public IReadOnlyList<OyuncuDurumu> Oyuncular = Array.Empty<OyuncuDurumu>();

        public OyuncuId? A;
        public OyuncuId? B;
        public OyuncuId? C;

        /// <summary>Sadece seçim yapacak kişiye dolu gelir: SoruSorma'da A'ya, CevapSecme'de B'ye.</summary>
        public IReadOnlyList<OyuncuId> Secilebilir = Array.Empty<OyuncuId>();

        /// <summary>Bu turun mini oyunu. Karışık ayarında o tur için seçilen oyun.</summary>
        public MiniOyunTuru MiniOyun;
        public bool BHamleYapti;
        public bool CHamleYapti;
        public int Beraberlik;
        /// <summary>Ifsa ve Gume aşamasında dolu: iki hamle ve sonuç.</summary>
        public MiniOyunSonucu SonMiniOyun;

        // --- Sadece sana ---

        /// <summary>Sadece B: CevapSecme ve MiniOyun aşamasında.</summary>
        public string Soru;
        /// <summary>Sadece B: soru yazılmadı, kulağına fısıldandı.</summary>
        public bool Fisilti;
        /// <summary>Curcuna, sadece A, SoruSorma: havuzdan sana düşen soru.</summary>
        public string AtananSoru;
        public bool DegistirmeHakki;
        /// <summary>Normal/Temiz, sadece A, SoruSorma: son çektiğin öneri.</summary>
        public string Oneri;
        /// <summary>Sadece B ve C, MiniOyun: kendi hamlen.</summary>
        public Hamle? Hamlen;

        // --- Herkese ---

        /// <summary>Ifsa aşamasında herkese açık soru. Fısıltıysa boş, A sesli söyler.</summary>
        public string IfsaSoru;
        public bool IfsaFisilti;
    }

    /// <summary>Herkesin görebildiği oyuncu bilgisi ve oyun sonu sayaçları.</summary>
    public sealed class OyuncuDurumu
    {
        public OyuncuId Id;
        public string Isim;
        /// <summary>Curcuna toplama: kaç soru yazdı (en fazla 2).</summary>
        public int HavuzaEkledigi;
        public int Gosterilme;      // C oldu
        public int SoruAlma;        // B oldu
        public int SoruSorma;       // A oldu ve sordu
        public int IfsaEttirme;     // C olarak kazandı
        public int Saklama;         // B olarak kazandı, soru güme gitti
        public int MiniOyunKazanma;
    }

    public sealed class MiniOyunSonucu
    {
        public MiniOyunTuru Tur;
        public Hamle? BHamle;
        public Hamle? CHamle;
        /// <summary>Zar atıldıysa (Zar oyunu ya da 3. beraberlik) atılan zarlar.</summary>
        public int BZar;
        public int CZar;
        public bool ZarlaKarar;
        public bool SureDoldu;
        public bool CKazandi;

        internal MiniOyunSonucu Kopya() => (MiniOyunSonucu)MemberwiseClone();
    }

    public enum OlayTuru
    {
        TurBasladi,
        SoruSoruldu,    // metin yok
        CevapSecildi,
        Berabere,
        Ifsa,           // soru metni burada, artık herkese açık
        Gume,           // metin yok
        SureDoldu,      // Kim: süresi dolan. Ardından TurBasladi gelir
        OyunBitti,
    }

    public sealed class Olay
    {
        public OlayTuru Tur;
        public OyuncuId? A;
        public OyuncuId? B;
        public OyuncuId? C;
        public OyuncuId? Kim;
        /// <summary>Sadece Ifsa olayında. Fısıltıysa boş.</summary>
        public string Soru;
    }
}
