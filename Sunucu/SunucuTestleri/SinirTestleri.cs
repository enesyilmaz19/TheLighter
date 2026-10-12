using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;

namespace SunucuTestleri
{
    /// <summary>
    /// Protokol.md, "Sınırlar": IP başına bağlantı ve giriş, bağlantı başına mesaj, başlatılmayan lobi.
    /// Her test kendi küçük sınırlı sunucusunu kurar: sınır hakları testler arasında paylaşılmasın.
    /// </summary>
    public class SinirTestleri
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

        void Kur(params (string ad, string deger)[] ayarlar) => sunucu = Kurulum.Sunucu(ayarlar);

        async Task<TestIstemci> Baglan(string ip = null)
        {
            var x = await TestIstemci.Baglan(sunucu, ip);
            istemciler.Add(x);
            return x;
        }

        static Task Katil(TestIstemci x, string oda, string isim = "Ali", string anahtar = null) =>
            x.Gonder(new { t = "katil", oda, isim, anahtar });

        [Test]
        public async Task IP_basina_baglanti_siniri_asilinca_baglanti_acilmaz_biri_kapaninca_acilir()
        {
            Kur(("Sinirlar:IpBasinaBaglanti", "3"));
            var ilk = await Baglan();
            await Baglan();
            await Baglan();
            Assert.That(async () => await TestIstemci.Baglan(sunucu), Throws.Exception, "4. bağlantı (HTTP 429)");

            await ilk.DisposeAsync();
            TestIstemci yeni = null;
            for (var son = DateTime.UtcNow.AddSeconds(5); yeni == null && DateTime.UtcNow < son; await Task.Delay(50))
            {
                try { yeni = await Baglan(); } catch (Exception) { }
            }
            Assert.That(yeni, Is.Not.Null, "kapanan bağlantının hakkı geri gelmeli");
        }

        [Test]
        public async Task Farkli_IP_ler_birbirinin_hakkini_yemez_ayni_IPv6_blogu_ortak_sayilir()
        {
            Kur(("Sinirlar:IpBasinaBaglanti", "1"));
            await Baglan("203.0.113.1");
            await Baglan("203.0.113.2");
            Assert.That(async () => await TestIstemci.Baglan(sunucu, "203.0.113.1"), Throws.Exception);

            await Baglan("2001:db8:1:2::1");
            Assert.That(async () => await TestIstemci.Baglan(sunucu, "2001:db8:1:2:ffff::9"), Throws.Exception,
                "aynı /64 bloğundan adres değiştirmek sınırı aşmaz");
            await Baglan("2001:db8:1:3::1");
        }

        [Test]
        public async Task Dakikadaki_giris_hakki_bitince_odaKur_ve_katil_COK_HIZLI()
        {
            Kur(("Sinirlar:IpBasinaGirisDakikada", "3"));
            var x = await Baglan();
            for (int i = 0; i < 3; i++)
            {
                await Katil(x, "ZZZZZ");
                await x.Hata("ODA_YOK");
            }
            await Katil(x, "ZZZZZ");
            await x.Hata("COK_HIZLI");
            await x.Gonder(new { t = "odaKur", isim = "Ali" });
            await x.Hata("COK_HIZLI");

            var baska = await Baglan("203.0.113.7");
            await baska.Gonder(new { t = "odaKur", isim = "Ali" });
            await baska.Bekle("hosgeldin");
        }

        [Test]
        public async Task Cok_hizli_mesaj_islenmez_COK_HIZLI_baglanti_acik_kalir()
        {
            Kur(("Sinirlar:MesajPatlama", "5"), ("Sinirlar:MesajSaniyede", "5"));
            var x = await Baglan();
            for (int i = 0; i < 15; i++) await x.Gonder("{\"t\":\"bilinmeyen\"}");

            var kodlar = new List<string>();
            for (int i = 0; i < 15; i++) kodlar.Add((string)(await x.Bekle("hata"))["kod"]);
            // 15 mesaj bir saniyeden kısa sürede gider: en fazla bir kez 5 hak dolar, en az 5'i reddedilir.
            Assert.That(kodlar.Count(k => k == "GECERSIZ_MESAJ"), Is.InRange(5, 10));
            Assert.That(kodlar.Count(k => k == "COK_HIZLI"), Is.EqualTo(15 - kodlar.Count(k => k == "GECERSIZ_MESAJ")));

            await Task.Delay(1100);
            await x.Gonder(new { t = "odaKur", isim = "Ali" });
            await x.Bekle("hosgeldin");
        }

        [Test]
        public async Task Baslatilmayan_lobi_suresi_dolunca_kapanir_anahtarla_donus_ODA_YOK()
        {
            Kur(("Sinirlar:LobiSn", "2"));
            var x = await Baglan();
            await x.Gonder(new { t = "odaKur", isim = "Ali" });
            await x.Bekle("hosgeldin");
            Assert.That(await x.KapanmaBekle(TimeSpan.FromSeconds(5)), Is.True, "lobi kapanınca bağlantı kesilir");

            var y = await Baglan();
            await Katil(y, x.Oda, anahtar: x.Anahtar);
            await y.Hata("ODA_YOK");
        }

        [Test]
        public async Task Baslamis_oyun_lobi_suresinden_etkilenmez()
        {
            Kur(("Sinirlar:LobiSn", "2"));
            var o = new List<TestIstemci>();
            for (int i = 0; i < 4; i++)
            {
                var x = await Baglan();
                if (i == 0) await x.Gonder(new { t = "odaKur", isim = "Oyuncu1" });
                else await Katil(x, o[0].Oda, "Oyuncu" + (i + 1));
                await x.Bekle("hosgeldin");
                o.Add(x);
            }
            await o[0].Gonder(new { t = "baslat" });
            foreach (var x in o) await x.Bekle("durum", d => (string)d["asama"] == "soruSorma");

            await Task.Delay(3000);
            foreach (var x in o) Assert.That(x.Kapandi, Is.False);
            await o[0].Gonder(new { t = "bitir" });
            foreach (var x in o) await x.Bekle("durum", d => (string)d["asama"] == "oyunSonu");
        }
    }
}
