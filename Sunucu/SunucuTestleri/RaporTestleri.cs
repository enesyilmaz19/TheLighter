using System.Globalization;
using System.Text.Json.Nodes;
using NUnit.Framework;

namespace SunucuTestleri
{
    /// <summary>Protokol.md, "Rapor ve engelleme" (Google Play kullanıcı içeriği şartı).</summary>
    public class RaporTestleri : SunucuTesti
    {
        static Task Yaz(TestIstemci x, string metin, string kime = null) => x.Gonder(new { t = "sohbet", kime, metin });

        static Task<JsonNode> Mesaj(TestIstemci x, string metin) => x.Bekle("sohbet", m => (string)m["metin"] == metin);

        /// <summary>Bu odanın diske yazılmış raporları (bütün testler aynı klasöre yazar).</summary>
        static List<JsonNode> Raporlar(string oda)
        {
            var klasor = Path.Combine(Kurulum.ModerasyonKlasoru, "raporlar");
            if (!Directory.Exists(klasor)) return new List<JsonNode>();
            return Directory.GetFiles(klasor, "*.jsonl").SelectMany(File.ReadAllLines)
                .Select(s => JsonNode.Parse(s)).Where(r => (string)r["oda"] == oda).ToList();
        }

        static List<string> Metinler(JsonNode rapor) => rapor["metinler"].AsArray().Select(m => (string)m["metin"]).ToList();

        static string GeciciKlasor() => Path.Combine(Path.GetTempPath(), "cakmak-test-" + Guid.NewGuid().ToString("N"));

        // ---------------------------------------------------- Engelleme

        [Test]
        public async Task Engellenenin_genel_ve_DMi_gelmez_yazan_bilmez_kaldirinca_gelir()
        {
            var o = await OdaKur(3);
            await o[1].Gonder(new { t = "engelle", kim = o[0].Sen });
            var e = await o[1].Bekle("engeller");
            Assert.That(Idler(e["kimler"]), Is.EqualTo(new[] { o[0].Sen }));

            await Yaz(o[0], "genel1");
            await Mesaj(o[0], "genel1"); // kendi mesajı döner: engellendiğini anlamaz
            await Mesaj(o[2], "genel1");
            await Yaz(o[0], "dm1", o[1].Sen);
            await Mesaj(o[0], "dm1");
            // Mesajlar o[1]'e sırayla gider: işaretten önce gelmediyse hiç gelmedi.
            await Yaz(o[2], "isaret");
            await Mesaj(o[1], "isaret");
            Assert.That(o[1].Hepsi.Where(m => m.Contains("genel1") || m.Contains("dm1")), Is.Empty);

            // Engelleyen yazar, engellenen onu görür (grup sohbeti).
            await Yaz(o[1], "bendenHerkese");
            await Mesaj(o[0], "bendenHerkese");

            await o[1].Gonder(new { t = "engelle", kim = o[0].Sen, engel = false });
            e = await o[1].Bekle("engeller");
            Assert.That(e["kimler"].AsArray(), Is.Empty);
            await Yaz(o[0], "genel2");
            await Mesaj(o[1], "genel2");
        }

        [Test]
        public async Task Engel_anahtarla_donuste_bildirilir_cikip_ayni_isimle_donene_de_surer()
        {
            var o = await OdaKur(3);
            await o[1].Gonder(new { t = "engelle", kim = o[2].Sen });
            await o[1].Bekle("engeller");

            // Engellenen lobiden çıkıp aynı isimle döner: yeni kimlik, engel isme bağlı.
            await o[2].Kapat();
            await o[0].Bekle("durum", d => d["oyuncular"].AsArray().Count == 2);
            var donen = await Katil(o[0].Oda, o[2].Isim);
            await Yaz(donen, "yineBen");
            await Yaz(o[0], "isaret");
            await Mesaj(o[1], "isaret");
            Assert.That(o[1].Hepsi.Where(m => m.Contains("yineBen")), Is.Empty);

            // Engelleyen oyunda kopup anahtarla döner: listesi gelir.
            var o4 = await Katil(o[0].Oda, "Dorduncu");
            var oyun = new[] { o[0], o[1], donen, o4 };
            foreach (var x in oyun) await x.Bekle("durum", d => d["oyuncular"].AsArray().Count == 4);
            await o[0].Gonder(new { t = "baslat" });
            foreach (var x in oyun) await x.Bekle("durum", d => (string)d["asama"] == "soruSorma");
            await o[1].Kapat();
            var geri = await Baglan();
            await geri.Gonder(new { t = "katil", oda = o[0].Oda, isim = o[1].Isim, anahtar = o[1].Anahtar });
            await geri.Bekle("hosgeldin");
            var e = await geri.Bekle("engeller");
            Assert.That(Idler(e["kimler"]), Is.EqualTo(new[] { donen.Sen }));
        }

        [Test]
        public async Task Engelle_ve_rapor_kendine_ya_da_odada_olmayana_GECERSIZ_GIRDI()
        {
            var o = await OdaKur(2);
            foreach (var t in new[] { "engelle", "rapor" })
            {
                await o[0].Gonder(new { t, kim = o[0].Sen });
                await o[0].Hata("GECERSIZ_GIRDI");
                await o[0].Gonder(new { t, kim = "p9" });
                await o[0].Hata("GECERSIZ_GIRDI");
            }
            Assert.That(Raporlar(o[0].Oda), Is.Empty);
        }

        // ---------------------------------------------------- Rapor

