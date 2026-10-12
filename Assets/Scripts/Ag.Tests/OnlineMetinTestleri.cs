using System.IO;
using NUnit.Framework;
using TheLighter.Ag;
using TheLighter.Akis;

namespace TheLighter.Testler
{
    /// <summary>Sunucudan gelebilecek her kodun ve oturumun çıkış nedenlerinin oyuncuya görünen metni var mı.</summary>
    public class OnlineMetinTestleri
    {
        static MetinTablosu Tablo()
        {
            string yol = Path.Combine("Assets", "UI", "Resources", "TheLighter", "Metinler.csv");
            Assert.IsTrue(File.Exists(yol), "Bulunamadı: " + yol);
            return MetinTablosu.CsvOku(File.ReadAllText(yol));
        }

        [Test]
        public void SunucuHataKodlarininMetniVar()
        {
            var t = Tablo();
            foreach (var kod in Cevirici.SunucuKodlari)
            {
                Assert.That(Cevirici.SonucKodu(kod), Is.Null, kod + " kural motorunun kodu olmamalı");
                Assert.IsTrue(t.Var("hata." + kod), "Eksik: hata." + kod);
            }
        }

        [Test]
        public void CikisNedenlerininMetniVar()
        {
            var t = Tablo();
            Assert.IsTrue(t.Var("online.cikis." + OnlineIstemci.NedenBaglanamadi));
            Assert.IsTrue(t.Var("online.cikis." + OnlineIstemci.NedenAyrildin));
        }
    }
}
