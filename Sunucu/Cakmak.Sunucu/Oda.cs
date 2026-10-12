using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using Cakmak.Kurallar;

namespace Cakmak.Sunucu
{
    /// <summary>
    /// Bir oda: lobi, oyuncular ve kural motoru. Her işlem tek kilit altında yapılır.
    /// Mesajlar kilit altında kuyruğa konur, bağlantının kendi döngüsü yollar. Kural kodu hiç kopyalanmaz.
    /// </summary>
    sealed class Oda
    {
        public const int EnFazlaOyuncu = Oyun.EnFazlaOyuncu;
        const int IsimEnFazla = 20;

        sealed class OdaOyuncusu
        {
            public OyuncuId Id;
            public string Isim;
            public string Anahtar;
            public Baglanti Baglanti;   // null = bağlı değil
            /// <summary>Oyunda bağlantısız geçen süre (oyun dururken işlemez). Sınırı geçince oyundan çıkar.</summary>
            public TimeSpan Kopuk;
            public bool Cikti;
            /// <summary>Oyun sürerken geldi: bekleme odasında, oyunda değil. Kurucu "tekrar oyna" deyince oyuncu olur.</summary>
            public bool Bekliyor;
        }

        public readonly string Kod;
        readonly object kilit = new object();
        readonly List<OdaOyuncusu> oyuncular = new List<OdaOyuncusu>();
        readonly IReadOnlyDictionary<string, IReadOnlyList<string>> paketler;
        readonly IReadOnlyList<string> sesliCezalar;
        readonly Ayarlar ayarlar = new Ayarlar();
        /// <summary>Kurucunun seçtiği paketler. Başta hepsi.</summary>
        List<string> seciliPaketler;
        OyuncuId kurucu;
        Oyun oyun;
        int surum;
        bool degisti;
        bool oyunBitti;
        readonly Sinirlar sinirlar;
        TimeSpan lobideGecen;
        /// <summary>Bağlı oyuncu 4'ün altında: oyun duruyor, bu kadar süredir.</summary>
        TimeSpan azOyuncuBekleme;
        /// <summary>Oyunda bağlı kimse yok, bu kadar süredir.</summary>
        TimeSpan kimseYok;
        /// <summary>Atılanlar bu odaya ne anahtarıyla ne aynı isimle dönebilir (Ana Plan 6.4).</summary>
        readonly HashSet<string> yasakliAnahtar = new HashSet<string>();
        readonly HashSet<string> yasakliIsim = new HashSet<string>();

        /// <summary>Oda silindi. Bu bayraktan sonra kimse katılamaz.</summary>
        public bool Kapandi { get; private set; }

        public Oda(string kod, IReadOnlyDictionary<string, IReadOnlyList<string>> paketler, IReadOnlyList<string> sesliCezalar,
            Sinirlar sinirlar)
        {
            Kod = kod;
            this.paketler = paketler;
            this.sesliCezalar = sesliCezalar;
            this.sinirlar = sinirlar;
            seciliPaketler = new List<string>(paketler.Keys);
        }

        /// <summary>Temizlendikten sonra (görünmez karakterler, boşluklar: <see cref="TemizMetin"/>) 1–20 karakter.</summary>
        public static bool IsimGecerli(string isim)
        {
            var t = TemizMetin.Temizle(isim);
            return t.Length > 0 && t.Length <= IsimEnFazla;
        }

        /// <summary>Yeniden bağlanma anahtarı, harf harf erken çıkmadan karşılaştırılır: süre farkından tahmin edilemesin.</summary>
        static bool AnahtarAyni(string kayitli, string gelen) =>
            CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(kayitli), Encoding.UTF8.GetBytes(gelen));

        // ------------------------------------------------------------ Katılma ve ayrılma

