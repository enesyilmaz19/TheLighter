using System.Text.RegularExpressions;
using NUnit.Framework;

namespace SunucuTestleri
{
    /// <summary>Protokol.md: Bağlantı, odaKur, katil, ayarlar, baslat, lobide kopma.</summary>
    public class LobiTestleri : SunucuTesti
    {
        // ------------------------------------------------------------ odaKur

        [Test]
        public async Task OdaKur_hosgeldin_p1_ve_32_karakter_anahtar()
        {
            var x = await Baglan();
            await x.Gonder(new { t = "odaKur", isim = "Enes" });
            var h = await x.Bekle("hosgeldin");

            Assert.That((string)h["sen"], Is.EqualTo("p1"));
            Assert.That((string)h["anahtar"], Has.Length.EqualTo(32));
        }

        [Test]
        public async Task Oda_kodu_5_harf_ve_I_O_yok()
        {
            // Kod rastgele: tek odaya bakmak I/O yasağını yakalamaz, birkaç oda kur.
            var kodlar = new List<string>();
            for (int i = 0; i < 20; i++)
            {
                var x = await Baglan();
                await x.Gonder(new { t = "odaKur", isim = "Kurucu" });
                kodlar.Add((string)(await x.Bekle("hosgeldin"))["oda"]);
            }

            foreach (var k in kodlar)
                Assert.That(Regex.IsMatch(k ?? "", "^[A-HJ-NP-Z]{5}$"), Is.True, $"oda kodu: {k}");
            // Protokol "bu kodla oda yok" diyor: açık iki oda aynı kodu taşıyamaz.
            Assert.That(kodlar, Is.Unique);
        }

        [Test]
        public async Task OdaKur_sonra_lobi_durumu_varsayilan_ayarlarla()
        {
            var x = await Baglan();
            await x.Gonder(new { t = "odaKur", isim = "Enes" });
            var h = await x.Bekle("hosgeldin");
            var d = await x.Bekle("durum");

            Assert.That((string)d["asama"], Is.EqualTo("lobi"));
            Assert.That((string)d["kurucu"], Is.EqualTo("p1"));
            Assert.That((string)d["oda"], Is.EqualTo((string)h["oda"]));

            var oyuncular = d["oyuncular"].AsArray();
            Assert.That(oyuncular, Has.Count.EqualTo(1));
            Assert.That((string)oyuncular[0]["id"], Is.EqualTo("p1"));
            Assert.That((string)oyuncular[0]["isim"], Is.EqualTo("Enes"));
            Assert.That((bool)oyuncular[0]["bagli"], Is.True);

            // Belirsizlik: Protokol varsayılanları açıkça yazmıyor. Lobi örneğindeki değerler alındı
            // (kural motorundaki Ayarlar varsayılanlarıyla da aynı).
            var ay = d["ayarlar"];
            Assert.That((string)ay["mod"], Is.EqualTo("normal"));
            Assert.That((string)ay["miniOyun"], Is.EqualTo("tkm"));
            Assert.That((int)ay["turSayisi"], Is.EqualTo(15));
            Assert.That((int)ay["soruSn"], Is.EqualTo(60));
            Assert.That((int)ay["cevapSn"], Is.EqualTo(20));
            Assert.That((int)ay["hamleSn"], Is.EqualTo(10));
        }

        // ------------------------------------------------------------ katil

        [Test]
        public async Task Katil_ikinci_oyuncu_p2_olur_herkes_durum_alir()
        {
            var p1 = await Baglan();
            await p1.Gonder(new { t = "odaKur", isim = "Enes" });
            var h1 = await p1.Bekle("hosgeldin");

            var p2 = await Baglan();
            await p2.Gonder(new { t = "katil", oda = (string)h1["oda"], isim = "Ali", anahtar = (string)null });
            var h2 = await p2.Bekle("hosgeldin");

            Assert.That((string)h2["sen"], Is.EqualTo("p2"));
            Assert.That((string)h2["oda"], Is.EqualTo((string)h1["oda"]));
            Assert.That((string)h2["anahtar"], Has.Length.EqualTo(32));
            Assert.That((string)h2["anahtar"], Is.Not.EqualTo((string)h1["anahtar"]));

            foreach (var x in new[] { p1, p2 })
            {
                var d = await x.Bekle("durum", m => m["oyuncular"].AsArray().Count == 2);
                Assert.That((string)d["asama"], Is.EqualTo("lobi"));
                Assert.That((string)d["kurucu"], Is.EqualTo("p1"));
                Assert.That((string)Oyuncu(d, "p2")["isim"], Is.EqualTo("Ali"));
            }

            // hosgeldin sadece gönderene gider.
            Assert.That(p1.Hepsi.Count(m => m.Contains("\"hosgeldin\"")), Is.EqualTo(1));
        }

