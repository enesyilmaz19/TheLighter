using System;
using System.Collections.Generic;
using NUnit.Framework;
using static Cakmak.Kurallar.Tests.Kurulum;

namespace Cakmak.Kurallar.Tests
{
    /// <summary>Casus (docs/Oyunlar.md, 4). Ortak oylamanın kuralları KostebekTestleri'nde.</summary>
    public class CasusTestleri
    {
        static Oyun YeniCasus(int kisi = 5, int tohum = 1, Action<Ayarlar> ayarla = null) =>
            Yeni(kisi, tohum, a =>
            {
                a.Oyun = OyunTuru.Casus;
                ayarla?.Invoke(a);
            });

        static OyuncuId CasusKim(Oyun oyun, int kisi = 5)
        {
            var bulunan = new List<OyuncuId>();
            for (int i = 1; i <= kisi; i++)
                if (oyun.GorunumAl(P(i)).Rolun == Rol.Casus) bulunan.Add(P(i));
            Assert.That(bulunan.Count, Is.EqualTo(1), "tam bir casus");
            return bulunan[0];
        }

        static string Sir(Oyun oyun, OyuncuId casus) => oyun.GorunumAl(Ilk(new[] { P(1), P(2) }, casus)).Sirrin;

        static string YanlisTahmin(Oyun oyun, OyuncuId casus) =>
            oyun.GorunumAl(casus).CasusListesi[0] == Sir(oyun, casus)
                ? oyun.GorunumAl(casus).CasusListesi[1] : oyun.GorunumAl(casus).CasusListesi[0];

        /// <summary>Son oylama, herkes casusu gösterir (casus başkasını): son şansı başlar.</summary>
        static void Yakala(Oyun oyun, OyuncuId casus)
        {
            Assert.That(oyun.OyunuBitir(), Is.EqualTo(Sonuc.Tamam));
            HerkesOyVerir(oyun, kim => kim == casus ? Ilk(new[] { P(1), P(2) }, casus) : casus);
            Assert.That(Durum(oyun).Asama, Is.EqualTo(Asama.CasusTahmini));
        }

        // ---------------------------------------------------- Kurulum ve gizlilik

        [Test]
        public void Mekanda_herkes_mekani_ve_ayri_rolunu_gorur_casus_sadece_rolunu()
        {
            for (int tohum = 1; tohum <= 30; tohum++)
            {
                var oyun = YeniCasus(Oyun.EnFazlaOyuncu, tohum);
                var casus = CasusKim(oyun, Oyun.EnFazlaOyuncu);
                var mekan = Sir(oyun, casus);
                var roller = new HashSet<string>();
                for (int i = 1; i <= Oyun.EnFazlaOyuncu; i++)
                {
                    var g = oyun.GorunumAl(P(i));
                    Assert.That(g.Oyun, Is.EqualTo(OyunTuru.Casus));
                    Assert.That(g.CasusListesi.Count, Is.EqualTo(CasusIcerik.Mekanlar.Length), "bütün mekânlar herkeste");
                    Assert.That(g.CasusKategori, Is.Null);
                    Assert.That(g.Gorevin, Is.Null);
                    Assert.That(g.SuclamaHakkin, Is.False, "erken suçlama sadece Köstebek'te");
                    if (P(i) == casus)
                    {
                        Assert.That(g.Sirrin, Is.Null, "casus mekânı bilmez");
                        Assert.That(g.MekanRolun, Is.Null);
                        continue;
                    }
                    Assert.That(g.Rolun, Is.EqualTo(Rol.Yok));
                    Assert.That(g.Sirrin, Is.EqualTo(mekan));
                    Assert.That(roller.Add(g.MekanRolun), Is.True, "herkese ayrı rol: " + g.MekanRolun);
                }
                Assert.That(oyun.GorunumAl(casus).CasusListesi, Has.Member(mekan));
                Assert.That(Array.Exists(CasusIcerik.Mekanlar, m => m.Ad == mekan && roller.IsSubsetOf(m.Ogeler)), Is.True, "roller o mekânın");
                Assert.That(oyun.Ozet().Casus, Is.Null, "oyun bitmeden özette casus yok");
            }
        }

