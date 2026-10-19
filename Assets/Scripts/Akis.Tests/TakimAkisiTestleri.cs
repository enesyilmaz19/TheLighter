using System;
using System.Collections.Generic;
using System.Linq;
using Cakmak.Kurallar;
using NUnit.Framework;
using TheLighter.Akis;
using static TheLighter.Tests.TestYardimci;

namespace TheLighter.Tests
{
    /// <summary>
    /// Takım Düellosu elden ele (tasks/g2-takim.md 4): kurucunun dağılımı oyuna gidiyor, tek sayıda kişide küçük takım
    /// avansla başlıyor, puan anları sonuç ekranında ve skoru tutuyor, 7'ye ulaşan takım o turun sonucu gösterildikten
    /// sonra kazanıyor. Gerçek kural motoruyla, oyun baştan sona.
    /// </summary>
    [TestFixture]
    public class TakimAkisiTestleri
    {
        static EldenEleAkisi Kur(int oyuncu, IReadOnlyDictionary<OyuncuId, int> dagilim = null, int tur = 0, int tohum = 5)
        {
            var ayar = new Ayarlar { Oyun = OyunTuru.Takim, MiniOyun = MiniOyunTuru.Zar, TurSayisi = tur, TakimDagilimi = dagilim };
            Assert.AreEqual(Sonuc.Tamam, EldenEleAkisi.Kur(ayar, Oyuncular(oyuncu), tohum, out var a));
            return a;
        }

        static int? TakimNe(Gorunum g, OyuncuId id) => g.Oyuncular.First(o => o.Id == id).Takim;

        sealed class Gozlem
        {
            public readonly List<PuanAni> Anlar = new List<PuanAni>();
            public int SonucEkrani;
            public EkranTuru SonEkranBitmeden;
            public IReadOnlyList<int> SonSkorBitmeden;
        }

        /// <summary>
        /// Oyunu baştan sona oynar. A hep rakip takımdan birine sorar. <paramref name="rakibe"/>: B çakmağı rakibe verir
        /// (puanlı düello); false: takım arkadaşına (puansız düello). Her sonuç ekranında puan anları skoru tutmalı.
        /// </summary>
        static Gozlem Oyna(EldenEleAkisi a, bool rakibe)
        {
            var gz = new Gozlem();
            var onceki = new List<int>(a.Ekran.Gorunum.TakimPuanlari);
            string sonAnahtar = null;
            for (int adim = 0; adim < 5000 && a.Ekran.Tur != EkranTuru.OyunSonu; adim++)
            {
                var e = a.Ekran;
                var g = e.Gorunum;
                gz.SonEkranBitmeden = e.Tur;
                gz.SonSkorBitmeden = g.TakimPuanlari;
                switch (e.Tur)
                {
                    case EkranTuru.Perde:
                    case EkranTuru.MiniOyunPerdesi:
                        Assert.AreEqual(0, e.PuanAnlari.Count, "puan anı sadece sonuç ekranında");
                        a.PerdeyiKaldir();
                        break;
                    case EkranTuru.SoruSorma:
                    {
                        // A hep rakibe sorar: B'nin takım arkadaşı A olmasın (C seçilebilsin).
                        int benim = TakimNe(g, g.Sen).Value;
                        var rakip = g.Secilebilir.Where(id => TakimNe(g, id) != benim).ToList();
                        Assert.AreEqual(Sonuc.Tamam, a.SoruGonder(rakip.Count > 0 ? rakip[0] : g.Secilebilir[0], "Kim en geç uyur?"));
                        break;
                    }
                    case EkranTuru.CevapSecme:
                    {
                        int benim = TakimNe(g, g.Sen).Value;
                        var aday = g.Secilebilir.Where(id => rakibe ? TakimNe(g, id) != benim : TakimNe(g, id) == benim).ToList();
                        Assert.Greater(aday.Count, 0, rakibe ? "rakip seçilebilir" : "takım arkadaşı seçilebilir");
                        Assert.AreEqual(Sonuc.Tamam, a.CevapSec(aday[0]));
                        break;
                    }
                    case EkranTuru.MiniOyun:
                        if (!g.BHamleYapti) a.Hamle(g.B.Value, Hamle.ZarAt());
                        else a.Hamle(g.C.Value, Hamle.ZarAt());
                        break;
                    case EkranTuru.Ifsa:
                    case EkranTuru.Gume:
                        if (e.Anahtar != sonAnahtar)
                        {
                            sonAnahtar = e.Anahtar;
                            gz.SonucEkrani++;
                            gz.Anlar.AddRange(e.PuanAnlari);
                            for (int t = 0; t < Oyun.TakimSayisi; t++)
                                Assert.AreEqual(onceki[t] + e.PuanAnlari.Count(p => p.Takim == t), g.TakimPuanlari[t],
                                    "sonuç ekranındaki puan anları skoru tutar (takım " + t + ")");
                            onceki = new List<int>(g.TakimPuanlari);
                            foreach (var p in e.PuanAnlari)
                            {
                                Assert.IsTrue(p.Kim.HasValue);
                                Assert.AreEqual(p.Takim, TakimNe(g, p.Kim.Value), "puanı alan kendi takımına");
                            }
                        }
                        if (e.CezaOnayBekliyor) a.CezalariOnayla();
                        a.Ilerle(TimeSpan.FromSeconds(1));
                        break;
                    default:
                        a.Ilerle(TimeSpan.FromSeconds(1));
                        break;
                }
            }
            Assert.AreEqual(EkranTuru.OyunSonu, a.Ekran.Tur, "oyun bitmedi");
            return gz;
        }

