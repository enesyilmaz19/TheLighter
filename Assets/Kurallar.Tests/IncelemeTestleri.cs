using System;
using System.Collections.Generic;
using NUnit.Framework;
using static Cakmak.Kurallar.Tests.Kurulum;
using static Cakmak.Kurallar.Tests.TekrarYardim;

namespace Cakmak.Kurallar.Tests
{
    /// <summary>S3 bağımsız incelemesinin bulguları (2026-10-10). Her biri önce kırmızıydı.</summary>
    public class IncelemeTestleri
    {
        // ---------------------------------------------------- 1. Kızgın Çakmak eşiği türetilemez

        [Test]
        public void Kizgin_cakmak_esigi_isi_degisiminden_turetilemez()
        {
            // Isı "kızgın"a döndükten kaç devir sonra yandığı tek bir sayı olmamalı.
            var kizgindanSonra = new HashSet<int>();
            for (int tohum = 1; tohum <= 60; tohum++)
            {
                var oyun = Yeni(tohum: tohum, ayarla: x =>
                {
                    x.CezaSeviyesi = CezaSeviyesi.Cesur;
                    x.OneriSorulari = Havuz;
                    x.TurSayisi = 0;
                });
                var olaylar = new List<Olay>();
                oyun.OlayOldu += olaylar.Add;
                int ilkKizgin = -1;
                for (int devir = 1; devir <= Oyun.EsikEnFazla + 1; devir++)
                {
                    int once = olaylar.Count;
                    TekTur(oyun);
                    if (olaylar.FindIndex(once, o => o.Tur == OlayTuru.Yandi) >= 0)
                    {
                        Assert.That(ilkKizgin, Is.GreaterThan(0), "yanmadan önce kızgın görünmeli");
                        kizgindanSonra.Add(devir - ilkKizgin);
                        break;
                    }
                    if (ilkKizgin < 0 && Durum(oyun).Isi == IsiSeviyesi.Kizgin) ilkKizgin = devir;
                    Saniye(oyun, 6);
                }
            }
            Assert.That(kizgindanSonra.Count, Is.GreaterThanOrEqualTo(2), "kızgından sonra yanma hep aynı devirde: eşik türetilebilir");
        }

        static void TekTur(Oyun oyun)
        {
            SorSec(oyun, "Soru?", out _, out var b, out var c);
            TkmOyna(oyun, b, c, cKazansin: false);
            KarariGec(oyun);
        }

        // ---------------------------------------------------- 2. Curcuna'da güme giden soru özette ve grup paketinde yok

        static Oyun CurcunaHazir(out List<string> yazilanlar)
        {
            var oyun = Yeni(kisi: 4, ayarla: x => { x.Mod = OyunModu.Curcuna; x.TurSayisi = 0; });
            yazilanlar = new List<string>();
            for (int i = 1; i <= 4; i++)
                for (int j = 1; j <= 2; j++)
                {
                    var s = $"Curcuna {i}-{j} kim?";
                    yazilanlar.Add(s);
                    oyun.HavuzaSoruEkle(P(i), s);
                }
            return oyun;
        }

        static void GumeHicbirYerdeYok(Oyun oyun, string gume)
        {
            oyun.OyunuBitir();
            var ozet = oyun.Ozet();
            Assert.That(ozet.CurcunaSorulari, Has.No.Member(gume), "güme giden Curcuna sorusu özette");
            Assert.That(GrupKaydi.Isle(null, ozet).GrupPaketi, Has.No.Member(gume), "güme giden soru grup paketinde");
        }

        [Test]
        public void Curcuna_gume_giden_soru_ozette_ve_grup_paketinde_yok()
        {
            var oyun = CurcunaHazir(out _);
            SorSec(oyun, null, out _, out var b, out var c);
            var gume = oyun.GorunumAl(b).Soru;
            TkmOyna(oyun, b, c, cKazansin: false);
            Assert.That(Durum(oyun).Asama, Is.EqualTo(Asama.Gume));
            GumeHicbirYerdeYok(oyun, gume);
        }

        [Test]
        public void Curcuna_B_nin_suresi_dolunca_soru_ozette_yok()
        {
            var oyun = CurcunaHazir(out _);
            var a = GercekA(oyun);
            var b = oyun.GorunumAl(a).Secilebilir[0];
            oyun.SoruSor(a, b, null);
            var gume = oyun.GorunumAl(b).Soru;
            Saniye(oyun, 20.1);
            GumeHicbirYerdeYok(oyun, gume);
        }

        [Test]
        public void Curcuna_oyun_tur_ortasinda_bitince_soru_ozette_yok()
        {
            var oyun = CurcunaHazir(out _);
            SorSec(oyun, null, out _, out var b, out _);
            GumeHicbirYerdeYok(oyun, oyun.GorunumAl(b).Soru);
        }

