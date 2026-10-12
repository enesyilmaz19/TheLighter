using System.Text.Json.Nodes;
using NUnit.Framework;

namespace SunucuTestleri
{
    /// <summary>Protokol.md, "Sohbet" ve "Temiz mod": genel + DM, uzunluk, Temiz filtresi, 3. ihlalde atma.</summary>
    public class SohbetTestleri : SunucuTesti
    {
        static Task Yaz(TestIstemci x, string metin, string kime = null) => x.Gonder(new { t = "sohbet", kime, metin });

        static Task<JsonNode> Mesaj(TestIstemci x, string metin) => x.Bekle("sohbet", m => (string)m["metin"] == metin);

        static async Task TemizYap(TestIstemci[] o)
        {
            await o[0].Gonder(new { t = "ayarlar", mod = "temiz" });
            foreach (var x in o) await x.Bekle("durum", d => (string)d["ayarlar"]["mod"] == "temiz");
        }

        // ---------------------------------------------------- Genel ve DM

        [Test]
        public async Task Genel_sohbet_temizlenip_odadaki_herkese_gider_yazana_da()
        {
            var o = await OdaKur(3);
            await Yaz(o[1], "  Selam" + char.ConvertFromUtf32(0x200B) + "   millet ");
            foreach (var x in o)
            {
                var m = await Mesaj(x, "Selam millet");
                Assert.That((string)m["kimden"], Is.EqualTo(o[1].Sen));
                Assert.That(m["kime"], Is.Null);
                Assert.That((string)m["id"], Does.StartWith("m"));
            }
        }

        [Test]
        public async Task Bekleme_odasindaki_genel_sohbeti_gorur_ve_yazar()
        {
            var o = await OyunBaslat(4);
            var x = await Katil(o[0].Oda, "Gec");
            await Yaz(x, "ben de geldim");
            foreach (var y in o) await Mesaj(y, "ben de geldim");
            await Yaz(o[2], "hoş geldin");
            await Mesaj(x, "hoş geldin");
        }

        [Test]
        public async Task DM_sadece_alana_ve_yazana_gider()
        {
            var o = await OdaKur(4);
            var gizli = GizliMetin("Dm");
            await Yaz(o[0], gizli, o[2].Sen);
            foreach (var x in new[] { o[0], o[2] })
            {
                var m = await Mesaj(x, gizli);
                Assert.That((string)m["kimden"], Is.EqualTo(o[0].Sen));
                Assert.That((string)m["kime"], Is.EqualTo(o[2].Sen));
            }

            // Sonraki genel mesaj geldiyse DM gelecek olsaydı çoktan gelmişti (bağlantı başına sıra korunur).
            await Yaz(o[1], "genel");
            foreach (var x in new[] { o[1], o[3] })
            {
                await Mesaj(x, "genel");
                Assert.That(string.Join("\n", x.Hepsi), Does.Not.Contain(gizli));
            }
        }

        [TestCase("{\"t\":\"sohbet\",\"kime\":\"p1\",\"metin\":\"kendime\"}", "GECERSIZ_GIRDI")]  // kendine DM
        [TestCase("{\"t\":\"sohbet\",\"kime\":\"p9\",\"metin\":\"yok\"}", "GECERSIZ_GIRDI")]      // odada yok
        [TestCase("{\"t\":\"sohbet\",\"kime\":null,\"metin\":\"   \"}", "GECERSIZ_GIRDI")]
        [TestCase("{\"t\":\"sohbet\",\"kime\":\"x\",\"metin\":\"a\"}", "GECERSIZ_MESAJ")]
        [TestCase("{\"t\":\"sohbet\",\"kime\":5,\"metin\":\"a\"}", "GECERSIZ_MESAJ")]
        [TestCase("{\"t\":\"sohbet\",\"kime\":null}", "GECERSIZ_MESAJ")]
        public async Task Gecersiz_sohbet(string mesaj, string kod)
        {
            var o = await OdaKur(3);
            await o[0].Gonder(mesaj);
            await o[0].Hata(kod);
        }

        [Test]
        public async Task En_fazla_200_karakter_kime_yoksa_genel()
        {
            var o = await OdaKur(3);
            await Yaz(o[0], new string('a', 201));
            await o[0].Hata("GECERSIZ_GIRDI");
            await o[0].Gonder(new { t = "sohbet", metin = new string('a', 200) });
            Assert.That((await Mesaj(o[1], new string('a', 200)))["kime"], Is.Null);
        }

        [Test]
        public async Task Odaya_girmeden_sohbet_ODADA_DEGILSIN()
        {
            var x = await Baglan();
            await Yaz(x, "selam");
            await x.Hata("ODADA_DEGILSIN");
        }

