using System.Text.Json.Nodes;
using Cakmak.Kurallar;
using NUnit.Framework;

namespace SunucuTestleri
{
    /// <summary>Protokol.md, S5: geç gelen bekleme odası, tekrar oyna, kurucunun atması.</summary>
    public class BeklemeAtmaTestleri : SunucuTesti
    {
        /// <summary>Oyuncu nesnelerinin kimlikleri (oyuncular, bekleyenler).</summary>
        static List<string> Kimlikler(JsonNode dizi) => dizi.AsArray().Select(p => (string)p["id"]).ToList();

        static List<string> Bekleyenler(JsonNode d) => Kimlikler(d["bekleyenler"]);

        // ---------------------------------------------------- Bekleme odası

        [Test]
        public async Task Gec_gelen_bekleme_odasinda_bekler_oynayamaz_sir_gormez()
        {
            var o = await OyunBaslat(4);
            var x = await Katil(o[0].Oda, "Gec");
            var d = await x.Bekle("durum");
            Assert.That((string)d["asama"], Is.EqualTo("beklemeOdasi"));
            Assert.That((int)d["tur"], Is.EqualTo(1));
            Assert.That(Kimlikler(d["oyuncular"]), Is.EquivalentTo(o.Select(y => y.Sen)));
            Assert.That(Bekleyenler(d), Is.EqualTo(new[] { x.Sen }));
            Assert.That(d["sana"], Is.Null, "oyunda değil, kendine özel alanı yok");
            foreach (var y in o)
                Assert.That(Bekleyenler(await y.Bekle("durum", m => Bekleyenler(m).Count == 1)), Has.Member(x.Sen));

            await x.Gonder(new { t = "komut", tur = 1, komut = "oneriCek" });
            await x.Hata("OYUNDA_DEGILSIN");

            var a = Kim(o, (string)o[0].SonDurum["a"]);
            Assert.That(Idler(a.SonDurum["secilebilir"]), Has.No.Member(x.Sen));
            var gizli = GizliMetin("Bekleyen");
            var (_, b) = await Sor(o, gizli);
            await x.Bekle("durum"); // soru soruldu, bekleyen de yeni durum aldı
            Assert.That(string.Join("\n", x.Hepsi), Does.Not.Contain(gizli), "bekleme odası soruyu görmez");
        }

        [Test]
        public async Task Bekleme_odasinda_kopan_yerini_kaybeder()
        {
            var o = await OyunBaslat(4);
            var x = await Katil(o[0].Oda, "Gec");
            await o[1].Bekle("durum", m => Bekleyenler(m).Count == 1);
            await x.Kapat();
            await o[1].Bekle("durum", m => (string)m["asama"] != "lobi" && Bekleyenler(m).Count == 0);
        }

        [Test]
        public async Task Yerini_kaybeden_anahtar_ve_ayni_isimle_yeniden_girer()
        {
            // Protokol.md: yeniden bağlanırken anahtar + aynı isim. Lobide kopanın anahtarı unutulur, isimle girer.
            var o = await OdaKur(3);
            var x = o[2];
            await x.Kapat();
            await o[0].Bekle("durum", m => m["oyuncular"].AsArray().Count == 2);

            var y = await Baglan();
            await y.Gonder(new { t = "katil", oda = o[0].Oda, isim = x.Isim, anahtar = x.Anahtar });
            await y.Bekle("hosgeldin");
            var d = await y.Bekle("durum");
            Assert.That(d["oyuncular"].AsArray().Select(p => (string)p["isim"]), Has.Member(x.Isim));
        }

        [Test]
        public async Task Oda_kapasitesi_bekleyenlerle_birlikte()
        {
            var o = await OyunBaslat(4);
            for (int i = 4; i < Oyun.GeceEnFazlaOyuncu; i++) await Katil(o[0].Oda, "Bekleyen" + i);
            var fazla = await Baglan();
            await fazla.Gonder(new { t = "katil", oda = o[0].Oda, isim = "Fazla", anahtar = (string)null });
            await fazla.Hata("ODA_DOLU");
        }

        // ---------------------------------------------------- Tekrar oyna

