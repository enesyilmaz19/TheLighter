using System.Text.Json.Nodes;
using NUnit.Framework;

namespace SunucuTestleri
{
    /// <summary>Protokol.md, "Takım Düellosu" (docs/Oyunlar.md, 3). Bütün kurallar kural testlerinde; burada mesajlar.</summary>
    public class TakimTestleri : SunucuTesti
    {
        static int Takimi(JsonNode durum, string id) => (int)Oyuncu(durum, id)["takim"];

        async Task<TestIstemci[]> TakimBaslat(int n, object takimlar = null)
        {
            var o = await OdaKur(n);
            await o[0].Gonder(new { t = "ayarlar", oyun = "takim", takimlar });
            foreach (var x in o) await x.Bekle("durum", d => (string)d["ayarlar"]["oyun"] == "takim");
            await o[0].Gonder(new { t = "baslat" });
            foreach (var x in o) await x.Bekle("durum", d => (string)d["asama"] == "soruSorma");
            return o;
        }

        [Test]
        public async Task Kurucunun_dagilimi_oyunda_gorunur_puanlar_ve_olaydaki_takim()
        {
            var o = await TakimBaslat(4, new Dictionary<string, int> { ["p1"] = 0, ["p2"] = 1, ["p3"] = 0, ["p4"] = 1 });
            var d = o[0].SonDurum;
            Assert.That((string)d["oyun"], Is.EqualTo("takim"));
            Assert.That(Takimi(d, "p1"), Is.EqualTo(0));
            Assert.That(Takimi(d, "p2"), Is.EqualTo(1));
            Assert.That(d["takimPuanlari"].AsArray().Select(x => (int)x), Is.EqualTo(new[] { 0, 0 }));

            // A, kendi takımından B'ye sorar; B rakipten C'yi seçer, C kazanır: C'nin takımına düello puanı.
            var a = Kim(o, (string)d["a"]);
            var b = o.First(x => x != a && Takimi(d, x.Sen) == Takimi(d, a.Sen));
            await a.Gonder(new { t = "komut", tur = 1, komut = "soruSor", hedef = b.Sen, metin = "Kim?" });
            await b.Bekle("durum", m => (string)m["asama"] == "cevapSecme");
            var c = o.First(x => Takimi(d, x.Sen) != Takimi(d, a.Sen));
            await b.Gonder(new { t = "komut", tur = 1, komut = "cevapSec", hedef = c.Sen });
            await c.Bekle("durum", m => (string)m["asama"] == "miniOyun");
            await Hamle(b, "tas");
            await Hamle(c, "kagit");
            var puan = await o[0].Bekle("olay", m => (string)m["olay"] == "duelloPuani");
            Assert.That((string)puan["kim"], Is.EqualTo(c.Sen));
            Assert.That((int)puan["takim"], Is.EqualTo(Takimi(d, c.Sen)));
            var sonra = await o[0].Bekle("durum", m => (string)m["asama"] == "ifsa");
            Assert.That((int)sonra["takimPuanlari"][Takimi(d, c.Sen)], Is.EqualTo(1));
        }

