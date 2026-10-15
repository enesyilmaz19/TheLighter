using System.Collections.Generic;
using Cakmak.Kurallar;

namespace TheLighter.Ag
{
    /// <summary>Sunucudan gelen bir mesaj. Biçim: docs/Protokol.md, "Sunucu → istemci".</summary>
    public abstract class GelenMesaj { }

    /// <summary><c>hosgeldin</c>: odaya girdin. <see cref="Anahtar"/> geri dönüş için saklanır, günlüğe yazılmaz.</summary>
    public sealed class Hosgeldin : GelenMesaj
    {
        public OyuncuId Sen;
        public string Anahtar;
        public string Oda;

        // Anahtar ekrana ya da günlüğe kazara düşmesin.
        public override string ToString() => "hosgeldin " + Sen + " " + Oda;
    }

    /// <summary><c>hata</c>: isteğin yapılmadı. Kod sunucunun ya da kural motorunun.</summary>
    public sealed class HataMesaji : GelenMesaj
    {
        /// <summary>Sunucudan gelen ham kod: "ODA_YOK", "SIRA_SENDE_DEGIL"...</summary>
        public string Kod;
        /// <summary>Kod kural motorunun <see cref="Cakmak.Kurallar.Sonuc"/> değeriyse o, değilse null.</summary>
        public Sonuc? Sonuc;
        /// <summary>UYGUNSUZ (Temiz mod): bu odada kaçıncı ihlal. Yoksa 0.</summary>
        public int Ihlal;
        /// <summary>UYGUNSUZ: kaçıncı ihlalde atılırsın. Yoksa 0.</summary>
        public int Sinir;
        /// <summary>ATILDIN: "kurucu" ya da "temiz". Yasaklı geri girişte null.</summary>
        public string Neden;

        public override string ToString() => "hata " + Kod;
    }

    /// <summary><c>olay</c>: sadece animasyon ve ses için. Ekran her zaman durum'dan çizilir.</summary>
    public sealed class OlayMesaji : GelenMesaj
    {
        /// <summary>Kural motorunun olayı. Sunucunun kendi olayı (<see cref="Atildi"/>) için null.</summary>
        public Olay Olay;
        /// <summary>Sunucunun olayı: kurucu <see cref="Kim"/>'i attı.</summary>
        public bool Atildi;
        public OyuncuId? Kim;
        /// <summary>Sadece <see cref="Atildi"/>'de: "kurucu" ya da "temiz".</summary>
        public string Neden;

        public override string ToString() => "olay " + (Atildi ? "atildi" : Olay.Tur.ToString());
    }

    /// <summary>
    /// <c>sohbet</c>: genel ya da DM mesajı. Genel yazana da döner (yazan kendi mesajını bununla ekler).
    /// <see cref="Kime"/> null ise genel.
    /// </summary>
    public sealed class SohbetGelen : GelenMesaj
    {
        public string Id;
        public OyuncuId Kimden;
        public OyuncuId? Kime;
        /// <summary>Adlı kanal ("takim", "katil", "oluler"); genel ve DM'de null.</summary>
        public SohbetKanali? Kanal;
        public string Metin;

        // Mesaj metni günlüğe düşmesin.
        public override string ToString() => "sohbet " + Id;
    }

    /// <summary>Odadaki birinin kural motorunda olmayan bilgileri.</summary>
    public sealed class OdaOyuncusu
    {
        public OyuncuId Id;
        public string Isim;
        /// <summary>0–9, arayüzün renk listesinde sıra.</summary>
        public int Renk;
        public bool Bagli;
    }

    /// <summary>Lobideki ayarlar (kurucu değiştirir, herkes görür).</summary>
    public sealed class OdaAyarlari
    {
        /// <summary>Hangi oyun (docs/Oyunlar.md). Mod her oyunda ayrı seçilir.</summary>
        public OyunTuru Oyun = OyunTuru.Cakmak;
        /// <summary>Rus Ruleti: herkesin canı (1–3).</summary>
        public int Can = 1;
        /// <summary>Takım Düellosu: kurucunun dağılımı (oyuncu → 0 ya da 1). Boş: rastgele ve dengeli.</summary>
        public Dictionary<OyuncuId, int> Takimlar = new Dictionary<OyuncuId, int>();
        public OyunModu Mod = OyunModu.Normal;
        public MiniOyunTuru MiniOyun = MiniOyunTuru.Tkm;
        public int TurSayisi = 15;
        public int SoruSn = 60;
        public int CevapSn = 20;
        public int HamleSn = 10;
        public CezaSeviyesi Ceza = CezaSeviyesi.Kapali;
        public bool Gorevler;
        public bool Kaos;
        public List<string> Paketler = new List<string>();
    }

    public enum OdaYeri { Lobi, BeklemeOdasi, Oyun }

    /// <summary>
    /// <c>durum</c>: odanın son hali, bu oyuncuya özel. Üç biçimi var (<see cref="Yer"/>).
    /// Oyundayken kural bilgisi <see cref="Gorunum"/>'de: ekran parçaları elden ele ile aynı tipten beslenir.
    /// </summary>
    public sealed class DurumMesaji : GelenMesaj
    {
        public int Surum;
        public string Oda;
        public OdaYeri Yer;
        public OyuncuId? Kurucu;
        public List<OdaOyuncusu> Oyuncular = new List<OdaOyuncusu>();
        /// <summary>Bekleme odasındakiler (oyun sürerken katılanlar), sonraki oyunda oynayacak.</summary>
        public List<OdaOyuncusu> Bekleyenler = new List<OdaOyuncusu>();

        /// <summary>Sadece lobide.</summary>
        public OdaAyarlari Ayarlar;
        /// <summary>Lobide: kurucunun yüklediği grup paketinin soru sayısı (sorular gelmez). 0 = yok.</summary>
        public int GrupPaketi;

        /// <summary>Bekleme odasında: oyun bitti, kurucunun "tekrar oyna"sı bekleniyor.</summary>
        public bool OyunBitti;
        /// <summary>Bekleme odasında oyunun turu. Oyunda <see cref="Gorunum"/>.Tur.</summary>
        public int Tur;
        public int ToplamTur;

        /// <summary>Sadece oyunda.</summary>
        public Gorunum Gorunum;
        /// <summary>Sadece oyunSonu'nda. Sayaçlar <see cref="Gorunum"/>.Oyuncular'dan doldurulur.</summary>
        public OyunOzeti Ozet;
        /// <summary>Bağlı oyuncu 4'ün altında, oyun durdu: bu süre içinde kimse dönmezse oyun biter. Yoksa null.</summary>
        public int? BeklemeKalanMs;

        public OdaOyuncusu Bul(OyuncuId id)
        {
            foreach (var o in Oyuncular) if (o.Id == id) return o;
            foreach (var o in Bekleyenler) if (o.Id == id) return o;
            return null;
        }

        public override string ToString() => "durum " + Surum + " " + Yer + (Gorunum != null ? " " + Gorunum.Asama : "");
    }
}
