using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using NUnit.Framework;

namespace SunucuTestleri
{
    /// <summary>Protokol.md, S3: ayarlar (ceza, görev, kaos, paketler), yeni komutlar, yeni durum alanları, özet.</summary>
    public class TekrarTestleri : SunucuTesti
    {
        static readonly TimeSpan TurArasi = TimeSpan.FromSeconds(10); // ifşa/güme ekranı 6 sn

        // Sunucu Assets/Icerik'teki paketleri okuyor. Test de aynı yerden bakar.
        static string IcerikKlasoru([CallerFilePath] string buDosya = "") =>
            Path.GetFullPath(Path.Combine(Path.GetDirectoryName(buDosya), "..", "..", "Assets", "Icerik"));

        static List<string> PaketAdlari() =>
            Directory.GetFiles(IcerikKlasoru(), "*.txt").Select(Path.GetFileNameWithoutExtension).OrderBy(x => x, StringComparer.Ordinal).ToList();

        static HashSet<string> PaketSorulari(string ad) =>
            File.ReadAllLines(Path.Combine(IcerikKlasoru(), ad + ".txt"))
                .Select(s => s.Trim()).Where(s => s.Length > 0 && !s.StartsWith('#')).ToHashSet();

        static Task Komut(TestIstemci x, int tur, string komut, object ek = null)
        {
            var m = new JsonObject { ["t"] = "komut", ["tur"] = tur, ["komut"] = komut };
            if (ek != null)
                foreach (var alan in JsonNode.Parse(System.Text.Json.JsonSerializer.Serialize(ek)).AsObject().ToList())
                    m[alan.Key] = alan.Value?.DeepClone();
            return x.Gonder(m.ToJsonString());
        }

        async Task<TestIstemci[]> AyarlaVeBaslat(int n, object ayarlar)
        {
            var o = await OdaKur(n);
            await o[0].Gonder(ayarlar);
            foreach (var x in o) await x.Bekle("durum"); // ayarların lobi durumu kuyrukta kalmasın
            await o[0].Gonder(new { t = "baslat" });
            foreach (var x in o) await x.Bekle("durum", d => (string)d["asama"] == "soruSorma");
            return o;
        }

        // ------------------------------------------------------------ Ayarlar

        [Test]
        public async Task Lobide_S3_ayarlari_varsayilan_kapali_paketler_hepsi()
        {
            var o = await OdaKur(4);
            await o[0].Gonder(new { t = "ayarlar", turSayisi = 10 }); // lobi durumu tekrar gelsin
            var a = (await o[1].Bekle("durum"))["ayarlar"];
            Assert.That((string)a["ceza"], Is.EqualTo("kapali"));
            Assert.That((bool)a["gorevler"], Is.False);
            Assert.That((bool)a["kaos"], Is.False);
            Assert.That(Idler(a["paketler"]), Is.EqualTo(PaketAdlari()));
            Assert.That(PaketAdlari(), Does.Contain("Genel"));
        }

        [Test]
        public async Task Ayarlar_ceza_gorev_kaos_paket_degisir_herkes_gorur()
        {
            var o = await OdaKur(4);
            await o[0].Gonder(new { t = "ayarlar", ceza = "cesur", gorevler = true, kaos = true, paketler = new[] { "Okul", "Genel" } });
            foreach (var x in o)
            {
                var a = (await x.Bekle("durum"))["ayarlar"];
                Assert.That((string)a["ceza"], Is.EqualTo("cesur"));
                Assert.That((bool)a["gorevler"], Is.True);
                Assert.That((bool)a["kaos"], Is.True);
                Assert.That(Idler(a["paketler"]), Is.EqualTo(new[] { "Okul", "Genel" }));
                Assert.That((string)a["mod"], Is.EqualTo("normal"), "gönderilmeyen alan değişmez");
            }
        }