        [Test]
        public async Task Katil_olmayan_oda_ODA_YOK()
        {
            var x = await Baglan();
            // Geçerli biçimde bir kod; başka testin odasıyla çakışma olasılığı 24^5'te bir.
            await x.Gonder(new { t = "katil", oda = "ZZZZZ", isim = "Enes", anahtar = (string)null });
            await x.Hata("ODA_YOK");
        }

        [Test]
        public async Task Katil_11_kisi_ODA_DOLU()
        {
            var o = await OdaKur(10);
            var x = await Baglan();
            await x.Gonder(new { t = "katil", oda = o[0].Oda, isim = "Onbirinci", anahtar = (string)null });
            await x.Hata("ODA_DOLU");
        }

        [TestCase("")]
        [TestCase("    ")]                    // boşluk silinince boş
        [TestCase("123456789012345678901")]  // 21 karakter
        public async Task Katil_gecersiz_isim_ISIM_GECERSIZ(string isim)
        {
            var o = await OdaKur(1);
            var x = await Baglan();
            await x.Gonder(new { t = "katil", oda = o[0].Oda, isim, anahtar = (string)null });
            await x.Hata("ISIM_GECERSIZ");
        }

        [TestCase("OYUNCU1")]
        [TestCase("  oyuncu1 ")]
        public async Task Katil_ayni_isim_buyuk_kucuk_harf_fark_etmez_ISIM_GECERSIZ(string isim)
        {
            var o = await OdaKur(1); // kurucunun ismi "Oyuncu1"
            var x = await Baglan();
            await x.Gonder(new { t = "katil", oda = o[0].Oda, isim, anahtar = (string)null });
            await x.Hata("ISIM_GECERSIZ");
        }

        [Test]
        public async Task OdaKur_bos_isim_ISIM_GECERSIZ()
        {
            var x = await Baglan();
            await x.Gonder(new { t = "odaKur", isim = "  " });
            await x.Hata("ISIM_GECERSIZ");
        }

        [Test]
        public async Task Isim_bas_son_bosluk_silinir_20_karakter_gecerli()
        {
            var o = await OdaKur(1);
            var x = await Baglan();
            await x.Gonder(new { t = "katil", oda = o[0].Oda, isim = "  12345678901234567890  ", anahtar = (string)null });
            await x.Bekle("hosgeldin");

            var d = await x.Bekle("durum", m => m["oyuncular"].AsArray().Count == 2);
            Assert.That((string)Oyuncu(d, x.Sen)["isim"], Is.EqualTo("12345678901234567890"));
        }

        [TestCase("odaKur")]
        [TestCase("katil")]
        public async Task Odadaki_baglanti_tekrar_girerse_ZATEN_ODADASIN(string t)
        {
            var o = await OdaKur(2);
            foreach (var x in o)
            {
                if (t == "odaKur") await x.Gonder(new { t, isim = "BaskaIsim" });
                else await x.Gonder(new { t, oda = o[0].Oda, isim = "BaskaIsim", anahtar = (string)null });
                await x.Hata("ZATEN_ODADASIN");
            }
        }

        // ------------------------------------------------------------ Oda komutları

        [TestCase("{\"t\":\"baslat\"}")]
        [TestCase("{\"t\":\"ayarlar\",\"soruSn\":30}")]
        [TestCase("{\"t\":\"komut\",\"tur\":1,\"komut\":\"soruSor\",\"hedef\":\"p2\",\"metin\":\"Merhaba\"}")]
        [TestCase("{\"t\":\"bitir\"}")]
        public async Task Odaya_girmeden_oda_komutu_ODADA_DEGILSIN(string json)
        {
            var x = await Baglan();
            await x.Gonder(json);
            await x.Hata("ODADA_DEGILSIN");
        }

