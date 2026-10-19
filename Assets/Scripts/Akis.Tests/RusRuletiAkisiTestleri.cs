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
    /// Rus Ruleti elden ele (tasks/g2-rus-ruleti.md 4): tetik ekranı kaybedende açılıyor, devret başkasına geçiyor,
    /// elenen hiçbir seçimde görünmüyor, final duyurusu bir kez geliyor. Gerçek kural motoruyla, oyun baştan sona.
    /// </summary>
    [TestFixture]
    public class RusRuletiAkisiTestleri
    {
        static EldenEleAkisi Kur(int can, int oyuncu = 5, int tohum = 7)
        {
            var ayar = new Ayarlar { Oyun = OyunTuru.RusRuleti, Can = can, MiniOyun = MiniOyunTuru.Zar };
            Assert.AreEqual(Sonuc.Tamam, EldenEleAkisi.Kur(ayar, Oyuncular(oyuncu), tohum, out var a));
            return a;
        }

        sealed class Gozlem
        {
            public int TetikEkrani;
            public int Devret;
            public int FinalDuyurusu;
            public int TetikSonucu;
            public readonly HashSet<OyuncuId> Elenenler = new HashSet<OyuncuId>();
        }

        /// <summary>Oyunu baştan sona oynar. Her ekranda değişmezleri denetler.</summary>
        static Gozlem Oyna(EldenEleAkisi a, bool devretDene)
        {
            var gz = new Gozlem();
            for (int adim = 0; adim < 5000 && a.Ekran.Tur != EkranTuru.OyunSonu; adim++)
            {
                var e = a.Ekran;
                var g = e.Gorunum;
                foreach (var o in g.Oyuncular) if (o.Elendi) gz.Elenenler.Add(o.Id);
                foreach (var id in g.Secilebilir)
                    Assert.IsFalse(gz.Elenenler.Contains(id), "elenen seçilebilir listesinde: " + id);
                if (e.FinalDuyurusu) gz.FinalDuyurusu++;
                if (e.TetikSonucu != null) gz.TetikSonucu++;

                switch (e.Tur)
                {
                    case EkranTuru.Perde:
                    case EkranTuru.MiniOyunPerdesi:
                        a.PerdeyiKaldir();
                        break;
                    case EkranTuru.SoruSorma:
                        Assert.AreEqual(Sonuc.Tamam, a.SoruGonder(g.Secilebilir[0], "Kim en geç uyur?"));
                        break;
                    case EkranTuru.CevapSecme:
                        Assert.AreEqual(Sonuc.Tamam, a.CevapSec(g.Secilebilir[0]));
                        break;
                    case EkranTuru.MiniOyun:
                        if (!g.BHamleYapti) a.Hamle(g.B.Value, Hamle.ZarAt());
                        else a.Hamle(g.C.Value, Hamle.ZarAt());
                        break;
                    case EkranTuru.Tetik:
                    {
                        gz.TetikEkrani++;
                        var cekecek = e.Kime.Value;
                        Assert.AreEqual(g.TetikCeken, cekecek, "tetik ekranı çekecek kişinin");
                        Assert.IsFalse(gz.Elenenler.Contains(cekecek));
                        var ben = g.Oyuncular.First(o => o.Id == cekecek);
                        if (devretDene && ben.DevretmeHakki)
                        {
                            var hedef = g.Oyuncular.First(o => o.Id != cekecek && !o.Elendi).Id;
                            Assert.AreEqual(Sonuc.Tamam, a.TetikDevret(hedef));
                            Assert.AreEqual(EkranTuru.Tetik, a.Ekran.Tur);
                            Assert.AreEqual(hedef, a.Ekran.Kime, "devret başkasına geçti");
                            Assert.IsFalse(a.Ekran.Gorunum.Oyuncular.First(o => o.Id == cekecek).DevretmeHakki, "devredenin hakkı bitti");
                            gz.Devret++;
                        }
                        else Assert.AreEqual(Sonuc.Tamam, a.TetikCek());
                        break;
                    }
                    default:
                        // Sonuç ekranları ve geçişler: kural motorunun süresi.
                        a.Ilerle(TimeSpan.FromSeconds(1));
                        break;
                }
            }
            Assert.AreEqual(EkranTuru.OyunSonu, a.Ekran.Tur, "oyun bitmedi");
            return gz;
        }

        [Test]
        public void Can1_BastanSona_FinalBirKez_KazananVeElenmeSirasi()
        {
            var a = Kur(can: 1);
            var gz = Oyna(a, devretDene: false);
            Assert.Greater(gz.TetikEkrani, 0);
            Assert.AreEqual(1, gz.FinalDuyurusu, "final duyurusu bir kez");
            Assert.Greater(gz.TetikSonucu, 0, "tetik sonucu duyuruldu");
            var r = a.Ekran.Ozet.RusRuleti;
            Assert.IsNotNull(r);
            Assert.IsTrue(r.Kazanan.HasValue);
            Assert.AreEqual(4, r.ElenmeSirasi.Count, "5 kişiden 4'ü elenir");
            Assert.IsFalse(r.ElenmeSirasi.Contains(r.Kazanan.Value));
        }

        [Test]
        public void Can3_Devret_TetikBaskasina()
        {
            var a = Kur(can: 3, oyuncu: 4, tohum: 11);
            var gz = Oyna(a, devretDene: true);
            Assert.Greater(gz.Devret, 0, "en az bir devret");
            Assert.IsNotNull(a.Ekran.Ozet.RusRuleti);
        }

        [Test]
        public void OyunuBitir_EnCokCaniOlanYaDaKazananYok()
        {
            var a = Kur(can: 2);
            Assert.AreEqual(Sonuc.Tamam, a.OyunuBitir());
            Assert.AreEqual(EkranTuru.OyunSonu, a.Ekran.Tur);
            Assert.IsNull(a.Ekran.Ozet.RusRuleti.Kazanan, "herkesin canı eşit: kazanan yok");
        }
    }
}