        [Test]
        public async Task Temiz_ve_cesur_birlikte_olamaz_hangi_sirayla_gelirse_gelsin()
        {
            var o = await OdaKur(4);
            await o[0].Gonder(new { t = "ayarlar", mod = "temiz", ceza = "cesur" });
            await o[0].Hata("GECERSIZ_GIRDI");

            await o[0].Gonder(new { t = "ayarlar", ceza = "cesur" });
            await o[0].Bekle("durum", d => (string)d["ayarlar"]["ceza"] == "cesur");
            await o[0].Gonder(new { t = "ayarlar", mod = "temiz" });
            await o[0].Hata("GECERSIZ_GIRDI");

            await o[0].Gonder(new { t = "ayarlar", mod = "temiz", ceza = "hafif" });
            var d = await o[0].Bekle("durum");
            Assert.That((string)d["ayarlar"]["mod"], Is.EqualTo("temiz"));
            Assert.That((string)d["ayarlar"]["ceza"], Is.EqualTo("hafif"));
        }

        [TestCase("{\"t\":\"ayarlar\",\"paketler\":[]}", "GECERSIZ_GIRDI")]
        [TestCase("{\"t\":\"ayarlar\",\"paketler\":[\"YokBoylePaket\"]}", "GECERSIZ_GIRDI")]
        [TestCase("{\"t\":\"ayarlar\",\"paketler\":[\"Genel\",\"Genel\"]}", "GECERSIZ_GIRDI")]
        [TestCase("{\"t\":\"ayarlar\",\"paketler\":[\"genel\"]}", "GECERSIZ_GIRDI")]
        [TestCase("{\"t\":\"ayarlar\",\"paketler\":\"Genel\"}", "GECERSIZ_MESAJ")]
        [TestCase("{\"t\":\"ayarlar\",\"paketler\":[1]}", "GECERSIZ_MESAJ")]
        [TestCase("{\"t\":\"ayarlar\",\"ceza\":\"agir\"}", "GECERSIZ_MESAJ")]
        [TestCase("{\"t\":\"ayarlar\",\"gorevler\":\"evet\"}", "GECERSIZ_MESAJ")]
        [TestCase("{\"t\":\"ayarlar\",\"kaos\":1}", "GECERSIZ_MESAJ")]
        public async Task Gecersiz_S3_ayari_reddedilir_hicbir_sey_degismez(string mesaj, string kod)
        {
            var o = await OdaKur(4);
            await o[0].Gonder(mesaj);
            await o[0].Hata(kod);

            await o[0].Gonder(new { t = "ayarlar", turSayisi = 7 });
            var a = (await o[0].Bekle("durum"))["ayarlar"];
            Assert.That((string)a["ceza"], Is.EqualTo("kapali"));
            Assert.That((bool)a["gorevler"], Is.False);
            Assert.That((bool)a["kaos"], Is.False);
            Assert.That(Idler(a["paketler"]), Is.EqualTo(PaketAdlari()));
        }

        [Test]
        public async Task Oneriler_sadece_secilen_paketten_gelir_ve_hepsi_gelir()
        {
            var okul = PaketSorulari("Okul");
            var o = await AyarlaVeBaslat(4, new { t = "ayarlar", paketler = new[] { "Okul" } });
            var a = Kim(o, (string)o[0].SonDurum["a"]);

            var gelen = new HashSet<string>();
            for (int i = 0; i < okul.Count; i++)
            {
                await Komut(a, 1, "oneriCek");
                var oneri = (string)(await a.Bekle("durum"))["sana"]["oneri"];
                Assert.That(okul, Does.Contain(oneri), "seçilmeyen paketten soru geldi");
                gelen.Add(oneri);
            }
            Assert.That(gelen, Has.Count.EqualTo(okul.Count), "deste bitmeden aynı soru iki kez geldi");
        }

        // ------------------------------------------------------------ Yeni komutlar

        [TestCase("oy")]
        [TestCase("ikiyeKatla")]
        [TestCase("bedelOde")]
        [TestCase("karariGec")]
        public async Task Yeni_komutlar_yanlis_asamada_YANLIS_ASAMA(string komut)
        {
            var o = await OyunBaslat(4);
            var a = Kim(o, (string)o[0].SonDurum["a"]);
            await Komut(a, 1, komut, new { ifsa = true }); // ifsa sadece oy'da okunur
            await a.Hata("YANLIS_ASAMA");
        }

