using System;
using System.Collections.Generic;
using Cakmak.Kurallar;
using TheLighter.Akis;

namespace TheLighter.Ag
{
    /// <summary>
    /// Online oyunun akışı: sunucunun son <c>durum</c>'undan bu cihazın ekranını seçer, komutları sunucuya yollar.
    /// Kural hesaplamaz, tahmin yapmaz: sıra kimde, kim seçilebilir, kim kazandı hep durum'dan gelir.
    /// Elden eleden farkı: perde yok (her cihaz tek oyuncu), saat sunucuda (menü açıkken de akar),
    /// sırası gelmeyen "bekleniyor" ekranında kimin ne yaptığını görür.
    /// </summary>
    public sealed class OnlineAkis : IOyunAkisi
    {
        readonly OnlineIstemci istemci;
        DurumMesaji durum;
        int durumKalanMs;
        double gecenMs;
        bool goreviGordum;
        List<Oyuncu> oyuncular = new List<Oyuncu>();
        readonly Dictionary<OyuncuId, int> renkler = new Dictionary<OyuncuId, int>();
        readonly Dictionary<OyuncuId, string> isimler = new Dictionary<OyuncuId, string>();

        /// <param name="ayarlar">Lobide kurucunun seçtiği ayarlar. <see cref="Ayarlar.OneriSorulari"/> "fikir ver" için cihazdaki paketlerden.</param>
        public OnlineAkis(OnlineIstemci istemci, Ayarlar ayarlar)
        {
            this.istemci = istemci ?? throw new ArgumentNullException(nameof(istemci));
            Ayarlar = ayarlar ?? new Ayarlar();
            Ekran = new EkranDurumu { Tur = EkranTuru.Bekleniyor, Gorunum = new Gorunum(), Anahtar = "" };
            Ilerle(TimeSpan.Zero);
        }

        public EkranDurumu Ekran { get; private set; }
        public bool Online => true;
        public OyuncuId? Ben => istemci.Sen;
        public bool Kurucuyum => istemci.Kurucuyum;
        public IReadOnlyList<Oyuncu> Oyuncular => oyuncular;
        public Ayarlar Ayarlar { get; }

        /// <summary>Online'da saat sunucuda: menü açık olsa da süre akar.</summary>
        public bool Duraklatildi { get; set; }

        public bool Bitti => Ekran.Tur == EkranTuru.OyunSonu;

        /// <summary>Son durum (bekleme, bekleyenler, bağlantı bilgisi için). Oyunda değilse null olabilir.</summary>
        public DurumMesaji Durum => durum;

        /// <summary>Oyuncunun bağlantısı açık mı (kopuksa "Bağlanıyor…").</summary>
        public bool Bagli(OyuncuId id)
        {
            var o = durum?.Bul(id);
            return o == null || o.Bagli;
        }

        /// <summary>Bağlı oyuncu 4'ün altında, oyun durdu: bitmesine kalan süre (yoksa null).</summary>
        public int? BeklemeKalanMs =>
            durum?.BeklemeKalanMs == null ? (int?)null : Math.Max(0, durum.BeklemeKalanMs.Value - (int)gecenMs);

        // ------------------------------------------------------------ Zaman

        public void Ilerle(TimeSpan gecen)
        {
            var son = istemci.SonDurum;
            if (son != null && son != durum && son.Yer == OdaYeri.Oyun)
            {
                durum = son;
                durumKalanMs = son.Gorunum.KalanMs;
                gecenMs = 0;
                KadroyuGuncelle(son);
            }
            else if (durum != null)
            {
                // Geri sayımı istemci yürütür. Oyun az oyuncu yüzünden durduysa aşama sayacı da durur
                // (bekleme sayacı ilerler).
                gecenMs += Math.Max(0, gecen.TotalMilliseconds);
            }
            Yenile();
        }

        void KadroyuGuncelle(DurumMesaji d)
        {
            var yeni = new List<Oyuncu>();
            foreach (var o in d.Gorunum.Oyuncular)
            {
                yeni.Add(new Oyuncu(o.Id, o.Isim));
                isimler[o.Id] = o.Isim;
            }
            foreach (var o in d.Oyuncular)
            {
                renkler[o.Id] = o.Renk;
                if (!string.IsNullOrEmpty(o.Isim)) isimler[o.Id] = o.Isim;
            }
            oyuncular = yeni;
        }

        // ------------------------------------------------------------ Ekran

