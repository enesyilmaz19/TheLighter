using System;
using System.Collections.Generic;
using System.Linq;
using Cakmak.Kurallar;
using NUnit.Framework;
using TheLighter.Ag;

namespace TheLighter.Testler
{
    /// <summary>
    /// Oturum: oda kur/katıl, kopunca kendiliğinden geri dönme, atılma, sürüm filtresi, mesaj sınırı.
    /// Ağ yok: sahte taşıma, saat elle ilerletilir.
    /// </summary>
    public class OnlineIstemciTestleri
    {
        static readonly Uri Adres = new Uri("ws://localhost:5000/ws");

        sealed class SahteTasima : ITasima
        {
            public readonly Queue<TasimaOlayi> Gelen = new Queue<TasimaOlayi>();
            public readonly List<string> Giden = new List<string>();
            public bool Acik;
            public bool Kapatildi;
            public Uri Adres;

            public void Ac(Uri adres) { Adres = adres; }
            public bool Gonder(string json)
            {
                if (!Acik) return false;
                Giden.Add(json);
                return true;
            }
            public void Kapat() { Kapatildi = true; Acik = false; }
            public bool Al(out TasimaOlayi olay)
            {
                if (Gelen.Count > 0) { olay = Gelen.Dequeue(); return true; }
                olay = default;
                return false;
            }

            public void Acil() { Acik = true; Gelen.Enqueue(new TasimaOlayi { Tur = TasimaOlayTuru.Acildi }); }
            public void Mesaj(string json) => Gelen.Enqueue(new TasimaOlayi { Tur = TasimaOlayTuru.Mesaj, Metin = json });
            public void Kop() { Acik = false; Gelen.Enqueue(new TasimaOlayi { Tur = TasimaOlayTuru.Kapandi, Metin = "test" }); }
            public JsonDeger Son => JsonDeger.Coz(Giden.Last());
        }

        sealed class SozlukKalici : IKalici
        {
            public readonly Dictionary<string, string> S = new Dictionary<string, string>();
            public string Oku(string ad) => S.TryGetValue(ad, out var d) ? d : null;
            public void Yaz(string ad, string deger) => S[ad] = deger;
            public void Sil(string ad) => S.Remove(ad);
        }

        List<SahteTasima> tasimalar;
        SozlukKalici kalici;
        List<string> gunluk;
        OnlineIstemci istemci;
        List<string> ayrilmalar;
        List<DurumMesaji> durumlar;
        List<HataMesaji> hatalar;

        SahteTasima T => tasimalar.Last();

        [SetUp]
        public void Kur()
        {
            tasimalar = new List<SahteTasima>();
            kalici = new SozlukKalici();
            gunluk = new List<string>();
            istemci = new OnlineIstemci(() => { var t = new SahteTasima(); tasimalar.Add(t); return t; }, kalici, gunluk.Add);
            ayrilmalar = new List<string>();
            durumlar = new List<DurumMesaji>();
            hatalar = new List<HataMesaji>();
            istemci.Ayrildi += ayrilmalar.Add;
            istemci.DurumGeldi += durumlar.Add;
            istemci.HataGeldi += hatalar.Add;
        }

        void Kare(double sn = 0.016) => istemci.Guncelle(TimeSpan.FromSeconds(sn));

        static string Hosgeldin(string sen = "p1", string anahtar = "GIZLI-ANAHTAR-123", string oda = "KXTPM") =>
            "{\"t\":\"hosgeldin\",\"sen\":\"" + sen + "\",\"anahtar\":\"" + anahtar + "\",\"oda\":\"" + oda + "\"}";

        static string Lobi(int surum, string kurucu = "p1") =>
            "{\"t\":\"durum\",\"surum\":" + surum + ",\"oda\":\"KXTPM\",\"asama\":\"lobi\",\"kurucu\":\"" + kurucu + "\"," +
            "\"oyuncular\":[{\"id\":\"p1\",\"isim\":\"Enes\",\"renk\":0,\"bagli\":true}],\"ayarlar\":{}}";

        static string Hata(string kod) => "{\"t\":\"hata\",\"kod\":\"" + kod + "\"}";

        /// <summary>Oda kurup lobiye girer (p1, kurucu).</summary>
        void OdayaGir()
        {
            istemci.OdaKur(Adres, "Enes");
            T.Acil();
            Kare();
            T.Mesaj(Hosgeldin());
            T.Mesaj(Lobi(1));
            Kare();
            Assert.That(istemci.Durum, Is.EqualTo(BaglantiDurumu.Odada));
        }

