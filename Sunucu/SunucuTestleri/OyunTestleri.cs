using System.Diagnostics;
using System.Text.Json.Nodes;
using NUnit.Framework;

namespace SunucuTestleri
{
    /// <summary>Protokol.md: baslat, komut, durum (oyunda), olay, gizlilik, kopma/geri dönme, bitir, süre.</summary>
    public class OyunTestleri : SunucuTesti
    {
        // ------------------------------------------------------------ Başlangıç

        [TestCase(4)]
        [TestCase(5)]
        public async Task Baslat_herkese_soruSorma_tur_1_secilebilir_sadece_A_ya(int n)
        {
            var o = await OyunBaslat(n);
            var aId = (string)o[0].SonDurum["a"];
            var idler = o.Select(x => x.Sen).ToList();
            Assert.That(idler, Does.Contain(aId), "a odadaki bir oyuncu olmalı");

            foreach (var x in o)
            {
                var d = x.SonDurum;
                Assert.That((string)d["asama"], Is.EqualTo("soruSorma"));
                Assert.That((int)d["tur"], Is.EqualTo(1));
                Assert.That((string)d["a"], Is.EqualTo(aId), "herkes aynı A'yı görmeli");
                Assert.That(d["b"], Is.Null);
                Assert.That(d["c"], Is.Null);

                var sec = Idler(d["secilebilir"]);
                if (x.Sen == aId)
                {
                    Assert.That(sec, Is.Not.Empty);
                    Assert.That(sec, Does.Not.Contain(aId), "A kendini seçemez");
                    Assert.That(sec, Is.SubsetOf(idler));
                }
                else
                {
                    Assert.That(sec, Is.Empty, $"{x.Sen} seçim yapmıyor, secilebilir boş olmalı");
                }
            }
        }

        // ------------------------------------------------------------ Gizlilik

        [Test]
        public async Task Gizlilik_soru_metni_sadece_B_nin_durumunda()
        {
            var o = await OyunBaslat(5);
            var metin = GizliMetin("Gizli");

            var (_, b) = await Sor(o, metin);
            Assert.That((string)b.SonDurum["sana"]["soru"], Is.EqualTo(metin));

            foreach (var x in o)
            {
                var olay = await x.Bekle("olay", m => (string)m["olay"] == "soruSoruldu");
                Assert.That(olay.ToJsonString(), Does.Not.Contain(metin), "soruSoruldu olayında metin olmamalı");
            }

            // Cevap seçme ve mini oyun aşamasında da sızmasın (C artık oyunda).
            await Sec(o, b);

            // Pozitif kontrol: ham dizede arama gerçekten çalışıyor (metin kaçışsız geliyor).
            Assert.That(b.Hepsi.Any(m => m.Contains(metin)), Is.True);
            foreach (var x in o.Where(x => x != b))
                Assert.That(x.Hepsi.Where(m => m.Contains(metin)), Is.Empty, $"{x.Sen} metni görmemeli (A dahil)");
            Assert.That(b.Hepsi.Where(m => m.Contains("\"olay\"") && m.Contains(metin)), Is.Empty,
                "ifşadan önce hiçbir olayda metin olmamalı");
        }

        // ------------------------------------------------------------ Tam tur

        [Test]
        public async Task Tam_tur_C_kazanirsa_ifsa_herkes_soruyu_gorur()
        {
            var o = await OyunBaslat(4);
            var metin = GizliMetin("Ifsa");
            var (_, b) = await Sor(o, metin);
            var c = await Sec(o, b);

            await Hamle(b, "makas");
            await Hamle(c, "tas"); // taş makası yener: C kazanır

            foreach (var x in o)
            {
                var d = await x.Bekle("durum", m => (string)m["asama"] == "ifsa");
                Assert.That((string)d["ifsa"]["soru"], Is.EqualTo(metin), $"{x.Sen}");
                Assert.That((bool)d["sonMiniOyun"]["cKazandi"], Is.True);

                var olay = await x.Bekle("olay", m => (string)m["olay"] == "ifsa");
                Assert.That((string)olay["soru"], Is.EqualTo(metin));
            }
        }

