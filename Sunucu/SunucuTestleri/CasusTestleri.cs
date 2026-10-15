using System.Text.Json.Nodes;
using NUnit.Framework;

namespace SunucuTestleri
{
    /// <summary>Protokol.md, "Casus" (docs/Oyunlar.md, 4). Bütün kurallar kural testlerinde; burada mesajlar ve gizlilik.</summary>
    public class CasusTestleri : SunucuTesti
    {
        async Task<TestIstemci[]> CasusBaslat(int n, string tur = "mekan")
        {
            var o = await OdaKur(n);
            await o[0].Gonder(new { t = "ayarlar", oyun = "casus", casus = tur });
            foreach (var x in o) await x.Bekle("durum", d => (string)d["ayarlar"]["casus"] == tur && (string)d["ayarlar"]["oyun"] == "casus");
            await o[0].Gonder(new { t = "baslat" });
            foreach (var x in o) await x.Bekle("durum", d => (string)d["asama"] == "soruSorma");
            return o;
        }

        static TestIstemci Casus(TestIstemci[] o)
        {
            var bulunan = o.Where(x => (string)x.SonDurum["sana"]["rol"] == "casus").ToList();
            Assert.That(bulunan.Count, Is.EqualTo(1), "tam bir casus");
            return bulunan[0];
        }

        static string Sir(TestIstemci[] o, TestIstemci casus) => (string)o.First(x => x != casus).SonDurum["sana"]["sir"];

        [Test]
        public async Task Mekan_ve_rol_sadece_sahibine_casusun_hicbir_mesajinda_sir_yok()
        {
            var o = await CasusBaslat(5);
            var casus = Casus(o);
            var mekan = Sir(o, casus);
            Assert.That(mekan, Is.Not.Null);
            foreach (var x in o)
            {
                var d = x.SonDurum;
                Assert.That((string)d["oyun"], Is.EqualTo("casus"));
                Assert.That(d["casusListesi"].AsArray().Select(s => (string)s), Has.Member(mekan), "liste herkeste");
                Assert.That(d["casusKategori"], Is.Null);
                if (x == casus)
                {
                    Assert.That(d["sana"]["sir"], Is.Null);
                    Assert.That(d["sana"]["mekanRolu"], Is.Null);
                    Assert.That(string.Join("\n", x.Hepsi), Does.Not.Contain("\"sir\":\""), "casusa sır hiç gitmedi");
                    continue;
                }
                Assert.That((string)d["sana"]["sir"], Is.EqualTo(mekan));
                Assert.That((string)d["sana"]["mekanRolu"], Is.Not.Null);
                Assert.That(string.Join("\n", x.Hepsi), Does.Not.Contain("\"rol\":\"casus\""));
            }
            Assert.That(o.Select(x => (string)x.SonDurum["sana"]["mekanRolu"]).Where(r => r != null).Distinct().Count(), Is.EqualTo(4), "herkese ayrı rol");
        }

        [Test]
        public async Task Kelimede_kategori_herkese_rol_yok()
        {
            var o = await CasusBaslat(4, "kelime");
            var casus = Casus(o);
            foreach (var x in o)
            {
                Assert.That((string)x.SonDurum["casusKategori"], Is.Not.Null);
                Assert.That(x.SonDurum["sana"]["mekanRolu"], Is.Null);
            }
            Assert.That(o[0].SonDurum["casusListesi"].AsArray().Select(s => (string)s), Has.Member(Sir(o, casus)));
        }

        [Test]
        public async Task Casusta_curcuna_ve_gorev_acilamaz_bilinmeyen_tur_GECERSIZ_MESAJ()
        {
            var o = await OdaKur(4);
            await o[0].Gonder(new { t = "ayarlar", oyun = "casus", mod = "curcuna" });
            await o[0].Hata("GECERSIZ_GIRDI");
            await o[0].Gonder(new { t = "ayarlar", oyun = "casus", gorevler = true });
            await o[0].Hata("GECERSIZ_GIRDI");
            await o[0].Gonder(new { t = "ayarlar", oyun = "casus", casus = "film" });
            await o[0].Hata("GECERSIZ_MESAJ");
        }

        [Test]
        public async Task Son_oylamada_yakalanan_herkese_acilir_son_sansta_bilirse_kazanir()
        {
            var o = await CasusBaslat(4);
            var casus = Casus(o);
            var mekan = Sir(o, casus);
            await o[0].Gonder(new { t = "bitir" });
            await o[1].Bekle("durum", m => (string)m["asama"] == "suclama");
            var masum = o.First(x => x != casus);
            foreach (var x in o)
                await x.Gonder(new { t = "komut", tur = 1, komut = "suclamaOyu", hedef = (x == casus ? masum : casus).Sen });

            var olay = await o[1].Bekle("olay", m => (string)m["olay"] == "casusYakalandi");
            Assert.That((string)olay["kim"], Is.EqualTo(casus.Sen));
            var d = await o[1].Bekle("durum", m => (string)m["asama"] == "casusTahmini");
            Assert.That((string)d["yakalanan"], Is.EqualTo(casus.Sen));

            await masum.Gonder(new { t = "komut", tur = 1, komut = "casusTahmini", tahmin = mekan });
            await masum.Hata("SIRA_SENDE_DEGIL");
            await casus.Gonder(new { t = "komut", tur = 1, komut = "casusTahmini", tahmin = mekan });
            await o[1].Bekle("olay", m => (string)m["olay"] == "casusTahminEtti");
            var son = await o[1].Bekle("durum", m => (string)m["asama"] == "oyunSonu");
            var s = son["ozet"]["casus"];
            Assert.That((string)s["kim"], Is.EqualTo(casus.Sen));
            Assert.That((string)s["kazanan"], Is.EqualTo("casus"));
            Assert.That((string)s["neden"], Is.EqualTo("sonSanstaBildi"));
            Assert.That((string)s["sir"], Is.EqualTo(mekan));
            Assert.That((string)s["tahmin"], Is.EqualTo(mekan));
        }

        [TestCase("{\"t\":\"komut\",\"tur\":1,\"komut\":\"casusTahmini\"}", "GECERSIZ_MESAJ")]
        [TestCase("{\"t\":\"komut\",\"tur\":1,\"komut\":\"casusTahmini\",\"tahmin\":\"Ay\"}", "GECERSIZ_GIRDI")]
        public async Task Gecersiz_tahmin(string mesaj, string kod)
        {
            var o = await CasusBaslat(4);
            var casus = Casus(o);
            await casus.Gonder(mesaj);
            await casus.Hata(kod);
        }

        [Test]
        public async Task Cakmak_oyununda_casus_tahmini_YANLIS_ASAMA_alanlar_bos()
        {
            var o = await OyunBaslat(4);
            var d = o[0].SonDurum;
            Assert.That(d["casusListesi"].AsArray(), Is.Empty);
            Assert.That(d["sana"]["sir"], Is.Null);
            Assert.That(d["yakalanan"], Is.Null);
            await o[0].Gonder(new { t = "komut", tur = 1, komut = "casusTahmini", tahmin = "Uçak" });
            await o[0].Hata("YANLIS_ASAMA");
        }
    }
}
