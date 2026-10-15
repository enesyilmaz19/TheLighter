using System;
using System.Collections.Generic;
using NUnit.Framework;
using static Cakmak.Kurallar.Tests.Kurulum;

namespace Cakmak.Kurallar.Tests
{
    /// <summary>Takım Düellosu (docs/Oyunlar.md, 3).</summary>
    public class TakimTestleri
    {
        static Oyun YeniTakim(int kisi = 4, int tohum = 1, Action<Ayarlar> ayarla = null) =>
            Yeni(kisi, tohum, a =>
            {
                a.Oyun = OyunTuru.Takim;
                ayarla?.Invoke(a);
            });

        static int Takimi(Oyun oyun, OyuncuId id)
        {
            foreach (var o in Durum(oyun).Oyuncular)
                if (o.Id == id) return o.Takim.Value;
            throw new AssertionException("oyuncu yok");
        }

        static OyuncuId Sec(Oyun oyun, IReadOnlyList<OyuncuId> liste, Func<OyuncuId, bool> uygun)
        {
            foreach (var id in liste)
                if (uygun(id)) return id;
            Assert.Fail("uygun aday yok");
            return default;
        }

        /// <summary>A, istenen takımdan bir B'ye sorar; B istenen takımdan bir C'yi seçer; düello; sonuç ekranında bırakır.</summary>
        static (OyuncuId a, OyuncuId b, OyuncuId c) Tur(Oyun oyun, Func<int, int> bTakimi, Func<int, int> cTakimi, bool cKazansin)
        {
            var a = SiradakiA(oyun);
            int aT = Takimi(oyun, a);
            var b = Sec(oyun, oyun.GorunumAl(a).Secilebilir, id => Takimi(oyun, id) == bTakimi(aT));
            Assert.That(oyun.SoruSor(a, b, "Kim?"), Is.EqualTo(Sonuc.Tamam));
            var c = Sec(oyun, oyun.GorunumAl(b).Secilebilir, id => Takimi(oyun, id) == cTakimi(Takimi(oyun, b)));
            Assert.That(oyun.CevapSec(b, c), Is.EqualTo(Sonuc.Tamam));
            TkmOyna(oyun, b, c, cKazansin);
            return (a, b, c);
        }

        static int Rakip(int t) => 1 - t;
        static int Ayni(int t) => t;

        // ---------------------------------------------------- Kurulum

        [Test]
        public void Takimlar_rastgele_ve_dengeli_tek_sayida_kucuk_takim_bir_puanla_baslar()
        {
            for (int kisi = 4; kisi <= 10; kisi++)
                for (int tohum = 1; tohum <= 5; tohum++)
                {
                    var g = Durum(YeniTakim(kisi, tohum));
                    int ilk = 0;
                    foreach (var o in g.Oyuncular)
                    {
                        Assert.That(o.Takim == 0 || o.Takim == 1, Is.True);
                        if (o.Takim == 0) ilk++;
                    }
                    int ikinci = kisi - ilk;
                    Assert.That(Math.Abs(ilk - ikinci), Is.LessThanOrEqualTo(1), $"{kisi} kişi");
                    var beklenen = ilk == ikinci ? new[] { 0, 0 } : ilk < ikinci ? new[] { 1, 0 } : new[] { 0, 1 };
                    Assert.That(g.TakimPuanlari, Is.EqualTo(beklenen), $"{kisi} kişi");
                    Assert.That(g.Gorevin, Is.Null);
                }
        }

        [Test]
        public void Kurucunun_dagilimi_kullanilir_gecersizi_kurulmaz()
        {
            var dagilim = new Dictionary<OyuncuId, int> { [P(1)] = 1, [P(2)] = 1, [P(3)] = 0, [P(4)] = 0, [P(5)] = 0 };
            var g = Durum(YeniTakim(5, ayarla: a => a.TakimDagilimi = dagilim));
            foreach (var o in g.Oyuncular) Assert.That(o.Takim, Is.EqualTo(dagilim[o.Id]));
            Assert.That(g.TakimPuanlari, Is.EqualTo(new[] { 0, 1 }), "küçük takım (1) 1 puanla başlar");

            var gecersizler = new[]
            {
                new Dictionary<OyuncuId, int> { [P(1)] = 0, [P(2)] = 1, [P(3)] = 0 },                       // eksik
                new Dictionary<OyuncuId, int> { [P(1)] = 0, [P(2)] = 0, [P(3)] = 0, [P(4)] = 1 },           // 3'e 1
                new Dictionary<OyuncuId, int> { [P(1)] = 0, [P(2)] = 2, [P(3)] = 1, [P(4)] = 1 },           // takım 2 yok
                new Dictionary<OyuncuId, int> { [P(1)] = 0, [P(2)] = 0, [P(3)] = 1, [P(9)] = 1 },           // odada olmayan
                new Dictionary<OyuncuId, int> { [P(1)] = 0, [P(2)] = 0, [P(3)] = 1, [P(4)] = 1, [P(9)] = 1 }, // fazladan biri
            };
            foreach (var d in gecersizler)
            {
                var ayar = new Ayarlar { Oyun = OyunTuru.Takim, TakimDagilimi = d };
                Assert.That(Oyun.Kur(ayar, Oyuncular(4), 1, out _), Is.EqualTo(Sonuc.GecersizGirdi));
            }
            Assert.That(Oyun.AyarlarUyumlu(new Ayarlar { Oyun = OyunTuru.Takim, GizliGorevler = true }), Is.False);
            Assert.That(Oyun.AyarlarUyumlu(new Ayarlar { Oyun = OyunTuru.Takim, KaosTurlari = true, CezaSeviyesi = CezaSeviyesi.Cesur }), Is.True);
        }