        [Test]
        public async Task Tam_tur_B_kazanirsa_gume_soru_kimseye_gitmez()
        {
            var o = await OyunBaslat(4);
            var metin = GizliMetin("Gume");
            var (_, b) = await Sor(o, metin);
            var c = await Sec(o, b);

            await Hamle(b, "tas");
            await Hamle(c, "makas"); // taş makası yener: B kazanır

            foreach (var x in o)
            {
                var d = await x.Bekle("durum", m => (string)m["asama"] == "gume");
                Assert.That(d["ifsa"], Is.Null);
                Assert.That((bool)d["sonMiniOyun"]["cKazandi"], Is.False);
                await x.Bekle("olay", m => (string)m["olay"] == "gume");
            }

            // Güme aşamasından sonra gelen her şeyi biraz topla.
            // Belirsizlik: güme aşamasının süresi protokolde yok, bu yüzden sonraki turu beklemiyoruz.
            await Task.Delay(1000);

            foreach (var x in o)
            {
                var hepsi = x.Hepsi;
                int gumeden = hepsi.FindIndex(m =>
                {
                    var j = JsonNode.Parse(m);
                    return (string)j["asama"] == "gume" || (string)j["olay"] == "gume";
                });
                Assert.That(hepsi.Skip(gumeden).Where(m => m.Contains(metin)), Is.Empty,
                    $"{x.Sen}: güme giden soru hiçbir mesajda olmamalı");
                if (x != b) Assert.That(hepsi.Where(m => m.Contains(metin)), Is.Empty, $"{x.Sen} metni hiç görmemeli");
            }
        }

        // ------------------------------------------------------------ Komut hataları

        [TestCase(0)]
        [TestCase(2)]
        public async Task Eski_tur_ESKI_TUR_ve_durum_degismez(int eskiTur)
        {
            var o = await OyunBaslat(4);
            var a = Kim(o, (string)o[0].SonDurum["a"]);
            var bId = (string)a.SonDurum["secilebilir"][0];
            var eski = GizliMetin("Eski");
            var yeni = GizliMetin("Yeni");

            await a.Gonder(new { t = "komut", tur = eskiTur, komut = "soruSor", hedef = bId, metin = eski });
            await a.Hata("ESKI_TUR");

            // Eski komut uygulandıysa durum cevapSecme'ye geçmiştir ve bu ikinci soru YANLIS_ASAMA alır.
            await a.Gonder(new { t = "komut", tur = 1, komut = "soruSor", hedef = bId, metin = yeni });
            var d = await Kim(o, bId).Bekle("durum", m => (string)m["asama"] == "cevapSecme");
            Assert.That((string)d["sana"]["soru"], Is.EqualTo(yeni));
            foreach (var x in o)
                Assert.That(x.Hepsi.Where(m => m.Contains(eski)), Is.Empty, $"{x.Sen}: eski turlu komut hiç uygulanmamalı");
        }

        [Test]
        public async Task A_olmayan_soru_sorarsa_SIRA_SENDE_DEGIL()
        {
            var o = await OyunBaslat(4);
            var aId = (string)o[0].SonDurum["a"];
            var x = o.First(i => i.Sen != aId);
            var hedef = o.First(i => i.Sen != aId && i != x).Sen;

            await x.Gonder(new { t = "komut", tur = 1, komut = "soruSor", hedef, metin = "Merhaba" });
            await x.Hata("SIRA_SENDE_DEGIL");
        }

        [Test]
        public async Task A_kendini_secerse_SECILEMEZ()
        {
            var o = await OyunBaslat(4);
            var a = Kim(o, (string)o[0].SonDurum["a"]);

            await a.Gonder(new { t = "komut", tur = 1, komut = "soruSor", hedef = a.Sen, metin = "Merhaba" });
            await a.Hata("SECILEMEZ");
        }

        [Test]
        public async Task B_A_yi_C_secerse_SECILEMEZ()
        {
            var o = await OyunBaslat(4);
            var (a, b) = await Sor(o, "Merhaba");

            await b.Gonder(new { t = "komut", tur = 1, komut = "cevapSec", hedef = a.Sen });
            await b.Hata("SECILEMEZ");
        }

