using System.Collections.Generic;
using NUnit.Framework;
using static Cakmak.Kurallar.Tests.Kurulum;

namespace Cakmak.Kurallar.Tests
{
    public class CurcunaTestleri
    {
        static readonly string[] Oneriler = { "Öneri 1?", "Öneri 2?", "Öneri 3?" };

        static Oyun CurcunaKur(int kisi = 4, string[] oneri = null, int turSayisi = 15) =>
            Yeni(kisi: kisi, ayarla: x =>
            {
                x.Mod = OyunModu.Curcuna;
                x.OneriSorulari = oneri ?? Oneriler;
                x.TurSayisi = turSayisi;
            });

        static void HerkesYazar(Oyun oyun, int kisi)
        {
            for (int i = 1; i <= kisi; i++)
                for (int j = 1; j <= 2; j++)
                    Assert.That(oyun.HavuzaSoruEkle(P(i), $"Soru {i}-{j}?"), Is.EqualTo(Sonuc.Tamam));
        }

        [Test]
        public void Curcuna_soru_toplamayla_baslar()
        {
            var g = Durum(CurcunaKur());
            Assert.That(g.Asama, Is.EqualTo(Asama.SoruToplama));
            Assert.That(g.A, Is.Null);
            Assert.That(g.KalanMs, Is.EqualTo(90000));
        }

        [Test]
        public void Kisi_basi_en_fazla_2_soru()
        {
            var oyun = CurcunaKur();
            oyun.HavuzaSoruEkle(P(1), "Bir?");
            oyun.HavuzaSoruEkle(P(1), "İki?");
            Assert.That(oyun.HavuzaSoruEkle(P(1), "Üç?"), Is.EqualTo(Sonuc.HakKalmadi));
            Assert.That(Durum(oyun).Oyuncular[0].HavuzaEkledigi, Is.EqualTo(2));
        }

        [Test]
        public void Herkes_2_soru_yazinca_tur_baslar_ve_A_ya_havuzdan_soru_atanir()
        {
            var oyun = CurcunaKur();
            HerkesYazar(oyun, 4);

            var g = Durum(oyun);
            Assert.That(g.Asama, Is.EqualTo(Asama.SoruSorma));
            var gA = oyun.GorunumAl(g.A.Value);
            Assert.That(gA.AtananSoru, Does.StartWith("Soru "));
            Assert.That(gA.DegistirmeHakki, Is.True);
        }

        [Test]
        public void Atanan_soru_sadece_A_ya_gorunur()
        {
            var oyun = CurcunaKur();
            HerkesYazar(oyun, 4);
            var a = SiradakiA(oyun);
            foreach (var o in Durum(oyun).Oyuncular)
                if (o.Id != a) Assert.That(oyun.GorunumAl(o.Id).AtananSoru, Is.Null);
        }

        [Test]
        public void Gonderilen_soru_atanan_sorudur_yazilan_metin_yok_sayilir()
        {
            var oyun = CurcunaKur();
            HerkesYazar(oyun, 4);
            var a = SiradakiA(oyun);
            var atanan = oyun.GorunumAl(a).AtananSoru;
            var b = oyun.GorunumAl(a).Secilebilir[0];
            oyun.SoruSor(a, b, "Kendi yazdığım soru");
            Assert.That(oyun.GorunumAl(b).Soru, Is.EqualTo(atanan));
        }

        [Test]
        public void Degistirme_hakki_tur_basina_bir_kez()
        {
            var oyun = CurcunaKur();
            HerkesYazar(oyun, 4);
            var a = SiradakiA(oyun);
            Assert.That(oyun.SoruyuDegistir(a), Is.EqualTo(Sonuc.Tamam));
            Assert.That(oyun.GorunumAl(a).DegistirmeHakki, Is.False);
            Assert.That(oyun.SoruyuDegistir(a), Is.EqualTo(Sonuc.HakKalmadi));

            // Sonraki turda hak yenilenir
            var b = oyun.GorunumAl(a).Secilebilir[0];
            oyun.SoruSor(a, b, null);
            var c = oyun.GorunumAl(b).Secilebilir[0];
            oyun.CevapSec(b, c);
            TkmOyna(oyun, b, c, cKazansin: true);
            Saniye(oyun, 6);
            Assert.That(oyun.GorunumAl(c).DegistirmeHakki, Is.True);
        }