        [TestCase("{\"t\":\"komut\",\"tur\":1,\"komut\":\"oy\"}")]
        [TestCase("{\"t\":\"komut\",\"tur\":1,\"komut\":\"oy\",\"ifsa\":\"evet\"}")]
        [TestCase("{\"t\":\"komut\",\"tur\":1,\"komut\":\"oy\",\"ifsa\":1}")]
        public async Task Oy_ifsa_alani_yoksa_ya_da_mantik_degilse_GECERSIZ_MESAJ(string mesaj)
        {
            var o = await OyunBaslat(4);
            await o[1].Gonder(mesaj);
            await o[1].Hata("GECERSIZ_MESAJ");
        }

        /// <summary>Hafif ceza: C kazanınca soru ifşa olacak, önce B'ye bedel sorulur.</summary>
        async Task<(TestIstemci[] o, TestIstemci a, TestIstemci b, TestIstemci c, string soru)> BedeleGetir()
        {
            var o = await AyarlaVeBaslat(4, new { t = "ayarlar", ceza = "hafif" });
            var soru = GizliMetin("Bedel");
            var (a, b) = await Sor(o, soru);
            var c = await Sec(o, b);
            await Hamle(b, "tas");
            await Hamle(c, "kagit"); // C kazandı
            foreach (var x in o) await x.Bekle("durum", d => (string)d["asama"] == "bedel");
            return (o, a, b, c, soru);
        }

        [Test]
        public async Task Bedel_odenirse_soru_gizli_kalir_B_iki_ceza_ceker()
        {
            var (o, a, b, c, soru) = await BedeleGetir();
            foreach (var x in o)
            {
                Assert.That((string)x.SonDurum["kararVeren"], Is.EqualTo(b.Sen));
                Assert.That((int)Oyuncu(x.SonDurum, b.Sen)["bedelHakki"], Is.EqualTo(2));
            }

            await Komut(c, 1, "bedelOde");
            await c.Hata("SIRA_SENDE_DEGIL");

            await Komut(b, 1, "bedelOde");
            foreach (var x in o)
            {
                var d = await x.Bekle("durum", m => (string)m["asama"] == "gume");
                Assert.That((bool)d["bedelOdendi"], Is.True);
                Assert.That(d["ifsa"], Is.Null);
                Assert.That(d["kararVeren"], Is.Null);
                var cezalar = d["turCezalari"].AsArray();
                Assert.That(cezalar, Has.Count.EqualTo(2), "bedel = 2 kart, kaybedenin normal kartı yerine");
                foreach (var cz in cezalar)
                {
                    Assert.That((string)cz["kim"], Is.EqualTo(b.Sen));
                    Assert.That((string)cz["neden"], Is.EqualTo("bedel"));
                    Assert.That((string)cz["ceza"]["tur"], Is.Not.EqualTo("sesli"), "Hafif'te sesli kart yok");
                }
                var bDurum = Oyuncu(d, b.Sen);
                Assert.That((int)bDurum["bedelHakki"], Is.EqualTo(1));
                Assert.That((int)bDurum["bedelOdeme"], Is.EqualTo(1));
                Assert.That((int)bDurum["cezaCekme"], Is.EqualTo(2));

                var odendi = await x.Bekle("olay", m => (string)m["olay"] == "bedelOdendi");
                Assert.That((string)odendi["kim"], Is.EqualTo(b.Sen));
                var cekildi = await x.Bekle("olay", m => (string)m["olay"] == "cezaCekildi");
                Assert.That((string)cekildi["kim"], Is.EqualTo(b.Sen));
                Assert.That(cekildi["ceza"]?["tur"], Is.Not.Null);
            }

            // Bedel ödenen soru A ve B dışında hiçbir mesaja girmemeli.
            foreach (var x in o.Where(x => x != a && x != b))
                Assert.That(string.Join("\n", x.Hepsi), Does.Not.Contain(soru), x.Sen + " soruyu gördü");
        }