        public void Katil(Baglanti b, string isim, string anahtar)
        {
            lock (kilit)
            {
                if (Kapandi) { b.Yolla(Protokol.Hata("ODA_YOK")); return; }
                if (anahtar != null && yasakliAnahtar.Contains(anahtar)) { b.Yolla(Protokol.Hata("ATILDIN")); return; }

                var eski = anahtar == null ? null : oyuncular.Find(o => AnahtarAyni(o.Anahtar, anahtar));
                if (eski != null)
                {
                    // Aynı kimlikle geri dönüş. Eski bağlantı hâlâ açıksa kesilir: bir kimlik iki cihazda açık kalmaz.
                    if (eski.Baglanti != null && eski.Baglanti != b)
                    {
                        eski.Baglanti.Oda = null;
                        eski.Baglanti.Kes();
                    }
                    eski.Baglanti = b;
                    b.Oda = this;
                    b.Kim = eski.Id;
                    // Oyundan çıkmış oyuncu geri girmez, izler (kural motoru onu bir daha seçmez). Bekleyen yine bekler.
                    if (oyun != null && !eski.Cikti && !eski.Bekliyor)
                    {
                        eski.Kopuk = TimeSpan.Zero;
                        oyun.OyuncuDondu(eski.Id);
                    }
                    b.Yolla(Protokol.Hosgeldin(eski.Id, eski.Anahtar, Kod));
                    HerkeseDurum();
                    return;
                }

                // Oda kapasitesi bekleyenlerle birlikte: sonraki oyunda hepsi oynayabilsin.
                if (oyuncular.Count >= EnFazlaOyuncu) { b.Yolla(Protokol.Hata("ODA_DOLU")); return; }

                var temiz = TemizMetin.Temizle(isim);
                var isimAnahtari = Oyuncu.IsimAnahtari(temiz);
                if (IsimGecerli(temiz) && yasakliIsim.Contains(isimAnahtari)) { b.Yolla(Protokol.Hata("ATILDIN")); return; }
                if (!IsimGecerli(temiz) || oyuncular.Exists(o => Oyuncu.IsimAnahtari(o.Isim) == isimAnahtari))
                {
                    b.Yolla(Protokol.Hata("ISIM_GECERSIZ"));
                    return;
                }

                var yeni = new OdaOyuncusu
                {
                    Id = BosKimlik(),
                    Isim = temiz,
                    Anahtar = RandomNumberGenerator.GetHexString(32, lowercase: true),
                    Baglanti = b,
                    // Oyun sürüyorsa bekleme odasına (Ana Plan 1: "sonraki oyunda oynar").
                    Bekliyor = oyun != null,
                };
                oyuncular.Add(yeni);
                if (oyuncular.Count == 1) kurucu = yeni.Id;
                b.Oda = this;
                b.Kim = yeni.Id;
                b.Yolla(Protokol.Hosgeldin(yeni.Id, yeni.Anahtar, Kod));
                HerkeseDurum();
            }
        }

        /// <summary>Bağlantı kapandı. Lobi boşaldıysa true: oda silinmeli. Oyundaki oda Saat'te (Ilerle) silinir.</summary>
        public bool Ayrildi(Baglanti b)
        {
            lock (kilit)
            {
                var o = oyuncular.Find(x => x.Baglanti == b);
                if (o == null) return false;

                if (oyun == null || o.Bekliyor)
                {
                    // Lobide ya da bekleme odasında kopan yerini kaybeder.
                    oyuncular.Remove(o);
                    if (oyun == null && oyuncular.Count == 0)
                    {
                        Kapandi = true;
                        return true;
                    }
                }
                else
                {
                    // Ana Plan 6.1: yeri 60 sn tutulur (Ilerle sayar), süreler işler, sırası gelirse atlanır.
                    o.Baglanti = null;
                    o.Kopuk = TimeSpan.Zero;
                    oyun.OyuncuKoptu(o.Id);
                }
                if (o.Id == kurucu) KurucuyuDevret();
                HerkeseDurum();
                return false;
            }
        }

