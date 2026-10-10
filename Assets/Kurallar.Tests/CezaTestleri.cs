using System.Collections.Generic;
using NUnit.Framework;
using static Cakmak.Kurallar.Tests.Kurulum;
using static Cakmak.Kurallar.Tests.TekrarYardim;

namespace Cakmak.Kurallar.Tests
{
    public class CezaTestleri
    {
        static Oyun CezaliOyun(CezaSeviyesi seviye, int tohum = 1, OyunModu mod = OyunModu.Normal) =>
            Yeni(tohum: tohum, ayarla: x =>
            {
                x.CezaSeviyesi = seviye;
                x.SesliCezalar = Sesli;
                x.OneriSorulari = Havuz;
                x.Mod = mod;
                x.TurSayisi = 0;
            });

        [Test]
        public void Temiz_modda_Cesur_kurulamaz()
        {
            var ayar = new Ayarlar { Mod = OyunModu.Temiz, CezaSeviyesi = CezaSeviyesi.Cesur };
            Assert.That(Oyun.Kur(ayar, Oyuncular(4), 1, out _), Is.EqualTo(Sonuc.GecersizGirdi));
            ayar.CezaSeviyesi = CezaSeviyesi.Hafif;
            Assert.That(Oyun.Kur(ayar, Oyuncular(4), 1, out _), Is.EqualTo(Sonuc.Tamam));
        }

