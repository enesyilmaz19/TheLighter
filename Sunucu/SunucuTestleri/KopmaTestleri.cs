using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;

namespace SunucuTestleri
{
    /// <summary>
    /// Protokol.md, "Bağlantı koparsa": 60 sn yer tutma, oyundan çıkma, 4'ün altında bekleme, AFK, herkes koparsa oda.
    /// Süreler kısaltılmış kendi sunucusunda (KopukSn, AzOyuncuSn).
    /// </summary>
    public class KopmaTestleri
    {
        static readonly TimeSpan Uzun = TimeSpan.FromSeconds(10);

        WebApplicationFactory<Program> sunucu;
        readonly List<TestIstemci> istemciler = new List<TestIstemci>();

        [TearDown]
        public async Task Kapat()
        {
            foreach (var i in istemciler) await i.DisposeAsync();
            istemciler.Clear();
            sunucu?.Dispose();
            sunucu = null;
        }

        async Task<TestIstemci> Baglan()
        {
            var x = await TestIstemci.Baglan(sunucu);
            istemciler.Add(x);
            return x;
        }

        /// <summary>n kişilik oyun, kısa sürelerle. Herkes soruSorma durumunu görene kadar bekler.</summary>
        async Task<List<TestIstemci>> OyunBaslat(int n, int kopukSn = 2, int azOyuncuSn = 3, int cevapSn = 20)
        {
            sunucu = Kurulum.Sunucu(("Sinirlar:KopukSn", kopukSn.ToString()), ("Sinirlar:AzOyuncuSn", azOyuncuSn.ToString()));
            var o = new List<TestIstemci>();
            for (int i = 0; i < n; i++)
            {
                var x = await Baglan();
                x.Isim = "Oyuncu" + (i + 1);
                if (i == 0) await x.Gonder(new { t = "odaKur", isim = x.Isim });
                else await x.Gonder(new { t = "katil", oda = o[0].Oda, isim = x.Isim, anahtar = (string)null });
                await x.Bekle("hosgeldin");
                o.Add(x);
            }
            await o[0].Gonder(new { t = "ayarlar", turSayisi = 0, cevapSn });
            foreach (var x in o) await x.Bekle("durum", d => (int)d["ayarlar"]["cevapSn"] == cevapSn);
            await o[0].Gonder(new { t = "baslat" });
            foreach (var x in o) await x.Bekle("durum", d => (string)d["asama"] == "soruSorma");
            foreach (var x in o) x.Temizle(); // lobi durumları kuyrukta kalıp koşullara takılmasın
            return o;
        }

        static JsonNode Oyuncu(JsonNode d, string id) => d["oyuncular"].AsArray().First(p => (string)p["id"] == id);

        /// <summary>Kopan oyuncuyu aynı anahtarla yeni bir bağlantıdan geri getirir.</summary>
        async Task<TestIstemci> DonDur(TestIstemci eski)
        {
            var y = await Baglan();
            await y.Gonder(new { t = "katil", oda = eski.Oda, isim = (string)null, anahtar = eski.Anahtar });
            await y.Bekle("hosgeldin");
            return y;
        }

        static TestIstemci A(List<TestIstemci> o) => o.Single(y => y.Sen == (string)o[0].SonDurum["a"]);

        /// <summary>A olmayan bir oyuncu.</summary>
        static TestIstemci AOlmayan(List<TestIstemci> o) => o.Last(y => y != A(o));

        [Test]
        public async Task Kopanin_yeri_tutulur_secilemez_donmezse_oyundan_cikar()
        {
            var o = await OyunBaslat(5, kopukSn: 2);
            var a = A(o);
            var x = AOlmayan(o);
            await x.DisposeAsync();

            var d = await a.Bekle("durum", m => (bool)Oyuncu(m, x.Sen)["bagli"] == false);
            Assert.That((bool)Oyuncu(d, x.Sen)["cikti"], Is.False, "yeri tutuluyor");
            Assert.That(d["secilebilir"].AsArray().Select(s => (string)s), Does.Not.Contain(x.Sen), "kopuk seçilemez");

            var olay = await o[0].Bekle("olay", m => (string)m["olay"] == "oyuncuCikti", Uzun);
            Assert.That((string)olay["kim"], Is.EqualTo(x.Sen));
            d = await o[0].Bekle("durum", m => (bool)Oyuncu(m, x.Sen)["cikti"], Uzun);
            Assert.That(d["bekleme"], Is.Null, "4 kişi kaldı, oyun sürüyor");
        }