        /// <summary>
        /// Kurucu koptu: kuruculuk ilk bağlı üyeye (önce gelen önce) geçer, geri dönünce geri almaz.
        /// Yoksa sınırsız oyunu kimse bitiremez. Bağlı kimse yoksa değişmez.
        /// </summary>
        void KurucuyuDevret()
        {
            var yeni = oyuncular.Find(x => x.Baglanti != null);
            if (yeni != null) kurucu = yeni.Id;
        }

        /// <summary>Odayı zorla kapatır, herkesin bağlantısını keser. Sadece oda bozulduğunda (beklenmeyen hata).</summary>
        public void Kapat()
        {
            lock (kilit)
            {
                Kapandi = true;
                foreach (var o in oyuncular) o.Baglanti?.Kes();
            }
        }

        // ------------------------------------------------------------ Kurucu işleri

        public void AyarlariDegistir(Baglanti b, JsonObject m)
        {
            lock (kilit)
            {
                if (!KurucuMu(b)) return;
                if (oyun != null) { b.Yolla(Protokol.Hata("OYUN_BASLADI")); return; }

                // Önce hepsini oku ve kontrol et, sonra uygula: yarım değişiklik olmasın.
                var mod = Protokol.Metin(m, "mod", false);
                var miniOyun = Protokol.Metin(m, "miniOyun", false);
                var turSayisi = Protokol.Sayi(m, "turSayisi", false);
                var soruSn = Protokol.Sayi(m, "soruSn", false);
                var cevapSn = Protokol.Sayi(m, "cevapSn", false);
                var hamleSn = Protokol.Sayi(m, "hamleSn", false);
                var ceza = Protokol.Metin(m, "ceza", false);
                var gorevler = Protokol.Mantik(m, "gorevler", false);
                var kaos = Protokol.Mantik(m, "kaos", false);
                var secilen = Protokol.MetinListesi(m, "paketler");
                var yeniMod = mod == null ? ayarlar.Mod : Protokol.Coz<OyunModu>(mod);
                var yeniMiniOyun = miniOyun == null ? ayarlar.MiniOyun : Protokol.Coz<MiniOyunTuru>(miniOyun);
                var yeniCeza = ceza == null ? ayarlar.CezaSeviyesi : Protokol.Coz<CezaSeviyesi>(ceza);

                // Reaksiyon sadece elden ele: online'da süre istemciden gelir ve telefonların dokunma gecikmesi farklıdır.
                // Temiz modda en fazla Hafif ceza (Ana Plan 8.1).
                if (yeniMiniOyun == MiniOyunTuru.Reaksiyon
                    || (yeniMod == OyunModu.Temiz && yeniCeza == CezaSeviyesi.Cesur)
                    || (secilen != null && !PaketlerGecerli(secilen))
                    || !Aralikta(turSayisi, 0, 100) || !Aralikta(soruSn, 5, 300) || !Aralikta(cevapSn, 5, 300) || !Aralikta(hamleSn, 5, 300))
                {
                    b.Yolla(Protokol.Hata("GECERSIZ_GIRDI"));
                    return;
                }

                ayarlar.Mod = yeniMod;
                ayarlar.MiniOyun = yeniMiniOyun;
                ayarlar.CezaSeviyesi = yeniCeza;
                if (turSayisi.HasValue) ayarlar.TurSayisi = turSayisi.Value;
                if (soruSn.HasValue) ayarlar.SoruSuresiSn = soruSn.Value;
                if (cevapSn.HasValue) ayarlar.CevapSuresiSn = cevapSn.Value;
                if (hamleSn.HasValue) ayarlar.HamleSuresiSn = hamleSn.Value;
                if (gorevler.HasValue) ayarlar.GizliGorevler = gorevler.Value;
                if (kaos.HasValue) ayarlar.KaosTurlari = kaos.Value;
                if (secilen != null) seciliPaketler = secilen;
                HerkeseDurum();
            }
        }

