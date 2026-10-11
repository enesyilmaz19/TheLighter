using System;
using System.Collections.Generic;
using Cakmak.Kurallar;
using NUnit.Framework;
using TheLighter.Akis;
using static TheLighter.Tests.TestYardimci;

namespace TheLighter.Tests
{
    /// <summary>S3 ve Curcuna akışları (elden ele): görev turu, soru toplama, Grup Kararı, Bedel, ceza onayı.</summary>
    [TestFixture]
    public class TekrarAkisiTestleri
    {
        static readonly TimeSpan Saniye = TimeSpan.FromSeconds(1);

        static readonly string[] Havuz =
        {
            "Grupta kim en geç kalır?", "Grupta kim en çok güler?", "Grupta kim en iyi yemek yapar?",
            "Grupta kim en çok uyur?", "Grupta kim en çok konuşur?", "Grupta kim en cimridir?",
        };

        static EldenEleAkisi Kur(Ayarlar ayar, int tohum = 1, int oyuncu = 5)
        {
            ayar.TekCihaz = true;
            if (ayar.OneriSorulari.Count == 0) ayar.OneriSorulari = Havuz;
            var s = EldenEleAkisi.Kur(ayar, Oyuncular(oyuncu), tohum, out var akis);
            Assert.AreEqual(Sonuc.Tamam, s);
            return akis;
        }

        /// <summary>Soru sorma perdesinden mini oyun ekranına kadar: B'ye sorar, B ilk seçilebilire verir.</summary>
        static void CevapSecilene(EldenEleAkisi a, out OyuncuId b, out OyuncuId c)
        {
            Assert.AreEqual(EkranTuru.Perde, a.Ekran.Tur);
            a.PerdeyiKaldir();
            Assert.AreEqual(EkranTuru.SoruSorma, a.Ekran.Tur);
            b = a.Ekran.Gorunum.Secilebilir[0];
            Assert.AreEqual(Sonuc.Tamam, a.SoruGonder(b, "Gizli soru?"));
            a.PerdeyiKaldir();
            c = a.Ekran.Gorunum.Secilebilir[0];
            Assert.AreEqual(Sonuc.Tamam, a.CevapSec(c));
        }

        // ---------- Gizli görevler ----------

        [Test]
        public void Gorevler_HerkesSirayla_PerdedeGorevYok_SaatDurur()
        {
            var a = Kur(new Ayarlar { GizliGorevler = true });
            var goren = new List<OyuncuId>();
            for (int i = 0; i < a.Oyuncular.Count; i++)
            {
                Assert.AreEqual(EkranTuru.Perde, a.Ekran.Tur);
                Assert.AreEqual(PerdeNedeni.Gorev, a.Ekran.PerdeNedeni);
                Assert.IsNull(a.Ekran.Gorunum.Gorevin, "Perdede kimsenin görevi olmamalı");
                for (int s = 0; s < 100; s++) a.Ilerle(Saniye);

                a.PerdeyiKaldir();
                Assert.AreEqual(EkranTuru.GorevGoster, a.Ekran.Tur);
                var kim = a.Ekran.Kime.Value;
                Assert.AreEqual(kim, a.Ekran.Gorunum.Sen, "Görev ekranı sahibinin görünümüyle çizilir");
                Assert.IsNotNull(a.Ekran.Gorunum.Gorevin);
                Assert.IsFalse(goren.Contains(kim), "Aynı kişi iki kez görmemeli");
                goren.Add(kim);
                for (int s = 0; s < 100; s++) a.Ilerle(Saniye);
                a.GorevGordum();
            }

            Assert.AreEqual(EkranTuru.Perde, a.Ekran.Tur);
            Assert.AreEqual(PerdeNedeni.SoruSirasi, a.Ekran.PerdeNedeni);
            a.PerdeyiKaldir();
            Assert.AreEqual(EkranTuru.SoruSorma, a.Ekran.Tur);
            Assert.AreEqual(60000, a.Ekran.Gorunum.KalanMs, "Görevlere bakılırken saat işlememeli");
        }

