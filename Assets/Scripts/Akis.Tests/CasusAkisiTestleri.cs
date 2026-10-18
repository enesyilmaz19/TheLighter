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
    /// Casus elden ele (tasks/g2-casus.md 4): rol perdesinde casusa sır hiç gelmiyor, ortadaki ekranlarda kimsenin sırrı yok,
    /// casus olmayanın "Mekânı biliyorum"u hiçbir şeyi değiştirmiyor, oylamada yakalanınca son şans ekranı, tahmin, oyun sonu.
    /// Gerçek kural motoruyla.
    /// </summary>
    [TestFixture]
    public class CasusAkisiTestleri
    {
        static EldenEleAkisi Kur(CasusTuru tur = CasusTuru.Mekan, int oyuncu = 5, int tohum = 4)
        {
            var ayar = new Ayarlar { Oyun = OyunTuru.Casus, Casus = tur, TurSayisi = 10 };
            Assert.AreEqual(Sonuc.Tamam, EldenEleAkisi.Kur(ayar, Oyuncular(oyuncu), tohum, out var a));
            return a;
        }

        /// <summary>Oyun başındaki rol turunu bitirir; her rol ekranındaki görünümü döndürür. Perdede kimsenin sırrı yok.</summary>
        static Dictionary<OyuncuId, Gorunum> RolTurunuGec(EldenEleAkisi a)
        {
            var roller = new Dictionary<OyuncuId, Gorunum>();
            for (int i = 0; i < 50; i++)
            {
                var e = a.Ekran;
                if (e.Tur == EkranTuru.Perde && e.PerdeNedeni == PerdeNedeni.Gorev)
                {
                    Assert.IsNull(e.Gorunum.Sirrin, "perdede sır yok");
                    Assert.AreEqual(Rol.Yok, e.Gorunum.Rolun, "perdede rol yok");
                    a.PerdeyiKaldir();
                }
                else if (e.Tur == EkranTuru.GorevGoster)
                {
                    roller[e.Kime.Value] = e.Gorunum;
                    a.GorevGordum();
                }
                else return roller;
            }
            Assert.Fail("rol turu bitmedi");
            return roller;
        }

        static OyuncuId CasusBul(Dictionary<OyuncuId, Gorunum> roller) => roller.First(r => r.Value.Rolun == Rol.Casus).Key;

        [Test]
        public void RolPerdesi_CasusaSirYok_DigerlerineMekanVeRol()
        {
            var a = Kur();
            var roller = RolTurunuGec(a);
            Assert.AreEqual(5, roller.Count, "herkes rol perdesinden geçer");
            Assert.AreEqual(1, roller.Values.Count(g => g.Rolun == Rol.Casus), "tek casus");
            var casus = CasusBul(roller);
            Assert.IsNull(roller[casus].Sirrin, "casusa sır hiç gelmez");
            Assert.IsNull(roller[casus].MekanRolun);
            string sir = null;
            foreach (var r in roller)
            {
                Assert.Greater(r.Value.CasusListesi.Count, 0, "liste herkeste");
                Assert.IsNull(r.Value.CasusKategori, "mekânda kategori yok");
                if (r.Key == casus) continue;
                Assert.IsNotNull(r.Value.Sirrin);
                Assert.IsNotNull(r.Value.MekanRolun, "mekânda herkesin rolü");
                Assert.AreEqual(Rol.Yok, r.Value.Rolun);
                sir = sir ?? r.Value.Sirrin;
                Assert.AreEqual(sir, r.Value.Sirrin, "herkes aynı mekânı bilir");
                CollectionAssert.Contains(r.Value.CasusListesi.ToList(), sir);
            }
            Assert.AreEqual(EkranTuru.Perde, a.Ekran.Tur);
            Assert.AreEqual(PerdeNedeni.SoruSirasi, a.Ekran.PerdeNedeni);
        }

        [Test]
        public void Kelime_KategoriHerkeste_RolYok()
        {
            var a = Kur(CasusTuru.Kelime);
            var roller = RolTurunuGec(a);
            var casus = CasusBul(roller);
            foreach (var r in roller)
            {
                Assert.IsNotNull(r.Value.CasusKategori, "kategori herkese açık");
                Assert.IsNull(r.Value.MekanRolun, "kelimede rol yok");
                if (r.Key != casus) Assert.IsNotNull(r.Value.Sirrin);
            }
            Assert.IsNull(roller[casus].Sirrin);
        }

        [Test]
        public void OrtadakiEkranlar_KimseninSirriYok()
        {
            var a = Kur();
            var roller = RolTurunuGec(a);
            for (int i = 0; i < 300 && a.Ekran.Tur != EkranTuru.OyunSonu && a.Ekran.Gorunum.Tur < 3; i++)
            {
                var e = a.Ekran;
                var g = e.Gorunum;
                switch (e.Tur)
                {
                    case EkranTuru.Perde:
                    case EkranTuru.MiniOyunPerdesi:
                        Assert.IsNull(g.Sirrin, "perdede sır yok");
                        a.PerdeyiKaldir();
                        break;
                    case EkranTuru.SoruSorma:
                        // A'nın kendi ekranı: sırrı (casussa rolü) onda.
                        Assert.AreEqual(roller[e.Kime.Value].Sirrin, g.Sirrin);
                        a.SoruGonder(g.Secilebilir[0], "Burada kim en çok sıkılır?");
                        break;
                    case EkranTuru.CevapSecme:
                        a.CevapSec(g.Secilebilir[0]);
                        break;
                    case EkranTuru.MiniOyun:
                        Assert.IsNull(g.Sirrin, "ortadaki ekranda sır yok");
                        Assert.IsNull(e.BGorunumu.Sirrin);
                        Assert.IsNull(e.CGorunumu.Sirrin);
                        Assert.AreEqual(Rol.Yok, e.BGorunumu.Rolun);
                        a.Hamle(g.B.Value, Hamle.TasKagitMakas(TkmSecim.Tas));
                        a.Hamle(g.C.Value, Hamle.TasKagitMakas(TkmSecim.Kagit));
                        break;
                    default:
                        Assert.IsNull(g.Sirrin, e.Tur + " ekranında sır yok");
                        Assert.IsNull(g.MekanRolun);
                        Assert.AreEqual(Rol.Yok, g.Rolun);
                        a.Ilerle(TimeSpan.FromSeconds(1));
                        break;
                }
            }
        }

        [Test]
        public void CasusOlmayanBilirimDerse_HicbirSeyDegismez()
        {
            var a = Kur();
            var roller = RolTurunuGec(a);
            var casus = CasusBul(roller);
            var masum = roller.Keys.First(k => k != casus);
            string sir = roller[masum].Sirrin;
            a.PerdeyiKaldir();
            var anahtar = a.Ekran.Anahtar;
            Assert.AreEqual(Sonuc.SiraSendeDegil, a.CasusTahmini(masum, sir), "casus değil: kural motoru reddeder");
            Assert.AreEqual(anahtar, a.Ekran.Anahtar, "ekran aynı kalır");
            Assert.IsFalse(a.Bitti);
        }

        [Test]
        public void CasusBilirimDer_Bilirse_OyunBiter()
        {
            var a = Kur(tohum: 8);
            var roller = RolTurunuGec(a);
            var casus = CasusBul(roller);
            string sir = roller.First(r => r.Key != casus).Value.Sirrin;
            Assert.AreEqual(Sonuc.GecersizGirdi, a.CasusTahmini(casus, "Listede olmayan yer"));
            Assert.AreEqual(Sonuc.Tamam, a.CasusTahmini(casus, sir));
            Assert.AreEqual(EkranTuru.OyunSonu, a.Ekran.Tur);
            var c = a.Ekran.Ozet.Casus;
            Assert.IsNotNull(c);
            Assert.AreEqual(casus, c.Casus);
            Assert.AreEqual(CasusKazanan.Casus, c.Kazanan);
            Assert.AreEqual(CasusNedeni.Bildi, c.Neden);
            Assert.AreEqual(sir, c.Sir);
            Assert.AreEqual(sir, c.Tahmin);
        }

        [Test]
        public void SonOylamadaYakalaninca_SonSans_TahminleOyunSonu()
        {
            var a = Kur(tohum: 6);
            var roller = RolTurunuGec(a);
            var casus = CasusBul(roller);
            string sir = roller.First(r => r.Key != casus).Value.Sirrin;
            string yanlis = roller[casus].CasusListesi.First(m => m != sir);

            Assert.AreEqual(Sonuc.Tamam, a.OyunuBitir(), "Casus'ta bitir önce son oylamayı açar");
            Assert.AreEqual(EkranTuru.SuclamaTartisma, a.Ekran.Tur);
            a.TartismayiBitir();
            var baska = roller.Keys.First(k => k != casus);
            for (int i = 0; i < 40 && a.Ekran.Gorunum.Asama == Asama.Suclama; i++)
            {
                var e = a.Ekran;
                if (e.Tur == EkranTuru.Perde) { a.PerdeyiKaldir(); continue; }
                Assert.AreEqual(EkranTuru.Suclama, e.Tur);
                Assert.IsNull(e.Gorunum.Sirrin, "oy ekranında sır durmaz");
                Assert.AreEqual(Sonuc.Tamam, a.SuclamaOyu(e.Kime == casus ? baska : casus));
            }

            Assert.AreEqual(EkranTuru.CasusTahmini, a.Ekran.Tur, "yakalandı: son şans");
            Assert.AreEqual(casus, a.Ekran.Kime, "telefon casusta");
            Assert.AreEqual(casus, a.Ekran.Gorunum.Yakalanan, "casus herkese açıldı");
            Assert.IsNull(a.Ekran.Gorunum.Sirrin, "son şans ekranı ortada: sır yok");
            Assert.Greater(a.Ekran.Gorunum.CasusListesi.Count, 0);
            Assert.IsFalse(a.Ekran.SaatDurur, "son şansın 30 sn'si işler");

            Assert.AreEqual(Sonuc.Tamam, a.CasusTahmini(casus, yanlis));
            Assert.AreEqual(EkranTuru.OyunSonu, a.Ekran.Tur);
            var c = a.Ekran.Ozet.Casus;
            Assert.AreEqual(CasusKazanan.Grup, c.Kazanan);
            Assert.AreEqual(CasusNedeni.Yakalandi, c.Neden);
            Assert.AreEqual(yanlis, c.Tahmin);
        }

        [Test]
        public void SonSansinSuresiDolunca_GrupKazanir()
        {
            var a = Kur(tohum: 2);
            var roller = RolTurunuGec(a);
            var casus = CasusBul(roller);
            var baska = roller.Keys.First(k => k != casus);
            a.OyunuBitir();
            a.TartismayiBitir();
            for (int i = 0; i < 40 && a.Ekran.Gorunum.Asama == Asama.Suclama; i++)
            {
                if (a.Ekran.Tur == EkranTuru.Perde) { a.PerdeyiKaldir(); continue; }
                a.SuclamaOyu(a.Ekran.Kime == casus ? baska : casus);
            }
            Assert.AreEqual(EkranTuru.CasusTahmini, a.Ekran.Tur);
            for (int i = 0; i < Oyun.CasusTahminiSn + 2 && !a.Bitti; i++) a.Ilerle(TimeSpan.FromSeconds(1));
            Assert.AreEqual(EkranTuru.OyunSonu, a.Ekran.Tur);
            Assert.AreEqual(CasusKazanan.Grup, a.Ekran.Ozet.Casus.Kazanan);
            Assert.IsNull(a.Ekran.Ozet.Casus.Tahmin, "tahmin etmedi");
        }
    }
}
