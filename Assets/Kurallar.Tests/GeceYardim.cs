using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using static Cakmak.Kurallar.Tests.Kurulum;

namespace Cakmak.Kurallar.Tests
{
    /// <summary>Çakmak Gecesi testlerinin ortak yardımcıları. Her test istemediği özel rolleri kapatır: oyundaki roller belli olur.</summary>
    static class GeceYardim
    {
        static readonly Rol[] OzelRoller = Oyun.GeceRolleri.Where(r => r != Rol.Masum && r != Rol.Katil).ToArray();

        /// <summary>Sadece <paramref name="acik"/> özel rolleri açık bir oyun.</summary>
        public static Oyun YeniGece(int kisi, Rol[] acik, Action<Ayarlar> ayarla = null, int tohum = 1) =>
            Yeni(kisi, tohum, a =>
            {
                a.Oyun = OyunTuru.CakmakGecesi;
                a.KapaliRoller = OzelRoller.Where(r => !acik.Contains(r)).ToList();
                ayarla?.Invoke(a);
            });

        public static Oyun YeniGece(int kisi, params Rol[] acik) => YeniGece(kisi, acik, null);

        public static List<OyuncuDurumu> Kisiler(Oyun oyun) => Durum(oyun).Oyuncular.ToList();

        public static OyuncuDurumu Kisi(Oyun oyun, OyuncuId id) => Kisiler(oyun).Find(o => o.Id == id);

        /// <summary>Kendini bu rolde bilenler (Deli sandığı rolde görünür).</summary>
        public static List<OyuncuId> Sahipleri(Oyun oyun, Rol rol) =>
            Kisiler(oyun).Where(o => oyun.GorunumAl(o.Id).Rolun == rol).Select(o => o.Id).ToList();

        public static OyuncuId Tek(Oyun oyun, Rol rol) => Sahipleri(oyun, rol).Single();

        public static List<OyuncuId> Canlilar(Oyun oyun) => Kisiler(oyun).Where(o => !o.Elendi).Select(o => o.Id).ToList();

        /// <summary>Gündüzün kalan turlarını oynar (C kazanır), linç başlayınca durur.</summary>
        public static int GunduzuBitir(Oyun oyun)
        {
            int tur = 0;
            while (Durum(oyun).Asama != Asama.Linc)
            {
                Assert.That(Durum(oyun).Asama, Is.EqualTo(Asama.SoruSorma));
                SorVeSec(oyun, "Sence katil kim?", out _, out var b, out var c);
                TkmOyna(oyun, b, c, cKazansin: true);
                TekrarYardim.KarariGec(oyun); // ceza açıksa bedel sorusu
                Saniye(oyun, 7);
                tur++;
            }
            return tur;
        }

        /// <summary>Gündüzü bitirir, linçte kimse oy vermez: gece başlar.</summary>
        public static void GeceyeGec(Oyun oyun)
        {
            GunduzuBitir(oyun);
            Saniye(oyun, 61);
            Assert.That(Durum(oyun).Asama, Is.EqualTo(Asama.Gece));
        }

        /// <summary>Linçte herkes <paramref name="hedef"/>'e oy verir, hedef kendisi başkasına.</summary>
        public static void Linc(Oyun oyun, OyuncuId hedef)
        {
            var oylayanlar = Durum(oyun).Oylayanlar.ToList();
            foreach (var kim in oylayanlar)
                Assert.That(oyun.LincOyu(kim, kim == hedef ? Ilk(oylayanlar, hedef) : hedef), Is.EqualTo(Sonuc.Tamam));
        }

        /// <summary>Verilen seçimler, sonra diğer herkes pas. Gece biter.</summary>
        public static void Gece(Oyun oyun, params (OyuncuId kim, OyuncuId? hedef, OyuncuId? ikinci)[] secimler)
        {
            foreach (var (kim, hedef, ikinci) in secimler)
                Assert.That(oyun.GeceSecimi(kim, hedef, ikinci), Is.EqualTo(Sonuc.Tamam), kim + " seçemedi");
            foreach (var kim in Durum(oyun).Oylayanlar.ToList())
            {
                if (Durum(oyun).Asama != Asama.Gece) break; // son seçenle gece bitti
                if (!oyun.GorunumAl(kim).OyVerdin) Assert.That(oyun.GeceSecimi(kim, null), Is.EqualTo(Sonuc.Tamam));
            }
            Assert.That(Durum(oyun).Asama, Is.Not.EqualTo(Asama.Gece));
        }

        public static List<OyuncuId> Olenler(List<Olay> olaylar) => olaylar.Where(o => o.Tur == OlayTuru.Elendi).Select(o => o.Kim.Value).ToList();

        /// <summary>Gündüz turuna gelene kadar ilerler: linçte kimse oy vermez, gece herkes pas geçer (kimse ölmez).</summary>
        public static void GunduzTurunaGel(Oyun oyun)
        {
            for (int i = 0; i < 10 && Durum(oyun).Asama != Asama.SoruSorma; i++)
            {
                if (Durum(oyun).Asama == Asama.Gece) Gece(oyun);
                else Saniye(oyun, 61);
            }
            Assert.That(Durum(oyun).Asama, Is.EqualTo(Asama.SoruSorma));
        }

        /// <summary>
        /// Bir tur: sıradaki A, <paramref name="b"/>'ye sorar, B <paramref name="c"/>'yi seçer, TKM'de C kazanır ya da kaybeder, sonuç ekranı
        /// biter. Tur düello oynanmadan bittiyse (Kuklacı seçti, Söndürücü, Hakem) olduğu kadar.
        /// </summary>
        public static void Tur(Oyun oyun, OyuncuId b, OyuncuId c, bool cKazansin, string soru = "Sence katil kim?")
        {
            GunduzTurunaGel(oyun);
            var a = SiradakiA(oyun);
            Assert.That(oyun.SoruSor(a, b, soru), Is.EqualTo(Sonuc.Tamam));
            if (Durum(oyun).Asama == Asama.CevapSecme) Assert.That(oyun.CevapSec(b, c), Is.EqualTo(Sonuc.Tamam));
            if (Durum(oyun).Asama == Asama.MiniOyun) TkmOyna(oyun, b, Durum(oyun).C.Value, cKazansin);
            TekrarYardim.KarariGec(oyun);
            Saniye(oyun, 7);
        }

        /// <summary>Sıradaki A <paramref name="kim"/> olsun: değilse onu C seçen bir tur oynar (sıradaki A, C olur).</summary>
        public static void SiraVer(Oyun oyun, OyuncuId kim, params OyuncuId[] karismasin)
        {
            GunduzTurunaGel(oyun);
            var a = SiradakiA(oyun);
            if (a == kim) return;
            var b = Canlilar(oyun).First(x => x != a && x != kim && !karismasin.Contains(x));
            Tur(oyun, b, kim, cKazansin: true);
            GunduzTurunaGel(oyun);
            Assert.That(SiradakiA(oyun), Is.EqualTo(kim));
        }
    }
}