        [Test]
        public void Gorevler_OrtakEkranlardaGorevYok()
        {
            var a = Kur(new Ayarlar { GizliGorevler = true });
            while (a.Ekran.Tur != EkranTuru.Perde || a.Ekran.PerdeNedeni == PerdeNedeni.Gorev)
            {
                if (a.Ekran.Tur == EkranTuru.Perde) a.PerdeyiKaldir();
                else a.GorevGordum();
            }
            CevapSecilene(a, out _, out _);
            Assert.AreEqual(EkranTuru.MiniOyunPerdesi, a.Ekran.Tur);
            Assert.IsNull(a.Ekran.Gorunum.Gorevin);
            a.PerdeyiKaldir();
            Assert.AreEqual(EkranTuru.MiniOyun, a.Ekran.Tur);
            Assert.IsNull(a.Ekran.Gorunum.Gorevin, "Ortadaki ekranda seyircinin görevi olmamalı");
            Assert.IsNull(a.Ekran.BGorunumu.Gorevin, "B'nin görevi ortadaki ekranda olmamalı");
            Assert.IsNull(a.Ekran.CGorunumu.Gorevin, "C'nin görevi ortadaki ekranda olmamalı");
        }

        // ---------- Curcuna ----------

        [Test]
        public void Curcuna_HerkesSirayla2SoruYazar_SaatDurur_SonraAtananSoru()
        {
            var a = Kur(new Ayarlar { Mod = OyunModu.Curcuna });
            var yazanlar = new List<OyuncuId>();
            for (int i = 0; i < a.Oyuncular.Count; i++)
            {
                Assert.AreEqual(EkranTuru.Perde, a.Ekran.Tur);
                Assert.AreEqual(PerdeNedeni.SoruYazma, a.Ekran.PerdeNedeni);
                a.PerdeyiKaldir();
                Assert.AreEqual(EkranTuru.SoruToplama, a.Ekran.Tur);
                var kim = a.Ekran.Kime.Value;
                Assert.IsFalse(yazanlar.Contains(kim));
                yazanlar.Add(kim);

                for (int s = 0; s < 300; s++) a.Ilerle(Saniye);
                Assert.AreEqual(EkranTuru.SoruToplama, a.Ekran.Tur, "Elden elede soru yazarken saat durur");

                var ilkAnahtar = a.Ekran.Anahtar;
                Assert.AreEqual(Sonuc.Tamam, a.HavuzaSoruEkle("Grupta kim " + i + ". soruyu yazdı?"));
                Assert.AreEqual(EkranTuru.SoruToplama, a.Ekran.Tur, "İkinci soru da aynı kişide");
                Assert.AreEqual(kim, a.Ekran.Kime.Value);
                Assert.AreNotEqual(ilkAnahtar, a.Ekran.Anahtar, "Yazı kutusu boşalsın diye ekran baştan çizilir");
                Assert.AreEqual(Sonuc.Tamam, a.HavuzaSoruEkle("Grupta kim " + i + ". ikinci soruyu yazdı?"));
            }

            Assert.AreEqual(EkranTuru.Perde, a.Ekran.Tur);
            Assert.AreEqual(PerdeNedeni.SoruSirasi, a.Ekran.PerdeNedeni);
            a.PerdeyiKaldir();
            Assert.AreEqual(EkranTuru.SoruSorma, a.Ekran.Tur);
            var atanan = a.Ekran.Gorunum.AtananSoru;
            Assert.IsNotNull(atanan, "Curcuna'da A'ya havuzdan soru atanır");
            Assert.IsTrue(a.Ekran.Gorunum.DegistirmeHakki);
            Assert.AreEqual(Sonuc.Tamam, a.SoruyuDegistir());
            Assert.IsFalse(a.Ekran.Gorunum.DegistirmeHakki);
            Assert.AreEqual(Sonuc.HakKalmadi, a.SoruyuDegistir());

            var b = a.Ekran.Gorunum.Secilebilir[0];
            var gidecek = a.Ekran.Gorunum.AtananSoru;
            Assert.AreEqual(Sonuc.Tamam, a.SoruGonder(b, "Bu yok sayılır?"));
            a.PerdeyiKaldir();
            Assert.AreEqual(gidecek, a.Ekran.Gorunum.Soru, "Curcuna'da atanan soru gider, yazılan metin değil");
        }

        [Test]
        public void Curcuna_SoruYazmaEkraniBaskasininSorusunuTasimaz()
        {
            var a = Kur(new Ayarlar { Mod = OyunModu.Curcuna });
            a.PerdeyiKaldir();
            Assert.AreEqual(Sonuc.Tamam, a.HavuzaSoruEkle("Grupta kim ilk yazanın sırrını bilir?"));
            Assert.AreEqual(Sonuc.Tamam, a.HavuzaSoruEkle("Grupta kim ilk yazanın ikinci sırrını bilir?"));
            Assert.AreEqual(EkranTuru.Perde, a.Ekran.Tur);
            a.PerdeyiKaldir();
            var g = a.Ekran.Gorunum;
            Assert.IsNull(g.Soru);
            Assert.IsNull(g.AtananSoru);
            Assert.IsNull(g.Oneri);
        }

