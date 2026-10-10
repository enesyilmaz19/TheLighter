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

        // --- S3: tekrar oynatma ---

        /// <summary>Bu turun kaos kuralı (yoksa Yok). Tur başında duyurulur.</summary>
        public KaosKurali Kaos;
        /// <summary>Kızgın Çakmak açıksa çakmağın ısısı, kapalıysa null. Eşik hiçbir görünümde yok.</summary>
        public IsiSeviyesi? Isi;
        /// <summary>Sadece sana: gizli görevin. Görevler kapalıysa null.</summary>
        public Gorev Gorevin;
        /// <summary>IkiyeKatla ve Bedel aşamasında karar verecek kişi.</summary>
        public OyuncuId? KararVeren;
        /// <summary>Oylama aşamasında: sen oy verdin mi (sadece oy verebilenler için anlamlı).</summary>
        public bool OyVerdin;
        /// <summary>Oylama aşamasında oy verebilecekler (A ve B hariç herkes).</summary>
        public IReadOnlyList<OyuncuId> Oylayanlar = Array.Empty<OyuncuId>();
        public int OyKullanan;
        /// <summary>Ifsa/Gume aşamasında, Grup Kararı oylamasının sonucu. Oylama olmadıysa null.</summary>
        public OylamaSonucu SonOylama;
        /// <summary>Gume aşamasında: soru bedelle gizli kaldı.</summary>
        public bool BedelOdendi;
        /// <summary>Bu turda çekilen cezalar (yanma, kaybetme, bedel). Tur başında boşalır.</summary>
        public IReadOnlyList<CezaCekimi> TurCezalari = Array.Empty<CezaCekimi>();
    }

    public sealed class OylamaSonucu
    {
        public int Ifsa;
        public int Gume;
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

        // --- S3 ---
        public int OynadigiMiniOyun;
        public int CezaCekme;
        public int Yanma;           // Kızgın Çakmak
        public int BedelOdeme;
        public int BedelHakki;      // kalan
        public int KaosKazanma;     // kaos turunda kazandığı mini oyun
        /// <summary>Üstündeki ⚙️ kısıtlamalar (🤡, havuzdan sor...). Herkese açık.</summary>
        public IReadOnlyList<Ceza> Kisitlamalar = Array.Empty<Ceza>();
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
        /// <summary>İkiye Katla kabul edildi, bu ikinci el.</summary>
        public bool Katlandi;

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
        KaosBasladi,    // Kaos: bu turun kuralı
        Yandi,          // Kim: Kızgın Çakmak'ta yanan (ardından CezaCekildi gelir)
        CezaCekildi,    // Kim + Ceza
        IkiyeKatlandi,  // Kim: bir el daha isteyen
        BedelOdendi,    // Kim: bedel ödeyen B
        OyVerildi,      // Kim: oy veren. Oyun ne olduğu gizli
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
        public Ceza Ceza;
        public KaosKurali Kaos;
    }
}
