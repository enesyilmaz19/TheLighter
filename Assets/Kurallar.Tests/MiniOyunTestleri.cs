using NUnit.Framework;
using static Cakmak.Kurallar.Tests.Kurulum;

namespace Cakmak.Kurallar.Tests
{
    public class MiniOyunTestleri
    {
        static Oyun MiniOyunaGetir(MiniOyunTuru tur, out OyuncuId b, out OyuncuId c, int tohum = 1)
        {
            var oyun = Yeni(tohum: tohum, ayarla: x => x.MiniOyun = tur);
            SorVeSec(oyun, "Soru?", out _, out b, out c);
            Assert.That(Durum(oyun).Asama, Is.EqualTo(Asama.MiniOyun));
            return oyun;
        }

        // ---------------------------------------------------- TKM

        [TestCase(TkmSecim.Tas, TkmSecim.Makas, false)]
        [TestCase(TkmSecim.Makas, TkmSecim.Kagit, false)]
        [TestCase(TkmSecim.Kagit, TkmSecim.Tas, false)]
        [TestCase(TkmSecim.Makas, TkmSecim.Tas, true)]
        [TestCase(TkmSecim.Kagit, TkmSecim.Makas, true)]
        [TestCase(TkmSecim.Tas, TkmSecim.Kagit, true)]
        public void Tkm_kurallari(TkmSecim bSecim, TkmSecim cSecim, bool cKazanir)
        {
            var oyun = MiniOyunaGetir(MiniOyunTuru.Tkm, out var b, out var c);
            oyun.MiniOyunHamlesi(b, Hamle.TasKagitMakas(bSecim));
            oyun.MiniOyunHamlesi(c, Hamle.TasKagitMakas(cSecim));
            Assert.That(Durum(oyun).Asama, Is.EqualTo(cKazanir ? Asama.Ifsa : Asama.Gume));
        }

        [Test]
        public void Tkm_beraberlikte_tekrar_oynanir()
        {
            var oyun = MiniOyunaGetir(MiniOyunTuru.Tkm, out var b, out var c);
            oyun.MiniOyunHamlesi(b, Hamle.TasKagitMakas(TkmSecim.Tas));
            oyun.MiniOyunHamlesi(c, Hamle.TasKagitMakas(TkmSecim.Tas));

            var g = Durum(oyun);
            Assert.That(g.Asama, Is.EqualTo(Asama.MiniOyun));
            Assert.That(g.Beraberlik, Is.EqualTo(1));
            Assert.That(g.BHamleYapti, Is.False);
            Assert.That(g.KalanMs, Is.EqualTo(10000));
        }

        [Test]
        public void Ucuncu_beraberlikte_zar_karar_verir()
        {
            var oyun = MiniOyunaGetir(MiniOyunTuru.Tkm, out var b, out var c);
            for (int i = 0; i < 3; i++)
            {
                oyun.MiniOyunHamlesi(b, Hamle.TasKagitMakas(TkmSecim.Kagit));
                oyun.MiniOyunHamlesi(c, Hamle.TasKagitMakas(TkmSecim.Kagit));
            }
            var g = Durum(oyun);
            Assert.That(g.Asama, Is.EqualTo(Asama.Ifsa).Or.EqualTo(Asama.Gume));
            Assert.That(g.SonMiniOyun.ZarlaKarar, Is.True);
            Assert.That(g.SonMiniOyun.BZar, Is.Not.EqualTo(g.SonMiniOyun.CZar));
            Assert.That(g.SonMiniOyun.CKazandi, Is.EqualTo(g.SonMiniOyun.CZar > g.SonMiniOyun.BZar));
        }

        [Test]
        public void Hamle_gizli_kalir_sonucta_acilir()
        {
            var oyun = MiniOyunaGetir(MiniOyunTuru.Tkm, out var b, out var c);
            oyun.MiniOyunHamlesi(b, Hamle.TasKagitMakas(TkmSecim.Makas));

            var gC = oyun.GorunumAl(c);
            Assert.That(gC.BHamleYapti, Is.True, "C, B'nin oynadığını görür");
            Assert.That(gC.Hamlen, Is.Null, "ama ne oynadığını görmez");
            Assert.That(gC.SonMiniOyun, Is.Null);
            Assert.That(oyun.GorunumAl(b).Hamlen.Value.Tkm, Is.EqualTo(TkmSecim.Makas));

            oyun.MiniOyunHamlesi(c, Hamle.TasKagitMakas(TkmSecim.Tas));
            var son = Durum(oyun).SonMiniOyun;
            Assert.That(son.BHamle.Value.Tkm, Is.EqualTo(TkmSecim.Makas));
            Assert.That(son.CHamle.Value.Tkm, Is.EqualTo(TkmSecim.Tas));
        }

        [Test]
        public void Hamle_degistirilemez()
        {
            var oyun = MiniOyunaGetir(MiniOyunTuru.Tkm, out var b, out _);
            oyun.MiniOyunHamlesi(b, Hamle.TasKagitMakas(TkmSecim.Tas));
            Assert.That(oyun.MiniOyunHamlesi(b, Hamle.TasKagitMakas(TkmSecim.Kagit)), Is.EqualTo(Sonuc.HakKalmadi));
        }

