using System;
using System.Collections.Generic;
using Cakmak.Kurallar;
using NUnit.Framework;
using TheLighter.Akis;
using static TheLighter.Tests.TestYardimci;

namespace TheLighter.Tests
{
    /// <summary>Elden ele akışı, Enes'in gerçek kural motoruyla (Cakmak.Kurallar.Oyun).</summary>
    [TestFixture]
    public class EldenEleAkisiTestleri
    {
        static readonly TimeSpan Saniye = TimeSpan.FromSeconds(1);

        static void SoruyuGonderVePerdeyiKaldir(EldenEleAkisi a, string soru, out OyuncuId b)
        {
            a.PerdeyiKaldir();
            b = a.Ekran.Gorunum.Secilebilir[0];
            Assert.AreEqual(Sonuc.Tamam, a.SoruGonder(b, soru));
            a.PerdeyiKaldir();
        }

        static void MiniOyunaGec(EldenEleAkisi a, out OyuncuId b, out OyuncuId c)
        {
            SoruyuGonderVePerdeyiKaldir(a, "Soru?", out b);
            c = a.Ekran.Gorunum.Secilebilir[0];
            Assert.AreEqual(Sonuc.Tamam, a.CevapSec(c));
            a.PerdeyiKaldir();
            Assert.AreEqual(EkranTuru.MiniOyun, a.Ekran.Tur);
        }

        [Test]
        public void IlkEkran_PerdeVeSaatDurur()
        {
            var a = YeniAkis();
            Assert.AreEqual(EkranTuru.Perde, a.Ekran.Tur);
            Assert.AreEqual(PerdeNedeni.SoruSirasi, a.Ekran.PerdeNedeni);
            Assert.IsNotNull(a.Ekran.Kime);
            Assert.IsNull(a.Ekran.SuresiDolan);
            for (int i = 0; i < 200; i++) a.Ilerle(Saniye);
            Assert.AreEqual(EkranTuru.Perde, a.Ekran.Tur, "Perdede saat işlememeli");
            a.PerdeyiKaldir();
            Assert.AreEqual(EkranTuru.SoruSorma, a.Ekran.Tur);
            Assert.AreEqual(60000, a.Ekran.Gorunum.KalanMs);
            Assert.AreEqual(a.Ekran.Kime.Value, a.Ekran.Gorunum.Sen, "Soru sorma ekranı A'nın görünümüyle çizilir");
        }

        [Test]
        public void Perdede_GizliBilgiYok_BPerdeyiKaldirincaGorur()
        {
            var a = YeniAkis();
            a.PerdeyiKaldir();
            var b = a.Ekran.Gorunum.Secilebilir[0];
            Assert.AreEqual(Sonuc.Tamam, a.SoruGonder(b, "Perde testi?"));
            Assert.AreEqual(EkranTuru.Perde, a.Ekran.Tur);
            Assert.AreEqual(PerdeNedeni.CevapSirasi, a.Ekran.PerdeNedeni);
            Assert.AreEqual(b, a.Ekran.Kime.Value);
            Assert.IsNull(a.Ekran.Gorunum.Soru, "Perdede soru görünmemeli");
            Assert.AreNotEqual(b, a.Ekran.Gorunum.Sen);

            a.PerdeyiKaldir();
            Assert.AreEqual(EkranTuru.CevapSecme, a.Ekran.Tur);
            Assert.AreEqual("Perde testi?", a.Ekran.Gorunum.Soru);
        }

        [Test]
        public void CevapSecince_MiniOyunPerdesi_SonraBolunmusEkran_GizliAlanlarBos()
        {
            var a = YeniAkis();
            MiniOyunaGec(a, out var b, out var c);
            Assert.AreEqual(b, a.Ekran.BGorunumu.Sen);
            Assert.AreEqual(c, a.Ekran.CGorunumu.Sen);
            Assert.IsNull(a.Ekran.BGorunumu.Soru, "Mini oyunda telefon ortada: soru ekran modelinde olmamalı");

            Assert.AreEqual(Sonuc.Tamam, a.Hamle(b, Hamle.TasKagitMakas(TkmSecim.Makas)));
            Assert.IsNull(a.Ekran.BGorunumu.Hamlen, "B'nin hamlesi ortadaki ekranda olmamalı");
            Assert.IsNull(a.Ekran.CGorunumu.Hamlen);
        }