        [Test]
        public void Cakmak_oyununda_takim_yok()
        {
            var g = Durum(Yeni());
            Assert.That(g.TakimPuanlari, Is.Empty);
            foreach (var o in g.Oyuncular) Assert.That(o.Takim, Is.Null);
        }

        // ---------------------------------------------------- Puan

        [Test]
        public void Farkli_takimdan_duelloyu_kazanan_takim_puan_alir()
        {
            var oyun = YeniTakim();
            var olaylar = new List<Olay>();
            oyun.OlayOldu += olaylar.Add;

            // B, A'nın takımından (bonus olmasın), C rakip takımdan; C kazanır.
            var (_, _, c) = Tur(oyun, Ayni, Rakip, cKazansin: true);
            int cT = Takimi(oyun, c);
            Assert.That(Durum(oyun).TakimPuanlari[cT], Is.EqualTo(1));
            Assert.That(Durum(oyun).TakimPuanlari[Rakip(cT)], Is.EqualTo(0));
            var olay = olaylar.Find(o => o.Tur == OlayTuru.DuelloPuani);
            Assert.That(olay.Kim, Is.EqualTo(c));
            Assert.That(olay.Takim, Is.EqualTo(cT));
            Assert.That(olaylar.Exists(o => o.Tur == OlayTuru.IfsaBonusu), Is.False, "A, kendi takımından birine sordu");
            Saniye(oyun, 7);

            // Bu kez B kazanır (güme): B'nin takımı puan alır.
            olaylar.Clear();
            var (_, b, _) = Tur(oyun, Ayni, Rakip, cKazansin: false);
            Assert.That(olaylar.Find(o => o.Tur == OlayTuru.DuelloPuani).Kim, Is.EqualTo(b));
        }

        [Test]
        public void Ayni_takimdan_B_ile_C_puan_yok_cakmak_takimda_kalir()
        {
            var oyun = YeniTakim();
            var olaylar = new List<Olay>();
            oyun.OlayOldu += olaylar.Add;
            // 2'ye 2'de B ile C aynı takımdansa A rakiptir; güme: ifşa bonusu da olmasın.
            var (_, b, c) = Tur(oyun, Rakip, Ayni, cKazansin: false);
            Assert.That(Takimi(oyun, b), Is.EqualTo(Takimi(oyun, c)));
            Assert.That(olaylar.Exists(o => o.Tur == OlayTuru.DuelloPuani || o.Tur == OlayTuru.IfsaBonusu), Is.False);
            Assert.That(Durum(oyun).TakimPuanlari, Is.EqualTo(new[] { 0, 0 }));
            Saniye(oyun, 7);
            Assert.That(SiradakiA(oyun), Is.EqualTo(c), "sıradaki soruyu C'nin, yani B'nin takımı sorar");
        }

        [Test]
        public void Rakibe_sorulan_soru_ifsa_olursa_soranin_takimina_bonus()
        {
            var oyun = YeniTakim();
            var olaylar = new List<Olay>();
            oyun.OlayOldu += olaylar.Add;
            // B rakip takımdan, C B'nin takımından (düello puanı yok); C kazanır, soru ifşa olur.
            var (a, _, _) = Tur(oyun, Rakip, Ayni, cKazansin: true);
            var bonus = olaylar.Find(o => o.Tur == OlayTuru.IfsaBonusu);
            Assert.That(bonus, Is.Not.Null);
            Assert.That(bonus.Kim, Is.EqualTo(a));
            Assert.That(Durum(oyun).TakimPuanlari[Takimi(oyun, a)], Is.EqualTo(1));
            Assert.That(olaylar.Exists(o => o.Tur == OlayTuru.DuelloPuani), Is.False);
            Saniye(oyun, 7);

            olaylar.Clear();
            Tur(oyun, Rakip, Ayni, cKazansin: false); // güme: bonus yok
            Assert.That(olaylar.Exists(o => o.Tur == OlayTuru.IfsaBonusu), Is.False);
        }

