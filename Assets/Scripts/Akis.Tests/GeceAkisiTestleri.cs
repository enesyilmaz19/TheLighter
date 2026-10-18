using System;
using System.Collections.Generic;
using System.Linq;
using Cakmak.Kurallar;
using NUnit.Framework;
using TheLighter.Akis;

namespace TheLighter.Tests
{
    /// <summary>
    /// Çakmak Gecesi elden ele (tasks/g2-cakmak-gecesi.md 4): kişi sınırı (18 / 10), rol perdesi (rol sadece sahibinde, katil
    /// listesi sadece katillerde, Deli'ye sandığı rol), gece dolaşması (her canlı bir kez, ölüler atlanır), bulgular ve katil
    /// bilgisi sadece sahibinin kendi ekranında, sabah duyurusu. Gerçek kural motoruyla.
    /// </summary>
    [TestFixture]
    public class GeceAkisiTestleri
    {
        static readonly string[] Isimler =
        {
            "Ali", "Ayşe", "Mehmet", "Zeynep", "Can", "Elif", "Burak", "Deniz", "Emre", "Selin",
            "Kaan", "Derya", "Umut", "Gül", "Oğuz", "Irmak", "Tolga", "Sena",
        };

        static List<Oyuncu> Kisiler(int n)
        {
            var l = new List<Oyuncu>();
            for (int i = 0; i < n; i++) l.Add(new Oyuncu(new OyuncuId(i + 1), Isimler[i]));
            return l;
        }

        static Ayarlar Ayar(int kurban = 1, bool rolAcilir = true) => new Ayarlar
        {
            Oyun = OyunTuru.CakmakGecesi, MiniOyun = MiniOyunTuru.Zar, TurSayisi = 0, GecelikKurban = kurban, RolAcilir = rolAcilir,
            TekCihaz = true,
        };

        static EldenEleAkisi Kur(int n, int tohum, int kurban = 1, bool rolAcilir = true)
        {
            Assert.AreEqual(Sonuc.Tamam, EldenEleAkisi.Kur(Ayar(kurban, rolAcilir), Kisiler(n), tohum, out var a));
            return a;
        }

        static void HerkeseAcikTemiz(Gorunum g, string nerede)
        {
            Assert.AreEqual(Rol.Yok, g.Rolun, nerede + ": rol yok");
            Assert.IsEmpty(g.Katiller, nerede + ": katil listesi yok");
            Assert.IsEmpty(g.Bulgularin, nerede + ": bulgu yok");
            Assert.IsEmpty(g.KatilSecimleri, nerede + ": katil oyları yok");
            Assert.IsNull(g.CellatHedefin, nerede + ": Cellat'ın hedefi yok");
            Assert.IsNull(g.KulakSorusu, nerede + ": Kulak'ın sorusu yok");
        }

        /// <summary>Oyun başındaki rol turunu bitirir; her rol ekranındaki görünümü döndürür.</summary>
        static Dictionary<OyuncuId, Gorunum> RolTurunuGec(EldenEleAkisi a)
        {
            var roller = new Dictionary<OyuncuId, Gorunum>();
            for (int i = 0; i < 60; i++)
            {
                var e = a.Ekran;
                if (e.Tur == EkranTuru.Perde && e.PerdeNedeni == PerdeNedeni.Gorev)
                {
                    HerkeseAcikTemiz(e.Gorunum, "rol perdesi öncesi");
                    a.PerdeyiKaldir();
                }
                else if (e.Tur == EkranTuru.GorevGoster)
                {
                    Assert.AreEqual(e.Kime, e.Gorunum.Sen, "rol ekranı sahibinin görünümüyle");
                    roller[e.Kime.Value] = e.Gorunum;
                    a.GorevGordum();
                }
                else return roller;
            }
            Assert.Fail("rol turu bitmedi");
            return roller;
        }

        [Test]
        public void KisiSiniri_Gece18_Digerleri10()
        {
            Assert.AreEqual(Oyun.GeceEnFazlaOyuncu, OyuncuSayisi.EnFazla(OyunTuru.CakmakGecesi));
            Assert.AreEqual(Oyun.GeceEnAzOyuncu, OyuncuSayisi.EnAz(OyunTuru.CakmakGecesi));
            Assert.AreEqual(Oyun.EnFazlaOyuncu, OyuncuSayisi.EnFazla(OyunTuru.Cakmak));
            Assert.AreEqual(18, OyuncuSayisi.HepsininEnFazlasi);
            Assert.AreEqual(Sonuc.Tamam, EldenEleAkisi.Kur(Ayar(), Kisiler(18), 1, out _), "Çakmak Gecesi 18 kişiyle");
            Assert.AreEqual(Sonuc.OyuncuSayisiGecersiz, EldenEleAkisi.Kur(Ayar(), Kisiler(5), 1, out _), "Çakmak Gecesi en az 6");
            Assert.AreEqual(Sonuc.OyuncuSayisiGecersiz, EldenEleAkisi.Kur(new Ayarlar(), Kisiler(11), 1, out _), "diğer oyunlar en fazla 10");
        }

