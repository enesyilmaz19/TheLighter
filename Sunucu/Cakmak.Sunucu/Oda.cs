using System;
using System.Collections.Generic;
using System.Security.Cryptography;
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
        }

        public readonly string Kod;
        readonly object kilit = new object();
        readonly List<OdaOyuncusu> oyuncular = new List<OdaOyuncusu>();
        readonly IReadOnlyList<string> oneriSorulari;
        readonly Ayarlar ayarlar = new Ayarlar();
        OyuncuId kurucu;
        Oyun oyun;
        int surum;
        bool degisti;

        /// <summary>Oda silindi. Bu bayraktan sonra kimse katılamaz.</summary>
        public bool Kapandi { get; private set; }

        public Oda(string kod, IReadOnlyList<string> oneriSorulari)
        {
            Kod = kod;
            this.oneriSorulari = oneriSorulari;
        }

        public static bool IsimGecerli(string isim)
        {
            var t = isim?.Trim();
            return !string.IsNullOrEmpty(t) && t.Length <= IsimEnFazla;
        }

        // ------------------------------------------------------------ Katılma ve ayrılma

        public void Katil(Baglanti b, string isim, string anahtar)
        {
            lock (kilit)
            {
                if (Kapandi) { b.Yolla(Protokol.Hata("ODA_YOK")); return; }

                var eski = anahtar == null ? null : oyuncular.Find(o => o.Anahtar == anahtar);
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
                    b.Yolla(Protokol.Hosgeldin(eski.Id, eski.Anahtar, Kod));
                    HerkeseDurum();
                    return;
                }

                if (oyun != null) { b.Yolla(Protokol.Hata("OYUN_BASLADI")); return; }
                if (oyuncular.Count >= EnFazlaOyuncu) { b.Yolla(Protokol.Hata("ODA_DOLU")); return; }

                var temiz = isim?.Trim();
                if (!IsimGecerli(temiz) || oyuncular.Exists(o => string.Equals(o.Isim, temiz, StringComparison.OrdinalIgnoreCase)))
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
                };
                oyuncular.Add(yeni);
                if (oyuncular.Count == 1) kurucu = yeni.Id;
                b.Oda = this;
                b.Kim = yeni.Id;
                b.Yolla(Protokol.Hosgeldin(yeni.Id, yeni.Anahtar, Kod));
                HerkeseDurum();
            }
        }

        /// <summary>Bağlantı kapandı. Odada bağlı kimse kalmadıysa true: oda silinmeli.</summary>
        public bool Ayrildi(Baglanti b)
        {
            lock (kilit)
            {
                var o = oyuncular.Find(x => x.Baglanti == b);
                if (o == null) return false;

                if (oyun == null)
                {
                    oyuncular.Remove(o);
                    if (o.Id == kurucu && oyuncular.Count > 0) kurucu = oyuncular[0].Id;
                }
                else
                {
                    // ponytail: oyunda kopan oyuncu oyunda kalır, süreler işler. 60 sn bekleme ve AFK kuralları S5'te.
                    o.Baglanti = null;
                    // Kurucu koptuysa kuruculuk ilk bağlı oyuncuya geçer. Yoksa sınırsız oyunu kimse bitiremez.
                    if (o.Id == kurucu)
                    {
                        var yeniKurucu = oyuncular.Find(x => x.Baglanti != null);
                        if (yeniKurucu != null) kurucu = yeniKurucu.Id;
                    }
                }

                if (!oyuncular.Exists(x => x.Baglanti != null))
                {
                    // ponytail: bağlı kimse kalmayınca oda hemen silinir. Herkes aynı anda koparsa geri dönemez. S5'te bir süre tutulacak.
                    Kapandi = true;
                    return true;
                }
                HerkeseDurum();
                return false;
            }
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
                var yeniMod = mod == null ? ayarlar.Mod : Protokol.Coz<OyunModu>(mod);
                var yeniMiniOyun = miniOyun == null ? ayarlar.MiniOyun : Protokol.Coz<MiniOyunTuru>(miniOyun);

                // Reaksiyon sadece elden ele: online'da süre istemciden gelir ve telefonların dokunma gecikmesi farklıdır.
                if (yeniMiniOyun == MiniOyunTuru.Reaksiyon
                    || !Aralikta(turSayisi, 0, 100) || !Aralikta(soruSn, 5, 300) || !Aralikta(cevapSn, 5, 300) || !Aralikta(hamleSn, 5, 300))
                {
                    b.Yolla(Protokol.Hata("GECERSIZ_GIRDI"));
                    return;
                }

                ayarlar.Mod = yeniMod;
                ayarlar.MiniOyun = yeniMiniOyun;
                if (turSayisi.HasValue) ayarlar.TurSayisi = turSayisi.Value;
                if (soruSn.HasValue) ayarlar.SoruSuresiSn = soruSn.Value;
                if (cevapSn.HasValue) ayarlar.CevapSuresiSn = cevapSn.Value;
                if (hamleSn.HasValue) ayarlar.HamleSuresiSn = hamleSn.Value;
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

                var kopya = new Ayarlar
                {
                    Mod = ayarlar.Mod,
                    MiniOyun = ayarlar.MiniOyun,
                    TurSayisi = ayarlar.TurSayisi,
                    SoruSuresiSn = ayarlar.SoruSuresiSn,
                    CevapSuresiSn = ayarlar.CevapSuresiSn,
                    HamleSuresiSn = ayarlar.HamleSuresiSn,
                    OneriSorulari = oneriSorulari,
                };
                var sonuc = Oyun.Kur(kopya, liste, RandomNumberGenerator.GetInt32(int.MaxValue), out var yeni);
                if (sonuc != Sonuc.Tamam) { b.Yolla(Protokol.Hata(Protokol.HataKodu(sonuc))); return; }

                oyun = yeni;
                oyun.OlayOldu += OlayGeldi;
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
                    default:
                        throw new GecersizMesaj();
                }
                Uygula(b, sonuc);
            }
        }

        // ------------------------------------------------------------ Saat

        /// <summary>Saat her ~100 ms'de çağırır. Bir aşama değiştiyse herkese yeni durum gider.</summary>
        public void Ilerle(TimeSpan gecen)
        {
            lock (kilit)
            {
                if (oyun == null || Kapandi) return;
                oyun.Ilerle(gecen);
                if (degisti) HerkeseDurum();
            }
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
                var json = Protokol.LobiDurumu(surum, Kod, kurucu, ekler, ayarlar);
                foreach (var o in oyuncular) o.Baglanti?.Yolla(json);
                return;
            }

            // Herkes kendi görünümünü alır. Gizlilik kural motorunda: soru sadece B'nin görünümünde.
            foreach (var o in oyuncular)
            {
                if (o.Baglanti == null) continue;
                o.Baglanti.Yolla(Protokol.OyunDurumu(surum, Kod, kurucu, EkBul, oyun.GorunumAl(o.Id)));
            }
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