        [Test]
        public void KurucuDagilimi_OyunaGider_DengesiziKurulumReddeder()
        {
            var dagilim = new Dictionary<OyuncuId, int>
            {
                [new OyuncuId(1)] = 1, [new OyuncuId(2)] = 0, [new OyuncuId(3)] = 1, [new OyuncuId(4)] = 0,
            };
            var a = Kur(4, dagilim);
            foreach (var o in a.Ekran.Gorunum.Oyuncular) Assert.AreEqual(dagilim[o.Id], o.Takim);
            CollectionAssert.AreEqual(new[] { 0, 0 }, a.Ekran.Gorunum.TakimPuanlari, "eşit takımlar sıfırdan");

            var dengesiz = new Dictionary<OyuncuId, int>(dagilim) { [new OyuncuId(2)] = 1 };
            var ayar = new Ayarlar { Oyun = OyunTuru.Takim, TakimDagilimi = dengesiz };
            Assert.AreEqual(Sonuc.GecersizGirdi, EldenEleAkisi.Kur(ayar, Oyuncular(4), 1, out _));
        }

        [Test]
        public void TekSayida_Rastgele_DengeliVeKucukTakimAvansli()
        {
            var a = Kur(5);
            var g = a.Ekran.Gorunum;
            int kirmizi = g.Oyuncular.Count(o => o.Takim == 0);
            int mavi = g.Oyuncular.Count(o => o.Takim == 1);
            Assert.AreEqual(5, kirmizi + mavi, "herkes bir takımda");
            Assert.AreEqual(1, Math.Abs(kirmizi - mavi), "rastgele ama dengeli");
            int kucuk = kirmizi < mavi ? 0 : 1;
            Assert.AreEqual(1, g.TakimPuanlari[kucuk], "küçük takım +1 avansla");
            Assert.AreEqual(0, g.TakimPuanlari[1 - kucuk]);
        }

        [Test]
        public void RakibeOynayinca_PuanAnlariSkoruTutar_YedideOyunBiter()
        {
            var a = Kur(4);
            var gz = Oyna(a, rakibe: true);

            var t = a.Ekran.Ozet.Takim;
            Assert.IsNotNull(t, "özette Takım Düellosu");
            Assert.IsTrue(t.Kazanan.HasValue, "sınırsız turda biri 7'ye ulaşır");
            Assert.GreaterOrEqual(t.Puanlar[t.Kazanan.Value], Oyun.HedefPuan);
            Assert.Greater(t.Puanlar[t.Kazanan.Value], t.Puanlar[1 - t.Kazanan.Value]);
            Assert.IsTrue(gz.Anlar.Any(p => !p.IfsaBonusu), "düello puanı geldi");
            Assert.That(gz.SonEkranBitmeden, Is.EqualTo(EkranTuru.Ifsa).Or.EqualTo(EkranTuru.Gume),
                "7'yi getiren turun sonucu oyun sonundan önce gösterildi");
            Assert.GreaterOrEqual(gz.SonSkorBitmeden[t.Kazanan.Value], Oyun.HedefPuan, "son sonuç ekranında 7 görünür");
        }

        [Test]
        public void TakimArkadasinaVerince_DuelloPuaniYok_TurBitinceOndeOlanYaDaBerabere()
        {
            var a = Kur(4, tur: 4, tohum: 9);
            var gz = Oyna(a, rakibe: false);
            Assert.AreEqual(4, gz.SonucEkrani);
            Assert.IsFalse(gz.Anlar.Any(p => !p.IfsaBonusu), "aynı takımdan B ile C: düello puanı yok");

            var t = a.Ekran.Ozet.Takim;
            Assert.IsNotNull(t);
            if (t.Puanlar[0] == t.Puanlar[1]) Assert.IsNull(t.Kazanan, "eşitlikte berabere");
            else Assert.AreEqual(t.Puanlar[0] > t.Puanlar[1] ? 0 : 1, t.Kazanan);
        }

        [Test]
        public void OyunuBitir_BasindaBerabere()
        {
            var a = Kur(4);
            Assert.AreEqual(Sonuc.Tamam, a.OyunuBitir());
            Assert.AreEqual(EkranTuru.OyunSonu, a.Ekran.Tur);
            Assert.IsNull(a.Ekran.Ozet.Takim.Kazanan, "0-0: berabere");
            CollectionAssert.AreEqual(new[] { 0, 0 }, a.Ekran.Ozet.Takim.Puanlar);
        }
    }
}