        public void Baslat(Baglanti b)
        {
            lock (kilit)
            {
                if (!KurucuMu(b)) return;
                if (oyun != null) { b.Yolla(Protokol.Hata("OYUN_BASLADI")); return; }

                var liste = new List<Oyuncu>();
                foreach (var o in oyuncular) liste.Add(new Oyuncu(o.Id, o.Isim));

                var sorular = new List<string>();
                foreach (var p in seciliPaketler) sorular.AddRange(paketler[p]);

                var kopya = new Ayarlar
                {
                    Mod = ayarlar.Mod,
                    MiniOyun = ayarlar.MiniOyun,
                    TurSayisi = ayarlar.TurSayisi,
                    SoruSuresiSn = ayarlar.SoruSuresiSn,
                    CevapSuresiSn = ayarlar.CevapSuresiSn,
                    HamleSuresiSn = ayarlar.HamleSuresiSn,
                    OneriSorulari = sorular,
                    CezaSeviyesi = ayarlar.CezaSeviyesi,
                    GizliGorevler = ayarlar.GizliGorevler,
                    KaosTurlari = ayarlar.KaosTurlari,
                    SesliCezalar = sesliCezalar,
                };
                var sonuc = Oyun.Kur(kopya, liste, new GuvenliRastgele(), out var yeni);
                if (sonuc != Sonuc.Tamam) { b.Yolla(Protokol.Hata(Protokol.HataKodu(sonuc))); return; }

                oyun = yeni;
                oyun.OlayOldu += OlayGeldi;
                HerkeseDurum();
            }
        }

        /// <summary>
        /// Kurucu birini atar (lobide, oyunda ya da bekleme odasında). Atılan "ATILDIN" alır, bağlantısı kapanır,
        /// bu odaya ne anahtarıyla ne aynı isimle dönebilir. Oyundaysa oyundan çıkar, sayaçları özette kalır.
        /// ponytail: hesap yok, yeni bir isimle girebilir (Ana Plan 6.4). Rapor + engelleme S6'da.
        /// </summary>
        public void At(Baglanti b, JsonObject m)
        {
            lock (kilit)
            {
                if (!KurucuMu(b)) return;
                var kim = Protokol.IdCoz(Protokol.Metin(m, "kim"));
                var o = oyuncular.Find(x => x.Id == kim);
                if (o == null || o.Id == kurucu) { b.Yolla(Protokol.Hata("GECERSIZ_GIRDI")); return; }

                yasakliAnahtar.Add(o.Anahtar);
                yasakliIsim.Add(Oyuncu.IsimAnahtari(o.Isim));
                if (o.Baglanti != null)
                {
                    o.Baglanti.Yolla(Protokol.Hata("ATILDIN"));
                    o.Baglanti.Oda = null;
                    o.Baglanti.Bitir(); // son mesaj gidince bağlantı kapanır
                    o.Baglanti = null;
                }
                var olay = Protokol.AtildiOlayi(o.Id);
                foreach (var x in oyuncular) x.Baglanti?.Yolla(olay);

                if (oyun == null || o.Bekliyor) oyuncular.Remove(o);
                else if (!o.Cikti)
                {
                    o.Cikti = true;
                    oyun.OyuncuCikti(o.Id);
                }
                HerkeseDurum();
            }
        }

        /// <summary>
        /// Oyun bitti, kurucu aynı odada yeni oyun ister: oda lobiye döner. Bağlı olmayanlar düşer, bekleme odasındakiler
        /// oyuncu olur, ayarlar ve atılanların yasağı kalır.
        /// </summary>
        public void TekrarOyna(Baglanti b)
        {
            lock (kilit)
            {
                if (!KurucuMu(b)) return;
                if (oyun == null) { b.Yolla(Protokol.Hata("OYUN_YOK")); return; }
                if (!oyunBitti) { b.Yolla(Protokol.Hata("YANLIS_ASAMA")); return; }

                oyuncular.RemoveAll(o => o.Baglanti == null);
                foreach (var o in oyuncular)
                {
                    o.Bekliyor = false;
                    o.Cikti = false;
                    o.Kopuk = TimeSpan.Zero;
                }
                oyun.OlayOldu -= OlayGeldi;
                oyun = null;
                oyunBitti = false;
                degisti = false;
                azOyuncuBekleme = TimeSpan.Zero;
                kimseYok = TimeSpan.Zero;
                lobideGecen = TimeSpan.Zero;
                HerkeseDurum();
            }
        }