        [Test]
        public async Task Rapor_raporlayanin_gordugunu_yazar_gormedigini_yazmaz_ayni_kisi_bir_kez_raporlanan_bilmez()
        {
            var o = await OdaKur(3);
            await Yaz(o[1], "herkeseSelam");
            await Mesaj(o[0], "herkeseSelam");
            await Yaz(o[1], "gizliDm", o[2].Sen);
            await Mesaj(o[2], "gizliDm");

            await o[0].Gonder(new { t = "rapor", kim = o[1].Sen });
            var r = await o[0].Bekle("raporAlindi");
            Assert.That((string)r["kim"], Is.EqualTo(o[1].Sen));
            await o[0].Gonder(new { t = "rapor", kim = o[1].Sen });
            await o[0].Bekle("raporAlindi");

            var rapor = Raporlar(o[0].Oda).Single(); // ikincisi yazılmadı
            Assert.That((string)rapor["raporlayan"]["isim"], Is.EqualTo(o[0].Isim));
            Assert.That((string)rapor["raporlanan"]["id"], Is.EqualTo(o[1].Sen));
            Assert.That((string)rapor["raporlanan"]["ip"], Is.Not.Empty);
            Assert.That(Metinler(rapor), Is.EqualTo(new[] { "herkeseSelam" }), "başkalarının DM'i girmez");
            Assert.That((string)rapor["metinler"][0]["kimden"]["id"], Is.EqualTo(o[1].Sen));
            Assert.That(o[1].Hepsi.Where(m => m.Contains("raporAlindi")), Is.Empty, "raporlanan bilmez");
        }

        [Test]
        public async Task Rapora_son_50_metin_girer()
        {
            var o = await OdaKur(2);
            for (int i = 1; i <= 55; i++) await Yaz(o[1], "satir" + i); // sohbet sınırı testlerde kapalı
            await Mesaj(o[0], "satir55");
            await o[0].Gonder(new { t = "rapor", kim = o[1].Sen });
            await o[0].Bekle("raporAlindi");
            var m = Metinler(Raporlar(o[0].Oda).Single());
            Assert.That(m, Has.Count.EqualTo(50));
            Assert.That((m.First(), m.Last()), Is.EqualTo(("satir6", "satir55")));
        }

        [Test]
        public async Task Ifsa_olan_soru_rapora_girer_gume_giden_girmez()
        {
            var o = await OyunBaslat(4);
            var metin = GizliMetin("Ifsa");
            var (a, b) = await Sor(o, metin);
            var c = await Sec(o, b);
            await Hamle(b, "makas");
            await Hamle(c, "tas"); // C kazanır: ifşa
            foreach (var x in o) await x.Bekle("olay", m => (string)m["olay"] == "ifsa");
            var d = o.Single(x => x != a && x != b && x != c);
            await d.Gonder(new { t = "rapor", kim = a.Sen });
            await d.Bekle("raporAlindi");
            var s = Raporlar(d.Oda).Single()["metinler"].AsArray().Single();
            Assert.That(((string)s["tur"], (string)s["metin"], (string)s["kimden"]["id"]), Is.EqualTo(("soru", metin, a.Sen)));

            o = await OyunBaslat(4);
            metin = GizliMetin("Gume");
            (a, b) = await Sor(o, metin);
            c = await Sec(o, b);
            await Hamle(b, "tas");
            await Hamle(c, "makas"); // B kazanır: güme
            foreach (var x in o) await x.Bekle("olay", m => (string)m["olay"] == "gume");
            await b.Gonder(new { t = "rapor", kim = a.Sen }); // B soruyu gördü ama güme gitti (Ana Plan 8.5)
            await b.Bekle("raporAlindi");
            Assert.That(Raporlar(b.Oda).Single()["metinler"].AsArray(), Is.Empty);
        }

        // ---------------------------------------------------- Moderasyon klasörü

        [Test]
        public async Task Yasakli_IP_baglanamaz_liste_sunucu_acikken_degisir()
        {
            var klasor = GeciciKlasor();
            Directory.CreateDirectory(klasor);
            try
            {
                using var sunucu = Kurulum.Sunucu(("Moderasyon:Klasor", klasor));
                await (await TestIstemci.Baglan(sunucu, ip: "203.0.113.5")).DisposeAsync();
                File.WriteAllLines(Path.Combine(klasor, "yasakli-ip.txt"), new[] { "# rapor 2026-10-19", "203.0.113.5" });
                Assert.That(async () => await TestIstemci.Baglan(sunucu, ip: "203.0.113.5"), Throws.Exception, "yasaklı (HTTP 403)");
                await (await TestIstemci.Baglan(sunucu, ip: "203.0.113.6")).DisposeAsync();
            }
            finally
            {
                Directory.Delete(klasor, true);
            }
        }

        [Test]
        public void Otuz_gunu_gecen_rapor_dosyasi_acilista_silinir()
        {
            var klasor = GeciciKlasor();
            var raporlar = Path.Combine(klasor, "raporlar");
            Directory.CreateDirectory(raporlar);
            string Dosya(int gunOnce) => Path.Combine(raporlar,
                DateTime.UtcNow.AddDays(-gunOnce).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + ".jsonl");
            File.WriteAllText(Dosya(32), "{}\n");
            File.WriteAllText(Dosya(29), "{}\n");
            try
            {
                using var sunucu = Kurulum.Sunucu(("Moderasyon:Klasor", klasor));
                _ = sunucu.Server; // sunucu açılır
                Assert.That(File.Exists(Dosya(32)), Is.False, "32 gün önceki");
                Assert.That(File.Exists(Dosya(29)), Is.True, "29 gün önceki");
            }
            finally
            {
                Directory.Delete(klasor, true);
            }
        }
    }
}