        [Test]
        public async Task Tekrar_oyna_lobiye_doner_bekleyen_oyuncu_olur_kopan_duser()
        {
            var o = await OyunBaslat(4);
            var x = await Katil(o[0].Oda, "Gec");
            await o[0].Gonder(new { t = "tekrarOyna" });
            await o[0].Hata("YANLIS_ASAMA");

            await o[3].Kapat(); // oyun bitmeden kopan
            await o[0].Gonder(new { t = "bitir" });
            await o[0].Bekle("durum", m => (string)m["asama"] == "oyunSonu");
            await o[1].Gonder(new { t = "tekrarOyna" });
            await o[1].Hata("KURUCU_DEGILSIN");

            await o[0].Gonder(new { t = "tekrarOyna" });
            var beklenen = new[] { o[0].Sen, o[1].Sen, o[2].Sen, x.Sen };
            foreach (var y in new[] { o[0], o[1], o[2], x })
            {
                var d = await y.Bekle("durum", m => (string)m["asama"] == "lobi");
                Assert.That(d["oyuncular"].AsArray().Select(p => (string)p["id"]), Is.EquivalentTo(beklenen));
                Assert.That((string)d["ayarlar"]["mod"], Is.EqualTo("normal"), "ayarlar kalır");
            }

            await o[0].Gonder(new { t = "baslat" });
            var oyunda = await x.Bekle("durum", m => (string)m["asama"] == "soruSorma");
            Assert.That((int)oyunda["tur"], Is.EqualTo(1));
            Assert.That(Bekleyenler(oyunda), Is.Empty);
        }

        // ---------------------------------------------------- Atma

        [Test]
        public async Task Lobide_atilan_ATILDIN_alir_baglantisi_kapanir_anahtarla_ve_ayni_isimle_donemez()
        {
            var o = await OdaKur(3);
            var atilan = o[2];
            await o[0].Gonder(new { t = "at", kim = atilan.Sen });
            await atilan.Hata("ATILDIN");
            Assert.That(await atilan.KapanmaBekle(), Is.True, "sunucu bağlantıyı kapatır");

            var olay = await o[1].Bekle("olay", m => (string)m["olay"] == "atildi");
            Assert.That((string)olay["kim"], Is.EqualTo(atilan.Sen));
            var d = await o[1].Bekle("durum", m => m["oyuncular"].AsArray().Count == 2);
            Assert.That(Kimlikler(d["oyuncular"]), Has.No.Member(atilan.Sen));

            var anahtarla = await Baglan();
            await anahtarla.Gonder(new { t = "katil", oda = o[0].Oda, isim = (string)null, anahtar = atilan.Anahtar });
            await anahtarla.Hata("ATILDIN");

            var ayniIsim = await Baglan();
            await ayniIsim.Gonder(new { t = "katil", oda = o[0].Oda, isim = atilan.Isim.ToUpperInvariant(), anahtar = (string)null });
            await ayniIsim.Hata("ATILDIN");
        }

        [TestCase("{\"t\":\"at\",\"kim\":\"p9\"}", "GECERSIZ_GIRDI")]
        [TestCase("{\"t\":\"at\",\"kim\":\"p1\"}", "GECERSIZ_GIRDI")] // kurucu kendini atamaz
        [TestCase("{\"t\":\"at\",\"kim\":\"x\"}", "GECERSIZ_MESAJ")]
        [TestCase("{\"t\":\"at\"}", "GECERSIZ_MESAJ")]
        public async Task Gecersiz_atma(string mesaj, string kod)
        {
            var o = await OdaKur(3);
            await o[0].Gonder(mesaj);
            await o[0].Hata(kod);
        }

        [Test]
        public async Task Sadece_kurucu_atar()
        {
            var o = await OdaKur(3);
            await o[1].Gonder(new { t = "at", kim = o[2].Sen });
            await o[1].Hata("KURUCU_DEGILSIN");
        }

        [Test]
        public async Task Oyunda_atilan_oyundan_cikar_sayaclari_kalir()
        {
            var o = await OyunBaslat(5);
            var atilan = o[4];
            await o[0].Gonder(new { t = "at", kim = atilan.Sen });
            await atilan.Hata("ATILDIN");
            Assert.That(await atilan.KapanmaBekle(), Is.True);

            await o[1].Bekle("olay", m => (string)m["olay"] == "oyuncuCikti" && (string)m["kim"] == atilan.Sen);
            var d = await o[1].Bekle("durum", m => (bool)Oyuncu(m, atilan.Sen)["cikti"]);
            Assert.That(Oyuncu(d, atilan.Sen), Is.Not.Null, "özette kalır");
        }

        [Test]
        public async Task Bekleme_odasindaki_atilinca_listeden_duser()
        {
            var o = await OyunBaslat(4);
            var x = await Katil(o[0].Oda, "Gec");
            await o[1].Bekle("durum", m => Bekleyenler(m).Count == 1);
            await o[0].Gonder(new { t = "at", kim = x.Sen });
            await x.Hata("ATILDIN");
            await o[1].Bekle("durum", m => (string)m["asama"] != "lobi" && Bekleyenler(m).Count == 0);
        }
    }
}