        // ---------------------------------------------------- 3. Soran Gizli gerçekten gizli

        [Test]
        public void Soran_gizli_sayac_ve_kisitlamalar_tur_ortasinda_A_yi_ele_vermez()
        {
            var oyun = KaosaGetir(KaosKurali.SoranGizli);
            var a = GercekA(oyun);
            OyuncuId baskasi = default;
            foreach (var o in Durum(oyun).Oyuncular) if (o.Id != a) { baskasi = o.Id; break; }

            var once = Imza(oyun.GorunumAl(baskasi));
            var b = oyun.GorunumAl(a).Secilebilir[0];
            oyun.SoruSor(a, b, "Gizli soran?");
            Assert.That(Imza(oyun.GorunumAl(baskasi)), Is.EqualTo(once), "A'nın sayacı tur ortasında değişti");
        }

        /// <summary>A dışındaki sayaçlar (SoruAlma, B'de değişir; o bilinir) hariç, A'yı ele verebilecek her şey.</summary>
        static string Imza(Gorunum g)
        {
            var s = new System.Text.StringBuilder();
            foreach (var o in g.Oyuncular)
            {
                s.Append(o.Id).Append(':').Append(o.SoruSorma).Append('/');
                foreach (var k in o.Kisitlamalar) s.Append(k.Tur).Append(k.Kalan);
                s.Append(';');
            }
            return s.ToString();
        }

        [Test]
        public void Soran_gizli_A_nin_suresi_dolunca_olay_A_yi_soylemez()
        {
            var oyun = KaosaGetir(KaosKurali.SoranGizli);
            var olaylar = new List<Olay>();
            oyun.OlayOldu += olaylar.Add;
            Saniye(oyun, 60.1);
            var sure = olaylar.Find(o => o.Tur == OlayTuru.SureDoldu);
            Assert.That(sure, Is.Not.Null);
            Assert.That(sure.Kim, Is.Null);
        }

        [Test]
        public void Soran_gizli_A_her_zaman_onceki_turun_C_si_degil()
        {
            int ayni = 0, toplam = 0;
            for (int i = 0; i < 12; i++)
            {
                for (int tohum = 1 + i * 250; tohum <= 3000; tohum++)
                {
                    var oyun = Yeni(kisi: 5, tohum: tohum, ayarla: x => { x.KaosTurlari = true; x.OneriSorulari = Havuz; x.TurSayisi = 0; });
                    OyuncuId oncekiC = default;
                    for (int t = 0; t < 4; t++)
                    {
                        SorSec(oyun, "Soru?", out _, out var b, out var c);
                        TkmOyna(oyun, b, c, true);
                        oncekiC = c;
                        Saniye(oyun, 6);
                    }
                    if (Durum(oyun).Kaos != KaosKurali.SoranGizli) continue;
                    toplam++;
                    if (GercekA(oyun) == oncekiC) ayni++;
                    break;
                }
            }
            Assert.That(toplam, Is.GreaterThan(5));
            Assert.That(ayni, Is.LessThan(toplam), "Soran Gizli'de A hep önceki C: kim olduğu belli");
        }

        // ---------------------------------------------------- 4. Özet oyun ortasında sır vermez

        [Test]
        public void Ozet_oyun_bitmeden_gorevleri_ve_ifsalari_vermez()
        {
            var oyun = Yeni(ayarla: x => { x.GizliGorevler = true; x.TurSayisi = 0; });
            SorVeSec(oyun, "İfşa?", out _, out var b, out var c);
            TkmOyna(oyun, b, c, cKazansin: true);
            Assert.That(oyun.Ozet().Gorevler, Is.Empty);
            Assert.That(oyun.Ozet().IfsaOlanlar, Is.Empty);
            oyun.OyunuBitir();
            Assert.That(oyun.Ozet().Gorevler, Has.Count.EqualTo(5));
            Assert.That(oyun.Ozet().IfsaOlanlar, Has.Count.EqualTo(1));
        }

        // ---------------------------------------------------- 5. Ters Dünya + Bedel

        [Test]
        public void Ters_dunya_bedel_odenince_mini_oyunu_kaybeden_yine_ceza_ceker()
        {
            var oyun = KaosaGetir(KaosKurali.TersDunya, x => x.CezaSeviyesi = CezaSeviyesi.Hafif);
            SorSec(oyun, "Ters?", out _, out var b, out var c);
            TkmOyna(oyun, b, c, cKazansin: false); // B kazandı → Ters Dünya: ifşa olacak
            Assert.That(Durum(oyun).Asama, Is.EqualTo(Asama.Bedel));
            oyun.BedelOde(b);
            var cezalar = Durum(oyun).TurCezalari;
            Assert.That(cezalar.Count, Is.EqualTo(3));
            Assert.That(new List<CezaCekimi>(cezalar).FindAll(x => x.Kim == b && x.Neden == CekimNedeni.Bedel), Has.Count.EqualTo(2));
            Assert.That(new List<CezaCekimi>(cezalar).FindAll(x => x.Kim == c && x.Neden == CekimNedeni.Kaybetti), Has.Count.EqualTo(1));
        }

