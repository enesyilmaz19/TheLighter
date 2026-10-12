using System;
using NUnit.Framework;
using static Cakmak.Kurallar.Tests.Kurulum;

namespace Cakmak.Kurallar.Tests
{
    /// <summary>Oyuncunun yazdığı metnin temizliği ve dışarıdan verilen rastgele kaynak (docs/Guvenlik.md).</summary>
    public class GuvenlikTestleri
    {
        static string K(int kod) => char.ConvertFromUtf32(kod);
        static readonly string SifirGenislik = K(0x200B);
        static readonly string YonCevir = K(0x202E);
        static readonly string HangulDolgu = K(0x3164);
        static readonly string Etiket = K(0xE0041); // BMP dışında, görünmez "A" etiketi

        [Test]
        public void Gorunmez_ve_yon_karakterleri_atilir_bosluklar_tek_olur()
        {
            Assert.That(TemizMetin.Temizle("  Ali" + SifirGenislik + "  " + YonCevir + "Veli\t\n "), Is.EqualTo("Ali Veli"));
            Assert.That(TemizMetin.Temizle("Kim" + Etiket + " en çok uyur?"), Is.EqualTo("Kim en çok uyur?"));
            Assert.That(TemizMetin.Temizle(HangulDolgu + SifirGenislik), Is.Empty);
            Assert.That(TemizMetin.Temizle(null), Is.Empty);
        }

        [TestCase(0x115F)]
        [TestCase(0x1160)]
        [TestCase(0x3164)]
        [TestCase(0xFFA0)]
        [TestCase(0x2800)]
        public void Bos_gorunen_her_karakter_bosluk_sayilir(int kod)
        {
            Assert.That(TemizMetin.Temizle(K(kod)), Is.Empty);
            Assert.That(TemizMetin.Temizle("Ali" + K(kod) + K(kod) + "Veli"), Is.EqualTo("Ali Veli"));
        }

        [Test]
        public void Emoji_ve_Turkce_harfler_kalir()
        {
            var emoji = K(0x1F600);
            Assert.That(TemizMetin.Temizle("Çağrı Işık " + emoji), Is.EqualTo("Çağrı Işık " + emoji));
        }

        [Test]
        public void Gorunmez_karakterle_baskasinin_kiligina_girilemez()
        {
            var liste = Oyuncular(3);
            liste.Add(new Oyuncu(P(8), "Ali"));
            liste.Add(new Oyuncu(P(9), "Al" + SifirGenislik + "i"));
            Assert.That(Oyun.Kur(new Ayarlar(), liste, 1, out _), Is.EqualTo(Sonuc.GecersizGirdi));
        }

        [Test]
        public void Bos_gorunen_isim_kurulmaz()
        {
            var liste = Oyuncular(3);
            liste.Add(new Oyuncu(P(9), HangulDolgu + SifirGenislik));
            Assert.That(Oyun.Kur(new Ayarlar(), liste, 1, out _), Is.EqualTo(Sonuc.GecersizGirdi));
        }

        [Test]
        public void Isim_ve_soru_temizlenmis_haliyle_saklanir_bos_gorunen_soru_sorulamaz()
        {
            var liste = Oyuncular(3);
            liste.Add(new Oyuncu(P(9), YonCevir + "Ayşe "));
            Assert.That(Oyun.Kur(new Ayarlar(), liste, 1, out var oyun), Is.EqualTo(Sonuc.Tamam));
            Assert.That(Durum(oyun).Oyuncular[3].Isim, Is.EqualTo("Ayşe"));

            var a = SiradakiA(oyun);
            var b = oyun.GorunumAl(a).Secilebilir[0];
            Assert.That(oyun.SoruSor(a, b, SifirGenislik + HangulDolgu), Is.EqualTo(Sonuc.GecersizGirdi));
            Assert.That(oyun.SoruSor(a, b, YonCevir + "Kim  en çok uyur?"), Is.EqualTo(Sonuc.Tamam));
            Assert.That(oyun.GorunumAl(b).Soru, Is.EqualTo("Kim en çok uyur?"));
        }

        [Test]
        public void Curcuna_havuzuna_bos_gorunen_soru_eklenemez()
        {
            var oyun = Yeni(ayarla: x => x.Mod = OyunModu.Curcuna);
            Assert.That(oyun.HavuzaSoruEkle(P(1), SifirGenislik + " " + YonCevir), Is.EqualTo(Sonuc.GecersizGirdi));
        }

        /// <summary>Her çağrıyı sayan kaynak: kural motoru verilen kaynağı mı kullanıyor.</summary>
        sealed class SayanRastgele : Random
        {
            public int Cagri;
            public SayanRastgele() : base(1) { }
            public override int Next(int enFazla) { Cagri++; return base.Next(enFazla); }
            public override int Next(int enAz, int enFazla) { Cagri++; return base.Next(enAz, enFazla); }
        }

        [Test]
        public void Disaridan_verilen_rastgele_kaynak_kullanilir()
        {
            var kaynak = new SayanRastgele();
            var ayar = new Ayarlar { GizliGorevler = true, MiniOyun = MiniOyunTuru.Zar };
            Assert.That(Oyun.Kur(ayar, Oyuncular(5), kaynak, out var oyun), Is.EqualTo(Sonuc.Tamam));
            int kurulumda = kaynak.Cagri;
            Assert.That(kurulumda, Is.GreaterThan(0), "görevler ve ilk A bu kaynaktan");

            SorVeSec(oyun, "Kim?", out _, out var b, out var c);
            oyun.MiniOyunHamlesi(b, Hamle.ZarAt());
            oyun.MiniOyunHamlesi(c, Hamle.ZarAt());
            Assert.That(kaynak.Cagri, Is.GreaterThan(kurulumda), "zarlar da bu kaynaktan");

            Assert.That(Oyun.Kur(new Ayarlar(), Oyuncular(5), (Random)null, out _), Is.EqualTo(Sonuc.GecersizGirdi));
        }
    }
}
