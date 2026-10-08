using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;

namespace SunucuTestleri
{
    /// <summary>Sunucu bütün testler için bir kez, süreç içinde kalkar. Her test kendi odasını kurar.</summary>
    [SetUpFixture]
    public class Kurulum
    {
        public static WebApplicationFactory<Program> Fabrika { get; private set; }

        [OneTimeSetUp]
        public void Kur() => Fabrika = new WebApplicationFactory<Program>();

        [OneTimeTearDown]
        public void Kaldir() => Fabrika?.Dispose();
    }

    /// <summary>Testlerin ortak yardımcıları: bağlan, oda kur, oyunu başlat, tur oynat.</summary>
    public abstract class SunucuTesti
    {
        readonly List<TestIstemci> istemciler = new List<TestIstemci>();

        [TearDown]
        public async Task BaglantilariKapat()
        {
            foreach (var i in istemciler) await i.DisposeAsync();
            istemciler.Clear();
        }

        protected async Task<TestIstemci> Baglan()
        {
            var i = await TestIstemci.Baglan(Kurulum.Fabrika);
            istemciler.Add(i);
            return i;
        }

        /// <summary>
        /// odaKur + (n-1) katil. İsimler "Oyuncu1"… Herkes n kişilik lobi durumunu görünce
        /// alınmamış mesajlar temizlenir (Hepsi korunur).
        /// </summary>
        protected async Task<TestIstemci[]> OdaKur(int n)
        {
            var o = new TestIstemci[n];
            o[0] = await Baglan();
            o[0].Isim = "Oyuncu1";
            await o[0].Gonder(new { t = "odaKur", isim = o[0].Isim });
            await o[0].Bekle("hosgeldin");
            for (int i = 1; i < n; i++)
                o[i] = await Katil(o[0].Oda, "Oyuncu" + (i + 1));

            foreach (var x in o) await x.Bekle("durum", d => d["oyuncular"].AsArray().Count == n);
            foreach (var x in o) x.Temizle();
            return o;
        }

        protected async Task<TestIstemci> Katil(string oda, string isim)
        {
            var x = await Baglan();
            x.Isim = isim;
            await x.Gonder(new { t = "katil", oda, isim, anahtar = (string)null });
            await x.Bekle("hosgeldin");
            return x;
        }

        /// <summary>n kişilik oda kurar, kurucu başlatır. Herkes soruSorma durumunu görene kadar bekler.</summary>
        protected async Task<TestIstemci[]> OyunBaslat(int n)
        {
            var o = await OdaKur(n);
            await o[0].Gonder(new { t = "baslat" });
            foreach (var x in o) await x.Bekle("durum", d => (string)d["asama"] == "soruSorma");
            return o;
        }

        /// <summary>A, kendi secilebilir listesinin ilkine soruyu sorar. Herkes cevapSecme durumunu görene kadar bekler.</summary>
        protected static async Task<(TestIstemci a, TestIstemci b)> Sor(TestIstemci[] o, string metin)
        {
            var a = Kim(o, (string)o[0].SonDurum["a"]);
            var bId = (string)a.SonDurum["secilebilir"][0];
            await a.Gonder(new { t = "komut", tur = 1, komut = "soruSor", hedef = bId, metin });
            foreach (var x in o) await x.Bekle("durum", d => (string)d["asama"] == "cevapSecme");
            return (a, Kim(o, bId));
        }

        /// <summary>B, kendi secilebilir listesinin ilkini C seçer. Herkes miniOyun durumunu görene kadar bekler.</summary>
        protected static async Task<TestIstemci> Sec(TestIstemci[] o, TestIstemci b)
        {
            var cId = (string)b.SonDurum["secilebilir"][0];
            await b.Gonder(new { t = "komut", tur = 1, komut = "cevapSec", hedef = cId });
            foreach (var x in o) await x.Bekle("durum", d => (string)d["asama"] == "miniOyun");
            return Kim(o, cId);
        }

        /// <summary>Taş-kağıt-makas hamlesi (varsayılan mini oyun tkm).</summary>
        protected static Task Hamle(TestIstemci x, string secim) =>
            x.Gonder(new { t = "komut", tur = 1, komut = "hamle", hamle = new { oyun = "tkm", secim } });

        protected static TestIstemci Kim(TestIstemci[] o, string id) => o.Single(x => x.Sen == id);

        protected static JsonNode Oyuncu(JsonNode durum, string id) =>
            durum["oyuncular"].AsArray().FirstOrDefault(p => (string)p["id"] == id);

        protected static List<string> Idler(JsonNode dizi) => dizi.AsArray().Select(s => (string)s).ToList();

        /// <summary>Gizli soru için benzersiz, sadece harf/rakam metin: JSON kaçışı ham dizede aramayı bozmasın.</summary>
        protected static string GizliMetin(string on) => on + Guid.NewGuid().ToString("N");
    }
}
