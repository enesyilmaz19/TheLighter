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
    /// Köstebek elden ele (tasks/g2-kostebek.md 5): rol perdesinde rol sadece köstebeğe görünüyor, suçlama oylaması
    /// telefonla dolaşıyor, "Oyunu bitir" son oylamayı açıyor. Gerçek kural motoruyla.
    /// </summary>
    [TestFixture]
    public class KostebekAkisiTestleri
    {
        static EldenEleAkisi Kur(int oyuncu = 5, int tohum = 3)
        {
            var ayar = new Ayarlar { Oyun = OyunTuru.Kostebek, TurSayisi = 10 };
            Assert.AreEqual(Sonuc.Tamam, EldenEleAkisi.Kur(ayar, Oyuncular(oyuncu), tohum, out var a));
            return a;
        }

        /// <summary>Oyun başındaki rol/görev turunu bitirir. Her görev ekranında görünen rolü döndürür.</summary>
        static Dictionary<OyuncuId, Rol> RolTurunuGec(EldenEleAkisi a)
        {
            var roller = new Dictionary<OyuncuId, Rol>();
            for (int i = 0; i < 50; i++)
            {
                var e = a.Ekran;
                if (e.Tur == EkranTuru.Perde && e.PerdeNedeni == PerdeNedeni.Gorev)
                {
                    Assert.AreEqual(Rol.Yok, e.Gorunum.Rolun, "perdede kimsenin rolü yok");
                    Assert.IsNull(e.Gorunum.Gorevin, "perdede kimsenin görevi yok");
                    a.PerdeyiKaldir();
                }
                else if (e.Tur == EkranTuru.GorevGoster)
                {
                    roller[e.Kime.Value] = e.Gorunum.Rolun;
                    Assert.IsNotNull(e.Gorunum.Gorevin, "Köstebek'te herkesin görevi var (ayar kapalı olsa da)");
                    a.GorevGordum();
                }
                else return roller;
            }
            Assert.Fail("rol turu bitmedi");
            return roller;
        }

        /// <summary>Suçlama oylamasını herkesin oyuyla bitirir: oy veren telefonu alınca hedefi seçer.</summary>
        static void Oyla(EldenEleAkisi a, Func<OyuncuId, OyuncuId> hedef)
        {
            Assert.AreEqual(EkranTuru.SuclamaTartisma, a.Ekran.Tur);
            a.TartismayiBitir();
            var verenler = new HashSet<OyuncuId>();
            for (int i = 0; i < 40 && a.Ekran.Gorunum.Asama == Asama.Suclama; i++)
            {
                var e = a.Ekran;
                if (e.Tur == EkranTuru.Perde)
                {
                    Assert.AreEqual(PerdeNedeni.Suclama, e.PerdeNedeni);
                    a.PerdeyiKaldir();
                    continue;
                }
                Assert.AreEqual(EkranTuru.Suclama, e.Tur);
                var kim = e.Kime.Value;
                Assert.IsTrue(verenler.Add(kim), "aynı kişiye telefon iki kez gelmez");
                Assert.AreEqual(kim, e.Gorunum.Sen);
                Assert.AreEqual(Rol.Yok, e.Gorunum.Rolun, "oy ekranında rol durmaz");
                Assert.AreEqual(Sonuc.Tamam, a.SuclamaOyu(hedef(kim)));
            }
        }

        [Test]
        public void RolPerdesi_HerkesGorevGorur_RolSadeceKostebekte()
        {
            var a = Kur();
            var roller = RolTurunuGec(a);
            Assert.AreEqual(5, roller.Count, "herkes perdeden geçer");
            Assert.AreEqual(1, roller.Values.Count(r => r == Rol.Kostebek), "tek köstebek, rolü sadece kendi ekranında");
            Assert.AreEqual(EkranTuru.Perde, a.Ekran.Tur);
            Assert.AreEqual(PerdeNedeni.SoruSirasi, a.Ekran.PerdeNedeni);
            Assert.AreEqual(Rol.Yok, a.Ekran.Gorunum.Rolun);
        }

        [Test]
        public void ErkenSuclama_TartismaSonraOylamaDolasir_YanlisKisiSupheliOlur()
        {
            var a = Kur();
            var roller = RolTurunuGec(a);
            var kostebek = roller.First(r => r.Value == Rol.Kostebek).Key;
            a.PerdeyiKaldir();
            Assert.AreEqual(EkranTuru.SoruSorma, a.Ekran.Tur);
            var soran = a.Ekran.Kime.Value;

            Assert.AreEqual(5, a.SuclamaHakkiOlanlar.Count, "başta herkesin hakkı var");
            var suclayan = a.Oyuncular.First(o => o.Id != soran).Id;
            Assert.AreEqual(Sonuc.Tamam, a.Sucla(suclayan));
            Assert.AreEqual(EkranTuru.SuclamaTartisma, a.Ekran.Tur);
            Assert.AreEqual(suclayan, a.Ekran.SuclamaBaslatan);
            Assert.IsTrue(a.Ekran.SaatDurur, "elden elede oylama boyunca kural motorunun saati durur");

            // Herkes köstebek olmayan aynı kişiyi gösterir (köstebek de kendini gösteremez: başka birini).
            var masum = a.Oyuncular.First(o => o.Id != kostebek).Id;
            var masum2 = a.Oyuncular.First(o => o.Id != kostebek && o.Id != masum).Id;
            Oyla(a, kim => kim == masum ? masum2 : masum);

            Assert.AreEqual(Asama.SoruSorma, a.Ekran.Gorunum.Asama, "yanlış suçlama: oyun sürer");
            Assert.AreEqual(EkranTuru.Perde, a.Ekran.Tur, "telefon A'ya geri döner");
            Assert.AreEqual(soran, a.Ekran.Kime);
            Assert.IsNotNull(a.Ekran.SuclamaSonucu);
            Assert.AreEqual(masum, a.Ekran.SuclamaSonucu.Suclanan);
            CollectionAssert.Contains(a.Ekran.Gorunum.Supheliler.ToList(), masum);
            Assert.IsFalse(a.SuclamaHakkiOlanlar.Contains(suclayan), "suçlayanın hakkı bitti");

            a.PerdeyiKaldir();
            Assert.IsNull(a.Ekran.SuclamaSonucu, "duyuru perdeyle kalkar");
            Assert.AreEqual(EkranTuru.SoruSorma, a.Ekran.Tur);
        }

        [Test]
        public void TartismaSuresiDolunca_OylamaBaslar()
        {
            var a = Kur();
            RolTurunuGec(a);
            a.PerdeyiKaldir();
            a.Sucla(a.Ekran.Kime.Value);
            Assert.AreEqual(EkranTuru.SuclamaTartisma, a.Ekran.Tur);
            for (int i = 0; i < 59; i++) a.Ilerle(TimeSpan.FromSeconds(1));
            Assert.AreEqual(EkranTuru.SuclamaTartisma, a.Ekran.Tur);
            a.Ilerle(TimeSpan.FromSeconds(2));
            Assert.AreEqual(EkranTuru.Perde, a.Ekran.Tur);
            Assert.AreEqual(PerdeNedeni.Suclama, a.Ekran.PerdeNedeni);
            Assert.AreEqual(Asama.Suclama, a.Ekran.Gorunum.Asama, "tartışmada kural motorunun süresi işlemedi");
        }

        [Test]
        public void OyunuBitir_SonOylamayiAcar_KostebekYakalanincaAcilis()
        {
            var a = Kur();
            var roller = RolTurunuGec(a);
            var kostebek = roller.First(r => r.Value == Rol.Kostebek).Key;

            Assert.AreEqual(Sonuc.Tamam, a.OyunuBitir());
            Assert.AreEqual(Asama.Suclama, a.Ekran.Gorunum.Asama);
            Assert.IsTrue(a.Ekran.Gorunum.SonSuclama, "son oylama");
            Assert.AreEqual(Sonuc.YanlisAsama, a.OyunuBitir(), "oylama sürerken ikinci bitir olmaz");

            var baska = a.Oyuncular.First(o => o.Id != kostebek).Id;
            Oyla(a, kim => kim == kostebek ? baska : kostebek);

            Assert.AreEqual(EkranTuru.OyunSonu, a.Ekran.Tur);
            Assert.IsNotNull(a.Ekran.Ozet.Kostebek);
            Assert.AreEqual(kostebek, a.Ekran.Ozet.Kostebek.Kostebek);
            Assert.AreEqual(KostebekKazanan.Grup, a.Ekran.Ozet.Kostebek.Kazanan);
            Assert.AreEqual(KostebekNedeni.Yakalandi, a.Ekran.Ozet.Kostebek.Neden);
        }

        [Test]
        public void Cakmak_RolYok_SuclamaHakkiYok()
        {
            var a = YeniAkis();
            Assert.AreEqual(0, a.SuclamaHakkiOlanlar.Count);
            a.PerdeyiKaldir();
            Assert.AreEqual(Sonuc.YanlisAsama, a.Sucla(a.Ekran.Kime.Value));
        }
    }
}