        [Test]
        public void MiniOyunPerdesindeSaatDurur()
        {
            var a = YeniAkis();
            SoruyuGonderVePerdeyiKaldir(a, "Soru?", out _);
            Assert.AreEqual(Sonuc.Tamam, a.CevapSec(a.Ekran.Gorunum.Secilebilir[0]));
            Assert.AreEqual(EkranTuru.MiniOyunPerdesi, a.Ekran.Tur);
            for (int i = 0; i < 60; i++) a.Ilerle(Saniye);
            Assert.AreEqual(EkranTuru.MiniOyunPerdesi, a.Ekran.Tur);
            a.PerdeyiKaldir();
            Assert.AreEqual(10000, a.Ekran.Gorunum.KalanMs);
        }

        [Test]
        public void SureGecince_AnahtarDegismez()
        {
            var a = YeniAkis();
            a.PerdeyiKaldir();
            var anahtar = a.Ekran.Anahtar;
            a.Ilerle(Saniye);
            Assert.AreEqual(anahtar, a.Ekran.Anahtar, "Süre akarken ekran baştan çizilmemeli (yazı kutusu silinir)");
            Assert.AreEqual(59000, a.Ekran.Gorunum.KalanMs);
        }

        [Test]
        public void Duraklatilinca_SaatIslemez()
        {
            var a = YeniAkis();
            a.PerdeyiKaldir();
            a.Duraklatildi = true;
            for (int i = 0; i < 100; i++) a.Ilerle(Saniye);
            Assert.AreEqual(EkranTuru.SoruSorma, a.Ekran.Tur);
            Assert.AreEqual(60000, a.Ekran.Gorunum.KalanMs);
        }

        [Test]
        public void SoruSuresiDolunca_PerdeSebebiniSoyler()
        {
            var a = YeniAkis();
            a.PerdeyiKaldir();
            var eskiA = a.Ekran.Kime.Value;
            a.Ilerle(TimeSpan.FromSeconds(60));
            Assert.AreEqual(EkranTuru.Perde, a.Ekran.Tur);
            Assert.AreEqual(eskiA, a.Ekran.SuresiDolan.Value);
            Assert.IsFalse(a.Ekran.SuresiDolanBydi);
            Assert.AreNotEqual(eskiA, a.Ekran.Kime.Value);
            a.PerdeyiKaldir();
            a.Ilerle(Saniye);
            Assert.IsNull(a.Ekran.SuresiDolan);
        }

        [Test]
        public void CevapSuresiDolunca_PerdeGumeyiSoyler()
        {
            var a = YeniAkis();
            SoruyuGonderVePerdeyiKaldir(a, "Güme gidecek?", out var b);
            a.Ilerle(TimeSpan.FromSeconds(20));
            Assert.AreEqual(EkranTuru.Perde, a.Ekran.Tur);
            Assert.AreEqual(PerdeNedeni.SoruSirasi, a.Ekran.PerdeNedeni);
            Assert.AreEqual(b, a.Ekran.SuresiDolan.Value);
            Assert.IsTrue(a.Ekran.SuresiDolanBydi);
        }

        [Test]
        public void Berabere_ElGosterilir_YeniHamledeKaybolur()
        {
            var a = YeniAkis();
            MiniOyunaGec(a, out var b, out var c);
            a.Hamle(b, Hamle.TasKagitMakas(TkmSecim.Tas));
            a.Hamle(c, Hamle.TasKagitMakas(TkmSecim.Tas));
            Assert.AreEqual(EkranTuru.MiniOyun, a.Ekran.Tur);
            Assert.AreEqual(1, a.Ekran.Gorunum.Beraberlik);
            Assert.IsNotNull(a.Ekran.SonBerabere);
            Assert.AreEqual(TkmSecim.Tas, a.Ekran.SonBerabere.BHamle.Tkm);
            Assert.AreEqual(TkmSecim.Tas, a.Ekran.SonBerabere.CHamle.Tkm);

            a.Hamle(b, Hamle.TasKagitMakas(TkmSecim.Kagit));
            Assert.IsNull(a.Ekran.SonBerabere);
        }

        [Test]
        public void OneriCek_GorunumeDuser_YaziKutusuSilinmez()
        {
            var oneriler = new List<string> { "Öneri bir?", "Öneri iki?" };
            var a = YeniAkis(oneriler: oneriler);
            a.PerdeyiKaldir();
            var anahtar = a.Ekran.Anahtar;
            Assert.AreEqual(Sonuc.Tamam, a.OneriCek());
            Assert.IsTrue(oneriler.Contains(a.Ekran.Gorunum.Oneri));
            Assert.AreEqual(anahtar, a.Ekran.Anahtar);

            var bos = YeniAkis();
            bos.PerdeyiKaldir();
            Assert.AreEqual(Sonuc.HavuzBos, bos.OneriCek());
        }