        [TestCase("{\"t\":\"ayarlar\",\"soruSn\":30}")]
        [TestCase("{\"t\":\"baslat\"}")]
        public async Task Kurucu_olmayan_lobide_KURUCU_DEGILSIN(string json)
        {
            var o = await OdaKur(4); // 4 kişi: baslat'ta oyuncu sayısı hatası araya girmesin
            await o[1].Gonder(json);
            await o[1].Hata("KURUCU_DEGILSIN");
        }

        [Test]
        public async Task Online_reaksiyon_secilemez_GECERSIZ_GIRDI()
        {
            // Ana Plan: Reaksiyon sadece elden ele. Online'da telefonlar arası dokunma gecikmesi farkı adil değil,
            // ayrıca süre istemciden geldiği için hile yapılabilir.
            var o = await OdaKur(1);
            await o[0].Gonder(new { t = "ayarlar", miniOyun = "reaksiyon" });
            await o[0].Hata("GECERSIZ_GIRDI");
        }

        [Test]
        public async Task Ayarlar_sadece_gonderilen_alanlari_degistirir()
        {
            var o = await OdaKur(2);

            await o[0].Gonder(new { t = "ayarlar", soruSn = 30 });
            foreach (var x in o)
            {
                var ay = (await x.Bekle("durum", d => (int)d["ayarlar"]["soruSn"] == 30))["ayarlar"];
                Assert.That((string)ay["mod"], Is.EqualTo("normal"));
                Assert.That((string)ay["miniOyun"], Is.EqualTo("tkm"));
                Assert.That((int)ay["turSayisi"], Is.EqualTo(15));
                Assert.That((int)ay["cevapSn"], Is.EqualTo(20));
                Assert.That((int)ay["hamleSn"], Is.EqualTo(10));
            }

            await o[0].Gonder(new { t = "ayarlar", mod = "temiz", hamleSn = 15 });
            foreach (var x in o)
            {
                var ay = (await x.Bekle("durum", d => (string)d["ayarlar"]["mod"] == "temiz"))["ayarlar"];
                Assert.That((int)ay["hamleSn"], Is.EqualTo(15));
                Assert.That((int)ay["soruSn"], Is.EqualTo(30), "önceki ayar korunmalı");
                Assert.That((string)ay["miniOyun"], Is.EqualTo("tkm"));
                Assert.That((int)ay["turSayisi"], Is.EqualTo(15));
                Assert.That((int)ay["cevapSn"], Is.EqualTo(20));
            }
        }

        [Test]
        public async Task Baslat_3_oyuncuyla_OYUNCU_SAYISI_GECERSIZ()
        {
            var o = await OdaKur(3);
            await o[0].Gonder(new { t = "baslat" });
            await o[0].Hata("OYUNCU_SAYISI_GECERSIZ");
        }

        [Test]
        public async Task Lobide_komut_OYUN_YOK()
        {
            var o = await OdaKur(4);
            await o[0].Gonder(new { t = "komut", tur = 1, komut = "soruSor", hedef = "p2", metin = "Merhaba" });
            await o[0].Hata("OYUN_YOK");
        }

        // ------------------------------------------------------------ Lobide kopma

        [Test]
        public async Task Lobide_kurucu_kopunca_kuruculuk_siradakine_gecer()
        {
            var o = await OdaKur(3);
            await o[0].Kapat();

            foreach (var x in o.Skip(1))
            {
                var d = await x.Bekle("durum", m => (string)m["kurucu"] == "p2");
                Assert.That(Oyuncu(d, "p1"), Is.Null, "lobide kopan odadan çıkar");
                Assert.That(d["oyuncular"].AsArray(), Has.Count.EqualTo(2));
            }

            // Yeni kurucu artık kurucu işi yapabilir.
            await o[1].Gonder(new { t = "ayarlar", soruSn = 40 });
            await o[2].Bekle("durum", m => (int)m["ayarlar"]["soruSn"] == 40);
        }
    }
}