        void Yenile()
        {
            var e = new EkranDurumu();
            if (durum == null || durum.Gorunum == null || !istemci.Sen.HasValue)
            {
                e.Tur = EkranTuru.Bekleniyor;
                e.Gorunum = new Gorunum();
                e.Anahtar = "yok";
                Ekran = e;
                return;
            }

            var ben = istemci.Sen.Value;
            var g = durum.Gorunum;
            g.Sen = ben;
            bool durdu = durum.BeklemeKalanMs.HasValue;
            g.KalanMs = durdu ? durumKalanMs : Math.Max(0, durumKalanMs - (int)gecenMs);
            e.Gorunum = g;
            string tur = "|" + g.Tur + "|" + g.A + "|" + g.B + "|" + g.C;

            // Gizli görev: oyunun başında bir kez, kendi ekranında (Ana Plan 8.2). Sıra bekletmez.
            if (g.Gorevin != null && !goreviGordum && g.Asama != Asama.OyunSonu)
            {
                Kur(e, EkranTuru.GorevGoster, "gorev");
                e.Kime = ben;
                Ekran = e;
                return;
            }

            // Oyundan çıkıp (60 sn) anahtarla dönen izler, oynamaz: seçim/komut ekranı açılmaz.
            if (BenCiktim && g.Asama != Asama.Ifsa && g.Asama != Asama.Gume && g.Asama != Asama.OyunSonu
                && g.Asama != Asama.IkiyeKatla && g.Asama != Asama.Bedel)
            {
                Bekle(e, null, "izle|" + g.Asama + tur);
                Ekran = e;
                return;
            }

            switch (g.Asama)
            {
                case Asama.SoruToplama:
                {
                    int yazdigi = 0;
                    foreach (var o in g.Oyuncular) if (o.Id == ben) yazdigi = o.HavuzaEkledigi;
                    if (yazdigi < Oyun.KisiBasiHavuzSorusu)
                    {
                        Kur(e, EkranTuru.SoruToplama, "|" + yazdigi);
                        e.Kime = ben;
                    }
                    else Bekle(e, null, "toplama");
                    break;
                }

                case Asama.SoruSorma:
                    if (g.A == ben)
                    {
                        Kur(e, EkranTuru.SoruSorma, tur);
                        e.Kime = ben;
                    }
                    else Bekle(e, g.A, "soru" + tur);
                    break;

                case Asama.CevapSecme:
                    if (g.B == ben)
                    {
                        Kur(e, EkranTuru.CevapSecme, tur);
                        e.Kime = ben;
                    }
                    else Bekle(e, g.B, "cevap" + tur);
                    break;

                case Asama.MiniOyun:
                    if (g.B == ben || g.C == ben)
                    {
                        // Her cihaz kendi yarısını çizer: kendi hamlesini yapınca ekran "rakip bekleniyor"a döner.
                        bool yaptim = g.B == ben ? g.BHamleYapti : g.CHamleYapti;
                        Kur(e, EkranTuru.MiniOyun, tur + "|" + g.Beraberlik + "|" + yaptim);
                        if (g.B == ben) e.BGorunumu = g; else e.CGorunumu = g;
                    }
                    else Bekle(e, g.B, "mini" + tur + "|" + g.Beraberlik);
                    break;

                case Asama.Oylama:
                {
                    bool oylayanim = false;
                    foreach (var id in g.Oylayanlar) if (id == ben) oylayanim = true;
                    if (oylayanim && !g.OyVerdin)
                    {
                        Kur(e, EkranTuru.Oylama, tur);
                        e.Kime = ben;
                    }
                    else Bekle(e, null, "oy" + tur);
                    break;
                }

                case Asama.IkiyeKatla:
                case Asama.Bedel:
                    Kur(e, EkranTuru.Karar, tur + "|" + g.Asama);
                    e.Kime = g.KararVeren;
                    break;

                case Asama.Ifsa:
                case Asama.Gume:
                    // Ceza kartları gösterilir ama saat durmaz (sunucunun 6 sn'si); onay beklenmez.
                    Kur(e, g.Asama == Asama.Ifsa ? EkranTuru.Ifsa : EkranTuru.Gume, tur);
                    e.CezaOnayBekliyor = false;
                    break;

                case Asama.OyunSonu:
                    Kur(e, EkranTuru.OyunSonu, "");
                    e.Ozet = durum.Ozet;
                    e.IfsaOlanlar = IfsaListesi(durum.Ozet);
                    break;

                default:
                    Bekle(e, null, "?");
                    break;
            }
            // Bekleme ekranında giriş yok: her yeni durumda baştan çizilsin (kim bağlı, kim AFK).
            if (e.Tur == EkranTuru.Bekleniyor) e.Anahtar += "|" + durum.Surum;
            Ekran = e;
        }

        static void Kur(EkranDurumu e, EkranTuru tur, string ek)
        {
            e.Tur = tur;
            e.Anahtar = tur + ek;
        }