        [Test]
        public void Havuz_bitince_karistirilip_bastan_alinir()
        {
            var oyun = CurcunaKur(turSayisi: 0);
            HerkesYazar(oyun, 4); // 8 soru
            var gelen = new Dictionary<string, int>();
            for (int i = 0; i < 16; i++)
            {
                var a = SiradakiA(oyun);
                var soru = oyun.GorunumAl(a).AtananSoru;
                Assert.That(soru, Is.Not.Null);
                gelen[soru] = gelen.TryGetValue(soru, out var n) ? n + 1 : 1;
                Saniye(oyun, 61); // A'nın süresi dolsun, sıradaki tura geç
            }
            Assert.That(gelen, Has.Count.EqualTo(8), "8 sorunun hepsi geldi");
            foreach (var adet in gelen.Values) Assert.That(adet, Is.EqualTo(2), "16 turda her soru tam 2 kez");
        }

        [Test]
        public void Sure_dolunca_eksikler_oneriden_tamamlanir()
        {
            var oyun = CurcunaKur();
            oyun.HavuzaSoruEkle(P(1), "Tek yazılan?");
            Saniye(oyun, 90.1);
            var g = Durum(oyun);
            Assert.That(g.Asama, Is.EqualTo(Asama.SoruSorma));
            Assert.That(oyun.GorunumAl(g.A.Value).AtananSoru, Is.Not.Null);
        }

        [Test]
        public void Havuz_tamamen_bossa_A_kendisi_yazar()
        {
            var oyun = CurcunaKur(oneri: new string[0]);
            Saniye(oyun, 90.1);
            var a = SiradakiA(oyun);
            Assert.That(oyun.GorunumAl(a).AtananSoru, Is.Null);
            var b = oyun.GorunumAl(a).Secilebilir[0];
            Assert.That(oyun.SoruSor(a, b, "Kendim yazdım?"), Is.EqualTo(Sonuc.Tamam));
            Assert.That(oyun.GorunumAl(b).Soru, Is.EqualTo("Kendim yazdım?"));
        }

        // ---------------------------------------------------- Öneri çek (Normal)

        [Test]
        public void Oneri_sadece_A_ya_gorunur()
        {
            var oyun = Yeni(ayarla: x => x.OneriSorulari = Oneriler);
            var a = SiradakiA(oyun);
            Assert.That(oyun.OneriCek(a), Is.EqualTo(Sonuc.Tamam));
            Assert.That(oyun.GorunumAl(a).Oneri, Does.StartWith("Öneri "));
            foreach (var o in Durum(oyun).Oyuncular)
                if (o.Id != a) Assert.That(oyun.GorunumAl(o.Id).Oneri, Is.Null);
        }

        [Test]
        public void Oneri_havuzu_bossa_HavuzBos()
        {
            var oyun = Yeni();
            Assert.That(oyun.OneriCek(SiradakiA(oyun)), Is.EqualTo(Sonuc.HavuzBos));
        }

        [Test]
        public void Curcuna_da_oneri_yok_Normal_de_degistirme_yok()
        {
            var normal = Yeni(ayarla: x => x.OneriSorulari = Oneriler);
            Assert.That(normal.SoruyuDegistir(SiradakiA(normal)), Is.EqualTo(Sonuc.YanlisAsama));

            var curcuna = CurcunaKur();
            HerkesYazar(curcuna, 4);
            Assert.That(curcuna.OneriCek(SiradakiA(curcuna)), Is.EqualTo(Sonuc.YanlisAsama));
        }
    }
}
