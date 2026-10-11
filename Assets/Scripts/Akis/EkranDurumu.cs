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

        /// <summary>Gösterilecek bir şey yok (geçiş anı). Normalde görünmez.</summary>
        Bekleniyor,

        // --- S3 ve Curcuna ---

        /// <summary>Curcuna başı: telefonu tutan kişi havuza 2 soru yazar. Elden elede saat durur.</summary>
        SoruToplama,

        /// <summary>Oyun başı: telefonu tutan kişi kendi gizli görevine bakar. Saat durur.</summary>
        GorevGoster,

        /// <summary>Grup Kararı: telefonu tutan kişi gizlice oy verir. Elden elede saat durur.</summary>
        Oylama,

        /// <summary>İkiye Katla ya da Bedel: karar veren 5 saniyede seçer. Telefon ortada, herkes görür.</summary>
        Karar,
    }

    public enum PerdeNedeni
    {
        /// <summary>Yeni tur: soru sorma sırası bu kişide.</summary>
        SoruSirasi,

        /// <summary>Bu kişiye soru soruldu, okuyacak ve çakmağı verecek.</summary>
        CevapSirasi,

        /// <summary>Curcuna: soru yazma sırası bu kişide.</summary>
        SoruYazma,

        /// <summary>Oyun başı: bu kişi gizli görevine bakacak.</summary>
        Gorev,

        /// <summary>Grup Kararı: bu kişi oy verecek.</summary>
        Oy,
    }

    /// <summary>İfşa olan bir soru. Herkese açık olduktan sonra kaydedilir. Güme giden soru hiç gelmez.</summary>
    public sealed class IfsaSatiri
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

        /// <summary>
        /// Perde: telefonu alacak kişi. SoruSorma: A. CevapSecme: B. SoruToplama: yazan.
        /// GorevGoster: görevine bakan. Oylama: oy veren. Karar: karar veren. Diğerlerinde null.
        /// </summary>
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
        /// Ekranda kullanılacak görünüm. SoruSorma'da A'nın, CevapSecme'de B'nin, GorevGoster'de görev sahibinin,
        /// Oylama'da oy verenin görünümü. Perdede ve herkesin baktığı ekranlarda gizli bilgisi olmayan bir
        /// seyircinin görünümü (görevi de temizlenmiş).
        /// </summary>
        public Gorunum Gorunum;

        /// <summary>
        /// Sadece MiniOyun ekranında: B'nin ve C'nin görünümleri. Telefon ikisinin arasında durduğu için
        /// gizli alanlar (soru, kendi hamlen, görev) temizlenmiş olarak gelir.
        /// </summary>
        public Gorunum BGorunumu;

        public Gorunum CGorunumu;

        /// <summary>MiniOyun: son berabere biten el. Yeni elde kimse oynamadıysa dolu.</summary>
        public BerabereEl SonBerabere;

        /// <summary>OyunSonu: oyun boyunca ifşa olan sorular (fısıldananlar dahil, metinsiz).</summary>
        public IReadOnlyList<IfsaSatiri> IfsaOlanlar;

        /// <summary>OyunSonu: kural motorunun özeti (unvanlar, görevler). Güme giden soru içinde yok.</summary>
        public OyunOzeti Ozet;

        /// <summary>
        /// Ifsa/Gume: bu turda çekilen ceza kartları henüz onaylanmadı. Kartlar okunup "Tamam" denene
        /// kadar saat durur (sesli cezayı yapmaya vakit kalsın).
        /// </summary>
        public bool CezaOnayBekliyor;

        /// <summary>Perdedeyse perdenin anahtarı. Aynı anahtarlı perde bir kez gösterilir.</summary>
        public string PerdeAnahtari;

        /// <summary>Değişirse ekran baştan çizilir. Aynı kalırsa sadece süre gibi alanlar güncellenir.</summary>
        public string Anahtar;

        /// <summary>
        /// Bu ekran açıkken kural motorunun saati ilerlemez. Perdeler (telefon el değiştiriyor), görev,
        /// Curcuna soru yazma ve Grup Kararı oylaması (telefon sırayla dolaşıyor, herkes masada) ve
        /// onaylanmamış ceza kartları.
        /// </summary>
        public bool SaatDurur =>
            Tur == EkranTuru.Perde || Tur == EkranTuru.MiniOyunPerdesi || Tur == EkranTuru.SoruToplama
            || Tur == EkranTuru.GorevGoster || Tur == EkranTuru.Oylama || CezaOnayBekliyor;
    }
}