        [Test]
        public void RolPerdesi_RolSadeceSahibinde_KatilListesiSadeceKatillerde()
        {
            var a = Kur(12, tohum: 3);
            var roller = RolTurunuGec(a);
            Assert.AreEqual(12, roller.Count, "herkes rol perdesinden geçer");
            int katil = 0;
            foreach (var r in roller)
            {
                Assert.AreNotEqual(Rol.Yok, r.Value.Rolun);
                if (Oyun.TarafI(r.Value.Rolun) == Taraf.Katil)
                {
                    katil++;
                    Assert.IsNotEmpty(r.Value.Katiller, "katil öbür katilleri görür");
                    CollectionAssert.Contains(r.Value.Katiller.Select(k => k.Kim).ToList(), r.Key);
                }
                else Assert.IsEmpty(r.Value.Katiller, "katil olmayanda katil listesi yok");
            }
            Assert.AreEqual(3, katil, "12 kişide 3 katil");
            // Perde kalkınca ortadaki ekranda kimsenin rolü yok.
            Assert.AreEqual(EkranTuru.Perde, a.Ekran.Tur);
            HerkeseAcikTemiz(a.Ekran.Gorunum, "ilk tur perdesi");
        }

        [Test]
        public void Deli_PerdedeSandigiRolu_GercegiOyunSonunda()
        {
            // Deli her oyunda çıkmaz: çıkan ilk tohumu bul.
            for (int tohum = 1; tohum < 80; tohum++)
            {
                var a = Kur(14, tohum);
                var roller = RolTurunuGec(a);
                a.OyunuBitir();
                Assert.AreEqual(EkranTuru.OyunSonu, a.Ekran.Tur);
                var gercek = a.Ekran.Ozet.Gece.Roller.FirstOrDefault(r => r.Rol == Rol.Deli);
                if (gercek == null) continue;
                var gordugu = roller[gercek.Kim].Rolun;
                Assert.AreNotEqual(Rol.Deli, gordugu, "Deli kendini Deli görmez");
                CollectionAssert.Contains(new[] { Rol.Koruyucu, Rol.Dedektif, Rol.Bekci, Rol.Falci }, gordugu);
                Assert.IsNull(a.Ekran.Ozet.Gece.Kazanan, "kurucu bitirdi: kazanan yok");
                return;
            }
            Assert.Inconclusive("80 tohumda Deli çıkmadı");
        }

        sealed class Gozlem
        {
            public readonly Dictionary<int, List<OyuncuId>> GeceEkranlari = new Dictionary<int, List<OyuncuId>>();
            public readonly Dictionary<int, HashSet<OyuncuId>> GeceBaslarkenCanlilar = new Dictionary<int, HashSet<OyuncuId>>();
            public int LincEkrani;
            public int SabahDuyurusu;
            public int Gun;
        }