        // ---------------------------------------------------- 6. "Beraberlik rakibin" berabere bitemeyen oyunda da kalkar

        [TestCase(MiniOyunTuru.Zar)]
        [TestCase(MiniOyunTuru.TekCift)]
        public void Beraberlik_rakibin_cezasi_bir_sonraki_mini_oyunda_kalkar(MiniOyunTuru tur)
        {
            for (int tohum = 1; tohum <= 400; tohum++)
            {
                var oyun = Yeni(tohum: tohum, ayarla: x => { x.CezaSeviyesi = CezaSeviyesi.Hafif; x.MiniOyun = tur; x.TurSayisi = 0; });
                SorSec(oyun, "Soru?", out _, out var b, out var c);
                MiniOyunOyna(oyun, tur, b, c);
                KarariGec(oyun);
                var cek = Durum(oyun).TurCezalari;
                if (cek.Count == 0 || cek[0].Ceza.Tur != CezaTuru.BeraberlikRakibe) continue;
                var cezali = cek[0].Kim;
                Saniye(oyun, 6);
                for (int t = 0; t < 30; t++)
                {
                    SorSec(oyun, "Soru?", out _, out var b2, out var c2);
                    MiniOyunOyna(oyun, tur, b2, c2);
                    bool oynadi = b2 == cezali || c2 == cezali;
                    KarariGec(oyun);
                    var kaldi = false;
                    foreach (var x in Durum(oyun).TurCezalari) if (x.Kim == cezali && x.Ceza.Tur == CezaTuru.BeraberlikRakibe) kaldi = true;
                    if (oynadi && !kaldi)
                    {
                        Assert.That(KisitlamaVar(oyun, cezali, CezaTuru.BeraberlikRakibe), Is.False, "oynadığı mini oyundan sonra kalkmalı");
                        return;
                    }
                    Saniye(oyun, 6);
                }
            }
            Assert.Fail("senaryo kurulamadı");
        }

        static void MiniOyunOyna(Oyun oyun, MiniOyunTuru tur, OyuncuId b, OyuncuId c)
        {
            if (tur == MiniOyunTuru.Zar) { oyun.MiniOyunHamlesi(b, Hamle.ZarAt()); oyun.MiniOyunHamlesi(c, Hamle.ZarAt()); }
            else { oyun.MiniOyunHamlesi(b, Hamle.TekCift(2, true)); oyun.MiniOyunHamlesi(c, Hamle.TekCift(1)); }
        }

        // ---------------------------------------------------- 7. İkiye Katla bir turda bir mini oyun sayar

        [Test]
        public void Ikiye_katla_sadece_son_el_sayilir()
        {
            var oyun = KaosaGetir(KaosKurali.IkiyeKatla);
            SorSec(oyun, "Soru?", out _, out var b, out var c);
            var bOnce = Oyuncu(oyun, b);
            var cOnce = Oyuncu(oyun, c);
            TkmOyna(oyun, b, c, cKazansin: false); // 1. el: B
            oyun.IkiyeKatla(c);
            TkmOyna(oyun, b, c, cKazansin: true);  // 2. el: C
            var bSon = Oyuncu(oyun, b);
            var cSon = Oyuncu(oyun, c);
            Assert.That(bSon.OynadigiMiniOyun - bOnce.OynadigiMiniOyun, Is.EqualTo(1));
            Assert.That(cSon.OynadigiMiniOyun - cOnce.OynadigiMiniOyun, Is.EqualTo(1));
            Assert.That(bSon.MiniOyunKazanma - bOnce.MiniOyunKazanma, Is.EqualTo(0), "1. eli kazanan sayılmaz");
            Assert.That(cSon.MiniOyunKazanma - cOnce.MiniOyunKazanma, Is.EqualTo(1));
            Assert.That(bSon.KaosKazanma - bOnce.KaosKazanma, Is.EqualTo(0));
        }

        [Test]
        public void Ikiye_katla_reddedilince_tek_el_sayilir()
        {
            var oyun = KaosaGetir(KaosKurali.IkiyeKatla);
            SorSec(oyun, "Soru?", out _, out var b, out var c);
            var bOnce = Oyuncu(oyun, b);
            TkmOyna(oyun, b, c, cKazansin: false);
            oyun.KarariGec(c);
            Assert.That(Oyuncu(oyun, b).MiniOyunKazanma - bOnce.MiniOyunKazanma, Is.EqualTo(1));
            Assert.That(Oyuncu(oyun, b).OynadigiMiniOyun - bOnce.OynadigiMiniOyun, Is.EqualTo(1));
        }