        [Test]
        public void IfsaOlanSoru_OyunSonundaListede()
        {
            var a = YeniAkis(tur: 0);
            MiniOyunaGec(a, out var b, out var c);
            a.Hamle(b, Hamle.TasKagitMakas(TkmSecim.Tas));
            a.Hamle(c, Hamle.TasKagitMakas(TkmSecim.Kagit));
            Assert.AreEqual(EkranTuru.Ifsa, a.Ekran.Tur);
            Assert.AreEqual("Soru?", a.Ekran.Gorunum.IfsaSoru);

            a.Duraklatildi = true;
            Assert.AreEqual(Sonuc.Tamam, a.OyunuBitir());
            Assert.IsFalse(a.Duraklatildi);
            Assert.AreEqual(EkranTuru.OyunSonu, a.Ekran.Tur);
            Assert.AreEqual(1, a.Ekran.IfsaOlanlar.Count);
            Assert.AreEqual("Soru?", a.Ekran.IfsaOlanlar[0].Soru);
            Assert.AreEqual(c, a.Ekran.IfsaOlanlar[0].Cevap.Value);
        }

        [Test]
        public void SiradakiAdim_GizliSecimdeOnceBSonraC()
        {
            var g = new Gorunum { Asama = Asama.MiniOyun, MiniOyun = MiniOyunTuru.Tkm };
            Assert.AreEqual(MiniOyunAdimi.BSeciyor, EldenEleAkisi.SiradakiAdim(g));
            g.BHamleYapti = true;
            Assert.AreEqual(MiniOyunAdimi.CSeciyor, EldenEleAkisi.SiradakiAdim(g));
            g.CHamleYapti = true;
            Assert.AreEqual(MiniOyunAdimi.Bekliyor, EldenEleAkisi.SiradakiAdim(g));

            var t = new Gorunum { Asama = Asama.MiniOyun, MiniOyun = MiniOyunTuru.TekCift };
            Assert.AreEqual(MiniOyunAdimi.BSeciyor, EldenEleAkisi.SiradakiAdim(t));

            var z = new Gorunum { Asama = Asama.MiniOyun, MiniOyun = MiniOyunTuru.Zar, CHamleYapti = true };
            Assert.AreEqual(MiniOyunAdimi.IkisiDeAtiyor, EldenEleAkisi.SiradakiAdim(z));

            var r = new Gorunum { Asama = Asama.MiniOyun, MiniOyun = MiniOyunTuru.Reaksiyon };
            Assert.AreEqual(MiniOyunAdimi.Reaksiyon, EldenEleAkisi.SiradakiAdim(r));
        }

        /// <summary>
        /// Birçok tohum ve her mini oyunla rastgele oyunlar oynar. Her adımda: soru metni sadece B'nin
        /// cevap seçme ekranında görünür, ifşa sadece ifşa ekranında, perdede ve ortadaki ekranda gizli alan yok,
        /// perdede saat işlemez. Oyun sonunda güme giden hiçbir soru listede değildir.
        /// </summary>
        [Test]
        public void RastgeleOyunlar_GizlilikHicBozulmaz()
        {
            var turler = new[] { MiniOyunTuru.Tkm, MiniOyunTuru.TekCift, MiniOyunTuru.Zar, MiniOyunTuru.Reaksiyon, MiniOyunTuru.Karisik };
            for (int tohum = 1; tohum <= 12; tohum++)
                foreach (var mini in turler)
                    OyunOyna(tohum, mini);
        }

