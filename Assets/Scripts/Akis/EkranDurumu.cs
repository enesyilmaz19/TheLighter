using System.Collections.Generic;
using Cakmak.Kurallar;

namespace TheLighter.Akis
{
    public enum EkranTuru
    {
        /// <summary>"Telefonu Ayşe'ye ver." Gizli bilgi yok, saat durur.</summary>
        Perde,

        /// <summary>"Telefonu ikinizin arasına koyun." Saat durur.</summary>
        MiniOyunPerdesi,

        SoruSorma,
        CevapSecme,
        MiniOyun,
        Ifsa,
        Gume,
        OyunSonu,

        /// <summary>Elden ele ekranı henüz olmayan aşama (Curcuna soru toplama).</summary>
        Bekleniyor,
    }

    public enum PerdeNedeni
    {
        /// <summary>Yeni tur: soru sorma sırası bu kişide.</summary>
        SoruSirasi,

        /// <summary>Bu kişiye soru soruldu, okuyacak ve çakmağı verecek.</summary>
        CevapSirasi,
    }

    /// <summary>İfşa olan bir soru. Herkese açık olduktan sonra kaydedilir. Güme giden soru hiç gelmez.</summary>
    public sealed class IfsaKaydi
    {
        /// <summary>Fısıldanan soruda null.</summary>
        public string Soru;

        public OyuncuId? Soran;
        public OyuncuId? Cevap;
    }

    /// <summary>Berabere biten elin hamleleri. İkisi de bu cihazda girildiği için istemci bilir.</summary>
    public sealed class BerabereEl
    {
        public MiniOyunTuru Tur;
        public Hamle BHamle;
        public Hamle CHamle;
    }

    /// <summary>Elden ele modunda şu an ekranda ne olması gerektiği.</summary>
    public sealed class EkranDurumu
    {
        public EkranTuru Tur;

        /// <summary>Perde: telefonu alacak kişi. SoruSorma: A. CevapSecme: B. Diğerlerinde null.</summary>
        public OyuncuId? Kime;

        public PerdeNedeni PerdeNedeni;

        /// <summary>
        /// Perdede: az önce süresi dolan kişi (yoksa null). Süre dolunca kural motoru yeni tura geçer,
        /// perde bunu söyler: "Ali'nin süresi doldu" ya da "Ayşe seçmedi, soru güme gitti".
        /// </summary>
        public OyuncuId? SuresiDolan;

        /// <summary>Süresi dolan B miydi (cevap seçemedi, soru güme gitti)?</summary>
        public bool SuresiDolanBydi;

        /// <summary>
        /// Ekranda kullanılacak görünüm. SoruSorma'da A'nın, CevapSecme'de B'nin görünümü.
        /// Perdede ve herkesin baktığı ekranlarda gizli bilgisi olmayan bir seyircinin görünümü.
        /// </summary>
        public Gorunum Gorunum;

        /// <summary>
        /// Sadece MiniOyun ekranında: B'nin ve C'nin görünümleri. Telefon ikisinin arasında durduğu için
        /// gizli alanlar (soru, kendi hamlen) temizlenmiş olarak gelir.
        /// </summary>
        public Gorunum BGorunumu;

        public Gorunum CGorunumu;

        /// <summary>MiniOyun: son berabere biten el. Yeni elde kimse oynamadıysa dolu.</summary>
        public BerabereEl SonBerabere;

        /// <summary>OyunSonu: oyun boyunca ifşa olan sorular.</summary>
        public IReadOnlyList<IfsaKaydi> IfsaOlanlar;

        /// <summary>Değişirse ekran baştan çizilir. Aynı kalırsa sadece süre gibi alanlar güncellenir.</summary>
        public string Anahtar;

        /// <summary>Bu ekran açıkken kural motorunun saati ilerlemez.</summary>
        public bool SaatDurur => Tur == EkranTuru.Perde || Tur == EkranTuru.MiniOyunPerdesi;
    }
}
