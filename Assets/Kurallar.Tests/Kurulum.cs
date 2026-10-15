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

        /// <summary>Listede, verilenler dışındaki ilk oyuncu.</summary>
        public static OyuncuId Ilk(IReadOnlyList<OyuncuId> liste, params OyuncuId[] haric)
        {
            foreach (var x in liste)
                if (Array.IndexOf(haric, x) < 0) return x;
            Assert.Fail("uygun aday yok");
            return default;
        }

        /// <summary>Verilen kişi (köstebek, casus) ne B ne C olur. A sorar, C kazanır, sonuç ekranı biter.</summary>
        public static void OnsuzTur(Oyun oyun, OyuncuId kim)
        {
            var a = SiradakiA(oyun);
            var b = Ilk(oyun.GorunumAl(a).Secilebilir, kim);
            Assert.That(oyun.SoruSor(a, b, "Kim en çok uyur?"), Is.EqualTo(Sonuc.Tamam));
            var c = Ilk(oyun.GorunumAl(b).Secilebilir, kim);
            Assert.That(oyun.CevapSec(b, c), Is.EqualTo(Sonuc.Tamam));
            TkmOyna(oyun, b, c, cKazansin: true);
            Saniye(oyun, 7);
        }

        /// <summary>"Kim?" oylamasında herkes sırayla oy verir (oylama biterse durur).</summary>
        public static void HerkesOyVerir(Oyun oyun, Func<OyuncuId, OyuncuId> kime, int kisi = 5)
        {
            for (int i = 1; i <= kisi && Durum(oyun).Asama == Asama.Suclama; i++)
                Assert.That(oyun.SuclamaOyu(P(i), kime(P(i))), Is.EqualTo(Sonuc.Tamam));
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