        [Test]
        public void Soran_Gizli_turunda_ifsa_bonusu_yok()
        {
            for (int tohum = 1; tohum <= 300; tohum++)
            {
                var oyun = YeniTakim(6, tohum, a => a.KaosTurlari = true);
                for (int i = 0; i < Oyun.KaosAraligi - 1; i++) { Tur(oyun, Ayni, Ayni, true); Saniye(oyun, 7); }
                if (Durum(oyun).Kaos != KaosKurali.SoranGizli) continue;

                var olaylar = new List<Olay>();
                oyun.OlayOldu += olaylar.Add;
                var a = TekrarYardim.GercekA(oyun);
                int aT = Takimi(oyun, a);
                var b = Sec(oyun, oyun.GorunumAl(a).Secilebilir, id => Takimi(oyun, id) != aT);
                Assert.That(oyun.SoruSor(a, b, "Kim?"), Is.EqualTo(Sonuc.Tamam));
                var c = Sec(oyun, oyun.GorunumAl(b).Secilebilir, id => Takimi(oyun, id) == Takimi(oyun, b));
                Assert.That(oyun.CevapSec(b, c), Is.EqualTo(Sonuc.Tamam));
                TkmOyna(oyun, b, c, cKazansin: true);
                Assert.That(Durum(oyun).Asama, Is.EqualTo(Asama.Ifsa));
                Assert.That(olaylar.Exists(o => o.Tur == OlayTuru.IfsaBonusu), Is.False, "puan A'nın takımını ele verirdi");
                return;
            }
            Assert.Fail("Soran Gizli turu bulunamadı");
        }

        // ---------------------------------------------------- Kazanma

        [Test]
        public void Yedi_puana_ulasan_takim_turun_sonucundan_sonra_kazanir()
        {
            // Her tur: B 0. takımdan, C 1. takımdan, B kazanır (güme, bonus yok) → 0. takıma tam 1 puan.
            var oyun = YeniTakim();
            for (int tur = 1; tur <= Oyun.HedefPuan; tur++)
            {
                Tur(oyun, _ => 0, _ => 1, cKazansin: false);
                Assert.That(Durum(oyun).TakimPuanlari[0], Is.EqualTo(tur));
                Assert.That(Durum(oyun).Asama, Is.EqualTo(Asama.Gume), "turun sonucu yine gösterilir");
                Saniye(oyun, 7);
                if (tur < Oyun.HedefPuan) Assert.That(Durum(oyun).Asama, Is.EqualTo(Asama.SoruSorma), tur + " puanda bitmez");
            }
            Assert.That(Durum(oyun).Asama, Is.EqualTo(Asama.OyunSonu));
            var sonuc = oyun.Ozet().Takim;
            Assert.That(sonuc.Kazanan, Is.EqualTo(0));
            Assert.That(sonuc.Puanlar, Is.EqualTo(new[] { Oyun.HedefPuan, 0 }));
        }

        [Test]
        public void Tur_sayisi_bitince_onde_olan_esitlikte_berabere()
        {
            var esit = YeniTakim(ayarla: a => a.TurSayisi = 1);
            Tur(esit, Rakip, Ayni, false); // aynı takım düellosu, güme: puan yok
            Saniye(esit, 7);
            Assert.That(Durum(esit).Asama, Is.EqualTo(Asama.OyunSonu));
            Assert.That(esit.Ozet().Takim.Kazanan, Is.Null);
            Assert.That(esit.Ozet().Takim.Puanlar, Is.EqualTo(new[] { 0, 0 }));

            var onde = YeniTakim(ayarla: a => a.TurSayisi = 1);
            var (_, _, c) = Tur(onde, Ayni, Rakip, true);
            Saniye(onde, 7);
            Assert.That(onde.Ozet().Takim.Kazanan, Is.EqualTo(Takimi(onde, c)));
        }

        [Test]
        public void Kurucu_bitirince_onde_olan_kazanir()
        {
            var oyun = YeniTakim();
            var (_, _, c) = Tur(oyun, Ayni, Rakip, true);
            Assert.That(oyun.OyunuBitir(), Is.EqualTo(Sonuc.Tamam));
            Assert.That(oyun.Ozet().Takim.Kazanan, Is.EqualTo(Takimi(oyun, c)));
        }
    }
}
