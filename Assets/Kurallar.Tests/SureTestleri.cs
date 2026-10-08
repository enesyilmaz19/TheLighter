using System.Collections.Generic;
using NUnit.Framework;
using static Cakmak.Kurallar.Tests.Kurulum;

namespace Cakmak.Kurallar.Tests
{
    public class SureTestleri
    {
        [Test]
        public void A_60_saniyede_sormazsa_sira_rastgele_baskasina_gecer()
        {
            for (int tohum = 1; tohum <= 50; tohum++)
            {
                var oyun = Yeni(tohum: tohum);
                var olaylar = new List<Olay>();
                oyun.OlayOldu += olaylar.Add;
                var a = SiradakiA(oyun);

                Saniye(oyun, 59.9);
                Assert.That(Durum(oyun).A, Is.EqualTo(a), "süre dolmadan sıra değişmez");
                Saniye(oyun, 0.2);

                var g = Durum(oyun);
                Assert.That(g.Asama, Is.EqualTo(Asama.SoruSorma));
                Assert.That(g.A, Is.Not.EqualTo(a), "süresi dolan tekrar seçilmez");
                Assert.That(g.Tur, Is.EqualTo(2), "yanan tur da sayılır");
                Assert.That(olaylar[0].Tur, Is.EqualTo(OlayTuru.SureDoldu));
                Assert.That(olaylar[0].Kim, Is.EqualTo(a));
            }
        }

        [Test]
        public void B_20_saniyede_secmezse_soru_gume_gider_sira_A_ve_B_disina_gecer()
        {
            for (int tohum = 1; tohum <= 50; tohum++)
            {
                var oyun = Yeni(tohum: tohum);
                var a = SiradakiA(oyun);
                var b = oyun.GorunumAl(a).Secilebilir[0];
                oyun.SoruSor(a, b, "Gizli kalmalı?");

                Saniye(oyun, 20.1);
                var g = Durum(oyun);
                Assert.That(g.Asama, Is.EqualTo(Asama.SoruSorma));
                Assert.That(g.A, Is.Not.EqualTo(a));
                Assert.That(g.A, Is.Not.EqualTo(b));
                foreach (var o in g.Oyuncular)
                {
                    Assert.That(oyun.GorunumAl(o.Id).Soru, Is.Null);
                    Assert.That(oyun.GorunumAl(o.Id).IfsaSoru, Is.Null);
                }
            }
        }

        [TestCase(true, false, false)]   // sadece B oynadı → B kazanır
        [TestCase(false, true, true)]    // sadece C oynadı → C kazanır
        [TestCase(false, false, false)]  // ikisi de oynamadı → B kazanır, soru gizli
        public void Mini_oyunda_hamle_yapmayan_kaybeder(bool bOynar, bool cOynar, bool cKazanir)
        {
            var oyun = Yeni();
            SorVeSec(oyun, "Soru?", out _, out var b, out var c);
            if (bOynar) oyun.MiniOyunHamlesi(b, Hamle.TasKagitMakas(TkmSecim.Tas));
            if (cOynar) oyun.MiniOyunHamlesi(c, Hamle.TasKagitMakas(TkmSecim.Tas));

            Saniye(oyun, 10.1);
            var g = Durum(oyun);
            Assert.That(g.Asama, Is.EqualTo(cKazanir ? Asama.Ifsa : Asama.Gume));
            Assert.That(g.SonMiniOyun.SureDoldu, Is.True);
        }

        [Test]
        public void Kalan_sure_gorunumde_azalir()
        {
            var oyun = Yeni();
            Saniye(oyun, 15);
            Assert.That(Durum(oyun).KalanMs, Is.EqualTo(45000));
        }

        [Test]
        public void Herkes_AFK_olsa_da_oyun_takilmadan_biter()
        {
            var oyun = Yeni(ayarla: x => x.TurSayisi = 10);
            for (int i = 0; i < 100 && Durum(oyun).Asama != Asama.OyunSonu; i++) Saniye(oyun, 61);
            Assert.That(Durum(oyun).Asama, Is.EqualTo(Asama.OyunSonu));
        }

        [Test]
        public void Oyun_sonunda_zaman_bir_sey_degistirmez()
        {
            var oyun = Yeni();
            oyun.OyunuBitir();
            var olaylar = new List<Olay>();
            oyun.OlayOldu += olaylar.Add;
            Saniye(oyun, 1000);
            Assert.That(olaylar, Is.Empty);
            Assert.That(Durum(oyun).KalanMs, Is.EqualTo(0));
        }
    }
}