        [Test]
        public void Kelimede_kategori_herkese_kelime_casus_disindakilere_rol_yok()
        {
            var oyun = YeniCasus(ayarla: a => a.Casus = CasusTuru.Kelime);
            var casus = CasusKim(oyun);
            var g = Durum(oyun);
            var grup = Array.Find(CasusIcerik.Kelimeler, k => k.Ad == g.CasusKategori);
            Assert.That(grup, Is.Not.Null);
            Assert.That(g.CasusListesi, Is.EqualTo(grup.Ogeler));
            var kelime = Sir(oyun, casus);
            Assert.That(grup.Ogeler, Has.Member(kelime));
            for (int i = 1; i <= 5; i++)
            {
                var x = oyun.GorunumAl(P(i));
                Assert.That(x.MekanRolun, Is.Null);
                Assert.That(x.CasusKategori, Is.EqualTo(grup.Ad));
                Assert.That(x.Sirrin, Is.EqualTo(P(i) == casus ? null : kelime));
            }
        }

        [Test]
        public void Casusta_curcuna_gorev_ve_kaos_kurulamaz_ceza_olur()
        {
            foreach (Action<Ayarlar> bozuk in new Action<Ayarlar>[]
                     { a => a.Mod = OyunModu.Curcuna, a => a.GizliGorevler = true, a => a.KaosTurlari = true })
            {
                var ayar = new Ayarlar { Oyun = OyunTuru.Casus };
                bozuk(ayar);
                Assert.That(Oyun.Kur(ayar, Oyuncular(5), 1, out _), Is.EqualTo(Sonuc.GecersizGirdi));
            }
            YeniCasus(ayarla: a => { a.Mod = OyunModu.Temiz; a.CezaSeviyesi = CezaSeviyesi.Hafif; });
        }

        [Test]
        public void Gorunum_listesi_kopya()
        {
            var oyun = YeniCasus();
            var liste = (List<string>)Durum(oyun).CasusListesi;
            liste.Clear();
            Assert.That(Durum(oyun).CasusListesi, Is.Not.Empty);
        }

        // ---------------------------------------------------- Oylama ve son şans

        [Test]
        public void Her_4_turda_casus_kim_oylamasi()
        {
            var oyun = YeniCasus();
            var casus = CasusKim(oyun);
            for (int i = 0; i < Oyun.CasusSuclamaAraligi; i++)
            {
                Assert.That(Durum(oyun).Asama, Is.EqualTo(Asama.SoruSorma), "tur " + (i + 1));
                OnsuzTur(oyun, casus);
            }
            var d = Durum(oyun);
            Assert.That(d.Asama, Is.EqualTo(Asama.Suclama));
            Assert.That(d.Tur, Is.EqualTo(5));
            Assert.That(d.SonSuclama, Is.False);
            Assert.That(oyun.Sucla(P(1)), Is.EqualTo(Sonuc.YanlisAsama), "erken suçlama yok");

            // Yanlış kişi: şüpheli olur, oyun sürer.
            var masum = Ilk(new[] { P(1), P(2) }, casus);
            HerkesOyVerir(oyun, kim => kim == masum ? casus : masum);
            d = Durum(oyun);
            Assert.That(d.Asama, Is.EqualTo(Asama.SoruSorma));
            Assert.That(d.Supheliler, Is.EqualTo(new[] { masum }));
        }

        [Test]
        public void Yakalanan_casus_herkese_acilir_son_sansta_bilirse_kazanir()
        {
            var oyun = YeniCasus();
            var casus = CasusKim(oyun);
            var olaylar = new List<Olay>();
            oyun.OlayOldu += olaylar.Add;
            Yakala(oyun, casus);
            Assert.That(olaylar.Exists(o => o.Tur == OlayTuru.CasusYakalandi && o.Kim == casus), Is.True);
            for (int i = 1; i <= 5; i++) Assert.That(oyun.GorunumAl(P(i)).Yakalanan, Is.EqualTo(casus));

            var masum = Ilk(new[] { P(1), P(2) }, casus);
            Assert.That(oyun.CasusTahmini(masum, Sir(oyun, casus)), Is.EqualTo(Sonuc.SiraSendeDegil));
            Assert.That(oyun.CasusTahmini(casus, Sir(oyun, casus)), Is.EqualTo(Sonuc.Tamam));
            Assert.That(olaylar.Exists(o => o.Tur == OlayTuru.CasusTahminEtti && o.Kim == casus), Is.True);
            var s = oyun.Ozet().Casus;
            Assert.That(s.Casus, Is.EqualTo(casus));
            Assert.That(s.Kazanan, Is.EqualTo(CasusKazanan.Casus));
            Assert.That(s.Neden, Is.EqualTo(CasusNedeni.SonSanstaBildi));
            Assert.That(s.Tahmin, Is.EqualTo(s.Sir));
            Assert.That(oyun.CasusTahmini(casus, s.Sir), Is.EqualTo(Sonuc.YanlisAsama), "oyun bitti");
        }

