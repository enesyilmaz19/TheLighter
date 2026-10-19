using System.IO;
using NUnit.Framework;
using TheLighter.Ag;
using TheLighter.Akis;

namespace TheLighter.Testler
{
    /// <summary>Sunucudan gelebilecek her kodun ve oturumun çıkış nedenlerinin oyuncuya görünen metni var mı.</summary>
    public class OnlineMetinTestleri
    {
        static MetinTablosu Tablo()
        {
            string yol = Path.Combine("Assets", "UI", "Resources", "TheLighter", "Metinler.csv");
            Assert.IsTrue(File.Exists(yol), "Bulunamadı: " + yol);
            return MetinTablosu.CsvOku(File.ReadAllText(yol));
        }

        [Test]
        public void SunucuHataKodlarininMetniVar()
        {
            var t = Tablo();
            foreach (var kod in Cevirici.SunucuKodlari)
            {
                Assert.That(Cevirici.SonucKodu(kod), Is.Null, kod + " kural motorunun kodu olmamalı");
                Assert.IsTrue(t.Var("hata." + kod), "Eksik: hata." + kod);
            }
        }

        [Test]
        public void CikisNedenlerininMetniVar()
        {
            var t = Tablo();
            Assert.IsTrue(t.Var("online.cikis." + OnlineIstemci.NedenBaglanamadi));
            Assert.IsTrue(t.Var("online.cikis." + OnlineIstemci.NedenAyrildin));
            // GUNCELLE: kendi ekranı var.
            foreach (var k in new[] { "hata." + OnlineIstemci.NedenGuncelle, "guncelle.baslik", "guncelle.aciklama", "guncelle.elden_ele" })
                Assert.IsTrue(t.Var(k), "Eksik: " + k);
        }

        /// <summary>S5: atılma nedenleri, Temiz uyarısı, grup paketi yükleme.</summary>
        [Test]
        public void SohbetTemizGrupMetinleriVar()
        {
            var t = Tablo();
            foreach (var neden in new[] { null, "kurucu", "temiz" })
                Assert.IsTrue(t.Var("hata." + OnlineIstemci.AtilmaNedeni(neden)), "Eksik: hata." + OnlineIstemci.AtilmaNedeni(neden));
            foreach (var k in new[] { "online.uygunsuz", "online.olay.atildi", "online.olay.atildi_temiz", "online.olay.atildi_kurucu",
                         "lobi.bizim_yukleniyor", "lobi.bizim_hata", "kurulum.bizim_paket", "hata.Uygunsuz" })
                Assert.IsTrue(t.Var(k), "Eksik: " + k);
        }

        /// <summary>Rapor ve engelleme ekranı, sunucu adresi yokken online menüsü (tasks/g2-android.md 1, 6).</summary>
        [Test]
        public void RaporEngelMetinleriVar()
        {
            var t = Tablo();
            foreach (var k in new[] { "rapor.engelle", "rapor.engeli_kaldir", "rapor.engel_ipucu", "rapor.engelli_ipucu", "rapor.raporla",
                         "rapor.onay", "rapor.gidiyor", "rapor.alindi", "rapor.engellemek_ister_misin", "rapor.gonderilemedi",
                         "rapor.tekrar", "rapor.engelli_etiket", "rapor.liste_ipucu", "online.sunucu_yok", "lobi.cik_soru",
                         "lobi.cik_ipucu", "lobi.cik_evet", "oyun.geri_soru", "oyun.geri_ipucu_online", "oyun.geri_evet" })
                Assert.IsTrue(t.Var(k), "Eksik: " + k);
        }
    }
}