        /// <summary>Oyunu <paramref name="enFazlaGece"/> gece bitene ya da oyun bitene kadar oynar; her ekranda gizliliği denetler.</summary>
        static Gozlem Oyna(EldenEleAkisi a, int enFazlaGece)
        {
            var gz = new Gozlem();
            for (int adim = 0; adim < 8000 && a.Ekran.Tur != EkranTuru.OyunSonu; adim++)
            {
                var e = a.Ekran;
                var g = e.Gorunum;
                gz.Gun = Math.Max(gz.Gun, g.Gun);
                if (gz.GeceEkranlari.Count >= enFazlaGece && g.Asama != Asama.Gece) break;
                foreach (var d in e.GeceDuyurulari) if (d.Tur == GeceDuyurusu.Turu.Sabah) gz.SabahDuyurusu++;
                switch (e.Tur)
                {
                    case EkranTuru.Perde:
                    case EkranTuru.MiniOyunPerdesi:
                        HerkeseAcikTemiz(g, "perde");
                        a.PerdeyiKaldir();
                        break;
                    case EkranTuru.GorevGoster:
                        a.GorevGordum();
                        break;
                    case EkranTuru.SoruSorma:
                        Assert.AreEqual(e.Kime, g.Sen);
                        a.SoruGonder(g.Secilebilir[0], "Sence katil kim?");
                        break;
                    case EkranTuru.CevapSecme:
                        a.CevapSec(g.Secilebilir[0]);
                        break;
                    case EkranTuru.MiniOyun:
                        HerkeseAcikTemiz(g, "mini oyun");
                        if (!g.BHamleYapti) a.Hamle(g.B.Value, Hamle.ZarAt());
                        else a.Hamle(g.C.Value, Hamle.ZarAt());
                        break;
                    case EkranTuru.SuclamaTartisma:
                        Assert.AreEqual(Asama.Linc, g.Asama, "Çakmak Gecesi'nde tartışma linçten önce");
                        HerkeseAcikTemiz(g, "linç tartışması");
                        a.TartismayiBitir();
                        break;
                    case EkranTuru.Linc:
                        gz.LincEkrani++;
                        Assert.AreEqual(e.Kime, g.Sen, "linç ekranı oy verenin kendi görünümüyle");
                        Assert.IsFalse(g.Oyuncular.First(o => o.Id == g.Sen).Elendi, "ölü oy vermez");
                        Assert.AreEqual(Sonuc.Tamam, a.LincOyu(g.Secilebilir[0]));
                        break;
                    case EkranTuru.Gece:
                    {
                        Assert.AreEqual(e.Kime, g.Sen, "gece ekranı seçenin kendi görünümüyle");
                        if (!gz.GeceEkranlari.TryGetValue(g.Gun, out var liste))
                        {
                            gz.GeceEkranlari[g.Gun] = liste = new List<OyuncuId>();
                            gz.GeceBaslarkenCanlilar[g.Gun] = new HashSet<OyuncuId>(g.Oyuncular.Where(o => !o.Elendi && !o.Cikti).Select(o => o.Id));
                        }
                        Assert.IsFalse(liste.Contains(g.Sen), "gece telefon aynı kişiye iki kez gelmez");
                        liste.Add(g.Sen);
                        Assert.IsFalse(g.Oyuncular.First(o => o.Id == g.Sen).Elendi, "ölüye gece ekranı gelmez");
                        Assert.AreEqual(Sonuc.Tamam, a.GeceSecimi(g.Secilebilir.Count > 0 ? g.Secilebilir[0] : (OyuncuId?)null,
                            g.IkinciSecilebilir.Count > 0 ? g.IkinciSecilebilir[0] : (OyuncuId?)null));
                        break;
                    }
                    case EkranTuru.Ifsa:
                    case EkranTuru.Gume:
                        HerkeseAcikTemiz(g, "sonuç");
                        a.Ilerle(TimeSpan.FromSeconds(1));
                        break;
                    default:
                        a.Ilerle(TimeSpan.FromSeconds(1));
                        break;
                }
            }
            return gz;
        }

        [Test]
        public void Gece_HerCanliBirKez_OluAtlanir_SabahDuyurusu()
        {
            var a = Kur(8, tohum: 5);
            RolTurunuGec(a);
            var gz = Oyna(a, enFazlaGece: 2);
            Assert.Greater(gz.LincEkrani, 0, "linç oldu");
            Assert.GreaterOrEqual(gz.GeceEkranlari.Count, 1, "en az bir gece");
            foreach (var gece in gz.GeceEkranlari)
                CollectionAssert.AreEquivalent(gz.GeceBaslarkenCanlilar[gece.Key], gece.Value, "gece telefonu her canlıya bir kez gelir (gün " + gece.Key + ")");
            if (!a.Bitti) Assert.Greater(gz.SabahDuyurusu, 0, "sabah duyurusu perdede");
        }

        [Test]
        public void OnIkiKisi_RolGizli_IkiKurban_OyunBiter()
        {
            var a = Kur(12, tohum: 11, kurban: 2, rolAcilir: false);
            RolTurunuGec(a);
            Oyna(a, enFazlaGece: 30);
            Assert.AreEqual(EkranTuru.OyunSonu, a.Ekran.Tur, "oyun bir tarafın kazanmasıyla biter");
            var s = a.Ekran.Ozet.Gece;
            Assert.IsNotNull(s);
            Assert.IsTrue(s.Kazanan.HasValue, "kazanan var");
            Assert.AreEqual(12, s.Roller.Count, "herkesin gerçek rolü özette");
            // Rol gizli ayarında oyun boyunca kimsenin rolü açılmadı.
            Assert.IsTrue(a.Ekran.Gorunum.Oyuncular.All(o => !o.AcikRol.HasValue), "rol açılmaz ayarı");
        }
    }
}
