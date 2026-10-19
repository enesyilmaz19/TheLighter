using System.Collections.Generic;
using System.Linq;
using Cakmak.Kurallar;
using NUnit.Framework;
using TheLighter.Ag;
using TheLighter.Akis;

namespace TheLighter.Testler
{
    /// <summary>
    /// Takım Düellosu online (tasks/g2-takim.md 3, 4): çevirici <c>takim</c>, <c>takimPuanlari</c>, olaylardaki <c>takim</c>,
    /// <c>ozet.takim</c>, lobide <c>takimlar</c> ve giden dağılım; takım sohbeti <c>kime: "takim"</c>.
    /// </summary>
    public class TakimOnlineTestleri
    {
        static readonly OyuncuId P1 = new OyuncuId(1), P2 = new OyuncuId(2), P3 = new OyuncuId(3), P4 = new OyuncuId(4);

        static string Durum(string asama, string ek = "") =>
            "{\"t\":\"durum\",\"surum\":5,\"oda\":\"KXTPM\",\"asama\":\"" + asama + "\",\"tur\":3,\"toplamTur\":15,\"kalanMs\":6000," +
            "\"kurucu\":\"p1\",\"a\":\"p1\",\"b\":\"p2\",\"c\":\"p3\",\"oyuncular\":[" +
            "{\"id\":\"p1\",\"isim\":\"Enes\",\"renk\":0,\"bagli\":true,\"takim\":0}," +
            "{\"id\":\"p2\",\"isim\":\"Ayşe\",\"renk\":1,\"bagli\":true,\"takim\":1}," +
            "{\"id\":\"p3\",\"isim\":\"Can\",\"renk\":2,\"bagli\":true,\"takim\":0}," +
            "{\"id\":\"p4\",\"isim\":\"Deniz\",\"renk\":3,\"bagli\":true,\"takim\":1}," +
            "{\"id\":\"p5\",\"isim\":\"Elif\",\"renk\":4,\"bagli\":true,\"takim\":1}]," +
            "\"oyun\":\"takim\",\"takimPuanlari\":[4,2],\"sana\":{}" + ek + "}";

        [Test]
        public void Durum_TakimVePuanlar()
        {
            var g = ((DurumMesaji)Cevirici.Coz(Durum("ifsa"), P2)).Gorunum;
            Assert.That(g.Oyun, Is.EqualTo(OyunTuru.Takim));
            Assert.That(g.TakimPuanlari, Is.EqualTo(new[] { 4, 2 }));
            Assert.That(g.Oyuncular.First(o => o.Id == P1).Takim, Is.EqualTo(0));
            Assert.That(g.Oyuncular.First(o => o.Id == P2).Takim, Is.EqualTo(1));

            var cakmak = ((DurumMesaji)Cevirici.Coz(Durum("ifsa").Replace("\"takim\":0", "\"takim\":null")
                .Replace("\"takim\":1", "\"takim\":null").Replace("[4,2]", "[]").Replace("\"oyun\":\"takim\"", "\"oyun\":\"cakmak\""), P2)).Gorunum;
            Assert.That(cakmak.TakimPuanlari, Is.Empty);
            Assert.That(cakmak.Oyuncular.All(o => !o.Takim.HasValue), Is.True, "diğer oyunlarda takım yok");
        }

        [Test]
        public void Olaylar_PuanAlanTakim()
        {
            var d = (OlayMesaji)Cevirici.Coz("{\"t\":\"olay\",\"olay\":\"duelloPuani\",\"kim\":\"p3\",\"takim\":0}");
            Assert.That(d.Olay.Tur, Is.EqualTo(OlayTuru.DuelloPuani));
            Assert.That(d.Kim, Is.EqualTo(P3));
            Assert.That(d.Olay.Takim, Is.EqualTo(0));
            var b = (OlayMesaji)Cevirici.Coz("{\"t\":\"olay\",\"olay\":\"ifsaBonusu\",\"kim\":\"p2\",\"takim\":1}");
            Assert.That(b.Olay.Tur, Is.EqualTo(OlayTuru.IfsaBonusu));
            Assert.That(b.Olay.Takim, Is.EqualTo(1));
            var ifsa = (OlayMesaji)Cevirici.Coz("{\"t\":\"olay\",\"olay\":\"ifsa\",\"a\":\"p1\",\"takim\":null}");
            Assert.That(ifsa.Olay.Takim, Is.Null, "sadece puan olaylarında dolu");
        }

        [Test]
        public void Ozet_Takim_KazananYaDaBerabere()
        {
            var d = (DurumMesaji)Cevirici.Coz(Durum("oyunSonu", ",\"ozet\":{\"oynananTur\":11,\"takim\":{\"kazanan\":0,\"puanlar\":[7,4]}}"), P1);
            Assert.That(d.Ozet.Takim, Is.Not.Null);
            Assert.That(d.Ozet.Takim.Kazanan, Is.EqualTo(0));
            Assert.That(d.Ozet.Takim.Puanlar, Is.EqualTo(new[] { 7, 4 }));
            Assert.That(d.Ozet.Oyuncular.First(o => o.Id == P4).Takim, Is.EqualTo(1), "üyeler oyun sonunda da");

            var berabere = (DurumMesaji)Cevirici.Coz(Durum("oyunSonu", ",\"ozet\":{\"takim\":{\"kazanan\":null,\"puanlar\":[3,3]}}"), P1);
            Assert.That(berabere.Ozet.Takim.Kazanan, Is.Null);
            var yok = (DurumMesaji)Cevirici.Coz(Durum("oyunSonu", ",\"ozet\":{\"takim\":null}"), P1);
            Assert.That(yok.Ozet.Takim, Is.Null);
        }

