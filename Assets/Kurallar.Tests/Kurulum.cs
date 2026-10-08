using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Cakmak.Kurallar.Tests
{
    /// <summary>Testlerin ortak yardımcıları. Oyuncular p1..pN.</summary>
    static class Kurulum
    {
        public static OyuncuId P(int n) => new OyuncuId(n);

        public static List<Oyuncu> Oyuncular(int kisi)
        {
            var liste = new List<Oyuncu>();
            for (int i = 1; i <= kisi; i++) liste.Add(new Oyuncu(P(i), "Oyuncu" + i));
            return liste;
        }

        public static Oyun Yeni(int kisi = 5, int tohum = 1, Action<Ayarlar> ayarla = null)
        {
            var ayar = new Ayarlar();
            ayarla?.Invoke(ayar);
            var sonuc = Oyun.Kur(ayar, Oyuncular(kisi), tohum, out var oyun);
            Assert.That(sonuc, Is.EqualTo(Sonuc.Tamam));
            return oyun;
        }

        /// <summary>Herhangi bir oyuncunun gözünden ortak durum.</summary>
        public static Gorunum Durum(Oyun oyun) => oyun.GorunumAl(P(1));

        public static OyuncuId SiradakiA(Oyun oyun) => Durum(oyun).A.Value;

        /// <summary>A'dan farklı ilk oyuncuyu B, ikisinden farklı ilkini C seçer.</summary>
        public static void SorVeSec(Oyun oyun, string soru, out OyuncuId a, out OyuncuId b, out OyuncuId c)
        {
            a = SiradakiA(oyun);
            b = oyun.GorunumAl(a).Secilebilir[0];
            Assert.That(oyun.SoruSor(a, b, soru), Is.EqualTo(Sonuc.Tamam));
            c = oyun.GorunumAl(b).Secilebilir[0];
            Assert.That(oyun.CevapSec(b, c), Is.EqualTo(Sonuc.Tamam));
        }

        public static void Saniye(Oyun oyun, double sn) => oyun.Ilerle(TimeSpan.FromSeconds(sn));

        /// <summary>TKM'de C kazansın (ifşa) ya da B kazansın (güme).</summary>
        public static void TkmOyna(Oyun oyun, OyuncuId b, OyuncuId c, bool cKazansin)
        {
            // C hep kağıt: B taş derse C kazanır, B makas derse B kazanır.
            var bS = cKazansin ? TkmSecim.Tas : TkmSecim.Makas;
            Assert.That(oyun.MiniOyunHamlesi(b, Hamle.TasKagitMakas(bS)), Is.EqualTo(Sonuc.Tamam));
            Assert.That(oyun.MiniOyunHamlesi(c, Hamle.TasKagitMakas(TkmSecim.Kagit)), Is.EqualTo(Sonuc.Tamam));
        }
    }
}
