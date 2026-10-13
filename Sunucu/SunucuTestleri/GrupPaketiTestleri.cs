using System.Text.Json.Nodes;
using NUnit.Framework;

namespace SunucuTestleri
{
    /// <summary>Protokol.md, "Grup paketi": kurucu lobide parça parça yükler, "@bizim" diye seçilir.</summary>
    public class GrupPaketiTestleri : SunucuTesti
    {
        const string Bizim = "@bizim";

        static Task Yukle(TestIstemci x, bool ilk, params string[] sorular) =>
            x.Gonder(new { t = "grupPaketi", ilk, sorular });

        static Task<JsonNode> Sayisi(TestIstemci x, int n) => x.Bekle("durum", d => (int)d["grupPaketi"] == n);

        static string[] Sorular(string on, int n) => Enumerable.Range(0, n).Select(i => on + i + " kim?").ToArray();

        [Test]
        public async Task Parca_parca_yuklenir_tekrarlar_atilir_herkes_sayiyi_gorur()
        {
            var o = await OdaKur(4);
            Assert.That((int)o[1].SonDurum["grupPaketi"], Is.EqualTo(0));

            await Yukle(o[0], true, "Kim en çok uyur?", "kim EN çok uyur?", "  Kim   geç kalır? ");
            await Yukle(o[0], false, "Kim geç kalır?", "Kim şarkı söyler?");
            foreach (var x in o) await Sayisi(x, 3);

            await Yukle(o[0], true, "Yeni paket kim?");
            await Sayisi(o[1], 1);
        }

        [Test]
        public async Task Secilince_oneri_sadece_grup_paketinden_gelir()
        {
            var o = await OdaKur(4);
            var gizli = GizliMetin("Grup") + " kim?";
            await Yukle(o[0], true, gizli);
            await Sayisi(o[0], 1);
            await o[0].Gonder(new { t = "ayarlar", paketler = new[] { Bizim } });
            await o[0].Bekle("durum", d => d["ayarlar"]["paketler"].AsArray().Select(p => (string)p).SequenceEqual(new[] { Bizim }));

            await o[0].Gonder(new { t = "baslat" });
            foreach (var x in o) await x.Bekle("durum", d => (string)d["asama"] == "soruSorma");
            var a = Kim(o, (string)o[0].SonDurum["a"]);
            await a.Gonder(new { t = "komut", tur = 1, komut = "oneriCek" });
            var d = await a.Bekle("durum", m => m["sana"]?["oneri"] != null);
            Assert.That((string)d["sana"]["oneri"], Is.EqualTo(gizli));
        }

        [Test]
        public async Task Temiz_modda_grup_paketi_kullanilmaz()
        {
            var o = await OdaKur(4);
            await Yukle(o[0], true, "Kim en çok uyur?");
            await Sayisi(o[0], 1);
            await o[0].Gonder(new { t = "ayarlar", mod = "temiz", paketler = new[] { Bizim } });
            await o[0].Bekle("durum", d => (string)d["ayarlar"]["mod"] == "temiz");

            await o[0].Gonder(new { t = "baslat" });
            foreach (var x in o) await x.Bekle("durum", d => (string)d["asama"] == "soruSorma");
            var a = Kim(o, (string)o[0].SonDurum["a"]);
            await a.Gonder(new { t = "komut", tur = 1, komut = "oneriCek" });
            await a.Hata("HAVUZ_BOS");
        }

        [Test]
        public async Task Yuklenmeden_secilemez()
        {
            var o = await OdaKur(3);
            await o[0].Gonder(new { t = "ayarlar", paketler = new[] { "Genel", Bizim } });
            await o[0].Hata("GECERSIZ_GIRDI");
        }