        // ---------------------------------------------------- 8. Grup Kararı: oy yoksa ceza yok

        [Test]
        public void Grup_karari_kimse_oy_vermezse_kimse_ceza_cekmez()
        {
            var oyun = KaosaGetir(KaosKurali.GrupKarari, x => x.CezaSeviyesi = CezaSeviyesi.Hafif);
            SorSec(oyun, "Soru?", out _, out _, out _);
            Saniye(oyun, 10.1);
            Assert.That(Durum(oyun).Asama, Is.EqualTo(Asama.Gume));
            Assert.That(Durum(oyun).TurCezalari, Is.Empty, "AFK oylayıcılar C'yi cezalandırmamalı");
        }

        // ---------------------------------------------------- 9. Karar ortasında oyun biterse

        [TestCase(KaosKurali.IkiyeKatla)]
        [TestCase(KaosKurali.TersDunya)]
        public void Karar_ortasinda_oyun_bitince_soru_hicbir_yerde_yok(KaosKurali kural)
        {
            const string gizli = "Karar ortası soru?";
            var oyun = KaosaGetir(kural, x => x.CezaSeviyesi = CezaSeviyesi.Hafif);
            var olaylar = new List<Olay>();
            oyun.OlayOldu += olaylar.Add;
            SorSec(oyun, gizli, out _, out var b, out var c);
            TkmOyna(oyun, b, c, cKazansin: false); // İkiye Katla kararı ya da (Ters Dünya'da) Bedel kararı
            Assert.That(Durum(oyun).Asama, Is.EqualTo(Asama.IkiyeKatla).Or.EqualTo(Asama.Bedel));
            oyun.OyunuBitir();
            var g = Durum(oyun);
            Assert.That(g.KararVeren, Is.Null);
            foreach (var o in g.Oyuncular)
            {
                Assert.That(oyun.GorunumAl(o.Id).Soru, Is.Null);
                Assert.That(oyun.GorunumAl(o.Id).IfsaSoru, Is.Null);
            }
            foreach (var olay in olaylar) Assert.That(olay.Soru, Is.Not.EqualTo(gizli));
            foreach (var k in oyun.Ozet().IfsaOlanlar) Assert.That(k.Soru, Is.Not.EqualTo(gizli), "yarıda kalan soru özette");
        }

        // ---------------------------------------------------- Küçük notlar

        [Test]
        public void Ayni_isim_iki_kez_kurulmaz_buyuk_kucuk_harf_farki_gozetmez()
        {
            var liste = Oyuncular(4);
            liste.Add(new Oyuncu(P(9), "oyuncu1"));
            Assert.That(Oyun.Kur(new Ayarlar(), liste, 1, out _), Is.EqualTo(Sonuc.GecersizGirdi));
        }

        [Test]
        public void Ani_olum_berabere_bitemeyen_oyunlarda_cikmaz()
        {
            for (int tohum = 1; tohum <= 200; tohum++)
            {
                var oyun = Yeni(kisi: 5, tohum: tohum, ayarla: x =>
                {
                    x.KaosTurlari = true;
                    x.MiniOyun = MiniOyunTuru.Zar;
                    x.OneriSorulari = Havuz;
                    x.TurSayisi = 0;
                });
                for (int i = 0; i < 4; i++) TurOyna(oyun, true);
                Assert.That(Durum(oyun).Kaos, Is.Not.EqualTo(KaosKurali.AniOlum));
            }
        }

        [Test]
        public void Kaos_gorevi_kaos_turuna_varilamayacak_kisa_oyunda_verilmez()
        {
            for (int tohum = 1; tohum <= 100; tohum++)
            {
                var oyun = Yeni(tohum: tohum, ayarla: x => { x.KaosTurlari = true; x.GizliGorevler = true; x.TurSayisi = 4; });
                foreach (var o in Durum(oyun).Oyuncular)
                    Assert.That(oyun.GorunumAl(o.Id).Gorevin.Tur, Is.Not.EqualTo(GorevTuru.KaosTurundaKazan));
            }
        }

        [Test]
        public void Havuzdan_sor_cezasi_curcunada_ve_bos_havuzda_cikmaz()
        {
            for (int tohum = 1; tohum <= 150; tohum++)
            {
                var oyun = Yeni(tohum: tohum, ayarla: x => { x.CezaSeviyesi = CezaSeviyesi.Hafif; x.TurSayisi = 0; }); // öneri havuzu boş
                SorVeSec(oyun, "Soru?", out _, out var b, out var c);
                TkmOyna(oyun, b, c, cKazansin: false);
                foreach (var x in Durum(oyun).TurCezalari) Assert.That(x.Ceza.Tur, Is.Not.EqualTo(CezaTuru.HavuzdanSor));
            }
        }
    }
}
