using System.Collections.Generic;
using Cakmak.Kurallar;
using NUnit.Framework;
using TheLighter.Akis;

namespace TheLighter.Tests
{
    /// <summary>
    /// Takım dağıtma ekranının modeli (tasks/g2-takim.md 4): dokununca takım değişiyor, lobiden çıkan düşüyor,
    /// dengesiz ya da eksik dağılımı kural motorunun kurulumu reddediyor (ekranda uyarı, "Başlat" kapalı).
    /// </summary>
    [TestFixture]
    public class TakimDagitimiTestleri
    {
        static readonly OyuncuId P1 = new OyuncuId(1), P2 = new OyuncuId(2), P3 = new OyuncuId(3), P4 = new OyuncuId(4), P5 = new OyuncuId(5);

        static List<OyuncuId> Kisiler(int n)
        {
            var l = new List<OyuncuId>();
            for (int i = 1; i <= n; i++) l.Add(new OyuncuId(i));
            return l;
        }

        [Test]
        public void Bos_Rastgele_DortKisiyleGecerli()
        {
            var d = new TakimDagitimi();
            Assert.IsTrue(d.Rastgele);
            Assert.IsNull(d.Kopya(), "rastgele: oyuna dağılım gitmez");
            Assert.AreEqual(true, d.Gecerli(Kisiler(4)));
            Assert.AreEqual(4, d.Takimsizlar(Kisiler(4)).Count);
        }

        [Test]
        public void Dokun_TakimsizKucukTakima_TakimdakiOburune()
        {
            var d = new TakimDagitimi();
            d.Dokun(P1);
            Assert.AreEqual(0, d.Takimi(P1), "eşitken ilk takım");
            d.Dokun(P2);
            Assert.AreEqual(1, d.Takimi(P2), "küçük takıma");
            d.Dokun(P1);
            Assert.AreEqual(1, d.Takimi(P1), "takımdaki öbür takıma geçer");
            d.Dokun(P1);
            Assert.AreEqual(0, d.Takimi(P1));
            Assert.IsFalse(d.Rastgele);
            d.Temizle();
            Assert.IsTrue(d.Rastgele, "Rastgele düğmesi dağılımı siler");
        }

        [Test]
        public void DengesizYaDaEksik_Gecersiz_DengeliGecerli()
        {
            var herkes = Kisiler(4);
            var d = new TakimDagitimi(new Dictionary<OyuncuId, int> { [P1] = 0, [P2] = 0, [P3] = 0, [P4] = 1 });
            Assert.AreEqual(false, d.Gecerli(herkes), "3-1 dengesiz");
            d.Dokun(P3);
            Assert.AreEqual(true, d.Gecerli(herkes), "2-2");
            Assert.AreEqual(new[] { P1, P2 }, d.Uyeler(herkes, 0).ToArray());
            Assert.AreEqual(new[] { P3, P4 }, d.Uyeler(herkes, 1).ToArray());

            var bes = Kisiler(5);
            Assert.AreEqual(false, d.Gecerli(bes), "yeni gelen takımsız: eksik");
            Assert.AreEqual(new[] { P5 }, d.Takimsizlar(bes).ToArray());
            d.Dokun(P5);
            Assert.AreEqual(true, d.Gecerli(bes), "3-2: tek kişi fark olabilir");
        }

        [Test]
        public void KisiSayisiYetmezse_Bilinmiyor()
        {
            var d = new TakimDagitimi(new Dictionary<OyuncuId, int> { [P1] = 0, [P2] = 0, [P3] = 0 });
            Assert.IsNull(d.Gecerli(Kisiler(3)), "3 kişiyle oyun kurulmaz: dağılım için uyarı yok");
        }

        [Test]
        public void LobidenCikan_DagilimdanDuser()
        {
            var d = new TakimDagitimi(new Dictionary<OyuncuId, int> { [P1] = 0, [P2] = 1, [P3] = 0, [P4] = 1, [P5] = 1 });
            var kalan = new List<OyuncuId> { P1, P2, P3, P4 };
            Assert.IsTrue(d.Esitle(kalan));
            Assert.IsNull(d.Takimi(P5));
            Assert.AreEqual(4, d.Dagilim.Count);
            Assert.AreEqual(true, d.Gecerli(kalan));
            Assert.IsFalse(d.Esitle(kalan), "ikinci kez değişmez");
        }

        [Test]
        public void Ayarla_GecersizTakimNumarasiAlinmaz()
        {
            var d = new TakimDagitimi(new Dictionary<OyuncuId, int> { [P1] = 0, [P2] = 2, [P3] = -1 });
            Assert.AreEqual(1, d.Dagilim.Count);
            d.Koy(P2, 5);
            Assert.IsNull(d.Takimi(P2));
        }
    }
}