        [Test]
        public void Son_sansta_bilemezse_ya_da_suresi_dolarsa_grup_kazanir()
        {
            var oyun = YeniCasus();
            var casus = CasusKim(oyun);
            Yakala(oyun, casus);
            var yanlis = YanlisTahmin(oyun, casus);
            Assert.That(oyun.CasusTahmini(casus, yanlis), Is.EqualTo(Sonuc.Tamam));
            var s = oyun.Ozet().Casus;
            Assert.That((s.Kazanan, s.Neden, s.Tahmin), Is.EqualTo((CasusKazanan.Grup, CasusNedeni.Yakalandi, yanlis)));

            oyun = YeniCasus();
            casus = CasusKim(oyun);
            Yakala(oyun, casus);
            Saniye(oyun, Oyun.CasusTahminiSn - 1);
            Assert.That(Durum(oyun).Asama, Is.EqualTo(Asama.CasusTahmini));
            Saniye(oyun, 2);
            Assert.That(Durum(oyun).Asama, Is.EqualTo(Asama.OyunSonu));
            s = oyun.Ozet().Casus;
            Assert.That((s.Kazanan, s.Neden, s.Tahmin), Is.EqualTo((CasusKazanan.Grup, CasusNedeni.Yakalandi, (string)null)));
        }

        [Test]
        public void Son_oylamada_yakalanmazsa_casus_kazanir_zorla_bitince_de()
        {
            var oyun = YeniCasus(ayarla: a => a.TurSayisi = 1);
            var casus = CasusKim(oyun);
            OnsuzTur(oyun, casus);
            Assert.That(Durum(oyun).SonSuclama, Is.True);
            Saniye(oyun, 61);
            var s = oyun.Ozet().Casus;
            Assert.That((s.Kazanan, s.Neden), Is.EqualTo((CasusKazanan.Casus, CasusNedeni.Yakalanmadi)));
            Assert.That(s.Sir, Is.Not.Null);

            oyun = YeniCasus();
            Assert.That(oyun.OyunuBitir(), Is.EqualTo(Sonuc.Tamam));
            Assert.That(oyun.OyunuBitir(), Is.EqualTo(Sonuc.YanlisAsama), "kurucu casussa ikinci bitirle kazanamasın");
            Assert.That(oyun.OyunuBitir(zorla: true), Is.EqualTo(Sonuc.Tamam));
            Assert.That(oyun.Ozet().Casus.Neden, Is.EqualTo(CasusNedeni.Yakalanmadi));
        }

        // ---------------------------------------------------- Yakalanmadan tahmin

