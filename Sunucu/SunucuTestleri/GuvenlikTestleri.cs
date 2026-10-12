using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;

namespace SunucuTestleri
{
    /// <summary>docs/Guvenlik.md: isim ve soru temizliği (ortak sunucuda).</summary>
    public class MetinGuvenligiTestleri : SunucuTesti
    {
        static string K(int kod) => char.ConvertFromUtf32(kod);
        static readonly string SifirGenislik = K(0x200B);
        static readonly string YonCevir = K(0x202E);
        static readonly string HangulDolgu = K(0x3164);

        [Test]
        public async Task Gorunmez_karakterli_isim_temizlenir_ayni_isim_sayilir()
        {
            var kurucu = await Baglan();
            await kurucu.Gonder(new { t = "odaKur", isim = YonCevir + "Al" + SifirGenislik + "i " });
            await kurucu.Bekle("hosgeldin");
            var d = await kurucu.Bekle("durum");
            Assert.That((string)d["oyuncular"][0]["isim"], Is.EqualTo("Ali"));

            var x = await Baglan();
            await x.Gonder(new { t = "katil", oda = kurucu.Oda, isim = "ALİ", anahtar = (string)null });
            await x.Hata("ISIM_GECERSIZ");
        }

        [Test]
        public async Task Bos_gorunen_isimle_girilemez()
        {
            var x = await Baglan();
            await x.Gonder(new { t = "odaKur", isim = HangulDolgu + SifirGenislik + " " });
            await x.Hata("ISIM_GECERSIZ");
        }

        [Test]
        public async Task Soru_temizlenmis_gider_bos_gorunen_soru_sorulamaz()
        {
            var o = await OyunBaslat(4);
            var a = Kim(o, (string)o[0].SonDurum["a"]);
            var bId = (string)a.SonDurum["secilebilir"][0];
            await a.Gonder(new { t = "komut", tur = 1, komut = "soruSor", hedef = bId, metin = SifirGenislik + HangulDolgu });
            await a.Hata("GECERSIZ_GIRDI");

            await a.Gonder(new { t = "komut", tur = 1, komut = "soruSor", hedef = bId, metin = YonCevir + "Kim  en çok uyur?" });
            var b = Kim(o, bId);
            var d = await b.Bekle("durum", m => (string)m["asama"] == "cevapSecme");
            Assert.That((string)d["sana"]["soru"], Is.EqualTo("Kim en çok uyur?"));
        }
    }

    /// <summary>docs/Guvenlik.md: tarayıcı, odasız bağlantı, toplam oda (her test kendi sunucusunda).</summary>
    public class SunucuGuvenligiTestleri
    {
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

        [Test]
        public async Task Reddedilen_odaKur_arkada_bos_oda_birakmaz()
        {
            sunucu = Kurulum.Sunucu(("Sinirlar:EnFazlaOda", "1"));
            var x = await Baglan();
            await x.Gonder(new { t = "odaKur", isim = char.ConvertFromUtf32(0x3164) + " " });
            await x.Hata("ISIM_GECERSIZ");
            await x.Gonder(new { t = "odaKur", isim = "Ali" });
            await x.Bekle("hosgeldin"); // sahipsiz bir oda kalsaydı SUNUCU_DOLU dönerdi
        }

        async Task<TestIstemci> Baglan()
        {
            var x = await TestIstemci.Baglan(sunucu);
            istemciler.Add(x);
            return x;
        }

        [Test]
        public async Task Tarayicidan_gelen_baglanti_reddedilir_uygulamadan_gelen_acilir()
        {
            sunucu = Kurulum.Sunucu();
            Assert.That(async () => await TestIstemci.Baglan(sunucu, origin: "https://kotu-site.example"), Throws.Exception,
                "Origin başlığı = tarayıcı (HTTP 403)");
            await Baglan();
        }

        [Test]
        public async Task Odaya_girmeyen_baglanti_kesilir_giren_kalir()
        {
            sunucu = Kurulum.Sunucu(("Sinirlar:OdasizSn", "1"));
            var bos = await Baglan();
            var giren = await Baglan();
            await giren.Gonder(new { t = "odaKur", isim = "Ali" });
            await giren.Bekle("hosgeldin");

            Assert.That(await bos.KapanmaBekle(TimeSpan.FromSeconds(4)), Is.True, "odasız bağlantı kesildi");
            Assert.That(giren.Kapandi, Is.False, "odadaki kalır");
        }

        [Test]
        public async Task Toplam_oda_siniri_dolunca_SUNUCU_DOLU_oda_kapaninca_yer_acilir()
        {
            sunucu = Kurulum.Sunucu(("Sinirlar:EnFazlaOda", "1"));
            var ilk = await Baglan();
            await ilk.Gonder(new { t = "odaKur", isim = "Ali" });
            await ilk.Bekle("hosgeldin");

            var ikinci = await Baglan();
            await ikinci.Gonder(new { t = "odaKur", isim = "Veli" });
            await ikinci.Hata("SUNUCU_DOLU");

            await ilk.Kapat(); // tek kişilik lobi boşalınca silinir
            for (var son = DateTime.UtcNow.AddSeconds(5); ; await Task.Delay(100))
            {
                await ikinci.Gonder(new { t = "odaKur", isim = "Veli" });
                var m = await ikinci.Bekle(null, x => (string)x["t"] == "hosgeldin" || (string)x["t"] == "hata");
                if ((string)m["t"] == "hosgeldin") break;
                Assert.That((string)m["kod"], Is.EqualTo("SUNUCU_DOLU"));
                Assert.That(DateTime.UtcNow, Is.LessThan(son), "oda silinince yer açılmalı");
            }
        }
    }
}