        [Test]
        public async Task En_fazla_500_soru_asan_parca_hic_eklenmez()
        {
            var o = await OdaKur(3);
            await Yukle(o[0], true, Sorular("A", 250));
            await Yukle(o[0], false, Sorular("B", 250));
            await Sayisi(o[0], 500);

            await Yukle(o[0], false, "A0 kim?", "Fazla kim?");
            await o[0].Hata("GECERSIZ_GIRDI");
            // Tek parçada 501, ilk = true: reddedilir, eski paket silinmez. Kısa sorular: mesaj 4096 baytı geçmesin.
            await Yukle(o[0], true, Enumerable.Range(0, 501).Select(i => i + "?").ToArray());
            await o[0].Hata("GECERSIZ_GIRDI");

            o[0].Temizle();
            await Yukle(o[0], false, "a0 KIM?"); // sadece tekrar: parça geçerli, sayı aynı
            Assert.That((int)(await o[0].Bekle("durum"))["grupPaketi"], Is.EqualTo(500));
        }

        [TestCase("{\"t\":\"grupPaketi\",\"sorular\":[]}", "GECERSIZ_GIRDI")]
        [TestCase("{\"t\":\"grupPaketi\",\"sorular\":[\"Kim?\",\"   \"]}", "GECERSIZ_GIRDI")]
        [TestCase("{\"t\":\"grupPaketi\",\"sorular\":\"Kim?\"}", "GECERSIZ_MESAJ")]
        [TestCase("{\"t\":\"grupPaketi\",\"sorular\":[1]}", "GECERSIZ_MESAJ")]
        [TestCase("{\"t\":\"grupPaketi\"}", "GECERSIZ_MESAJ")]
        [TestCase("{\"t\":\"grupPaketi\",\"sorular\":[\"Kim?\"],\"ilk\":\"evet\"}", "GECERSIZ_MESAJ")]
        public async Task Gecersiz_parca(string mesaj, string kod)
        {
            var o = await OdaKur(3);
            await o[0].Gonder(mesaj);
            await o[0].Hata(kod);
        }

        [Test]
        public async Task Cok_uzun_soru_GECERSIZ_GIRDI_parca_eklenmez()
        {
            var o = await OdaKur(3);
            await Yukle(o[0], true, "Kim?", new string('a', 141));
            await o[0].Hata("GECERSIZ_GIRDI");
            await Yukle(o[0], true, new string('a', 140));
            await Sayisi(o[0], 1);
        }

        [Test]
        public async Task Sadece_kurucu_sadece_lobide()
        {
            var o = await OdaKur(4);
            await Yukle(o[1], true, "Kim?");
            await o[1].Hata("KURUCU_DEGILSIN");

            await o[0].Gonder(new { t = "baslat" });
            await o[0].Bekle("durum", d => (string)d["asama"] == "soruSorma");
            await Yukle(o[0], true, "Kim?");
            await o[0].Hata("OYUN_BASLADI");
        }

        [Test]
        public async Task Tekrar_oynayinca_paket_ve_secim_kalir()
        {
            var o = await OdaKur(4);
            await Yukle(o[0], true, "Kim en çok uyur?", "Kim geç kalır?");
            await Sayisi(o[0], 2);
            await o[0].Gonder(new { t = "ayarlar", paketler = new[] { "Genel", Bizim } });
            await o[0].Gonder(new { t = "baslat" });
            await o[0].Bekle("durum", d => (string)d["asama"] == "soruSorma");
            await o[0].Gonder(new { t = "bitir" });
            await o[1].Bekle("durum", d => (string)d["asama"] == "oyunSonu");
            o[1].Temizle(); // oyundan önceki lobi durumları sayılmasın
            await o[0].Gonder(new { t = "tekrarOyna" });

            var d = await o[1].Bekle("durum", m => (string)m["asama"] == "lobi");
            Assert.That((int)d["grupPaketi"], Is.EqualTo(2));
            Assert.That(d["ayarlar"]["paketler"].AsArray().Select(p => (string)p), Has.Member(Bizim));
        }
    }
}