        /// <summary>Sıra başkasında. Kime: o an işi olan kişi (bekleme ekranında adı geçer).</summary>
        static void Bekle(EkranDurumu e, OyuncuId? kime, string ek)
        {
            e.Tur = EkranTuru.Bekleniyor;
            e.Kime = kime;
            e.Anahtar = "bekle|" + ek;
        }

        static IReadOnlyList<IfsaSatiri> IfsaListesi(OyunOzeti ozet)
        {
            var l = new List<IfsaSatiri>();
            if (ozet == null) return l;
            foreach (var k in ozet.IfsaOlanlar)
                l.Add(new IfsaSatiri { Soru = string.IsNullOrEmpty(k.Soru) ? null : k.Soru, Soran = k.A, Cevap = k.C });
            return l;
        }

        // ------------------------------------------------------------ Komutlar

        int Tur => durum?.Gorunum?.Tur ?? 0;

        Sonuc Yolla(string json) => istemci.Gonder(json) ? Sonuc.Tamam : Sonuc.YanlisAsama;

        /// <summary>Online'da perde yok.</summary>
        public void PerdeyiKaldir() { }

        public void GorevGordum()
        {
            goreviGordum = true;
            Yenile();
        }

        public Sonuc HavuzaSoruEkle(string metin) => Yolla(Cevirici.HavuzaEkle(Tur, metin ?? ""));

        public Sonuc SoruGonder(OyuncuId b, string metin) => Yolla(Cevirici.SoruSor(Tur, b, metin ?? ""));

        /// <summary>Online'da fısıltı yok: soru yazılır.</summary>
        public Sonuc Fisildadim(OyuncuId b) => Sonuc.YanlisAsama;

        /// <summary>Öneri sonraki durum'la gelir (<c>Gorunum.Oneri</c>).</summary>
        public Sonuc OneriCek() => Yolla(Cevirici.OneriCek(Tur));

        public Sonuc SoruyuDegistir() => Yolla(Cevirici.SoruyuDegistir(Tur));

        public Sonuc CevapSec(OyuncuId c) => Yolla(Cevirici.CevapSec(Tur, c));

        public Sonuc Hamle(OyuncuId kim, Hamle hamle) => Yolla(Cevirici.HamleYap(Tur, hamle));

        public Sonuc Oy(bool ifsa) => Yolla(Cevirici.Oy(Tur, ifsa));

        public Sonuc IkiyeKatla() => Yolla(Cevirici.IkiyeKatla(Tur));

        public Sonuc BedelOde() => Yolla(Cevirici.BedelOde(Tur));

        public Sonuc KarariGec() => Yolla(Cevirici.KarariGec(Tur));

        /// <summary>Online'da ceza kartı onayı yok (saat sunucuda).</summary>
        public void CezalariOnayla() { }

        /// <summary>Sadece kurucu. Oyun hemen biter.</summary>
        public Sonuc OyunuBitir() => Kurucuyum ? Yolla(Cevirici.Bitir()) : Sonuc.SiraSendeDegil;

        /// <summary>AFK etiketini kaldırır.</summary>
        public void Buradayim() => istemci.Gonder(Cevirici.Buradayim());

        /// <summary>Kurucu, oyun sonunda: oda lobiye döner.</summary>
        public Sonuc TekrarOyna() => Kurucuyum ? Yolla(Cevirici.TekrarOyna()) : Sonuc.SiraSendeDegil;

        // ------------------------------------------------------------ Yardımcılar

        public int Sira(OyuncuId id) => renkler.TryGetValue(id, out int r) ? r : id.Deger - 1;

        public string Isim(OyuncuId? id)
        {
            if (!id.HasValue) return "";
            return isimler.TryGetValue(id.Value, out var isim) ? isim : id.Value.ToString();
        }

        /// <summary>Bu cihazın oyuncusu AFK mı ("Buradayım" düğmesi).</summary>
        public bool BenAfkyim
        {
            get
            {
                if (durum?.Gorunum == null || !Ben.HasValue) return false;
                foreach (var o in durum.Gorunum.Oyuncular) if (o.Id == Ben.Value) return o.Afk;
                return false;
            }
        }

        /// <summary>Bu cihazın oyuncusu oyundan çıkmış (60 sn'de dönmedi): izler, oynamaz.</summary>
        public bool BenCiktim
        {
            get
            {
                if (durum?.Gorunum == null || !Ben.HasValue) return false;
                foreach (var o in durum.Gorunum.Oyuncular) if (o.Id == Ben.Value) return o.Cikti;
                return false;
            }
        }
    }
}