        // ------------------------------------------------------------ surum

        [Test]
        public async Task Surum_her_durumda_kesin_artar()
        {
            // Belirsizlik: protokol surum'u sadece örnekte gösteriyor, anlamını yazmıyor.
            // En doğal okuma: bir istemcinin aldığı ardışık durumlarda kesin artar.
            // Durum üreten adımlar: 4 katılış (lobi), baslat, soruSor, cevapSec.
            var o = await OyunBaslat(4);
            var (_, b) = await Sor(o, "Surum sorusu");
            await Sec(o, b);

            foreach (var x in o)
            {
                var surumler = x.Hepsi.Select(m => JsonNode.Parse(m))
                    .Where(m => (string)m["t"] == "durum")
                    .Select(m => (long)m["surum"])
                    .ToList();
                Assert.That(surumler, Has.Count.GreaterThanOrEqualTo(3), $"{x.Sen}");
                for (int i = 1; i < surumler.Count; i++)
                    Assert.That(surumler[i], Is.GreaterThan(surumler[i - 1]), $"{x.Sen}: surum sırası {string.Join(",", surumler)}");
            }
        }

        // ------------------------------------------------------------ Kopma ve geri dönme

        [Test]
        public async Task Oyunda_kopan_bagli_false_olur_anahtarla_ayni_kimlikle_doner()
        {
            var o = await OyunBaslat(4);
            var giden = o[2];
            string id = giden.Sen, oda = giden.Oda, anahtar = giden.Anahtar, isim = giden.Isim;

            await giden.Kapat();
            foreach (var x in o.Where(x => x != giden))
            {
                var d = await x.Bekle("durum", m => (bool)Oyuncu(m, id)["bagli"] == false);
                Assert.That((string)d["asama"], Is.EqualTo("soruSorma"), "oyunda kopan oyunda kalır");
            }

            // Belirsizlik: geri dönüşte isim gerekli mi/kontrol edilir mi yazmıyor; aynı isim gönderiliyor.
            var donen = await Baglan();
            await donen.Gonder(new { t = "katil", oda, isim, anahtar });
            var h = await donen.Bekle("hosgeldin");
            Assert.That((string)h["sen"], Is.EqualTo(id));

            var dd = await donen.Bekle("durum", m => (bool)Oyuncu(m, id)["bagli"]);
            Assert.That((string)dd["asama"], Is.EqualTo("soruSorma"), "geri dönen oyunun içine döner");
            foreach (var x in o.Where(x => x != giden))
                await x.Bekle("durum", m => (bool)Oyuncu(m, id)["bagli"]);
        }

        [TestCase(null)]
        [TestCase("xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx")] // 32 karakter, yanlış
        public async Task Oyun_basladiktan_sonra_anahtarsiz_katil_OYUN_BASLADI(string anahtar)
        {
            var o = await OyunBaslat(4);
            var x = await Baglan();
            await x.Gonder(new { t = "katil", oda = o[0].Oda, isim = "Yabanci", anahtar });
            await x.Hata("OYUN_BASLADI");
        }

        // ------------------------------------------------------------ bitir

        [Test]
        public async Task Kurucu_bitirince_herkes_oyunSonu_gorur()
        {
            var o = await OyunBaslat(4);
            await o[0].Gonder(new { t = "bitir" });
            foreach (var x in o)
                await x.Bekle("durum", m => (string)m["asama"] == "oyunSonu");
        }

        // ------------------------------------------------------------ İnceleme sonrası eklenenler (G1)

        [Test]
        public async Task Oyunda_kurucu_kopunca_kuruculuk_sonraki_bagli_oyuncuya_gecer()
        {
            // Yoksa sınırsız oyunu kimse bitiremez. Ana Plan 6.1: "Kurucu çıktı → en eski oyuncuya geçer, oyun durmaz".
            var o = await OyunBaslat(4);
            await o[0].Kapat();
            foreach (var x in o.Skip(1))
                await x.Bekle("durum", m => (string)m["kurucu"] == o[1].Sen && (string)m["asama"] == "soruSorma");

            await o[1].Gonder(new { t = "bitir" });
            foreach (var x in o.Skip(1))
                await x.Bekle("durum", m => (string)m["asama"] == "oyunSonu");
        }

