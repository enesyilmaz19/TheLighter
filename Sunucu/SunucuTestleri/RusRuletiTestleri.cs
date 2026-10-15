using System.Text.Json.Nodes;
using NUnit.Framework;

namespace SunucuTestleri
{
    /// <summary>Protokol.md, "Rus Ruleti" (docs/Oyunlar.md, 2). Bütün oyun kural testlerinde; burada mesajlar.</summary>
    public class RusRuletiTestleri : SunucuTesti
    {
        async Task<TestIstemci[]> RusRuletiBaslat(int n, int can)
        {
            var o = await OdaKur(n);
            await o[0].Gonder(new { t = "ayarlar", oyun = "rusRuleti", can });
            foreach (var x in o) await x.Bekle("durum", d => (string)d["ayarlar"]["oyun"] == "rusRuleti");
            await o[0].Gonder(new { t = "baslat" });
            foreach (var x in o) await x.Bekle("durum", d => (string)d["asama"] == "soruSorma");
            return o;
        }

        /// <summary>A sorar, B seçer, C kazanır; sonuç ekranından (6 sn) sonra herkes tetik aşamasını görür. Kaybeden B döner.</summary>
        static async Task<TestIstemci> TetigeKadar(TestIstemci[] o)
        {
            var (_, b) = await Sor(o, "Kim?");
            var c = await Sec(o, b);
            await Hamle(b, "tas");
            await Hamle(c, "kagit");
            foreach (var x in o) await x.Bekle("durum", d => (string)d["asama"] == "tetik", TimeSpan.FromSeconds(10));
            var olay = await o[0].Bekle("olay", m => (string)m["olay"] == "tetikBasladi");
            Assert.That((string)olay["kim"], Is.EqualTo(b.Sen));
            return b;
        }

        [Test]
        public async Task Ayarlar_can_ve_uyumsuzlar()
        {
            var o = await OdaKur(4);
            await o[0].Gonder(new { t = "ayarlar", oyun = "rusRuleti", can = 3 });
            var d = await o[1].Bekle("durum", m => (string)m["ayarlar"]["oyun"] == "rusRuleti");
            Assert.That((int)d["ayarlar"]["can"], Is.EqualTo(3));

            await o[0].Gonder(new { t = "ayarlar", can = 4 });
            await o[0].Hata("GECERSIZ_GIRDI");
            await o[0].Gonder(new { t = "ayarlar", ceza = "hafif" });
            await o[0].Hata("GECERSIZ_GIRDI");
            await o[0].Gonder(new { t = "ayarlar", gorevler = true });
            await o[0].Hata("GECERSIZ_GIRDI");
            await o[0].Gonder(new { t = "ayarlar", oyun = "cakmak", ceza = "hafif" });
            await o[0].Bekle("durum", m => (string)m["ayarlar"]["ceza"] == "hafif");
            await o[0].Gonder(new { t = "ayarlar", oyun = "rusRuleti" });
            await o[0].Hata("GECERSIZ_GIRDI");
        }

        [Test]
        public async Task Kaybeden_tetik_ceker_devret_olayi_hedefi_tasir_can_herkese_gorunur()
        {
            var o = await RusRuletiBaslat(5, can: 3);
            foreach (var p in o[0].SonDurum["oyuncular"].AsArray())
            {
                Assert.That((int)p["can"], Is.EqualTo(3));
                Assert.That((bool)p["devretmeHakki"], Is.True);
                Assert.That((bool)p["elendi"], Is.False);
            }
            Assert.That((int)o[0].SonDurum["toplamTur"], Is.EqualTo(0));
            Assert.That(o[0].SonDurum["isi"], Is.Not.Null);

            var b = await TetigeKadar(o);
            Assert.That((string)o[0].SonDurum["tetikCeken"], Is.EqualTo(b.Sen));
            Assert.That((bool)o[0].SonDurum["final"], Is.False);

            var hedef = o.First(x => x != b);
            await b.Gonder(new { t = "komut", tur = 1, komut = "tetikDevret", hedef = hedef.Sen });
            var devret = await o[2].Bekle("olay", m => (string)m["olay"] == "tetikDevredildi");
            Assert.That((string)devret["kim"], Is.EqualTo(b.Sen));
            Assert.That((string)devret["hedef"], Is.EqualTo(hedef.Sen));
            await o[2].Bekle("durum", m => (string)m["tetikCeken"] == hedef.Sen);

            await b.Gonder(new { t = "komut", tur = 1, komut = "tetikCek" });
            await b.Hata("SIRA_SENDE_DEGIL");
            await hedef.Gonder(new { t = "komut", tur = 1, komut = "tetikCek" });
            var cek = await o[3].Bekle("olay", m => (string)m["olay"] == "tetikBos" || (string)m["olay"] == "tetikPatladi");
            Assert.That((string)cek["kim"], Is.EqualTo(hedef.Sen));
            await o[3].Bekle("durum", m => (string)m["asama"] == "soruSorma" && (int)m["tur"] == 2);
        }

        [Test]
        public async Task Kurucu_bitirince_ozette_kazanan_en_cok_canli_esitlikte_yok()
        {
            var o = await RusRuletiBaslat(4, can: 2);
            await o[0].Gonder(new { t = "bitir" });
            var d = await o[1].Bekle("durum", m => (string)m["asama"] == "oyunSonu");
            var rr = d["ozet"]["rusRuleti"];
            Assert.That(rr, Is.Not.Null);
            Assert.That(rr["kazanan"], Is.Null, "herkesin canı eşit");
            Assert.That(rr["elenmeSirasi"].AsArray(), Is.Empty);
            Assert.That(d["ozet"]["kostebek"], Is.Null);
        }

        [Test]
        public async Task Cakmak_oyununda_tetik_YANLIS_ASAMA()
        {
            var o = await OyunBaslat(4);
            Assert.That((int)o[0].SonDurum["oyuncular"][0]["can"], Is.EqualTo(0));
            await o[0].Gonder(new { t = "komut", tur = 1, komut = "tetikCek" });
            await o[0].Hata("YANLIS_ASAMA");
        }

        [TestCase("{\"t\":\"komut\",\"tur\":1,\"komut\":\"tetikDevret\"}", "GECERSIZ_MESAJ")]
        [TestCase("{\"t\":\"komut\",\"tur\":1,\"komut\":\"tetikDevret\",\"hedef\":\"p2\"}", "YANLIS_ASAMA")]
        public async Task Gecersiz_tetik(string mesaj, string kod)
        {
            var o = await RusRuletiBaslat(4, can: 1);
            await o[0].Gonder(mesaj);
            await o[0].Hata(kod);
        }
    }
}