        [Test]
        public void OdaKur_AcilincaOdaKurYollar_HosgeldinleOdaya()
        {
            istemci.OdaKur(Adres, "Enes");
            Assert.That(istemci.Durum, Is.EqualTo(BaglantiDurumu.Giriyor));
            Assert.That(T.Adres, Is.EqualTo(Adres));
            Kare();
            Assert.That(T.Giden, Is.Empty, "bağlantı açılmadan yollanmaz");

            T.Acil();
            Kare();
            Assert.That(T.Son["t"].Metin, Is.EqualTo("odaKur"));
            Assert.That(T.Son["isim"].Metin, Is.EqualTo("Enes"));

            T.Mesaj(Hosgeldin());
            T.Mesaj(Lobi(1));
            Kare();
            Assert.That(istemci.Durum, Is.EqualTo(BaglantiDurumu.Odada));
            Assert.That(istemci.Sen, Is.EqualTo(new OyuncuId(1)));
            Assert.That(istemci.Oda, Is.EqualTo("KXTPM"));
            Assert.That(istemci.Kurucuyum, Is.True);
            Assert.That(durumlar.Single().Yer, Is.EqualTo(OdaYeri.Lobi));
            Assert.That(kalici.Oku(OnlineIstemci.KayitOda), Is.EqualTo("KXTPM"));
            Assert.That(kalici.Oku(OnlineIstemci.KayitAnahtar), Is.EqualTo("GIZLI-ANAHTAR-123"));
        }

        [Test]
        public void Anahtar_GunlugeHicYazilmaz()
        {
            OdayaGir();
            T.Kop();
            Kare();
            Kare(1.1);
            T.Acil();
            Kare();
            T.Mesaj(Hata("ODA_YOK"));
            Kare();
            Assert.That(gunluk, Is.Not.Empty);
            Assert.That(gunluk.Any(s => s.Contains("GIZLI-ANAHTAR-123")), Is.False);
        }

        [Test]
        public void Katil_KoduBuyukHarfeCevirir_IlkGiristeAnahtarYok()
        {
            istemci.Katil(Adres, " kxtpm ", "Ayşe");
            T.Acil();
            Kare();
            Assert.That(T.Son["t"].Metin, Is.EqualTo("katil"));
            Assert.That(T.Son["oda"].Metin, Is.EqualTo("KXTPM"));
            Assert.That(T.Son["isim"].Metin, Is.EqualTo("Ayşe"));
            Assert.That(T.Son["anahtar"].Var, Is.False);
        }

        [Test]
        public void Katil_AyniOdanınSakliAnahtariylaGeriDoner_BaskaOdadaAnahtarYollamaz()
        {
            kalici.Yaz(OnlineIstemci.KayitOda, "KXTPM");
            kalici.Yaz(OnlineIstemci.KayitAnahtar, "ESKI");

            istemci.Katil(Adres, "KXTPM", "Ayşe");
            T.Acil();
            Kare();
            Assert.That(T.Son["anahtar"].Metin, Is.EqualTo("ESKI"));

            istemci.Katil(Adres, "ABCDE", "Ayşe");
            T.Acil();
            Kare();
            Assert.That(T.Son["anahtar"].Var, Is.False);
        }

        [Test]
        public void EskiSurumluDurum_Atilir()
        {
            OdayaGir();
            T.Mesaj(Lobi(5));
            T.Mesaj(Lobi(3));
            T.Mesaj(Lobi(5));
            Kare();
            Assert.That(durumlar.Select(d => d.Surum), Is.EqualTo(new[] { 1, 5, 5 }));
            Assert.That(istemci.SonDurum.Surum, Is.EqualTo(5));
        }