        [Test]
        public async Task Suresinde_donen_oyuna_devam_eder()
        {
            var o = await OyunBaslat(5, kopukSn: 3);
            var a = A(o);
            var x = AOlmayan(o);
            await x.DisposeAsync();
            await a.Bekle("durum", m => (bool)Oyuncu(m, x.Sen)["bagli"] == false);

            var y = await DonDur(x);
            var d = await y.Bekle("durum");
            Assert.That((bool)Oyuncu(d, x.Sen)["bagli"], Is.True);
            d = await a.Bekle("durum", m => (bool)Oyuncu(m, x.Sen)["bagli"]);
            Assert.That(d["secilebilir"].AsArray().Select(s => (string)s), Does.Contain(x.Sen), "dönen yine seçilebilir");
            await Task.Delay(3500);
            Assert.That((bool)Oyuncu(y.SonDurum, x.Sen)["cikti"], Is.False, "döndü, çıkmadı");
        }

        [Test]
        public async Task Dortun_altinda_oyun_durur_donen_olunca_surer()
        {
            var o = await OyunBaslat(4, kopukSn: 1, azOyuncuSn: 30);
            var x = o[3];
            await x.DisposeAsync();
            var d = await o[0].Bekle("durum", m => m["bekleme"] != null);
            Assert.That((int)d["bekleme"]["kalanMs"], Is.InRange(1, 30000));

            await Task.Delay(1500); // durmasaydı 1 sn'lik yer tutma dolar, x oyundan çıkardı
            var y = await DonDur(x);
            d = await o[0].Bekle("durum", m => m["bekleme"] == null && (bool)Oyuncu(m, x.Sen)["bagli"]);
            Assert.That((bool)Oyuncu(d, x.Sen)["cikti"], Is.False, "beklerken yer tutma süresi işlemez");
            Assert.That((string)d["asama"], Is.EqualTo("soruSorma"));
        }

        [Test]
        public async Task Dortun_altinda_kimse_donmezse_oyun_biter()
        {
            var o = await OyunBaslat(4, azOyuncuSn: 1);
            await o[3].DisposeAsync();
            await o[0].Bekle("durum", m => (string)m["asama"] == "oyunSonu", Uzun);
        }

        [Test]
        public async Task Herkes_koparsa_oda_bir_sure_tutulur_sonra_silinir()
        {
            var o = await OyunBaslat(4, azOyuncuSn: 2);
            foreach (var x in o) await x.DisposeAsync();
            await Task.Delay(700); // sunucu saati birkaç kez tıklasın: "hemen silme" olsaydı burada silinirdi

            var donen = await DonDur(o[0]);
            await donen.Bekle("durum");

            foreach (var x in istemciler.ToList()) await x.DisposeAsync();
            await Task.Delay(3000);
            var gec = await Baglan();
            await gec.Gonder(new { t = "katil", oda = o[0].Oda, isim = (string)null, anahtar = o[0].Anahtar });
            await gec.Hata("ODA_YOK");
        }

        [TestCase("buradayim")]
        [TestCase("komut")]
        public async Task Ust_uste_suresini_dolduran_AFK_olur_bir_mesaj_yollayinca_kalkar(string mesaj)
        {
            var o = await OyunBaslat(5, cevapSn: 5);
            var x = AOlmayan(o);
            for (int tur = 1; tur <= 2; tur++)
            {
                var d = o[0].SonDurum;
                if (tur > 1) d = await o[0].Bekle("durum", m => (int)m["tur"] == tur && (string)m["asama"] == "soruSorma", Uzun);
                var a = o.Single(y => y.Sen == (string)d["a"]);
                Assert.That(a, Is.Not.SameAs(x));
                await a.Gonder(new { t = "komut", tur, komut = "soruSor", hedef = x.Sen, metin = "Kim uyuyor?" });
                // x cevap seçmez, 5 sn'de süresi dolar
            }
            var afk = await o[0].Bekle("olay", m => (string)m["olay"] == "afk", Uzun);
            Assert.That((string)afk["kim"], Is.EqualTo(x.Sen));
            var son = await o[0].Bekle("durum", m => (bool)Oyuncu(m, x.Sen)["afk"], Uzun);
            Assert.That(son["secilebilir"].AsArray().Select(s => (string)s), Does.Not.Contain(x.Sen));

            // Herhangi bir komut da (geçersiz olsa bile) "başındayım" demektir.
            if (mesaj == "buradayim") await x.Gonder(new { t = "buradayim" });
            else await x.Gonder(new { t = "komut", tur = (int)son["tur"], komut = "oneriCek" });
            await o[0].Bekle("olay", m => (string)m["olay"] == "afkBitti" && (string)m["kim"] == x.Sen);
            await o[0].Bekle("durum", m => !(bool)Oyuncu(m, x.Sen)["afk"]);
        }
    }
}