        [Test]
        public void Casus_istedigi_an_tahmin_eder_bilirse_kazanir()
        {
            var oyun = YeniCasus();
            var casus = CasusKim(oyun);
            var masum = Ilk(new[] { P(1), P(2) }, casus);
            // Düellonun ortasında bile.
            var a = SiradakiA(oyun);
            var b = Ilk(oyun.GorunumAl(a).Secilebilir, casus);
            Assert.That(oyun.SoruSor(a, b, "Burada en çok kim terler?"), Is.EqualTo(Sonuc.Tamam));
            Assert.That(oyun.CevapSec(b, Ilk(oyun.GorunumAl(b).Secilebilir, casus)), Is.EqualTo(Sonuc.Tamam));
            Assert.That(Durum(oyun).Asama, Is.EqualTo(Asama.MiniOyun));

            Assert.That(oyun.CasusTahmini(masum, Sir(oyun, casus)), Is.EqualTo(Sonuc.SiraSendeDegil), "sadece casus");
            Assert.That(oyun.CasusTahmini(casus, "Ay"), Is.EqualTo(Sonuc.GecersizGirdi), "listede yok");
            Assert.That(oyun.CasusTahmini(casus, null), Is.EqualTo(Sonuc.GecersizGirdi));
            Assert.That(Durum(oyun).Asama, Is.EqualTo(Asama.MiniOyun), "geçersiz tahmin hak yemez");

            Assert.That(oyun.CasusTahmini(casus, Sir(oyun, casus)), Is.EqualTo(Sonuc.Tamam));
            var s = oyun.Ozet().Casus;
            Assert.That((s.Kazanan, s.Neden), Is.EqualTo((CasusKazanan.Casus, CasusNedeni.Bildi)));
        }

        [Test]
        public void Yanlis_tahmin_grup_kazanir()
        {
            var oyun = YeniCasus(ayarla: a => a.Casus = CasusTuru.Kelime);
            var casus = CasusKim(oyun);
            Assert.That(oyun.CasusTahmini(casus, YanlisTahmin(oyun, casus)), Is.EqualTo(Sonuc.Tamam));
            var s = oyun.Ozet().Casus;
            Assert.That((s.Kazanan, s.Neden), Is.EqualTo((CasusKazanan.Grup, CasusNedeni.YanlisTahmin)));
        }

        [Test]
        public void Casus_oyundan_cikarsa_grup_kazanir()
        {
            var oyun = YeniCasus();
            var casus = CasusKim(oyun);
            Assert.That(oyun.OyuncuCikti(casus), Is.EqualTo(Sonuc.Tamam));
            var s = oyun.Ozet().Casus;
            Assert.That((s.Kazanan, s.Neden), Is.EqualTo((CasusKazanan.Grup, CasusNedeni.Kacti)));
        }

        [Test]
        public void Diger_oyunlarda_casus_yok()
        {
            var oyun = Yeni();
            Assert.That(oyun.CasusTahmini(P(1), "Uçak"), Is.EqualTo(Sonuc.YanlisAsama));
            var g = Durum(oyun);
            Assert.That(g.CasusListesi, Is.Empty);
            Assert.That(g.Sirrin, Is.Null);
            oyun.OyunuBitir();
            Assert.That(oyun.Ozet().Casus, Is.Null);
        }

        // ---------------------------------------------------- İçerik

        [Test]
        public void Icerik_kurallara_uyuyor()
        {
            Assert.That(CasusIcerik.Mekanlar.Length, Is.GreaterThanOrEqualTo(30));
            Assert.That(CasusIcerik.Kelimeler.Length, Is.GreaterThanOrEqualTo(5));
            foreach (var gruplar in new[] { CasusIcerik.Mekanlar, CasusIcerik.Kelimeler })
            {
                var adlar = new HashSet<string>();
                foreach (var g in gruplar)
                {
                    Assert.That(adlar.Add(g.Ad), Is.True, "aynı ad iki kez: " + g.Ad);
                    Assert.That(TemizFiltre.Uygunsuz(g.Ad), Is.False, g.Ad);
                    var ogeler = new HashSet<string>();
                    foreach (var x in g.Ogeler)
                    {
                        Assert.That(string.IsNullOrWhiteSpace(x) || x.Trim() != x, Is.False, g.Ad + ": '" + x + "'");
                        Assert.That(ogeler.Add(x), Is.True, g.Ad + ": aynısı iki kez: " + x);
                        Assert.That(TemizFiltre.Uygunsuz(x), Is.False, g.Ad + ": " + x);
                    }
                }
            }
            foreach (var m in CasusIcerik.Mekanlar)
                Assert.That(m.Ogeler.Count, Is.GreaterThanOrEqualTo(Oyun.EnFazlaOyuncu - 1), m.Ad + ": casus dışındaki herkese ayrı rol");
            foreach (var k in CasusIcerik.Kelimeler)
                Assert.That(k.Ogeler.Count, Is.GreaterThanOrEqualTo(10), k.Ad);
        }
    }
}