        [Test]
        public async Task Oneri_cek_soru_paketinden_A_ya_soru_getirir()
        {
            // Sunucu Assets/Icerik paketlerini öneri havuzu olarak yüklüyor mu? Yüklemezse HAVUZ_BOS döner.
            var o = await OyunBaslat(4);
            var a = Kim(o, (string)o[0].SonDurum["a"]);
            await a.Gonder(new { t = "komut", tur = 1, komut = "oneriCek" });
            var d = await a.Bekle("durum", m => m["sana"]["oneri"] != null);
            Assert.That((string)d["sana"]["oneri"], Does.Contain("kim").IgnoreCase.And.EndWith("?"));
            foreach (var x in o.Where(x => x != a))
                Assert.That(x.SonDurum["sana"]["oneri"], Is.Null, "öneri sadece A'ya gider");
        }

        [Test]
        public async Task Baska_cihazdan_donunce_eski_baglanti_kapanir()
        {
            var o = await OyunBaslat(4);
            var eski = o[2];
            var yeni = await Baglan();
            await yeni.Gonder(new { t = "katil", oda = eski.Oda, isim = eski.Isim, anahtar = eski.Anahtar });
            Assert.That((string)(await yeni.Bekle("hosgeldin"))["sen"], Is.EqualTo(eski.Sen));
            Assert.That(await eski.KapanmaBekle(), Is.True, "aynı kimlik iki cihazda açık kalmamalı");
        }

        [Test]
        public async Task Hamle_icinde_ayni_alan_iki_kez_GECERSIZ_MESAJ_ve_baglanti_acik_kalir()
        {
            var o = await OyunBaslat(4);
            var (_, b) = await Sor(o, GizliMetin("Soru"));
            await Sec(o, b);

            await b.Gonder("{\"t\":\"komut\",\"tur\":1,\"komut\":\"hamle\",\"hamle\":{\"oyun\":\"tkm\",\"oyun\":\"tkm\",\"secim\":\"tas\"}}");
            await b.Hata("GECERSIZ_MESAJ");

            await Hamle(b, "tas");
            await b.Bekle("durum", m => (bool)m["bHamleYapti"]);
        }

        [Test]
        public async Task Kurucu_olmayan_bitirirse_KURUCU_DEGILSIN()
        {
            var o = await OyunBaslat(4);
            await o[1].Gonder(new { t = "bitir" });
            await o[1].Hata("KURUCU_DEGILSIN");
        }

        // ------------------------------------------------------------ Süre (yavaş: ~5 sn)

        [Test]
        public async Task Soru_suresi_dolunca_yeni_tur_baska_A_ile_baslar()
        {
            var o = await OdaKur(4);
            await o[0].Gonder(new { t = "ayarlar", soruSn = 5 });
            foreach (var x in o) await x.Bekle("durum", m => (int)m["ayarlar"]["soruSn"] == 5);

            await o[0].Gonder(new { t = "baslat" });
            var ilk = await o[0].Bekle("durum", m => (string)m["asama"] == "soruSorma");
            var saat = Stopwatch.StartNew();
            Assert.That((int)ilk["kalanMs"], Is.InRange(1, 5000));

            // Kimse bir şey yapmıyor. Süre 5 sn + sunucunun sayaç adımı.
            var ikinci = await o[0].Bekle("durum", m => (int)m["tur"] == 2, TimeSpan.FromSeconds(10));
            Assert.That(saat.Elapsed, Is.GreaterThan(TimeSpan.FromSeconds(4)), "süre dolmadan tur geçmemeli");
            Assert.That((string)ikinci["asama"], Is.EqualTo("soruSorma"));
            // Belirsizlik: "yeni A, süresi dolan A'dan farklı" protokolde yok; kural motorunun davranışı (görev isteği).
            Assert.That((string)ikinci["a"], Is.Not.EqualTo((string)ilk["a"]));
            await o[0].Bekle("olay", m => (string)m["olay"] == "sureDoldu");
        }
    }
}
