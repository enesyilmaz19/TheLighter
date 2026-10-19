using System;
using System.Collections.Generic;
using System.Linq;
using Cakmak.Kurallar;
using NUnit.Framework;
using TheLighter.Ag;
using TheLighter.Akis;

namespace TheLighter.Testler
{
    /// <summary>Rus Ruleti online (tasks/g2-rus-ruleti.md 3, 4): çevirici yeni alanları, "tetik" aşaması, olaylar ve hedef, ozet.rusRuleti.</summary>
    public class RusRuletiOnlineTestleri
    {
        static readonly OyuncuId P1 = new OyuncuId(1), P2 = new OyuncuId(2), P3 = new OyuncuId(3);

        static string Durum(string asama, string ek = "", int surum = 5) =>
            "{\"t\":\"durum\",\"surum\":" + surum + ",\"oda\":\"KXTPM\",\"asama\":\"" + asama + "\",\"tur\":4,\"toplamTur\":0,\"kalanMs\":10000," +
            "\"kurucu\":\"p1\",\"b\":\"p1\",\"c\":\"p2\",\"oyuncular\":[" +
            "{\"id\":\"p1\",\"isim\":\"Enes\",\"renk\":0,\"bagli\":true,\"can\":2,\"elendi\":false,\"devretmeHakki\":true}," +
            "{\"id\":\"p2\",\"isim\":\"Ayşe\",\"renk\":1,\"bagli\":true,\"can\":1,\"elendi\":false,\"devretmeHakki\":false}," +
            "{\"id\":\"p3\",\"isim\":\"Can\",\"renk\":2,\"bagli\":true,\"can\":0,\"elendi\":true,\"devretmeHakki\":true}," +
            "{\"id\":\"p4\",\"isim\":\"Deniz\",\"renk\":3,\"bagli\":true,\"can\":1,\"elendi\":false,\"devretmeHakki\":true}]," +
            "\"oyun\":\"rusRuleti\",\"isi\":\"sicak\",\"sana\":{}" + ek + "}";

        [Test]
        public void Durum_RusRuletiAlanlari()
        {
            var d = (DurumMesaji)Cevirici.Coz(Durum("tetik", ",\"tetikCeken\":\"p1\",\"final\":true"), P2);
            var g = d.Gorunum;
            Assert.That(g.Asama, Is.EqualTo(Asama.Tetik));
            Assert.That(g.Oyun, Is.EqualTo(OyunTuru.RusRuleti));
            Assert.That(g.TetikCeken, Is.EqualTo(P1));
            Assert.That(g.Final, Is.True);
            Assert.That(g.Isi, Is.EqualTo(IsiSeviyesi.Sicak));
            Assert.That(g.ToplamTur, Is.EqualTo(0));
            var p1 = g.Oyuncular.First(o => o.Id == P1);
            Assert.That(p1.Can, Is.EqualTo(2));
            Assert.That(p1.DevretmeHakki, Is.True);
            var p3 = g.Oyuncular.First(o => o.Id == P3);
            Assert.That(p3.Elendi, Is.True);
        }

        [Test]
        public void Olaylar_TetikVeHedef()
        {
            var d = (OlayMesaji)Cevirici.Coz("{\"t\":\"olay\",\"olay\":\"tetikDevredildi\",\"kim\":\"p1\",\"hedef\":\"p4\"}");
            Assert.That(d.Olay.Tur, Is.EqualTo(OlayTuru.TetikDevredildi));
            Assert.That(d.Kim, Is.EqualTo(P1));
            Assert.That(d.Olay.Hedef, Is.EqualTo(new OyuncuId(4)));
            foreach (var ad in new[] { "tetikBasladi", "tetikBos", "tetikPatladi", "elendi", "finalBasladi", "kararBekleniyor" })
                Assert.That(Cevirici.Coz("{\"t\":\"olay\",\"olay\":\"" + ad + "\",\"kim\":\"p2\"}"), Is.Not.Null, ad);
        }

        [Test]
        public void Ozet_RusRuleti()
        {
            var d = (DurumMesaji)Cevirici.Coz(Durum("oyunSonu", ",\"ozet\":{\"oynananTur\":9,\"rusRuleti\":{\"kazanan\":\"p4\",\"elenmeSirasi\":[\"p3\",\"p2\",\"p1\"]}}"), P1);
            Assert.That(d.Ozet.RusRuleti.Kazanan, Is.EqualTo(new OyuncuId(4)));
            Assert.That(d.Ozet.RusRuleti.ElenmeSirasi, Is.EqualTo(new[] { P3, P2, P1 }));
            var yok = (DurumMesaji)Cevirici.Coz(Durum("oyunSonu", ",\"ozet\":{\"rusRuleti\":{\"kazanan\":null,\"elenmeSirasi\":[]}}"), P1);
            Assert.That(yok.Ozet.RusRuleti.Kazanan, Is.Null);
        }

