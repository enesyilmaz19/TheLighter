using NUnit.Framework;
using static Cakmak.Kurallar.Tests.Kurulum;

namespace Cakmak.Kurallar.Tests
{
    public class KurulumTestleri
    {
        [TestCase(3)]
        [TestCase(11)]
        public void OyuncuSayisi_4_ile_10_disinda_kurulmaz(int kisi)
        {
            var sonuc = Oyun.Kur(new Ayarlar(), Oyuncular(kisi), 1, out var oyun);
            Assert.That(sonuc, Is.EqualTo(Sonuc.OyuncuSayisiGecersiz));
            Assert.That(oyun, Is.Null);
        }

        [TestCase(4)]
        [TestCase(10)]
        public void OyuncuSayisi_sinirlarda_kurulur(int kisi)
        {
            Assert.That(Oyun.Kur(new Ayarlar(), Oyuncular(kisi), 1, out _), Is.EqualTo(Sonuc.Tamam));
        }

        [Test]
        public void Ayni_kimlik_iki_kez_kurulmaz()
        {
            var liste = Oyuncular(4);
            liste.Add(new Oyuncu(P(1), "Kopya"));
            Assert.That(Oyun.Kur(new Ayarlar(), liste, 1, out _), Is.EqualTo(Sonuc.GecersizGirdi));
        }

        [Test]
        public void Bos_isim_kurulmaz()
        {
            var liste = Oyuncular(4);
            liste.Add(new Oyuncu(P(9), "  "));
            Assert.That(Oyun.Kur(new Ayarlar(), liste, 1, out _), Is.EqualTo(Sonuc.GecersizGirdi));
        }

        [Test]
        public void Normal_modda_oyun_soru_sormayla_baslar()
        {
            var g = Durum(Yeni());
            Assert.That(g.Asama, Is.EqualTo(Asama.SoruSorma));
            Assert.That(g.Tur, Is.EqualTo(1));
            Assert.That(g.A.HasValue, Is.True);
            Assert.That(g.KalanMs, Is.EqualTo(60000));
        }

        [Test]
        public void Oyunda_olmayanin_gorunumu_yok()
        {
            Assert.That(Yeni().GorunumAl(P(99)), Is.Null);
        }

        [Test]
        public void Paket_bos_ve_yorum_satirlarini_atlar()
        {
            var sorular = Paket.Ayristir("# Paket: Okul\n\nBirinci soru?\r\n  # yorum\n  İkinci soru?  \n");
            Assert.That(sorular, Is.EqualTo(new[] { "Birinci soru?", "İkinci soru?" }));
        }

        [Test]
        public void Paket_bos_metinde_bos_liste()
        {
            Assert.That(Paket.Ayristir(null), Is.Empty);
            Assert.That(Paket.Ayristir(""), Is.Empty);
        }
    }
}
