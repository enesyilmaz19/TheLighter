using System.Text.Json.Nodes;
using NUnit.Framework;

namespace SunucuTestleri
{
    /// <summary>Protokol.md, "Köstebek" (docs/Oyunlar.md, 1).</summary>
    public class KostebekTestleri : SunucuTesti
    {
        async Task<TestIstemci[]> KostebekBaslat(int n)
        {
            var o = await OdaKur(n);
            await o[0].Gonder(new { t = "ayarlar", oyun = "kostebek" });
            foreach (var x in o) await x.Bekle("durum", d => (string)d["ayarlar"]["oyun"] == "kostebek");
            await o[0].Gonder(new { t = "baslat" });
            foreach (var x in o) await x.Bekle("durum", d => (string)d["asama"] == "soruSorma");
            return o;
        }

        static TestIstemci Kostebek(TestIstemci[] o)
        {
            var bulunan = o.Where(x => (string)x.SonDurum["sana"]["rol"] == "kostebek").ToList();
            Assert.That(bulunan.Count, Is.EqualTo(1), "tam bir köstebek");
            return bulunan[0];
        }

        [Test]
        public async Task Tek_kisi_rolunu_gorur_herkesin_gorevi_var_digerlerinin_mesajinda_rol_yok()
        {
            var o = await KostebekBaslat(5);
            var k = Kostebek(o);
            foreach (var x in o)
            {
                var d = x.SonDurum;
                Assert.That((string)d["oyun"], Is.EqualTo("kostebek"));
                Assert.That(d["sana"]["gorevin"], Is.Not.Null, "Köstebek'te herkesin görevi var");
                Assert.That((bool)d["sana"]["suclamaHakki"], Is.True);
                if (x != k) Assert.That(string.Join("\n", x.Hepsi), Does.Not.Contain("\"rol\":\"kostebek\""));
            }
        }

        [Test]
        public async Task Kostebekte_kaos_acilamaz_sirasi_fark_etmez()
        {
            var o = await OdaKur(4);
            await o[0].Gonder(new { t = "ayarlar", oyun = "kostebek", kaos = true });
            await o[0].Hata("GECERSIZ_GIRDI");
            await o[0].Gonder(new { t = "ayarlar", kaos = true });
            await o[0].Bekle("durum", d => (bool)d["ayarlar"]["kaos"]);
            await o[0].Gonder(new { t = "ayarlar", oyun = "kostebek" });
            await o[0].Hata("GECERSIZ_GIRDI");
            await o[0].Gonder(new { t = "ayarlar", oyun = "vampir" });
            await o[0].Hata("GECERSIZ_MESAJ");
        }

        [Test]
        public async Task Erken_suclama_oylama_kostebek_yakalanir_grup_kazanir()
        {
            var o = await KostebekBaslat(5);
            var k = Kostebek(o);
            var suclayan = o[0] == k ? o[1] : o[0];
            await suclayan.Gonder(new { t = "komut", tur = 1, komut = "sucla" });
            var olay = await o[2].Bekle("olay", m => (string)m["olay"] == "suclamaBasladi");
            Assert.That((string)olay["kim"], Is.EqualTo(suclayan.Sen));
            var d = await o[2].Bekle("durum", m => (string)m["asama"] == "suclama");
            Assert.That(Idler(d["oylayanlar"]).Count, Is.EqualTo(5));
            Assert.That((bool)d["sonSuclama"], Is.False);

            foreach (var x in o)
            {
                var hedef = x == k ? (o[0] == k ? o[1] : o[0]).Sen : k.Sen;
                await x.Gonder(new { t = "komut", tur = 1, komut = "suclamaOyu", hedef });
            }
            var son = await o[3].Bekle("durum", m => (string)m["asama"] == "oyunSonu");
            var sonuc = son["ozet"]["kostebek"];
            Assert.That((string)sonuc["kim"], Is.EqualTo(k.Sen));
            Assert.That((string)sonuc["kazanan"], Is.EqualTo("grup"));
            Assert.That((string)sonuc["neden"], Is.EqualTo("yakalandi"));
            Assert.That(son["ozet"]["gorevler"].AsArray().Count, Is.EqualTo(5));
        }

        [Test]
        public async Task Yanlis_kisi_supheli_olur_oyun_surer()
        {
            var o = await KostebekBaslat(4);
            var k = Kostebek(o);
            var masum = o.First(x => x != k);
            var baska = o.First(x => x != k && x != masum);
            await o[0].Gonder(new { t = "komut", tur = 1, komut = "sucla" });
            await o[0].Bekle("durum", m => (string)m["asama"] == "suclama");
            foreach (var x in o)
                await x.Gonder(new { t = "komut", tur = 1, komut = "suclamaOyu", hedef = (x == masum ? baska : masum).Sen });

            var bitti = await o[1].Bekle("olay", m => (string)m["olay"] == "suclamaBitti");
            Assert.That((string)bitti["kim"], Is.EqualTo(masum.Sen));
            var d = await o[1].Bekle("durum", m => (string)m["asama"] == "soruSorma");
            Assert.That(Idler(d["supheliler"]), Is.EqualTo(new[] { masum.Sen }));
            Assert.That((bool)o[0].SonDurum["sana"]["suclamaHakki"], Is.False, "erken suçlama hakkı bitti");
        }

        [Test]
        public async Task Kurucu_bitirince_son_oylama_ikinci_bitir_reddedilir()
        {
            var o = await KostebekBaslat(4);
            await o[0].Gonder(new { t = "bitir" });
            var d = await o[1].Bekle("durum", m => (string)m["asama"] == "suclama");
            Assert.That((bool)d["sonSuclama"], Is.True);
            await o[0].Gonder(new { t = "bitir" });
            await o[0].Hata("YANLIS_ASAMA");
        }

        [TestCase("{\"t\":\"komut\",\"tur\":1,\"komut\":\"suclamaOyu\",\"hedef\":\"p2\"}", "YANLIS_ASAMA")]
        [TestCase("{\"t\":\"komut\",\"tur\":1,\"komut\":\"suclamaOyu\"}", "GECERSIZ_MESAJ")]
        public async Task Gecersiz_suclama_oyu(string mesaj, string kod)
        {
            var o = await KostebekBaslat(4);
            await o[0].Gonder(mesaj);
            await o[0].Hata(kod);
        }

        [Test]
        public async Task Cakmak_oyununda_sucla_YANLIS_ASAMA_rol_yok()
        {
            var o = await OyunBaslat(4);
            Assert.That((string)o[0].SonDurum["oyun"], Is.EqualTo("cakmak"));
            Assert.That(o[0].SonDurum["sana"]["rol"], Is.Null);
            await o[0].Gonder(new { t = "komut", tur = 1, komut = "sucla" });
            await o[0].Hata("YANLIS_ASAMA");
        }
    }
}