        [Test]
        public void Seyirci_hamle_yapamaz_ve_yanlis_oyun_reddedilir()
        {
            var oyun = MiniOyunaGetir(MiniOyunTuru.Tkm, out var b, out var c);
            foreach (var o in Durum(oyun).Oyuncular)
                if (o.Id != b && o.Id != c)
                    Assert.That(oyun.MiniOyunHamlesi(o.Id, Hamle.TasKagitMakas(TkmSecim.Tas)), Is.EqualTo(Sonuc.SiraSendeDegil));
            Assert.That(oyun.MiniOyunHamlesi(b, Hamle.ZarAt()), Is.EqualTo(Sonuc.GecersizGirdi));
        }

        // ---------------------------------------------------- Tek-Çift

        [TestCase(2, true, 3, false)]   // toplam 5 tek, B "tek" dedi → B kazanır
        [TestCase(2, false, 3, true)]   // toplam 5 tek, B "çift" dedi → C kazanır
        [TestCase(4, false, 2, false)]  // toplam 6 çift, B "çift" dedi → B kazanır
        [TestCase(1, true, 1, true)]    // toplam 2 çift, B "tek" dedi → C kazanır
        public void TekCift_toplamin_tekligi_belirler(int bParmak, bool bTekDiyor, int cParmak, bool cKazanir)
        {
            var oyun = MiniOyunaGetir(MiniOyunTuru.TekCift, out var b, out var c);
            oyun.MiniOyunHamlesi(b, Hamle.TekCift(bParmak, bTekDiyor));
            oyun.MiniOyunHamlesi(c, Hamle.TekCift(cParmak));
            Assert.That(Durum(oyun).Asama, Is.EqualTo(cKazanir ? Asama.Ifsa : Asama.Gume));
        }

        [TestCase(0)]
        [TestCase(6)]
        public void TekCift_parmak_1_ile_5_arasi(int parmak)
        {
            var oyun = MiniOyunaGetir(MiniOyunTuru.TekCift, out var b, out _);
            Assert.That(oyun.MiniOyunHamlesi(b, Hamle.TekCift(parmak, true)), Is.EqualTo(Sonuc.GecersizGirdi));
        }

        // ---------------------------------------------------- Zar

        [Test]
        public void Zar_asla_berabere_bitmez_buyuk_kazanir()
        {
            for (int tohum = 1; tohum <= 200; tohum++)
            {
                var oyun = MiniOyunaGetir(MiniOyunTuru.Zar, out var b, out var c, tohum);
                oyun.MiniOyunHamlesi(b, Hamle.ZarAt());
                oyun.MiniOyunHamlesi(c, Hamle.ZarAt());
                var son = Durum(oyun).SonMiniOyun;
                Assert.That(son.BZar, Is.InRange(1, 6));
                Assert.That(son.CZar, Is.InRange(1, 6));
                Assert.That(son.BZar, Is.Not.EqualTo(son.CZar));
                Assert.That(son.CKazandi, Is.EqualTo(son.CZar > son.BZar));
            }
        }

        // ---------------------------------------------------- Reaksiyon

        [TestCase(200, 300, false)]   // B daha hızlı
        [TestCase(300, 200, true)]    // C daha hızlı
        [TestCase(-1, 400, true)]     // B erken bastı
        [TestCase(50, 400, true)]     // B'nin 50 ms'i insan dışı, erken sayılır
        [TestCase(400, -1, false)]    // C erken bastı
        public void Reaksiyon_hizli_kazanir_erken_basan_kaybeder(int bMs, int cMs, bool cKazanir)
        {
            var oyun = MiniOyunaGetir(MiniOyunTuru.Reaksiyon, out var b, out var c);
            oyun.MiniOyunHamlesi(b, Hamle.Reaksiyon(bMs));
            oyun.MiniOyunHamlesi(c, Hamle.Reaksiyon(cMs));
            Assert.That(Durum(oyun).Asama, Is.EqualTo(cKazanir ? Asama.Ifsa : Asama.Gume));
        }

        [TestCase(-1, -1)]
        [TestCase(250, 250)]
        public void Reaksiyon_ikisi_de_erken_ya_da_esitse_berabere(int bMs, int cMs)
        {
            var oyun = MiniOyunaGetir(MiniOyunTuru.Reaksiyon, out var b, out var c);
            oyun.MiniOyunHamlesi(b, Hamle.Reaksiyon(bMs));
            oyun.MiniOyunHamlesi(c, Hamle.Reaksiyon(cMs));
            Assert.That(Durum(oyun).Beraberlik, Is.EqualTo(1));
        }

        // ---------------------------------------------------- Karışık

        [Test]
        public void Karisik_her_tur_TKM_TekCift_Zar_dan_birini_secer()
        {
            var gorulen = new System.Collections.Generic.HashSet<MiniOyunTuru>();
            for (int tohum = 1; tohum <= 50; tohum++)
            {
                var oyun = Yeni(tohum: tohum, ayarla: x => x.MiniOyun = MiniOyunTuru.Karisik);
                var tur = Durum(oyun).MiniOyun;
                Assert.That(tur, Is.EqualTo(MiniOyunTuru.Tkm).Or.EqualTo(MiniOyunTuru.TekCift).Or.EqualTo(MiniOyunTuru.Zar));
                gorulen.Add(tur);
            }
            Assert.That(gorulen, Has.Count.EqualTo(3));
        }
    }
}