        public void Bitir(Baglanti b)
        {
            lock (kilit)
            {
                if (!KurucuMu(b)) return;
                if (oyun == null) { b.Yolla(Protokol.Hata("OYUN_YOK")); return; }
                Uygula(b, oyun.OyunuBitir());
            }
        }

        // ------------------------------------------------------------ Oyun komutları

        public void Komut(Baglanti b, JsonObject m)
        {
            lock (kilit)
            {
                if (!UyeMi(b)) return;
                if (oyun == null) { b.Yolla(Protokol.Hata("OYUN_YOK")); return; }
                if (oyuncular.Find(o => o.Baglanti == b).Bekliyor) { b.Yolla(Protokol.Hata("OYUNDA_DEGILSIN")); return; }
                Buradayim(b.Kim); // komut yollayan başında: AFK ise kalkar

                var tur = Protokol.Sayi(m, "tur").Value;
                var komut = Protokol.Metin(m, "komut");
                if (tur != oyun.GorunumAl(b.Kim).Tur) { b.Yolla(Protokol.Hata("ESKI_TUR")); return; }

                Sonuc sonuc;
                switch (komut)
                {
                    case "havuzaEkle":
                        sonuc = oyun.HavuzaSoruEkle(b.Kim, Protokol.Metin(m, "metin"));
                        break;
                    case "oneriCek":
                        sonuc = oyun.OneriCek(b.Kim);
                        break;
                    case "soruyuDegistir":
                        sonuc = oyun.SoruyuDegistir(b.Kim);
                        break;
                    case "soruSor":
                        var hedef = Protokol.IdCoz(Protokol.Metin(m, "hedef"));
                        var metin = Protokol.Metin(m, "metin", false);
                        // Online'da fısıltı yok: metinsiz soru sadece Curcuna'da (soru atanır).
                        sonuc = metin == null && ayarlar.Mod != OyunModu.Curcuna
                            ? Sonuc.GecersizGirdi
                            : oyun.SoruSor(b.Kim, hedef, metin ?? "");
                        break;
                    case "cevapSec":
                        sonuc = oyun.CevapSec(b.Kim, Protokol.IdCoz(Protokol.Metin(m, "hedef")));
                        break;
                    case "hamle":
                        sonuc = oyun.MiniOyunHamlesi(b.Kim, Protokol.HamleCoz(m["hamle"] as JsonObject));
                        break;
                    case "oy":
                        sonuc = oyun.Oy(b.Kim, Protokol.Mantik(m, "ifsa").Value);
                        break;
                    case "ikiyeKatla":
                        sonuc = oyun.IkiyeKatla(b.Kim);
                        break;
                    case "bedelOde":
                        sonuc = oyun.BedelOde(b.Kim);
                        break;
                    case "karariGec":
                        sonuc = oyun.KarariGec(b.Kim);
                        break;
                    default:
                        throw new GecersizMesaj();
                }
                Uygula(b, sonuc);
            }
        }

        /// <summary>"Buradayım" (ekrana dokundu): AFK etiketi kalkar. Lobide bir şey yapmaz.</summary>
        public void Buradayim(Baglanti b)
        {
            lock (kilit)
            {
                if (!UyeMi(b) || oyun == null) return;
                Buradayim(b.Kim);
            }
        }

        void Buradayim(OyuncuId kim)
        {
            oyun.Buradayim(kim);
            if (degisti) HerkeseDurum(); // sadece AFK gerçekten kalktıysa (AfkBitti olayı)
        }

        // ------------------------------------------------------------ Saat