        [Test]
        public async Task Bedel_gecilirse_soru_ifsa_olur_kaybeden_B_bir_kart_ceker()
        {
            var (o, _, b, _, soru) = await BedeleGetir();
            await Komut(b, 1, "karariGec");
            foreach (var x in o)
            {
                var d = await x.Bekle("durum", m => (string)m["asama"] == "ifsa");
                Assert.That((string)d["ifsa"]["soru"], Is.EqualTo(soru));
                Assert.That((bool)d["bedelOdendi"], Is.False);
                var cezalar = d["turCezalari"].AsArray();
                Assert.That(cezalar, Has.Count.EqualTo(1));
                Assert.That((string)cezalar[0]["kim"], Is.EqualTo(b.Sen));
                Assert.That((string)cezalar[0]["neden"], Is.EqualTo("kaybetti"));
                var bDurum = Oyuncu(d, b.Sen);
                Assert.That((int)bDurum["bedelHakki"], Is.EqualTo(2), "geçilen bedel hak yemez");
                Assert.That(bDurum["kisitlamalar"].AsArray(), Has.Count.EqualTo(1), "Hafif kart herkese açık kısıtlama olur");
            }
        }

        [Test]
        public async Task Mini_oyun_suresi_dolup_bedele_gelinince_B_karari_gorur()
        {
            // Sadece C hamle yaptı: süre dolunca C kazanır, soru ifşa olacak, önce B'ye bedel. Bu geçiş saatten
            // geliyor (komuttan değil); olay yayılmasaydı sunucu durum yollamaz, B bedel sorusunu hiç görmezdi.
            var o = await AyarlaVeBaslat(4, new { t = "ayarlar", ceza = "hafif", hamleSn = 5 });
            var (_, b) = await Sor(o, GizliMetin("Sure"));
            var c = await Sec(o, b);
            await Hamle(c, "kagit");
            var olay = await b.Bekle("olay", m => (string)m["olay"] == "kararBekleniyor", TurArasi);
            Assert.That((string)olay["kim"], Is.EqualTo(b.Sen));
            var d = await b.Bekle("durum", m => (string)m["asama"] == "bedel", TurArasi);
            Assert.That((string)d["kararVeren"], Is.EqualTo(b.Sen));
        }

        [Test]
        public async Task Bedel_suresi_dolarsa_karar_gecilmis_sayilir()
        {
            var (o, _, _, _, _) = await BedeleGetir();
            foreach (var x in o) await x.Bekle("durum", m => (string)m["asama"] == "ifsa", TurArasi);
        }

        // ------------------------------------------------------------ Durum alanları

        [Test]
        public async Task Isi_sadece_cesurda_ilk_turda_soguk()
        {
            var cesur = await AyarlaVeBaslat(4, new { t = "ayarlar", ceza = "cesur" });
            foreach (var x in cesur)
            {
                Assert.That((string)x.SonDurum["isi"], Is.EqualTo("soguk"));
                Assert.That((string)x.SonDurum["kaos"], Is.EqualTo("yok"));
            }

            var kapali = await OyunBaslat(4);
            foreach (var x in kapali)
            {
                Assert.That(x.SonDurum["isi"], Is.Null);
                Assert.That(x.SonDurum["sana"]["gorevin"], Is.Null, "görevler kapalı");
                Assert.That(x.SonDurum["ozet"], Is.Null);
                Assert.That(x.SonDurum["turCezalari"].AsArray(), Is.Empty);
            }
        }