        static string Lobi(string takimlar, string oyuncular = "\"p1\",\"p2\",\"p3\",\"p4\"") =>
            "{\"t\":\"durum\",\"surum\":2,\"oda\":\"KXTPM\",\"asama\":\"lobi\",\"kurucu\":\"p1\",\"oyuncular\":[" +
            string.Join(",", oyuncular.Split(',').Select((id, i) => "{\"id\":" + id + ",\"isim\":\"O" + i + "\",\"renk\":" + i + ",\"bagli\":true}")) +
            "],\"ayarlar\":{\"oyun\":\"takim\",\"takimlar\":" + takimlar + "}}";

        [Test]
        public void Lobi_Takimlar_BosRastgele()
        {
            var d = (DurumMesaji)Cevirici.Coz(Lobi("{\"p1\":0,\"p2\":1,\"p3\":0,\"p4\":1}"));
            Assert.That(d.Ayarlar.Oyun, Is.EqualTo(OyunTuru.Takim));
            Assert.That(d.Ayarlar.Takimlar.Count, Is.EqualTo(4));
            Assert.That(d.Ayarlar.Takimlar[P2], Is.EqualTo(1));
            Assert.That(new TakimDagitimi(d.Ayarlar.Takimlar).Gecerli(d.Oyuncular.Select(o => o.Id).ToList()), Is.True);

            var bos = (DurumMesaji)Cevirici.Coz(Lobi("{}"));
            Assert.That(bos.Ayarlar.Takimlar, Is.Empty, "boş: rastgele");
            Assert.That(new TakimDagitimi(bos.Ayarlar.Takimlar).Rastgele, Is.True);
        }

        [Test]
        public void Lobi_CikanDagilimdanDuser_GelenTakimsizBaslatKapali()
        {
            // p4 çıktı (sunucu dağılımdan düşürdü), p5 geldi: p5 takımsız, kural motoru kurmaz.
            var d = (DurumMesaji)Cevirici.Coz(Lobi("{\"p1\":0,\"p2\":1,\"p3\":0}", "\"p1\",\"p2\",\"p3\",\"p5\""));
            var kisiler = d.Oyuncular.Select(o => o.Id).ToList();
            var dag = new TakimDagitimi(d.Ayarlar.Takimlar);
            dag.Esitle(kisiler);
            Assert.That(dag.Takimi(P4), Is.Null);
            Assert.That(dag.Takimsizlar(kisiler), Is.EqualTo(new[] { new OyuncuId(5) }));
            Assert.That(dag.Gecerli(kisiler), Is.False, "Başlat kapalı, uyarı: herkes bir takımda olmalı");
            dag.Dokun(new OyuncuId(5));
            Assert.That(dag.Takimi(new OyuncuId(5)), Is.EqualTo(1), "küçük takıma");
            Assert.That(dag.Gecerli(kisiler), Is.True);
        }

        [Test]
        public void Giden_TakimlarTamDagilim_BosNesneRastgele_YoksaGitmez()
        {
            var a = JsonDeger.Coz(Cevirici.AyarDegistir(oyun: OyunTuru.Takim,
                takimlar: new Dictionary<OyuncuId, int> { [P1] = 0, [P2] = 1 }));
            Assert.That(a["oyun"].Metin, Is.EqualTo("takim"));
            Assert.That(a["takimlar"]["p1"].Tamsayi, Is.EqualTo(0));
            Assert.That(a["takimlar"]["p2"].Tamsayi, Is.EqualTo(1));

            var bos = JsonDeger.Coz(Cevirici.AyarDegistir(takimlar: new Dictionary<OyuncuId, int>()));
            Assert.That(bos["takimlar"].Tur, Is.EqualTo(JsonTuru.Nesne));
            Assert.That(bos["takimlar"].Alanlar.Count(), Is.EqualTo(0), "{} = rastgele");

            Assert.That(JsonDeger.Coz(Cevirici.AyarDegistir(kaos: false))["takimlar"].Var, Is.False, "değişmeyen alan gitmez");
        }

        [Test]
        public void Giden_TakimSohbeti()
        {
            var s = JsonDeger.Coz(Cevirici.Sohbet(SohbetYeri.Adli(SohbetKanali.Takim), "plan"));
            Assert.That(s["t"].Metin, Is.EqualTo("sohbet"));
            Assert.That(s["kime"].Metin, Is.EqualTo("takim"));
            Assert.That(JsonDeger.Coz(Cevirici.Sohbet(SohbetYeri.Genel, "selam"))["kime"].Var, Is.False);
            Assert.That(JsonDeger.Coz(Cevirici.Sohbet(SohbetYeri.DmIle(P3), "psst"))["kime"].Metin, Is.EqualTo("p3"));
        }
    }
}