        /// <summary>Saat her ~100 ms'de çağırır. Bir aşama değiştiyse herkese yeni durum gider.</summary>
        /// <returns>true: oda silinmeli (lobi başlatılmadan çok bekledi ya da oyunda bağlı kimse kalmadı).</returns>
        public bool Ilerle(TimeSpan gecen)
        {
            lock (kilit)
            {
                if (Kapandi) return false;
                if (oyun == null)
                {
                    // Bağlı ama hiç başlatılmayan oda sonsuza kadar yer tutmasın.
                    lobideGecen += gecen;
                    return lobideGecen >= sinirlar.LobiSuresi;
                }

                // Herkes koptuysa oda hemen silinmez: dönen olursa oyun sürer. Kimse dönmezse silinir.
                if (oyuncular.Exists(o => o.Baglanti != null)) kimseYok = TimeSpan.Zero;
                else if ((kimseYok += gecen) >= sinirlar.AzOyuncuSuresi)
                {
                    Kapandi = true;
                    return true;
                }

                if (!oyunBitti) OyunuIlerlet(gecen);
                if (degisti) HerkeseDurum();
                return false;
            }
        }

        /// <summary>
        /// Ana Plan 6.1. Bağlı oyuncu 4'ün altındaysa oyun durur (süreler ve kopukların 60 sn'si işlemez), 2 dk bekler,
        /// kimse dönmezse biter. Yoksa kopuklar sayılır, 60 sn'de dönmeyen oyundan çıkar.
        /// </summary>
        void OyunuIlerlet(TimeSpan gecen)
        {
            int bagli = 0;
            foreach (var o in oyuncular) if (o.Baglanti != null && !o.Cikti && !o.Bekliyor) bagli++;
            if (bagli < Oyun.EnAzOyuncu)
            {
                if (azOyuncuBekleme == TimeSpan.Zero) degisti = true; // durum'a "bekleme" girsin
                azOyuncuBekleme += gecen;
                if (azOyuncuBekleme >= sinirlar.AzOyuncuSuresi) oyun.OyunuBitir();
                return;
            }
            if (azOyuncuBekleme > TimeSpan.Zero)
            {
                azOyuncuBekleme = TimeSpan.Zero;
                degisti = true;
            }

            foreach (var o in oyuncular)
            {
                if (o.Baglanti != null || o.Cikti || o.Bekliyor) continue;
                o.Kopuk += gecen;
                if (o.Kopuk < sinirlar.KopukSuresi) continue;
                o.Cikti = true;
                oyun.OyuncuCikti(o.Id);
            }
            oyun.Ilerle(gecen);
        }

        // ------------------------------------------------------------ İç

        void Uygula(Baglanti b, Sonuc sonuc)
        {
            if (sonuc != Sonuc.Tamam) b.Yolla(Protokol.Hata(Protokol.HataKodu(sonuc)));
            else HerkeseDurum();
        }

        void OlayGeldi(Olay olay)
        {
            // Kural motoru bir şey değiştirdi. Olay herkese gider, ardından yeni durum.
            degisti = true;
            if (olay.Tur == OlayTuru.OyunBitti) oyunBitti = true;
            var json = Protokol.OlayYaz(olay);
            foreach (var o in oyuncular) o.Baglanti?.Yolla(json);
        }

        /// <summary>Bu bağlantı hâlâ bu oyuncunun güncel bağlantısı mı? Başka cihazdan dönen eskisinin yerini alır.</summary>
        bool UyeMi(Baglanti b)
        {
            if (!Kapandi && oyuncular.Exists(o => o.Baglanti == b)) return true;
            b.Yolla(Protokol.Hata("ODADA_DEGILSIN"));
            return false;
        }

        bool KurucuMu(Baglanti b)
        {
            if (!UyeMi(b)) return false;
            if (b.Kim == kurucu) return true;
            b.Yolla(Protokol.Hata("KURUCU_DEGILSIN"));
            return false;
        }