        [Test]
        public async Task Gorev_herkese_kendisininki_ozet_sadece_oyun_sonunda()
        {
            var o = await AyarlaVeBaslat(4, new { t = "ayarlar", gorevler = true });
            var gorevler = new Dictionary<string, JsonNode>();
            foreach (var x in o)
            {
                var g = x.SonDurum["sana"]["gorevin"];
                Assert.That(g, Is.Not.Null, x.Sen);
                Assert.That((string)g["tur"], Is.Not.Null.And.Not.Empty);
                Assert.That((string)g["zorluk"], Is.Not.Null.And.Not.Empty);
                Assert.That(x.SonDurum["ozet"], Is.Null, "oyun ortasında özet yok");
                gorevler[x.Sen] = g;
            }

            await o[0].Gonder(new { t = "bitir" });
            foreach (var x in o)
            {
                var d = await x.Bekle("durum", m => (string)m["asama"] == "oyunSonu");
                var ozet = d["ozet"];
                Assert.That(ozet, Is.Not.Null);
                Assert.That((int)ozet["oynananTur"], Is.EqualTo(1));
                Assert.That(ozet["unvanlar"], Is.InstanceOf<JsonArray>());
                Assert.That(ozet["ifsaOlanlar"].AsArray(), Is.Empty);
                Assert.That(ozet["curcunaSorulari"].AsArray(), Is.Empty);

                var sonuclar = ozet["gorevler"].AsArray();
                Assert.That(sonuclar.Select(s => (string)s["kim"]), Is.EquivalentTo(o.Select(y => y.Sen)), "oyun sonunda herkesin görevi açılır");
                foreach (var s in sonuclar)
                    Assert.That(JsonNode.DeepEquals(s["gorev"], gorevler[(string)s["kim"]]), Is.True, "özetteki görev oyundakiyle aynı");
            }
        }

        [Test]
        public async Task Ifsa_olan_soru_ozette_A_B_C_ile()
        {
            var o = await AyarlaVeBaslat(4, new { t = "ayarlar", turSayisi = 1 });
            var soru = GizliMetin("Ozet");
            var (a, b) = await Sor(o, soru);
            var c = await Sec(o, b);
            await Hamle(b, "tas");
            await Hamle(c, "kagit");
            foreach (var x in o)
            {
                var d = await x.Bekle("durum", m => (string)m["asama"] == "oyunSonu", TurArasi);
                var kayit = d["ozet"]["ifsaOlanlar"].AsArray().Single();
                Assert.That((string)kayit["soru"], Is.EqualTo(soru));
                Assert.That((string)kayit["a"], Is.EqualTo(a.Sen));
                Assert.That((string)kayit["b"], Is.EqualTo(b.Sen));
                Assert.That((string)kayit["c"], Is.EqualTo(c.Sen));
            }
        }

        [Test]
        public async Task Kaos_acikken_5_turda_kaos_duyurulur_oncesinde_yok()
        {
            var o = await AyarlaVeBaslat(4, new { t = "ayarlar", kaos = true });
            for (int tur = 1; tur <= 4; tur++)
            {
                if (tur > 1) // 1. turun durumunu AyarlaVeBaslat aldı
                    foreach (var x in o) await x.Bekle("durum", d => (string)d["asama"] == "soruSorma" && (int)d["tur"] == tur, TurArasi);
                Assert.That((string)o[0].SonDurum["kaos"], Is.EqualTo("yok"), "tur " + tur);

                var a = Kim(o, (string)o[0].SonDurum["a"]);
                var b = Kim(o, (string)a.SonDurum["secilebilir"][0]);
                await Komut(a, tur, "soruSor", new { hedef = b.Sen, metin = "Kim en çok uyur?" });
                await b.Bekle("durum", d => (string)d["asama"] == "cevapSecme" && (int)d["tur"] == tur);
                var c = Kim(o, (string)b.SonDurum["secilebilir"][0]);
                await Komut(b, tur, "cevapSec", new { hedef = c.Sen });
                await b.Bekle("durum", d => (string)d["asama"] == "miniOyun" && (int)d["tur"] == tur);
                await Komut(b, tur, "hamle", new { hamle = new { oyun = "tkm", secim = "tas" } });
                await Komut(c, tur, "hamle", new { hamle = new { oyun = "tkm", secim = "kagit" } });
            }

            string kaos = null;
            foreach (var x in o)
            {
                var d = await x.Bekle("durum", m => (int)m["tur"] == 5 && (string)m["asama"] == "soruSorma", TurArasi);
                kaos ??= (string)d["kaos"];
                Assert.That((string)d["kaos"], Is.EqualTo(kaos), "herkes aynı kaosu görür");
                var olay = await x.Bekle("olay", m => (string)m["olay"] == "kaosBasladi");
                Assert.That((string)olay["kaos"], Is.EqualTo(kaos));
            }
            Assert.That(kaos, Is.Not.EqualTo("yok").And.Not.Null);
        }
    }
}
