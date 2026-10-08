using NUnit.Framework;

namespace SunucuTestleri
{
    /// <summary>Protokol.md: Bağlantı (tek JSON nesnesi, en fazla 4 KB) ve GECERSIZ_MESAJ.</summary>
    public class MesajTestleri : SunucuTesti
    {
        [TestCase("bu json degil")]
        [TestCase("[1,2,3]")]                          // JSON ama nesne değil
        [TestCase("{\"isim\":\"Enes\"}")]               // t yok
        [TestCase("{\"t\":\"yokBoyleBirSey\"}")]        // t bilinmiyor
        [TestCase("{\"t\":\"odaKur\",\"isim\":5}")]     // alan yanlış tipte
        [TestCase("{\"t\":\"odaKur\"}")]                // alan eksik (belirsizlik: ISIM_GECERSIZ de savunulabilir; protokol "alan eksik" = GECERSIZ_MESAJ diyor)
        [TestCase("{\"t\":\"odaKur\",\"isim\":\"a\",\"isim\":\"b\"}")]   // aynı alan iki kez (inceleme bulgusu 1)
        [TestCase("{\"t\":\"odaKur\",\"t\":\"katil\",\"isim\":\"a\"}")]  // t iki kez
        public async Task Bozuk_mesaj_GECERSIZ_MESAJ_ve_baglanti_acik_kalir(string ham)
        {
            var x = await Baglan();
            await x.Gonder(ham);
            await x.Hata("GECERSIZ_MESAJ");

            // Bağlantı açık kalmalı: sonraki geçerli mesaj çalışır.
            await x.Gonder(new { t = "odaKur", isim = "Enes" });
            await x.Bekle("hosgeldin");
        }

        [Test]
        public async Task Ikili_cerceve_GECERSIZ_MESAJ_ve_baglanti_acik_kalir()
        {
            var x = await Baglan();
            await x.IkiliGonder(new byte[] { 1, 2, 3 });
            await x.Hata("GECERSIZ_MESAJ");
            await x.Gonder(new { t = "odaKur", isim = "Enes" });
            await x.Bekle("hosgeldin");
        }

        [Test]
        public async Task Mesaj_4KB_dan_buyukse_baglanti_kapanir()
        {
            var x = await Baglan();
            // Geçerli JSON, ama 4 KB'tan (4096 bayt) belirgin büyük: sınırın 4000 mi 4096 mı olduğu fark etmesin.
            await x.Gonder("{\"t\":\"odaKur\",\"isim\":\"" + new string('a', 6000) + "\"}");
            Assert.That(await x.KapanmaBekle(), Is.True, "4 KB'tan büyük mesaj bağlantıyı kapatmalı");
        }
    }
}
