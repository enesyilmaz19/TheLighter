using System;
using System.Linq;
using Cakmak.Kurallar;
using NUnit.Framework;
using TheLighter.Ag;

namespace TheLighter.Testler
{
    /// <summary>Sohbetin istemci tarafı: kanallar, okunmamışlar, 200 karakter, saniyede bir mesaj.</summary>
    public class SohbetDefteriTestleri
    {
        static readonly OyuncuId P1 = new OyuncuId(1), P2 = new OyuncuId(2), P3 = new OyuncuId(3);

        [Test]
        public void GenelVeDmAyriKanallar_DmSekmesiAcilir()
        {
            var d = new SohbetDefteri();
            d.Ekle(new SohbetMesaji { Kim = P2, Metin = "selam" });
            d.Ekle(new SohbetMesaji { Kanal = P3, Kim = P3, Metin = "gizli" });
            Assert.That(d.Mesajlar(null).Single().Metin, Is.EqualTo("selam"));
            Assert.That(d.Mesajlar(P3).Single().Metin, Is.EqualTo("gizli"));
            Assert.That(d.Mesajlar(P2), Is.Empty);
            Assert.That(d.DmKanallari, Is.EqualTo(new[] { P3 }));
        }

        [Test]
        public void Okunmamis_BaskasindanGeleniSayar_KendiminiSaymaz()
        {
            var d = new SohbetDefteri();
            d.Ekle(new SohbetMesaji { Kim = P2, Metin = "a" });
            d.Ekle(new SohbetMesaji { Kim = P2, Metin = "b" });
            d.Ekle(new SohbetMesaji { Kim = P1, Metin = "c", Benim = true });
            d.Ekle(new SohbetMesaji { Kanal = P3, Kim = P3, Metin = "d" });
            Assert.That(d.Okunmamis(null), Is.EqualTo(2));
            Assert.That(d.ToplamOkunmamis, Is.EqualTo(3));
            d.Okundu(null);
            Assert.That(d.Okunmamis(null), Is.EqualTo(0));
            Assert.That(d.ToplamOkunmamis, Is.EqualTo(1));
        }

        [Test]
        public void Hazirla_BosVeUzunReddedilir()
        {
            Assert.That(SohbetDefteri.Hazirla("  merhaba  "), Is.EqualTo("merhaba"));
            Assert.That(SohbetDefteri.Hazirla("   "), Is.Null);
            Assert.That(SohbetDefteri.Hazirla(null), Is.Null);
            Assert.That(SohbetDefteri.Hazirla(new string('x', SohbetDefteri.EnFazlaKarakter)), Is.Not.Null);
            Assert.That(SohbetDefteri.Hazirla(new string('x', SohbetDefteri.EnFazlaKarakter + 1)), Is.Null);
        }

        [Test]
        public void HizSiniri_SaniyedeBir()
        {
            var d = new SohbetDefteri();
            Assert.That(d.GonderebilirMi, Is.True);
            d.Gonderildi();
            Assert.That(d.GonderebilirMi, Is.False);
            d.Ilerle(TimeSpan.FromSeconds(0.6));
            Assert.That(d.GonderebilirMi, Is.False);
            d.Ilerle(TimeSpan.FromSeconds(0.5));
            Assert.That(d.GonderebilirMi, Is.True);
        }

        [Test]
        public void KanalBasinaSonYuzMesaj_TemizleHepsiniSiler()
        {
            var d = new SohbetDefteri();
            for (int i = 0; i < SohbetDefteri.KanalBasinaMesaj + 5; i++) d.Ekle(new SohbetMesaji { Kim = P2, Metin = "m" + i });
            Assert.That(d.Mesajlar(null).Count, Is.EqualTo(SohbetDefteri.KanalBasinaMesaj));
            Assert.That(d.Mesajlar(null).First().Metin, Is.EqualTo("m5"));
            d.Temizle();
            Assert.That(d.Mesajlar(null), Is.Empty);
            Assert.That(d.DmKanallari, Is.Empty);
            Assert.That(d.ToplamOkunmamis, Is.EqualTo(0));
        }
    }
}