        [Test]
        public void Kopunca_BeklemeArtarak_AnahtarVeAyniIsimleGeriDoner()
        {
            OdayaGir();
            int ilk = tasimalar.Count;

            T.Kop();
            Kare();
            Assert.That(istemci.Durum, Is.EqualTo(BaglantiDurumu.YenidenBaglaniyor));
            Assert.That(ayrilmalar, Is.Empty);

            Kare(0.9);
            Assert.That(tasimalar.Count, Is.EqualTo(ilk), "1 sn dolmadan denenmez");
            Kare(0.2);
            Assert.That(tasimalar.Count, Is.EqualTo(ilk + 1));

            // İkinci deneme de açılamadı: bu sefer 2 sn.
            T.Kop();
            Kare();
            Kare(1.5);
            Assert.That(tasimalar.Count, Is.EqualTo(ilk + 1));
            Kare(0.6);
            Assert.That(tasimalar.Count, Is.EqualTo(ilk + 2));

            T.Acil();
            Kare();
            Assert.That(T.Son["t"].Metin, Is.EqualTo("katil"));
            Assert.That(T.Son["oda"].Metin, Is.EqualTo("KXTPM"));
            Assert.That(T.Son["isim"].Metin, Is.EqualTo("Enes"));
            Assert.That(T.Son["anahtar"].Metin, Is.EqualTo("GIZLI-ANAHTAR-123"));

            T.Mesaj(Hosgeldin());
            T.Mesaj(Lobi(7));
            Kare();
            Assert.That(istemci.Durum, Is.EqualTo(BaglantiDurumu.Odada));
            Assert.That(istemci.SonDurum.Surum, Is.EqualTo(7));

            // Başarılı dönüşten sonra bekleme baştan: yine 1 sn.
            T.Kop();
            Kare();
            int simdi = tasimalar.Count;
            Kare(1.05);
            Assert.That(tasimalar.Count, Is.EqualTo(simdi + 1));
        }

        [Test]
        public void Bekleme_EnFazla10Sn()
        {
            OdayaGir();
            for (int i = 0; i < 8; i++)
            {
                T.Kop();
                Kare();
                int n = tasimalar.Count;
                Kare(10.01);
                Assert.That(tasimalar.Count, Is.EqualTo(n + 1), "deneme " + i);
            }
        }

        [Test]
        public void Atildin_TekrarDenemez_AnahtarUnutulur()
        {
            OdayaGir();
            T.Mesaj(Hata("ATILDIN"));
            T.Kop();
            Kare();
            Assert.That(ayrilmalar, Is.EqualTo(new[] { "ATILDIN" }));
            Assert.That(istemci.Durum, Is.EqualTo(BaglantiDurumu.Kapali));
            Assert.That(kalici.Oku(OnlineIstemci.KayitAnahtar), Is.Null);
            int n = tasimalar.Count;
            Kare(30);
            Assert.That(tasimalar.Count, Is.EqualTo(n), "atılan kendiliğinden geri dönmez");
        }

        [Test]
        public void GeriDonerkenOdaYok_AnaMenuyeNedeniyle()
        {
            OdayaGir();
            T.Kop();
            Kare();
            Kare(1.1);
            T.Acil();
            Kare();
            T.Mesaj(Hata("ODA_YOK"));
            Kare();
            Assert.That(ayrilmalar, Is.EqualTo(new[] { "ODA_YOK" }));
            Assert.That(istemci.Durum, Is.EqualTo(BaglantiDurumu.Kapali));
            Assert.That(kalici.Oku(OnlineIstemci.KayitOda), Is.Null);
        }

        [Test]
        public void HicBaglanamazsa_Baglanamadi()
        {
            istemci.OdaKur(Adres, "Enes");
            T.Kop();
            Kare();
            Assert.That(ayrilmalar, Is.EqualTo(new[] { OnlineIstemci.NedenBaglanamadi }));
            Assert.That(istemci.Durum, Is.EqualTo(BaglantiDurumu.Kapali));
        }

        [TestCase("ISIM_GECERSIZ")]
        [TestCase("ODA_DOLU")]
        [TestCase("SUNUCU_DOLU")]
        public void Girilemezse_KoduylaCikar_BaglantiKapanir(string kod)
        {
            istemci.Katil(Adres, "KXTPM", "Ayşe");
            T.Acil();
            Kare();
            T.Mesaj(Hata(kod));
            Kare();
            Assert.That(ayrilmalar, Is.EqualTo(new[] { kod }));
            Assert.That(T.Kapatildi, Is.True, "odasız bağlantı açık kalmasın");
        }

        [Test]
        public void GiristeCokHizli_BirazSonraYenidenDener()
        {
            istemci.OdaKur(Adres, "Enes");
            T.Acil();
            Kare();
            T.Mesaj(Hata("COK_HIZLI"));
            Kare();
            Assert.That(ayrilmalar, Is.Empty);
            int n = tasimalar.Count;
            Kare(1.1);
            Assert.That(tasimalar.Count, Is.EqualTo(n + 1));
            T.Acil();
            Kare();
            Assert.That(T.Son["t"].Metin, Is.EqualTo("odaKur"), "oda henüz yoktu: yine oda kurar");
        }