        // ---------- Bedel ve ceza onayı ----------

        [Test]
        public void Bedel_KararEkrani_SonraCezaOnayiSaatiDurdurur()
        {
            var a = Kur(new Ayarlar { CezaSeviyesi = CezaSeviyesi.Hafif });
            CevapSecilene(a, out var b, out var c);
            a.PerdeyiKaldir();
            Assert.AreEqual(Sonuc.Tamam, a.Hamle(b, Hamle.TasKagitMakas(TkmSecim.Tas)));
            Assert.AreEqual(Sonuc.Tamam, a.Hamle(c, Hamle.TasKagitMakas(TkmSecim.Kagit)));

            // C kazandı, soru ifşa olmak üzere: B'ye bedel fırsatı.
            Assert.AreEqual(EkranTuru.Karar, a.Ekran.Tur);
            Assert.AreEqual(Asama.Bedel, a.Ekran.Gorunum.Asama);
            Assert.AreEqual(b, a.Ekran.Kime.Value);
            Assert.IsNull(a.Ekran.Gorunum.Soru, "Ortadaki ekranda soru olmamalı");

            Assert.AreEqual(Sonuc.Tamam, a.KarariGec());
            Assert.AreEqual(EkranTuru.Ifsa, a.Ekran.Tur);
            Assert.Greater(a.Ekran.Gorunum.TurCezalari.Count, 0, "Kaybeden B ceza çekmeli");
            Assert.IsTrue(a.Ekran.CezaOnayBekliyor);
            int kalan = a.Ekran.Gorunum.KalanMs;
            for (int s = 0; s < 30; s++) a.Ilerle(Saniye);
            Assert.AreEqual(EkranTuru.Ifsa, a.Ekran.Tur, "Ceza kartları okunurken saat durur");
            Assert.AreEqual(kalan, a.Ekran.Gorunum.KalanMs);

            a.CezalariOnayla();
            Assert.IsFalse(a.Ekran.CezaOnayBekliyor);
            for (int s = 0; s < 7; s++) a.Ilerle(Saniye);
            Assert.AreEqual(EkranTuru.Perde, a.Ekran.Tur, "Onaydan sonra sonuç süresi işler, yeni tur");
            Assert.AreEqual(c, a.Ekran.Kime.Value);
        }

        [Test]
        public void Bedel_Odeyince_GumeVeBedelYazisi()
        {
            var a = Kur(new Ayarlar { CezaSeviyesi = CezaSeviyesi.Hafif });
            CevapSecilene(a, out var b, out var c);
            a.PerdeyiKaldir();
            a.Hamle(b, Hamle.TasKagitMakas(TkmSecim.Tas));
            a.Hamle(c, Hamle.TasKagitMakas(TkmSecim.Kagit));
            Assert.AreEqual(Sonuc.Tamam, a.BedelOde());
            Assert.AreEqual(EkranTuru.Gume, a.Ekran.Tur);
            Assert.IsTrue(a.Ekran.Gorunum.BedelOdendi);
            Assert.IsNull(a.Ekran.Gorunum.IfsaSoru);
        }

        [Test]
        public void CezaKapali_OnayBeklenmez()
        {
            var a = Kur(new Ayarlar());
            CevapSecilene(a, out var b, out var c);
            a.PerdeyiKaldir();
            a.Hamle(b, Hamle.TasKagitMakas(TkmSecim.Tas));
            a.Hamle(c, Hamle.TasKagitMakas(TkmSecim.Kagit));
            Assert.AreEqual(EkranTuru.Ifsa, a.Ekran.Tur);
            Assert.IsFalse(a.Ekran.CezaOnayBekliyor);
        }

        // ---------- Grup Kararı ----------