        // ---------------------------------------------------- Temiz mod

        [Test]
        public async Task Normal_modda_filtre_yok()
        {
            var o = await OdaKur(3);
            await Yaz(o[1], "naber amk");
            await Mesaj(o[2], "naber amk");
        }

        [Test]
        public async Task Temiz_modda_yakalanan_kimseye_gitmez_ucuncu_ihlalde_atilir()
        {
            var o = await OdaKur(4);
            await TemizYap(o);
            var kufurlu = "naber amk " + Guid.NewGuid().ToString("N");

            for (int i = 1; i <= 2; i++)
            {
                await Yaz(o[1], kufurlu);
                var h = await o[1].Bekle("hata");
                Assert.That((string)h["kod"], Is.EqualTo("UYGUNSUZ"));
                Assert.That((int)h["ihlal"], Is.EqualTo(i));
                Assert.That((int)h["sinir"], Is.EqualTo(3));
            }
            await Yaz(o[2], "temiz mesaj");
            foreach (var x in o)
            {
                await Mesaj(x, "temiz mesaj");
                Assert.That(string.Join("\n", x.Hepsi), Does.Not.Contain(kufurlu), "yakalanan mesaj kimseye gitmez");
            }

            await Yaz(o[1], kufurlu);
            var atildin = await o[1].Bekle("hata");
            Assert.That((string)atildin["kod"], Is.EqualTo("ATILDIN"));
            Assert.That((string)atildin["neden"], Is.EqualTo("temiz"));
            Assert.That(await o[1].KapanmaBekle(), Is.True);

            var olay = await o[0].Bekle("olay", m => (string)m["olay"] == "atildi");
            Assert.That((string)olay["kim"], Is.EqualTo(o[1].Sen));
            Assert.That((string)olay["neden"], Is.EqualTo("temiz"));
            await o[0].Bekle("durum", d => d["oyuncular"].AsArray().Count == 3);

            var geri = await Baglan();
            await geri.Gonder(new { t = "katil", oda = o[0].Oda, isim = o[1].Isim, anahtar = (string)null });
            await geri.Hata("ATILDIN");
        }

        [Test]
        public async Task Temiz_modda_soru_da_filtrelenir_ihlaller_sohbetle_ortak_sayilir()
        {
            var o = await OdaKur(4);
            await TemizYap(o);
            await o[0].Gonder(new { t = "baslat" });
            foreach (var x in o) await x.Bekle("durum", d => (string)d["asama"] == "soruSorma");

            var a = Kim(o, (string)o[0].SonDurum["a"]);
            var b = (string)a.SonDurum["secilebilir"][0];
            await a.Gonder(new { t = "komut", tur = 1, komut = "soruSor", hedef = b, metin = "Kim en çok s1kt1r der?" });
            var h = await a.Bekle("hata");
            Assert.That((string)h["kod"], Is.EqualTo("UYGUNSUZ"));
            Assert.That((int)h["ihlal"], Is.EqualTo(1));

            await Yaz(a, "aq");
            Assert.That((int)(await a.Bekle("hata"))["ihlal"], Is.EqualTo(2), "soru ve sohbet aynı sayaçta");

            await a.Gonder(new { t = "komut", tur = 1, komut = "soruSor", hedef = b, metin = "Kim en çok uyur?" });
            await a.Bekle("durum", d => (string)d["asama"] == "cevapSecme");
        }

        [Test]
        public async Task Temiz_ihlalle_atilan_kurucuysa_kuruculuk_devredilir()
        {
            var o = await OdaKur(4);
            await TemizYap(o);
            for (int i = 0; i < 3; i++) await Yaz(o[0], "aq");
            Assert.That((string)(await o[0].Bekle("hata", m => (string)m["kod"] == "ATILDIN"))["neden"], Is.EqualTo("temiz"));
            var d = await o[1].Bekle("durum", m => m["oyuncular"].AsArray().Count == 3);
            Assert.That((string)d["kurucu"], Is.EqualTo(o[1].Sen));
        }

        [Test]
        public async Task Lobide_cikip_ayni_isimle_girince_ihlal_sayaci_sifirlanmaz()
        {
            var o = await OdaKur(4);
            await TemizYap(o);
            await Yaz(o[3], "aq");
            Assert.That((int)(await o[3].Bekle("hata"))["ihlal"], Is.EqualTo(1));
            await o[3].Kapat();
            await o[0].Bekle("durum", d => d["oyuncular"].AsArray().Count == 3);

            var x = await Katil(o[0].Oda, o[3].Isim);
            await Yaz(x, "aq");
            Assert.That((int)(await x.Bekle("hata"))["ihlal"], Is.EqualTo(2));
        }
    }
}