        void HerkeseDurum()
        {
            degisti = false;
            surum++;
            if (oyun == null)
            {
                var ekler = new List<Protokol.OyuncuEki>();
                foreach (var o in oyuncular) ekler.Add(Ek(o));
                var json = Protokol.LobiDurumu(surum, Kod, kurucu, ekler, ayarlar, seciliPaketler);
                foreach (var o in oyuncular) o.Baglanti?.Yolla(json);
                return;
            }

            // Herkes kendi görünümünü alır. Gizlilik kural motorunda: soru sadece B'nin görünümünde.
            // Özet sadece oyun bitince: ortasında görevleri ve Soran Gizli'nin A'sını verir.
            // Bekleme odasındakiler oyunda değil: sadece herkese açık bilgiyi (oyuncular, tur) görür.
            var bekleyenler = new List<Protokol.OyuncuEki>();
            foreach (var o in oyuncular) if (o.Bekliyor) bekleyenler.Add(Ek(o));
            OyunOzeti ozet = null;
            string bekleyenDurumu = null;
            foreach (var o in oyuncular)
            {
                if (o.Baglanti == null) continue;
                if (o.Bekliyor)
                {
                    bekleyenDurumu ??= BeklemeOdasiDurumu(bekleyenler);
                    o.Baglanti.Yolla(bekleyenDurumu);
                    continue;
                }
                var g = oyun.GorunumAl(o.Id);
                if (g.Asama == Asama.OyunSonu) ozet ??= oyun.Ozet();
                o.Baglanti.Yolla(Protokol.OyunDurumu(surum, Kod, kurucu, EkBul, g, ozet, BeklemeKalanMs(), bekleyenler));
            }
        }

        string BeklemeOdasiDurumu(List<Protokol.OyuncuEki> bekleyenler)
        {
            var oynayanlar = new List<Protokol.OyuncuEki>();
            foreach (var o in oyuncular) if (!o.Bekliyor) oynayanlar.Add(Ek(o));
            // Tur bilgisi herkese açık: oyundaki herhangi birinin görünümünden sadece onu al.
            var g = oyun.GorunumAl(oyuncular.Find(o => !o.Bekliyor).Id);
            return Protokol.BeklemeOdasiDurumu(surum, Kod, kurucu, oynayanlar, bekleyenler, g.Tur, g.ToplamTur, oyunBitti);
        }

        /// <summary>Oyun az oyuncu yüzünden duruyorsa bitmesine kalan süre, durmuyorsa null.</summary>
        int? BeklemeKalanMs() => azOyuncuBekleme > TimeSpan.Zero && !oyunBitti
            ? (int)Math.Ceiling(Math.Max(0, (sinirlar.AzOyuncuSuresi - azOyuncuBekleme).TotalMilliseconds))
            : (int?)null;

        /// <summary>En az bir paket, hepsi sunucuda var, tekrar yok.</summary>
        bool PaketlerGecerli(List<string> secilen)
        {
            var gorulen = new HashSet<string>();
            foreach (var p in secilen)
                if (!paketler.ContainsKey(p) || !gorulen.Add(p)) return false;
            return secilen.Count > 0;
        }

        Protokol.OyuncuEki EkBul(OyuncuId id) => Ek(oyuncular.Find(o => o.Id == id));

        static Protokol.OyuncuEki Ek(OdaOyuncusu o) =>
            new Protokol.OyuncuEki(o.Id, o.Isim, (o.Id.Deger - 1) % 10, o.Baglanti != null);

        /// <summary>En küçük boş numara: p1..p10. Lobiden çıkanın numarası yeniden kullanılır.</summary>
        OyuncuId BosKimlik()
        {
            for (int n = 1; ; n++)
                if (!oyuncular.Exists(o => o.Id.Deger == n)) return new OyuncuId(n);
        }

        static bool Aralikta(int? deger, int en, int enFazla) => !deger.HasValue || (deger >= en && deger <= enFazla);
    }
}