        static void OyunOyna(int tohum, MiniOyunTuru mini)
        {
            var a = YeniAkis(tohum, mini, 6, 5, new List<string> { "Havuz sorusu?" });
            var r = new Random(tohum * 31 + (int)mini);
            var gumeGiden = new HashSet<string>();
            var ifsaOlan = new HashSet<string>();
            string sonSoru = null;
            int soruNo = 0;
            int adim = 0;

            for (; adim < 5000 && !a.Bitti; adim++)
            {
                var e = a.Ekran;
                GizlilikKontrol(e, tohum, mini);

                switch (e.Tur)
                {
                    case EkranTuru.Perde:
                    case EkranTuru.MiniOyunPerdesi:
                        if (e.SuresiDolanBydi && sonSoru != null) gumeGiden.Add(sonSoru);
                        int kalan = e.Gorunum.KalanMs;
                        a.Ilerle(TimeSpan.FromSeconds(30));
                        Assert.AreEqual(kalan, a.Ekran.Gorunum.KalanMs, "Perdede saat işledi");
                        a.PerdeyiKaldir();
                        break;

                    case EkranTuru.SoruSorma:
                        if (r.Next(8) == 0) { a.Ilerle(TimeSpan.FromSeconds(61)); break; }
                        if (r.Next(4) == 0) Assert.AreEqual(Sonuc.Tamam, a.OneriCek());
                        var b = e.Gorunum.Secilebilir[r.Next(e.Gorunum.Secilebilir.Count)];
                        if (r.Next(5) == 0) { Assert.AreEqual(Sonuc.Tamam, a.Fisildadim(b)); sonSoru = null; }
                        else
                        {
                            sonSoru = "Soru " + tohum + "-" + mini + "-" + (++soruNo) + "?";
                            Assert.AreEqual(Sonuc.Tamam, a.SoruGonder(b, sonSoru));
                        }
                        break;

                    case EkranTuru.CevapSecme:
                        if (r.Next(8) == 0) { a.Ilerle(TimeSpan.FromSeconds(21)); break; }
                        var sec = e.Gorunum.Secilebilir;
                        Assert.AreEqual(Sonuc.Tamam, a.CevapSec(sec[r.Next(sec.Count)]));
                        break;

                    case EkranTuru.MiniOyun:
                        if (r.Next(10) == 0) { a.Ilerle(TimeSpan.FromSeconds(11)); break; }
                        HamleYap(a, e, r);
                        break;

                    case EkranTuru.Ifsa:
                        if (e.Gorunum.IfsaSoru != null) ifsaOlan.Add(e.Gorunum.IfsaSoru);
                        a.Ilerle(Saniye);
                        break;

                    case EkranTuru.Gume:
                        if (sonSoru != null) gumeGiden.Add(sonSoru);
                        a.Ilerle(Saniye);
                        break;

                    default:
                        Assert.Fail("Beklenmeyen ekran: " + e.Tur);
                        break;
                }
            }

            Assert.IsTrue(a.Bitti, "Oyun bitmedi: tohum " + tohum + " " + mini);
            foreach (var k in a.Ekran.IfsaOlanlar)
            {
                if (k.Soru == null) continue;
                Assert.IsFalse(gumeGiden.Contains(k.Soru), "Güme giden soru listede: " + k.Soru);
                Assert.IsTrue(ifsaOlan.Contains(k.Soru));
            }
        }

        static void HamleYap(EldenEleAkisi a, EkranDurumu e, Random r)
        {
            var g = e.BGorunumu;
            var b = e.BGorunumu.Sen;
            var c = e.CGorunumu.Sen;
            switch (EldenEleAkisi.SiradakiAdim(g))
            {
                case MiniOyunAdimi.BSeciyor:
                    Assert.AreEqual(Sonuc.Tamam, a.Hamle(b, GizliHamle(g.MiniOyun, r, true)));
                    break;
                case MiniOyunAdimi.CSeciyor:
                    Assert.AreEqual(Sonuc.Tamam, a.Hamle(c, GizliHamle(g.MiniOyun, r, false)));
                    break;
                case MiniOyunAdimi.IkisiDeAtiyor:
                    Assert.AreEqual(Sonuc.Tamam, a.Hamle(g.BHamleYapti ? c : b, Hamle.ZarAt()));
                    break;
                case MiniOyunAdimi.Reaksiyon:
                    Assert.AreEqual(Sonuc.Tamam, a.Hamle(g.BHamleYapti ? c : b, Hamle.Reaksiyon(r.Next(-50, 600))));
                    break;
                default:
                    Assert.Fail("Mini oyunda beklenmeyen adım");
                    break;
            }
        }

        static Hamle GizliHamle(MiniOyunTuru tur, Random r, bool b)
        {
            return tur == MiniOyunTuru.TekCift
                ? Hamle.TekCift(r.Next(1, 6), b && r.Next(2) == 0)
                : Hamle.TasKagitMakas((TkmSecim)r.Next(3));
        }

        static void GizlilikKontrol(EkranDurumu e, int tohum, MiniOyunTuru mini)
        {
            string yer = " (" + e.Tur + ", tohum " + tohum + ", " + mini + ")";
            foreach (var g in new[] { e.Gorunum, e.BGorunumu, e.CGorunumu })
            {
                if (g == null) continue;
                if (g.Soru != null)
                {
                    Assert.AreEqual(EkranTuru.CevapSecme, e.Tur, "Soru metni yanlış ekranda" + yer);
                    Assert.AreEqual(e.Kime.Value, g.Sen, "Soru metni B'den başkasına gösterildi" + yer);
                }
                if (g.IfsaSoru != null) Assert.AreEqual(EkranTuru.Ifsa, e.Tur, "İfşa yanlış ekranda" + yer);
                if (g.Oneri != null) Assert.AreEqual(EkranTuru.SoruSorma, e.Tur, "Öneri yanlış ekranda" + yer);
                if (g.Hamlen.HasValue) Assert.Fail("Hamle ekran modelinde" + yer);
            }
        }
    }
}
