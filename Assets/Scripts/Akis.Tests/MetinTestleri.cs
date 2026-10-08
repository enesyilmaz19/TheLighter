using System;
using System.IO;
using NUnit.Framework;
using TheLighter.Akis;
using Cakmak.Kurallar;

namespace TheLighter.Tests
{
    [TestFixture]
    public class MetinTestleri
    {
        [Test]
        public void YonelmeEki()
        {
            Assert.AreEqual("Ali'ye", TurkceEk.Yonelme("Ali"));
            Assert.AreEqual("Ayşe'ye", TurkceEk.Yonelme("Ayşe"));
            Assert.AreEqual("Mehmet'e", TurkceEk.Yonelme("Mehmet"));
            Assert.AreEqual("Burak'a", TurkceEk.Yonelme("Burak"));
            Assert.AreEqual("Umut'a", TurkceEk.Yonelme("Umut"));
            Assert.AreEqual("Gül'e", TurkceEk.Yonelme("Gül"));
        }

        [Test]
        public void BulunmaEki()
        {
            Assert.AreEqual("Ali'de", TurkceEk.Bulunma("Ali"));
            Assert.AreEqual("Mehmet'te", TurkceEk.Bulunma("Mehmet"));
            Assert.AreEqual("Burak'ta", TurkceEk.Bulunma("Burak"));
            Assert.AreEqual("Can'da", TurkceEk.Bulunma("Can"));
            Assert.AreEqual("Elif'te", TurkceEk.Bulunma("Elif"));
        }

        [Test]
        public void IlgiVeBelirtmeEki()
        {
            Assert.AreEqual("Ali'nin", TurkceEk.Ilgi("Ali"));
            Assert.AreEqual("Burak'ın", TurkceEk.Ilgi("Burak"));
            Assert.AreEqual("Umut'un", TurkceEk.Ilgi("Umut"));
            Assert.AreEqual("Gül'ün", TurkceEk.Ilgi("Gül"));
            Assert.AreEqual("Duru'nun", TurkceEk.Ilgi("Duru"));
            Assert.AreEqual("Ali'yi", TurkceEk.Belirtme("Ali"));
            Assert.AreEqual("Mehmet'i", TurkceEk.Belirtme("Mehmet"));
            Assert.AreEqual("Burak'ı", TurkceEk.Belirtme("Burak"));
            Assert.AreEqual("Oğuz'u", TurkceEk.Belirtme("Oğuz"));
        }

        [Test]
        public void BuyukHarfliIsim()
        {
            Assert.AreEqual("IŞIK'a", TurkceEk.Yonelme("IŞIK"));
            Assert.AreEqual("İREM'e", TurkceEk.Yonelme("İREM"));
        }

        /// <summary>Kodda enum adından üretilen anahtarlar ("hata." + kod, "mini.ad." + tür) tabloda var mı.</summary>
        [Test]
        public void MetinTablosunda_EnumAnahtarlariVar()
        {
            string yol = Path.Combine("Assets", "UI", "Resources", "TheLighter", "Metinler.csv");
            Assert.IsTrue(File.Exists(yol), "Bulunamadı: " + yol);
            var t = MetinTablosu.CsvOku(File.ReadAllText(yol));
            foreach (Sonuc k in Enum.GetValues(typeof(Sonuc)))
                Assert.IsTrue(t.Var("hata." + k), "Eksik: hata." + k);
            foreach (MiniOyunTuru m in Enum.GetValues(typeof(MiniOyunTuru)))
            {
                Assert.IsTrue(t.Var("mini.ad." + m), "Eksik: mini.ad." + m);
                Assert.IsTrue(t.Var("mini.kural." + m), "Eksik: mini.kural." + m);
            }
        }

        [Test]
        public void CsvOkunur()
        {
            string csv = "﻿Key,Turkish(tr)\r\nbir,Merhaba\r\niki,\"Virgül, tırnak \"\"x\"\" ve\nsatır\"\r\nuc,{0} {1}\r\n# yorum,yok\r\n";
            var t = MetinTablosu.CsvOku(csv);
            Assert.AreEqual(3, t.Sayi);
            Assert.AreEqual("Merhaba", t.Al("bir"));
            Assert.AreEqual("Virgül, tırnak \"x\" ve\nsatır", t.Al("iki"));
            Assert.AreEqual("Ali Ayşe'ye", t.Al("uc", "Ali", "Ayşe'ye"));
            Assert.AreEqual("[yok]", t.Al("yok"));
        }
    }
}