        /// <summary>Kaos açık bir oyunda 5. tura kadar oynar. 5. turun kaosu Grup Kararı ise akışı döner.</summary>
        static EldenEleAkisi GrupKarariTuru()
        {
            for (int tohum = 1; tohum < 400; tohum++)
            {
                var a = Kur(new Ayarlar { KaosTurlari = true, TurSayisi = 10 }, tohum);
                for (int tur = 1; tur < Oyun.KaosAraligi; tur++)
                {
                    CevapSecilene(a, out var b, out var c);
                    a.PerdeyiKaldir();
                    a.Hamle(b, Hamle.TasKagitMakas(TkmSecim.Tas));
                    a.Hamle(c, Hamle.TasKagitMakas(TkmSecim.Makas));
                    for (int s = 0; s < 7; s++) a.Ilerle(Saniye);
                }
                Assert.AreEqual(Oyun.KaosAraligi, a.Ekran.Gorunum.Tur);
                if (a.Ekran.Gorunum.Kaos == KaosKurali.GrupKarari) return a;
            }
            Assert.Fail("400 tohumda Grup Kararı çıkmadı");
            return null;
        }

        [Test]
        public void GrupKarari_TelefonOylayanlaraSiraylaGider_OyGizli_SaatDurur()
        {
            var a = GrupKarariTuru();
            CevapSecilene(a, out var b, out _);
            var oyVeren = new List<OyuncuId>();
            Assert.AreEqual(EkranTuru.Perde, a.Ekran.Tur);
            Assert.AreEqual(PerdeNedeni.Oy, a.Ekran.PerdeNedeni);

            while (a.Ekran.Tur == EkranTuru.Perde && a.Ekran.PerdeNedeni == PerdeNedeni.Oy)
            {
                Assert.IsNull(a.Ekran.Gorunum.Soru);
                a.PerdeyiKaldir();
                Assert.AreEqual(EkranTuru.Oylama, a.Ekran.Tur);
                var kim = a.Ekran.Kime.Value;
                Assert.AreNotEqual(b, kim, "B oy vermez");
                Assert.IsFalse(oyVeren.Contains(kim));
                Assert.IsNull(a.Ekran.Gorunum.Soru, "Oy veren soruyu görmez");
                for (int s = 0; s < 60; s++) a.Ilerle(Saniye);
                Assert.AreEqual(EkranTuru.Oylama, a.Ekran.Tur, "Telefon dolaşırken saat durur");
                oyVeren.Add(kim);
                Assert.AreEqual(Sonuc.Tamam, a.Oy(true));
            }

            Assert.AreEqual(a.Oyuncular.Count - 2, oyVeren.Count, "A ve B hariç herkes oy verir");
            Assert.AreEqual(EkranTuru.Ifsa, a.Ekran.Tur);
            Assert.IsNotNull(a.Ekran.Gorunum.SonOylama);
            Assert.AreEqual(oyVeren.Count, a.Ekran.Gorunum.SonOylama.Ifsa);
        }

        // ---------- Oyun sonu ----------

        [Test]
        public void OyunSonu_OzetVar_GumeGidenSoruYok()
        {
            var a = Kur(new Ayarlar { GizliGorevler = true, TurSayisi = 1 });
            while (a.Ekran.Tur != EkranTuru.Perde || a.Ekran.PerdeNedeni == PerdeNedeni.Gorev)
            {
                if (a.Ekran.Tur == EkranTuru.Perde) a.PerdeyiKaldir();
                else a.GorevGordum();
            }
            a.PerdeyiKaldir();
            var b = a.Ekran.Gorunum.Secilebilir[0];
            a.SoruGonder(b, "Güme gidecek gizli soru?");
            a.PerdeyiKaldir();
            var c = a.Ekran.Gorunum.Secilebilir[0];
            a.CevapSec(c);
            a.PerdeyiKaldir();
            a.Hamle(b, Hamle.TasKagitMakas(TkmSecim.Tas));
            a.Hamle(c, Hamle.TasKagitMakas(TkmSecim.Makas));
            Assert.AreEqual(EkranTuru.Gume, a.Ekran.Tur);
            for (int s = 0; s < 7; s++) a.Ilerle(Saniye);

            Assert.AreEqual(EkranTuru.OyunSonu, a.Ekran.Tur);
            Assert.IsNotNull(a.Ekran.Ozet);
            Assert.AreEqual(a.Oyuncular.Count, a.Ekran.Ozet.Gorevler.Count, "Oyun bitince herkesin görevi açılır");
            Assert.AreEqual(0, a.Ekran.Ozet.IfsaOlanlar.Count);
            Assert.AreEqual(0, a.Ekran.IfsaOlanlar.Count);
        }
    }
}