        [Test]
        public void Kapali_iken_ceza_ve_bedel_yok()
        {
            var oyun = CezaliOyun(CezaSeviyesi.Kapali);
            SorVeSec(oyun, "Soru?", out _, out var b, out var c);
            TkmOyna(oyun, b, c, cKazansin: true);
            var g = Durum(oyun);
            Assert.That(g.Asama, Is.EqualTo(Asama.Ifsa), "bedel aşaması olmamalı");
            Assert.That(g.TurCezalari, Is.Empty);
            Assert.That(g.Isi, Is.Null, "Kızgın Çakmak sadece Cesur'da");
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Hafif_kaybeden_bir_ayarli_kart_ceker(bool cKazansin)
        {
            var oyun = CezaliOyun(CezaSeviyesi.Hafif);
            SorVeSec(oyun, "Soru?", out _, out var b, out var c);
            TkmOyna(oyun, b, c, cKazansin);
            KarariGec(oyun); // C kazanırsa B'ye bedel fırsatı

            var g = Durum(oyun);
            Assert.That(g.TurCezalari, Has.Count.EqualTo(1));
            var cekim = g.TurCezalari[0];
            Assert.That(cekim.Kim, Is.EqualTo(cKazansin ? b : c), "kaybeden çeker");
            Assert.That(cekim.Ceza.Tur, Is.Not.EqualTo(CezaTuru.Sesli), "Hafif'te sesli ceza yok");
            Assert.That(Oyuncu(oyun, cekim.Kim).Kisitlamalar, Has.Count.EqualTo(1));
        }

        [Test]
        public void Cesur_hem_sesli_hem_ayarli_kart_cikarir()
        {
            var turler = new HashSet<CezaTuru>();
            for (int tohum = 1; tohum <= 60; tohum++)
            {
                var oyun = CezaliOyun(CezaSeviyesi.Cesur, tohum);
                SorVeSec(oyun, "Soru?", out _, out var b, out var c);
                TkmOyna(oyun, b, c, cKazansin: false);
                foreach (var cek in Durum(oyun).TurCezalari) turler.Add(cek.Ceza.Tur);
            }
            Assert.That(turler, Does.Contain(CezaTuru.Sesli));
            Assert.That(turler.Count, Is.GreaterThan(2));
        }

        // ---------------------------------------------------- Bedel

        static Oyun BedelAsamasina(out OyuncuId b, out OyuncuId c, string soru = "Bedel sorusu?")
        {
            var oyun = CezaliOyun(CezaSeviyesi.Hafif);
            SorVeSec(oyun, soru, out _, out b, out c);
            TkmOyna(oyun, b, c, cKazansin: true);
            Assert.That(Durum(oyun).Asama, Is.EqualTo(Asama.Bedel));
            return oyun;
        }

        [Test]
        public void C_kazaninca_B_ye_bedel_firsati_soru_henuz_kimsede_degil()
        {
            const string gizli = "Bedel öncesi gizli?";
            var oyun = BedelAsamasina(out var b, out _, gizli);
            Assert.That(Durum(oyun).KararVeren, Is.EqualTo(b));
            Assert.That(Durum(oyun).KalanMs, Is.EqualTo(5000));
            foreach (var o in Durum(oyun).Oyuncular)
            {
                var g = oyun.GorunumAl(o.Id);
                Assert.That(g.IfsaSoru, Is.Null, "bedel kararı verilmeden ifşa yok");
                if (o.Id != b) Assert.That(g.Soru, Is.Null);
            }
            Assert.That(oyun.BedelOde(Durum(oyun).Oyuncular[0].Id == b ? Durum(oyun).Oyuncular[1].Id : Durum(oyun).Oyuncular[0].Id),
                Is.EqualTo(Sonuc.SiraSendeDegil));
        }

        [Test]
        public void Bedel_odenirse_soru_gume_gider_B_iki_kart_ceker()
        {
            const string gizli = "Bedelle kurtulan soru?";
            var oyun = BedelAsamasina(out var b, out _, gizli);
            var olaylar = new List<Olay>();
            oyun.OlayOldu += olaylar.Add;

            Assert.That(oyun.BedelOde(b), Is.EqualTo(Sonuc.Tamam));
            var g = Durum(oyun);
            Assert.That(g.Asama, Is.EqualTo(Asama.Gume));
            Assert.That(g.BedelOdendi, Is.True);
            Assert.That(g.TurCezalari, Has.Count.EqualTo(2));
            Assert.That(g.TurCezalari, Has.All.Matches<CezaCekimi>(x => x.Kim == b && x.Neden == CekimNedeni.Bedel));
            Assert.That(Oyuncu(oyun, b).BedelHakki, Is.EqualTo(1));
            Assert.That(olaylar.Exists(o => o.Tur == OlayTuru.BedelOdendi && o.Kim == b), Is.True);

            foreach (var o in g.Oyuncular)
            {
                var og = oyun.GorunumAl(o.Id);
                Assert.That(og.Soru, Is.Not.EqualTo(gizli));
                Assert.That(og.IfsaSoru, Is.Not.EqualTo(gizli));
            }
            foreach (var olay in olaylar) Assert.That(olay.Soru, Is.Not.EqualTo(gizli));
            Assert.That(oyun.Ozet().IfsaOlanlar, Is.Empty, "bedelle kurtulan soru özette yok");
        }

        [Test]
        public void Bedel_reddedilirse_ya_da_sure_dolarsa_ifsa()
        {
            var oyun = BedelAsamasina(out var b, out _);
            Assert.That(oyun.KarariGec(b), Is.EqualTo(Sonuc.Tamam));
            Assert.That(Durum(oyun).Asama, Is.EqualTo(Asama.Ifsa));

            var oyun2 = BedelAsamasina(out _, out _);
            Saniye(oyun2, 5.1);
            Assert.That(Durum(oyun2).Asama, Is.EqualTo(Asama.Ifsa));
        }

        [Test]
        public void Bedel_hakki_bitince_firsat_yok()
        {
            var oyun = CezaliOyun(CezaSeviyesi.Hafif);
            // Aynı B iki kez bedel ödeyene kadar oyna, üçüncü kez fırsat çıkmamalı.
            var harcanan = new Dictionary<OyuncuId, int>();
            for (int i = 0; i < 40; i++)
            {
                SorVeSec(oyun, "Soru?", out _, out var b, out var c);
                TkmOyna(oyun, b, c, cKazansin: true);
                var g = Durum(oyun);
                harcanan.TryGetValue(b, out var n);
                if (n >= Oyun.BedelHakkiBaslangic)
                {
                    Assert.That(g.Asama, Is.EqualTo(Asama.Ifsa), "hakkı biten B'ye bedel fırsatı çıkmaz");
                    return;
                }
                Assert.That(g.Asama, Is.EqualTo(Asama.Bedel));
                oyun.BedelOde(b);
                harcanan[b] = n + 1;
                Saniye(oyun, 6);
            }
            Assert.Fail("40 turda aynı B üç kez gelmedi");
        }

        // ---------------------------------------------------- Kısıtlamalar

        /// <summary>İstenen kısıtlamayı ilk turda kaybedene düşüren tohumu bulur. Kaybeden sonraki turun A'sı olur.</summary>
        static Oyun KisitlamayaGetir(CezaTuru tur, out OyuncuId ceza)
        {
            for (int tohum = 1; tohum <= 500; tohum++)
            {
                var oyun = CezaliOyun(CezaSeviyesi.Hafif, tohum);
                SorVeSec(oyun, "Soru?", out _, out _, out var c);
                // B kazanır → C kaybeder, ceza çeker, sıradaki A da C olur.
                TkmOyna(oyun, Durum(oyun).B.Value, c, cKazansin: false);
                if (Durum(oyun).TurCezalari[0].Ceza.Tur != tur) continue;
                ceza = c;
                return oyun;
            }
            throw new AssertionException(tur + " cezası çıkmadı");
        }

        [Test]
        public void Palyaco_sonraki_iki_tur_surer()
        {
            var oyun = KisitlamayaGetir(CezaTuru.Palyaco, out var kim);
            for (int i = 1; i <= 2; i++)
            {
                Saniye(oyun, 6);
                Assert.That(KisitlamaVar(oyun, kim, CezaTuru.Palyaco), Is.True, i + ". turda 🤡 var");
                TekTurBitir(oyun);
            }
            Saniye(oyun, 6);
            Assert.That(KisitlamaVar(oyun, kim, CezaTuru.Palyaco), Is.False, "3. turda kalkmış");
        }

        static void TekTurBitir(Oyun oyun)
        {
            SorSec(oyun, "Soru?", out _, out var b, out var c);
            TkmOyna(oyun, b, c, cKazansin: false);
            KarariGec(oyun);
        }

        [Test]
        public void HavuzdanSor_cezasi_sonraki_soruyu_havuzdan_atar_yazilani_yok_sayar()
        {
            var oyun = KisitlamayaGetir(CezaTuru.HavuzdanSor, out var kim);
            Saniye(oyun, 6);
            Assert.That(GercekA(oyun), Is.EqualTo(kim), "kaybeden C sıradaki A");
            var atanan = oyun.GorunumAl(kim).AtananSoru;
            Assert.That(atanan, Does.StartWith("Havuz sorusu"));
            Assert.That(oyun.OneriCek(kim), Is.EqualTo(Sonuc.YanlisAsama), "zorunlu soru varken öneri yok");

            var b = oyun.GorunumAl(kim).Secilebilir[0];
            oyun.SoruSor(kim, b, "Kendi yazdığım");
            Assert.That(oyun.GorunumAl(b).Soru, Is.EqualTo(atanan));
            Assert.That(KisitlamaVar(oyun, kim, CezaTuru.HavuzdanSor), Is.False, "bir kez işler");
        }

        [Test]
        public void SorunIfsa_cezasi_B_kazansa_da_soruyu_ifsa_eder_bedel_firsati_yok()
        {
            var oyun = KisitlamayaGetir(CezaTuru.SorunIfsa, out var kim);
            Saniye(oyun, 6);
            var b = oyun.GorunumAl(kim).Secilebilir[0];
            oyun.SoruSor(kim, b, "Zorla açılan soru?");
            var c = oyun.GorunumAl(b).Secilebilir[0];
            oyun.CevapSec(b, c);
            TkmOyna(oyun, b, c, cKazansin: false); // B kazanıyor
            Assert.That(Durum(oyun).Asama, Is.EqualTo(Asama.Ifsa));
            Assert.That(Durum(oyun).IfsaSoru, Is.EqualTo("Zorla açılan soru?"));
        }

        [Test]
        public void BeraberlikRakibe_cezasi_beraberligi_rakibe_verir()
        {
            var oyun = KisitlamayaGetir(CezaTuru.BeraberlikRakibe, out var kim);
            Saniye(oyun, 6);
            // Cezalı kişi A oldu; B olarak mini oyuna girmesi için onu soru alan yapacak bir tur bul.
            for (int i = 0; i < 30; i++)
            {
                var a = GercekA(oyun);
                var secenek = oyun.GorunumAl(a).Secilebilir;
                if (!new List<OyuncuId>(secenek).Contains(kim)) { TekTurBitir(oyun); Saniye(oyun, 6); continue; }
                oyun.SoruSor(a, kim, "Soru?");
                var c = oyun.GorunumAl(kim).Secilebilir[0];
                oyun.CevapSec(kim, c);
                oyun.MiniOyunHamlesi(kim, Hamle.TasKagitMakas(TkmSecim.Tas));
                oyun.MiniOyunHamlesi(c, Hamle.TasKagitMakas(TkmSecim.Tas));
                var g = Durum(oyun);
                Assert.That(g.SonMiniOyun.CKazandi, Is.True, "B'nin beraberliği C'ye gitti");
                Assert.That(KisitlamaVar(oyun, kim, CezaTuru.BeraberlikRakibe), Is.False);
                return;
            }
            Assert.Fail("cezalı kişi B olamadı");
        }

        // ---------------------------------------------------- Kızgın Çakmak

        [Test]
        public void Kizgin_cakmak_esik_8_ile_15_devir_arasinda_yakar()
        {
            for (int tohum = 1; tohum <= 40; tohum++)
            {
                var oyun = CezaliOyun(CezaSeviyesi.Cesur, tohum);
                var olaylar = new List<Olay>();
                oyun.OlayOldu += olaylar.Add;
                int devir = 0;
                while (!olaylar.Exists(o => o.Tur == OlayTuru.Yandi))
                {
                    Assert.That(Durum(oyun).Isi, Is.Not.Null);
                    TekTurBitir(oyun);
                    devir++;
                    Saniye(oyun, 6);
                    Assert.That(devir, Is.LessThanOrEqualTo(Oyun.EsikEnFazla));
                }
                Assert.That(devir, Is.GreaterThanOrEqualTo(Oyun.EsikEnAz));
                var yandi = olaylar.Find(o => o.Tur == OlayTuru.Yandi);
                Assert.That(Oyuncu(oyun, yandi.Kim.Value).Yanma, Is.EqualTo(1));
                Assert.That(olaylar.FindAll(o => o.Tur == OlayTuru.CezaCekildi && o.Kim == yandi.Kim && o.Ceza != null).Count,
                    Is.GreaterThanOrEqualTo(2), "büyük ceza: iki kart");
            }
        }

        [Test]
        public void Isi_soguk_baslar_yanmadan_once_hep_kizgin_yandiktan_sonra_sogur()
        {
            for (int tohum = 1; tohum <= 40; tohum++)
            {
                var oyun = CezaliOyun(CezaSeviyesi.Cesur, tohum);
                var olaylar = new List<Olay>();
                oyun.OlayOldu += olaylar.Add;
                Assert.That(Durum(oyun).Isi, Is.EqualTo(IsiSeviyesi.Soguk));
                var onceki = IsiSeviyesi.Soguk;
                for (int i = 0; i < Oyun.EsikEnFazla; i++)
                {
                    int sayi = olaylar.Count;
                    TekTurBitir(oyun);
                    if (olaylar.FindIndex(sayi, o => o.Tur == OlayTuru.Yandi) >= 0)
                    {
                        Assert.That(onceki, Is.EqualTo(IsiSeviyesi.Kizgin), "yanmadan önceki devirde kızgın olmalı");
                        Assert.That(Durum(oyun).Isi, Is.EqualTo(IsiSeviyesi.Soguk), "yandıktan sonra soğur");
                        break;
                    }
                    onceki = Durum(oyun).Isi.Value;
                    Saniye(oyun, 6);
                }
            }
        }
    }
}