        [Test]
        public async Task Takim_ayarlari_lobide_dogrulanir_cikan_dusar_eksikse_baslamaz()
        {
            var o = await OdaKur(5);
            await o[0].Gonder(new { t = "ayarlar", oyun = "takim", takimlar = new Dictionary<string, int> { ["p1"] = 2 } });
            await o[0].Hata("GECERSIZ_GIRDI");
            await o[0].Gonder(new { t = "ayarlar", takimlar = new Dictionary<string, int> { ["p9"] = 0 } });
            await o[0].Hata("GECERSIZ_GIRDI");
            await o[0].Gonder("{\"t\":\"ayarlar\",\"takimlar\":{\"p1\":\"kirmizi\"}}");
            await o[0].Hata("GECERSIZ_MESAJ");
            await o[0].Gonder(new { t = "ayarlar", oyun = "takim", gorevler = true });
            await o[0].Hata("GECERSIZ_GIRDI");

            var dagilim = new Dictionary<string, int> { ["p1"] = 0, ["p2"] = 0, ["p3"] = 1, ["p4"] = 1, ["p5"] = 1 };
            await o[0].Gonder(new { t = "ayarlar", oyun = "takim", takimlar = dagilim });
            var d = await o[1].Bekle("durum", m => (string)m["ayarlar"]["oyun"] == "takim");
            Assert.That(d["ayarlar"]["takimlar"].AsObject().Count, Is.EqualTo(5));

            await o[4].Kapat(); // p5 lobiden çıktı: dağılımdan düşer, 2'ye 2 kalır
            d = await o[1].Bekle("durum", m => m["oyuncular"].AsArray().Count == 4);
            Assert.That(d["ayarlar"]["takimlar"]["p5"], Is.Null);

            await Katil(o[0].Oda, "Yeni"); // dağılımda yok: başlamaz
            await o[1].Bekle("durum", m => m["oyuncular"].AsArray().Count == 5);
            await o[0].Gonder(new { t = "baslat" });
            await o[0].Hata("GECERSIZ_GIRDI");

            await o[0].Gonder(new { t = "ayarlar", takimlar = new Dictionary<string, int>() }); // boş = rastgele
            await o[0].Bekle("durum", m => m["ayarlar"]["takimlar"].AsObject().Count == 0);
            await o[0].Gonder(new { t = "baslat" });
            await o[0].Bekle("durum", m => (string)m["asama"] == "soruSorma");
        }

        [Test]
        public async Task Takim_sohbeti_sadece_takima_gider()
        {
            var o = await TakimBaslat(4, new Dictionary<string, int> { ["p1"] = 0, ["p2"] = 1, ["p3"] = 0, ["p4"] = 1 });
            var gizli = GizliMetin("Takim");
            await o[0].Gonder(new { t = "sohbet", kime = "takim", metin = gizli });
            foreach (var x in new[] { o[0], o[2] })
            {
                var m = await x.Bekle("sohbet", s => (string)s["metin"] == gizli);
                Assert.That((string)m["kime"], Is.EqualTo("takim"));
                Assert.That((string)m["kimden"], Is.EqualTo(o[0].Sen));
            }
            await o[1].Gonder(new { t = "sohbet", kime = (string)null, metin = "genel" });
            foreach (var x in new[] { o[1], o[3] })
            {
                await x.Bekle("sohbet", s => (string)s["metin"] == "genel");
                Assert.That(string.Join("\n", x.Hepsi), Does.Not.Contain(gizli), "rakip takım görmez");
            }
        }

        [Test]
        public async Task Takim_oyunu_yokken_takim_sohbeti_GECERSIZ_GIRDI()
        {
            var o = await OdaKur(4);
            await o[0].Gonder(new { t = "sohbet", kime = "takim", metin = "lobide" });
            await o[0].Hata("GECERSIZ_GIRDI");
            await o[0].Gonder(new { t = "baslat" });
            await o[0].Bekle("durum", m => (string)m["asama"] == "soruSorma");
            await o[0].Gonder(new { t = "sohbet", kime = "takim", metin = "çakmakta" });
            await o[0].Hata("GECERSIZ_GIRDI");
        }

        [Test]
        public async Task Kurucu_bitirince_ozette_takim_tekrar_oynayinca_dagilim_sifirlanir()
        {
            var o = await TakimBaslat(4, new Dictionary<string, int> { ["p1"] = 0, ["p2"] = 1, ["p3"] = 0, ["p4"] = 1 });
            await o[0].Gonder(new { t = "bitir" });
            var d = await o[1].Bekle("durum", m => (string)m["asama"] == "oyunSonu");
            Assert.That(d["ozet"]["takim"]["kazanan"], Is.Null, "0'a 0 berabere");
            Assert.That(d["ozet"]["takim"]["puanlar"].AsArray().Select(x => (int)x), Is.EqualTo(new[] { 0, 0 }));

            await o[0].Gonder(new { t = "tekrarOyna" });
            var lobi = await o[1].Bekle("durum", m => (string)m["asama"] == "lobi");
            Assert.That(lobi["ayarlar"]["takimlar"].AsObject().Count, Is.EqualTo(0));
            Assert.That((string)lobi["ayarlar"]["oyun"], Is.EqualTo("takim"), "oyun ayarı kalır");
        }
    }
}
