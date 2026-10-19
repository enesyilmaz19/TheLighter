using System;
using NUnit.Framework;
using TheLighter.Akis;

namespace TheLighter.Tests
{
    /// <summary>
    /// Android geri tuşu (tasks/g2-android.md 3): ekran ekran ne yaptığı. Açık katmanı kapatır, menüde ana menüye döner,
    /// odada ve oyunda sorar; uygulamayı hiçbir yerde habersiz kapatmaz.
    /// </summary>
    [TestFixture]
    public class GeriTusuTestleri
    {
        [Test]
        public void AcikKatmanVarsa_HerEkrandaOnuKapatir()
        {
            foreach (GeriEkrani e in Enum.GetValues(typeof(GeriEkrani)))
                Assert.AreEqual(GeriEylemi.KatmaniKapat, GeriTusu.Ne(e, true, false), e.ToString());
        }

        [TestCase(GeriEkrani.Kurulum)]
        [TestCase(GeriEkrani.OnlineMenu)]
        [TestCase(GeriEkrani.Guncelle)]
        public void Menude_AnaMenuyeDoner(GeriEkrani e)
        {
            Assert.AreEqual(GeriEylemi.AnaMenuyeDon, GeriTusu.Ne(e, false, false));
        }

        [TestCase(GeriEkrani.Oyun)]
        [TestCase(GeriEkrani.Lobi)]
        [TestCase(GeriEkrani.BeklemeOdasi)]
        public void OdadaVeOyunda_CikisSorar(GeriEkrani e)
        {
            Assert.AreEqual(GeriEylemi.CikisSor, GeriTusu.Ne(e, false, false));
        }

        [TestCase(GeriEkrani.AnaMenu)]
        [TestCase(GeriEkrani.Kabul)]
        public void UygulamaHabersizKapanmaz_Sorar(GeriEkrani e)
        {
            Assert.AreEqual(GeriEylemi.CikisSor, GeriTusu.Ne(e, false, false));
        }

        [Test]
        public void Baglanirken_HicbirSeyYapmaz()
        {
            // "Bağlanıyor…" katmanının kendi "Vazgeç"i var; geri tuşu kazara odadan çıkarmasın.
            foreach (GeriEkrani e in Enum.GetValues(typeof(GeriEkrani)))
            {
                Assert.AreEqual(GeriEylemi.Yok, GeriTusu.Ne(e, false, true), e.ToString());
                Assert.AreEqual(GeriEylemi.Yok, GeriTusu.Ne(e, true, true), e.ToString());
            }
        }

        [Test]
        public void KullanimSartlari_IlkAcilistaVeSurumArtinca()
        {
            Assert.IsTrue(Sartlar.KabulGerekli(0), "hiç kabul edilmedi");
            Assert.IsFalse(Sartlar.KabulGerekli(Sartlar.Surum), "bu sürüm kabul edildi");
            Assert.IsTrue(Sartlar.KabulGerekli(Sartlar.Surum - 1), "şartlar değişti: yeniden sorulur");
        }
    }
}