        [Test]
        public void Giden_CanVeTetikKomutlari()
        {
            var a = JsonDeger.Coz(Cevirici.AyarDegistir(oyun: OyunTuru.RusRuleti, can: 3, ceza: CezaSeviyesi.Kapali));
            Assert.That(a["oyun"].Metin, Is.EqualTo("rusRuleti"));
            Assert.That(a["can"].Tamsayi, Is.EqualTo(3));
            var lobi = (DurumMesaji)Cevirici.Coz("{\"t\":\"durum\",\"surum\":1,\"asama\":\"lobi\",\"oyuncular\":[],\"ayarlar\":{\"oyun\":\"rusRuleti\",\"can\":3}}");
            Assert.That(lobi.Ayarlar.Can, Is.EqualTo(3));
            Assert.That(JsonDeger.Coz(Cevirici.TetikCek(4))["komut"].Metin, Is.EqualTo("tetikCek"));
            var dv = JsonDeger.Coz(Cevirici.TetikDevret(4, new OyuncuId(4)));
            Assert.That(dv["komut"].Metin, Is.EqualTo("tetikDevret"));
            Assert.That(dv["hedef"].Metin, Is.EqualTo("p4"));
        }

        // ------------------------------------------------------------ Akış

        sealed class Sahte : ITasima
        {
            public readonly Queue<TasimaOlayi> Gelen = new Queue<TasimaOlayi>();
            public readonly List<string> Giden = new List<string>();
            bool acik;
            public void Ac(Uri adres) { }
            public bool Gonder(string json) { if (!acik) return false; Giden.Add(json); return true; }
            public void Kapat() { acik = false; }
            public bool Al(out TasimaOlayi o)
            {
                if (Gelen.Count > 0) { o = Gelen.Dequeue(); return true; }
                o = default;
                return false;
            }
            public void Acil() { acik = true; Gelen.Enqueue(new TasimaOlayi { Tur = TasimaOlayTuru.Acildi }); }
            public void Mesaj(string j) => Gelen.Enqueue(new TasimaOlayi { Tur = TasimaOlayTuru.Mesaj, Metin = j });
        }

        [Test]
        public void Tetik_HerkesIzler_KomutlarGider()
        {
            var tasima = new Sahte();
            var istemci = new OnlineIstemci(() => tasima, null);
            istemci.Katil(new Uri("ws://localhost:5000/ws"), "KXTPM", "Ayşe");
            tasima.Acil();
            tasima.Mesaj("{\"t\":\"hosgeldin\",\"sen\":\"p2\",\"anahtar\":\"k\",\"oda\":\"KXTPM\"}");
            istemci.Guncelle(TimeSpan.Zero);
            var akis = new OnlineAkis(istemci, new Ayarlar { Oyun = OyunTuru.RusRuleti });

            tasima.Mesaj(Durum("tetik", ",\"tetikCeken\":\"p1\""));
            istemci.Guncelle(TimeSpan.Zero);
            akis.Ilerle(TimeSpan.Zero);
            Assert.That(akis.Ekran.Tur, Is.EqualTo(EkranTuru.Tetik), "tetiği çekmeyen de izler");
            Assert.That(akis.Ekran.Kime, Is.EqualTo(P1));

            Assert.That(akis.TetikDevret(new OyuncuId(4)), Is.EqualTo(Sonuc.Tamam));
            Assert.That(akis.TetikCek(), Is.EqualTo(Sonuc.Tamam));
            istemci.Guncelle(TimeSpan.Zero);
            var son2 = JsonDeger.Coz(tasima.Giden[tasima.Giden.Count - 2]);
            var son = JsonDeger.Coz(tasima.Giden.Last());
            Assert.That(son2["komut"].Metin, Is.EqualTo("tetikDevret"));
            Assert.That(son2["hedef"].Metin, Is.EqualTo("p4"));
            Assert.That(son["komut"].Metin, Is.EqualTo("tetikCek"));
            Assert.That(son["tur"].Tamsayi, Is.EqualTo(4));
        }
    }
}
