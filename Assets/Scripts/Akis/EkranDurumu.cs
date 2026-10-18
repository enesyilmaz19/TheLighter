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

        // --- Yeni oyunlar (docs/Oyunlar.md) ---

        /// <summary>
        /// Elden ele, "Köstebek kim?" oylamasından önce: telefon ortada, herkes tartışır (istemcinin sayacı).
        /// Kural motorunun saati bu sırada ve oylama boyunca durur. Online'da yok (sunucunun süresi tartışmayı da kapsar).
        /// </summary>
        SuclamaTartisma,

        /// <summary>"Köstebek kim?": telefonu tutan kişi (online'da herkes kendi telefonunda) gizlice birini gösterir.</summary>
        Suclama,

        /// <summary>
        /// Rus Ruleti: düelloyu kaybeden tetiği çeker ya da devreder. Açık bilgi: elden elede perde yok, telefon kaybedende;
        /// online'da herkes izler, düğmeler sadece çekecek kişide. Süre dolunca tetik kendiliğinden çekilir.
        /// </summary>
        Tetik,

        /// <summary>
        /// Casus yakalandı, son şansı: listeden mekânı (kelimeyi) seçer, 30 sn. Açık bilgi (casus herkese açıldı): elden elede
        /// perde yok, telefon casusta; online'da herkes izler, liste sadece casusta seçilir. Saat işler.
        /// </summary>
        CasusTahmini,

        /// <summary>
        /// Çakmak Gecesi, linç: telefonu tutan (online'da herkes kendi telefonunda) bir canlıyı gösterir. Suikastçı burada
        /// suikast da yapabilir. Elden elede telefon oy verecekler arasında dolaşır, saat durur.
        /// </summary>
        Linc,

        /// <summary>
        /// Çakmak Gecesi, gece: telefonu tutan (online'da herkes) rolüne göre bir seçim yapar ya da pas der. Katil tarafında
        /// kurban oyu ve (varsa) yetenek. Elden elede telefon canlılar arasında dolaşır, saat durur.
        /// </summary>
        Gece,
    }

    /// <summary>Oyuna göre kişi sınırı: kurallardaki sabitler (Çakmak Gecesi 6–18, diğerleri 4–10). Kurulum ekranı buna bakar.</summary>
    public static class OyuncuSayisi
    {
        public static int EnAz(OyunTuru oyun) => oyun == OyunTuru.CakmakGecesi ? Oyun.GeceEnAzOyuncu : Oyun.EnAzOyuncu;

        public static int EnFazla(OyunTuru oyun) => oyun == OyunTuru.CakmakGecesi ? Oyun.GeceEnFazlaOyuncu : Oyun.EnFazlaOyuncu;

        /// <summary>Bütün oyunların en fazlası (kurulumda satır eklerken; seçili oyunun sınırı ayrıca denetlenir).</summary>
        public static int HepsininEnFazlasi => Oyun.GeceEnFazlaOyuncu > Oyun.EnFazlaOyuncu ? Oyun.GeceEnFazlaOyuncu : Oyun.EnFazlaOyuncu;
    }

    /// <summary>Kişiye özel bilgi: ekranın kimin görünümüyle çizileceği ve oyun başında rol perdesi kimlere gideceği için.</summary>
    public static class KisiyeOzel
    {
        /// <summary>
        /// Oyun başında telefon (online'da ekran) bu kişiye bir kez rol ya da görev göstermeli mi: gizli görev (Köstebek'te herkeste),
        /// rol (Köstebek, Casus, Çakmak Gecesi) ya da Casus'un sırrı. Hepsi kural motorunun görünümünden.
        /// </summary>
        public static bool RolPerdesiVar(Gorunum g) =>
            g != null && (g.Gorevin != null || g.Rolun != Rol.Yok || g.Sirrin != null);

        /// <summary>
        /// Ortadaki (herkesin baktığı) ekran için kişiye özel alanları boşaltır: görev, rol, suçlama hakkı, Casus'un sırrı ve rolü.
        /// Ekranı çizen görünüm köstebeğin ya da casusun olsa bile ortada bunlar durmaz.
        /// </summary>
        public static Gorunum Temizle(Gorunum g)
        {
            g.Gorevin = null;
            g.Rolun = Rol.Yok;
            g.SuclamaHakkin = false;
            g.Sirrin = null;
            g.MekanRolun = null;
            // Çakmak Gecesi: katiller, gece bakışları, haklar, Cellat'ın hedefi, bombalar, Kulak'ın sorusu.
            g.Katiller = System.Array.Empty<OyuncuRolu>();
            g.KatilSecimleri = System.Array.Empty<KatilSecimi>();
            g.Bulgularin = System.Array.Empty<Bulgu>();
            g.KalanHakkin = null;
            g.CellatHedefin = null;
            g.Bombalilar = System.Array.Empty<OyuncuId>();
            g.KulakSorusu = null;
            return g;
        }
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

        /// <summary>"Köstebek kim?" oylaması: bu kişi oy verecek.</summary>
        Suclama,

        /// <summary>Çakmak Gecesi: bu kişi linç oyu verecek.</summary>
        Linc,

        /// <summary>Çakmak Gecesi: bu kişi gece seçimini yapacak.</summary>
        Gece,
    }

    /// <summary>Çakmak Gecesi'nde az önce olanlar: sabah (gece ölenler), linç ve suikast. Bir sonraki ortadaki ekranda duyurulur.</summary>
    public sealed class GeceDuyurusu
    {
        public enum Turu { Sabah, Linc, Suikast }

        public Turu Tur;
        /// <summary>Sabah: gece ölenler (boşsa kimse ölmedi). Linç: elenen (yoksa boş). Suikast: ölen.</summary>
        public IReadOnlyList<OyuncuId> Olenler = System.Array.Empty<OyuncuId>();
    }

    /// <summary>Çakmak Gecesi: düello oynanmadan bitti. Sonuç ekranında herkes görür, kimin yaptığını değil.</summary>
    public enum DuelloNotu { Yok, HakemKarari, Sonduruldu }

    /// <summary>Takım Düellosu: bu turda alınan puan (düello puanı ya da ifşa bonusu). Sonuç ekranında duyurulur.</summary>
    public sealed class PuanAni
    {
        /// <summary>true: ifşa bonusu (Kim = A); false: düello puanı (Kim = düelloyu kazanan).</summary>
        public bool IfsaBonusu;
        public OyuncuId? Kim;
        public int Takim;
    }

    /// <summary>Rus Ruleti: az önce çekilen tetiğin sonucu. Bir sonraki ekranda duyurulur ("Tık…", "BAM!", "Elendi").</summary>
    public sealed class TetikSonucu
    {
        public OyuncuId Kim;
        public bool Patladi;
        public bool Elendi;
    }

    /// <summary>Biten "kim?" oylamasının sonucu: en çok oyu alan, eşitlikte null. Bir sonraki ekranda duyurulur.</summary>
    public sealed class SuclamaSonucu
    {
        public OyuncuId? Suclanan;
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

        /// <summary>
        /// Elden ele: az önce biten "kim?" oylaması (yoksa null). Perde kaldırılana kadar duyurulur:
        /// "Ali suçlandı… köstebek değildi" ya da "Kimse suçlanmadı".
        /// </summary>
        public SuclamaSonucu SuclamaSonucu;

        /// <summary>Elden ele, Rus Ruleti: az önce çekilen tetiğin sonucu (yoksa null). Perde kaldırılana kadar duyurulur.</summary>
        public TetikSonucu TetikSonucu;

        /// <summary>Elden ele, Çakmak Gecesi: perdede ve tartışmada duyurulacaklar (sabah, linç, suikast). Perde kalkınca silinir.</summary>
        public IReadOnlyList<GeceDuyurusu> GeceDuyurulari = new List<GeceDuyurusu>();

        /// <summary>Elden ele, Çakmak Gecesi: bu turun düellosu Hakem kararıyla ya da sönerek bitti (Ifsa/Gume ekranında).</summary>
        public DuelloNotu DuelloNotu;

        /// <summary>Elden ele, Takım Düellosu: bu turun puanları (Ifsa/Gume ekranında). Online'da olaylardan bildirim.</summary>
        public IReadOnlyList<PuanAni> PuanAnlari = new List<PuanAni>();

        /// <summary>Elden ele, Rus Ruleti: final başladı, ilk final perdesinde bir kez "FİNAL! Son 3 kişi".</summary>
        public bool FinalDuyurusu;

        /// <summary>SuclamaTartisma: oylamayı erken suçlamayla başlatan (aralıkla ya da son oylamada null).</summary>
        public OyuncuId? SuclamaBaslatan;

        /// <summary>SuclamaTartisma: tartışmanın kalan süresi (istemcinin sayacı).</summary>
        public int TartismaKalanMs;

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
            || Tur == EkranTuru.GorevGoster || Tur == EkranTuru.Oylama || CezaOnayBekliyor
            || Tur == EkranTuru.SuclamaTartisma || Tur == EkranTuru.Suclama || Tur == EkranTuru.Linc || Tur == EkranTuru.Gece
            || (Tur == EkranTuru.Perde && PerdeNedeni == PerdeNedeni.Suclama);
    }
}