        [Test]
        public void OdadayokenHata_EkranaGider_Cikmaz()
        {
            OdayaGir();
            T.Mesaj(Hata("SIRA_SENDE_DEGIL"));
            T.Mesaj(Hata("ESKI_TUR"));
            Kare();
            Assert.That(hatalar.Select(h => h.Kod), Is.EqualTo(new[] { "SIRA_SENDE_DEGIL", "ESKI_TUR" }));
            Assert.That(hatalar[0].Sonuc, Is.EqualTo(Sonuc.SiraSendeDegil));
            Assert.That(istemci.Durum, Is.EqualTo(BaglantiDurumu.Odada));
        }

        [Test]
        public void MesajSiniri_FazlasiSiradaBekler_SonraGider()
        {
            OdayaGir();
            int once = T.Giden.Count;
            for (int i = 0; i < 30; i++) Assert.That(istemci.Gonder(Cevirici.Buradayim()), Is.True);
            int hemen = T.Giden.Count - once;
            Assert.That(hemen, Is.LessThanOrEqualTo(10), "arka arkaya en fazla 10");
            Kare(1);
            int birSnSonra = T.Giden.Count - once;
            Assert.That(birSnSonra - hemen, Is.LessThanOrEqualTo(10), "saniyede 10'dan az");
            Assert.That(birSnSonra, Is.GreaterThan(hemen));
            for (int i = 0; i < 10; i++) Kare(0.5);
            Assert.That(T.Giden.Count - once, Is.EqualTo(30), "sıradakiler kaybolmaz");
        }

        [Test]
        public void CokBuyukMesaj_Gonderilmez()
        {
            OdayaGir();
            Assert.That(istemci.Gonder(Cevirici.HavuzaEkle(0, new string('x', 5000))), Is.False);
        }

        [Test]
        public void OdadaDegilken_Gonderilmez()
        {
            Assert.That(istemci.Gonder(Cevirici.Baslat()), Is.False);
        }

        [Test]
        public void Ayril_BaglantiKapanir_AnahtarUnutulur_TekrarDenemez()
        {
            OdayaGir();
            var t = T;
            istemci.Ayril();
            Assert.That(t.Kapatildi, Is.True);
            Assert.That(ayrilmalar, Is.EqualTo(new[] { OnlineIstemci.NedenAyrildin }));
            Assert.That(kalici.Oku(OnlineIstemci.KayitAnahtar), Is.Null);
            int n = tasimalar.Count;
            Kare(30);
            Assert.That(tasimalar.Count, Is.EqualTo(n));
        }

        [Test]
        public void UygulamaDonunce_HemenYenidenBaglanir()
        {
            OdayaGir();
            var eski = T;
            istemci.UygulamaDondu();
            Assert.That(eski.Kapatildi, Is.True);
            Assert.That(T, Is.Not.SameAs(eski));
            T.Acil();
            Kare();
            Assert.That(T.Son["anahtar"].Metin, Is.EqualTo("GIZLI-ANAHTAR-123"));
        }

        [Test]
        public void EskiBaglantininGecGelenMesaji_Islenmez()
        {
            OdayaGir();
            var eski = T;
            istemci.UygulamaDondu();
            eski.Mesaj(Lobi(99));
            Kare();
            Assert.That(durumlar.Any(d => d.Surum == 99), Is.False);
        }

        [Test]
        public void TanimayanMesaj_AtlanirOturumSurer()
        {
            OdayaGir();
            T.Mesaj("çöp");
            T.Mesaj("{\"t\":\"yeniTip\"}");
            T.Mesaj(Lobi(2));
            Kare();
            Assert.That(istemci.Durum, Is.EqualTo(BaglantiDurumu.Odada));
            Assert.That(istemci.SonDurum.Surum, Is.EqualTo(2));
        }

        [Test]
        public void Tur_SonOyunDurumundan()
        {
            OdayaGir();
            Assert.That(istemci.Tur, Is.EqualTo(0));
            T.Mesaj("{\"t\":\"durum\",\"surum\":5,\"asama\":\"soruSorma\",\"tur\":3}");
            Kare();
            Assert.That(istemci.Tur, Is.EqualTo(3));
            Assert.That(istemci.SonDurum.Gorunum.Sen, Is.EqualTo(new OyuncuId(1)), "görünüm kimin olduğunu bilir");
        }
    }
}
