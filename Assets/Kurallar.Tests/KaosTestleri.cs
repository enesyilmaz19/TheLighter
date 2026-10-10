using System.Collections.Generic;
using NUnit.Framework;
using static Cakmak.Kurallar.Tests.Kurulum;
using static Cakmak.Kurallar.Tests.TekrarYardim;

namespace Cakmak.Kurallar.Tests
{
    public class KaosTestleri
    {
        [Test]
        public void Kaos_kapaliyken_hic_cikmaz_acikken_sadece_her_5_turda()
        {
            var kapali = Yeni(ayarla: x => x.TurSayisi = 0);
            var acik = Yeni(ayarla: x => { x.TurSayisi = 0; x.KaosTurlari = true; x.OneriSorulari = Havuz; });
            var olaylar = new List<Olay>();
            acik.OlayOldu += olaylar.Add;
            for (int tur = 1; tur <= 15; tur++)
            {
                Assert.That(Durum(kapali).Kaos, Is.EqualTo(KaosKurali.Yok));
                var kaos = Durum(acik).Kaos;
                if (tur % 5 == 0) Assert.That(kaos, Is.Not.EqualTo(KaosKurali.Yok), "tur " + tur);
                else Assert.That(kaos, Is.EqualTo(KaosKurali.Yok), "tur " + tur);
                TurOyna(kapali, true);
                TurOyna(acik, true);
            }
            Assert.That(olaylar.FindAll(o => o.Tur == OlayTuru.KaosBasladi), Has.Count.EqualTo(3));
        }

        [TestCase(true, Asama.Gume)]
        [TestCase(false, Asama.Ifsa)]
        public void Ters_dunya_B_kazanirsa_ifsa(bool cKazansin, Asama beklenen)
        {
            var oyun = KaosaGetir(KaosKurali.TersDunya);
            SorSec(oyun, "Ters soru?", out _, out var b, out var c);
            TkmOyna(oyun, b, c, cKazansin);
            Assert.That(Durum(oyun).Asama, Is.EqualTo(beklenen));
        }

        [Test]
        public void Grup_karari_A_ve_B_disindakiler_oylar_cogunluk_ifsa_ederse_ifsa()
        {
            var oyun = KaosaGetir(KaosKurali.GrupKarari);
            SorSec(oyun, "Oylanan soru?", out var a, out var b, out var c);
            var g = Durum(oyun);
            Assert.That(g.Asama, Is.EqualTo(Asama.Oylama));
            var oylayanlar = new List<OyuncuId>(g.Oylayanlar);
            Assert.That(oylayanlar, Has.No.Member(a).And.No.Member(b).And.Member(c));
            Assert.That(oylayanlar, Has.Count.EqualTo(3));

            Assert.That(oyun.Oy(a, true), Is.EqualTo(Sonuc.SiraSendeDegil));
            Assert.That(oyun.Oy(oylayanlar[0], true), Is.EqualTo(Sonuc.Tamam));
            Assert.That(oyun.Oy(oylayanlar[0], false), Is.EqualTo(Sonuc.HakKalmadi));
            Assert.That(oyun.GorunumAl(oylayanlar[0]).OyVerdin, Is.True);
            Assert.That(oyun.MiniOyunHamlesi(b, Hamle.TasKagitMakas(TkmSecim.Tas)), Is.EqualTo(Sonuc.YanlisAsama), "mini oyun yok");
            oyun.Oy(oylayanlar[1], true);
            oyun.Oy(oylayanlar[2], false);

            var son = Durum(oyun);
            Assert.That(son.Asama, Is.EqualTo(Asama.Ifsa));
            Assert.That(son.SonOylama.Ifsa, Is.EqualTo(2));
            Assert.That(son.SonOylama.Gume, Is.EqualTo(1));
            Assert.That(son.SonMiniOyun, Is.Null);
        }

        [Test]
        public void Grup_karari_esitlik_ya_da_hic_oy_yoksa_gume()
        {
            var oyun = KaosaGetir(KaosKurali.GrupKarari);
            SorSec(oyun, "Soru?", out _, out _, out _);
            Saniye(oyun, 10.1);
            Assert.That(Durum(oyun).Asama, Is.EqualTo(Asama.Gume));
            Assert.That(Durum(oyun).SonOylama.Ifsa, Is.EqualTo(0));
        }

        [Test]
        public void Kor_soru_A_soruyu_gormez_B_havuzdan_gelen_soruyu_gorur()
        {
            var oyun = KaosaGetir(KaosKurali.KorSoru);
            var a = GercekA(oyun);
            Assert.That(oyun.GorunumAl(a).AtananSoru, Is.Null, "A kendi sorusunu görmez");
            var b = oyun.GorunumAl(a).Secilebilir[0];
            oyun.SoruSor(a, b, "A'nın yazdığı yok sayılır");
            Assert.That(oyun.GorunumAl(b).Soru, Does.StartWith("Havuz sorusu"));
            Assert.That(oyun.GorunumAl(a).Soru, Is.Null);
        }

        [Test]
        public void Soran_gizli_A_sadece_kendi_gorunumunde_B_A_yi_de_secebilir()
        {
            var oyun = KaosaGetir(KaosKurali.SoranGizli);
            var olaylar = new List<Olay>();
            oyun.OlayOldu += olaylar.Add;
            var a = GercekA(oyun);
            foreach (var o in Durum(oyun).Oyuncular)
                if (o.Id != a) Assert.That(oyun.GorunumAl(o.Id).A, Is.Null);

            var b = oyun.GorunumAl(a).Secilebilir[0];
            oyun.SoruSor(a, b, "Kim sordu bunu?");
            Assert.That(new List<OyuncuId>(oyun.GorunumAl(b).Secilebilir), Has.Member(a), "A çıkarılırsa kim olduğu anlaşılır");
            Assert.That(oyun.CevapSec(b, a), Is.EqualTo(Sonuc.Tamam));
            foreach (var olay in olaylar) Assert.That(olay.A, Is.Null, olay.Tur.ToString());
        }

        [Test]
        public void Soran_gizli_tek_cihazda_hic_cikmaz()
        {
            for (int tohum = 1; tohum <= 300; tohum++)
            {
                var oyun = Yeni(kisi: 5, tohum: tohum, ayarla: x =>
                {
                    x.KaosTurlari = true;
                    x.TekCihaz = true;
                    x.OneriSorulari = Havuz;
                    x.TurSayisi = 0;
                });
                for (int i = 0; i < 4; i++) TurOyna(oyun, true);
                Assert.That(Durum(oyun).Kaos, Is.Not.EqualTo(KaosKurali.SoranGizli));
            }
        }

        [Test]
        public void Ani_olum_beraberlikte_B_kaybeder()
        {
            var oyun = KaosaGetir(KaosKurali.AniOlum);
            SorSec(oyun, "Soru?", out _, out var b, out var c);
            oyun.MiniOyunHamlesi(b, Hamle.TasKagitMakas(TkmSecim.Tas));
            oyun.MiniOyunHamlesi(c, Hamle.TasKagitMakas(TkmSecim.Tas));
            Assert.That(Durum(oyun).Asama, Is.EqualTo(Asama.Ifsa));
            Assert.That(Durum(oyun).SonMiniOyun.CKazandi, Is.True);
        }

        [Test]
        public void Ceza_turu_seviye_kapaliyken_de_kaybedene_hafif_ceza()
        {
            var oyun = KaosaGetir(KaosKurali.CezaTuru);
            SorSec(oyun, "Soru?", out _, out var b, out var c);
            TkmOyna(oyun, b, c, cKazansin: false);
            var g = Durum(oyun);
            Assert.That(g.TurCezalari, Has.Count.EqualTo(1));
            Assert.That(g.TurCezalari[0].Kim, Is.EqualTo(c));
            Assert.That(g.TurCezalari[0].Ceza.Tur, Is.Not.EqualTo(CezaTuru.Sesli));
        }

        [Test]
        public void Yon_degisti_sira_C_ye_gecmez()
        {
            for (int deneme = 0; deneme < 5; deneme++)
            {
                var oyun = KaosaGetir(KaosKurali.YonDegisti, x => x.MiniOyun = MiniOyunTuru.Tkm);
                SorSec(oyun, "Soru?", out _, out var b, out var c);
                TkmOyna(oyun, b, c, cKazansin: true);
                Saniye(oyun, 6);
                Assert.That(GercekA(oyun), Is.Not.EqualTo(c));
            }
        }

        [Test]
        public void Ikiye_katla_kaybeden_bir_el_daha_ister_yine_kaybederse_iki_ceza()
        {
            var oyun = KaosaGetir(KaosKurali.IkiyeKatla, x => x.CezaSeviyesi = CezaSeviyesi.Hafif);
            SorSec(oyun, "Katlanan soru?", out _, out var b, out var c);
            TkmOyna(oyun, b, c, cKazansin: false); // C kaybetti
            var g = Durum(oyun);
            Assert.That(g.Asama, Is.EqualTo(Asama.IkiyeKatla));
            Assert.That(g.KararVeren, Is.EqualTo(c));
            Assert.That(g.IfsaSoru, Is.Null);
            Assert.That(oyun.IkiyeKatla(b), Is.EqualTo(Sonuc.SiraSendeDegil), "sadece kaybeden ister");

            Assert.That(oyun.IkiyeKatla(c), Is.EqualTo(Sonuc.Tamam));
            Assert.That(Durum(oyun).Asama, Is.EqualTo(Asama.MiniOyun));
            TkmOyna(oyun, b, c, cKazansin: false); // yine kaybetti
            var son = Durum(oyun);
            Assert.That(son.Asama, Is.EqualTo(Asama.Gume), "ikinci elde karar sorulmaz");
            Assert.That(son.SonMiniOyun.Katlandi, Is.True);
            Assert.That(son.TurCezalari.Count, Is.EqualTo(2));
            Assert.That(son.TurCezalari, Has.All.Matches<CezaCekimi>(x => x.Kim == c));
        }

        [Test]
        public void Ikiye_katlayan_ikinci_eli_kazanirsa_sonuc_doner()
        {
            var oyun = KaosaGetir(KaosKurali.IkiyeKatla);
            SorSec(oyun, "Soru?", out _, out var b, out var c);
            TkmOyna(oyun, b, c, cKazansin: false); // C kaybetti
            oyun.IkiyeKatla(c);
            TkmOyna(oyun, b, c, cKazansin: true);  // C kazandı
            Assert.That(Durum(oyun).Asama, Is.EqualTo(Asama.Ifsa));
        }

        [Test]
        public void Ikiye_katla_reddedilirse_ya_da_sure_dolarsa_ilk_sonuc_gecer()
        {
            var oyun = KaosaGetir(KaosKurali.IkiyeKatla);
            SorSec(oyun, "Soru?", out _, out var b, out var c);
            TkmOyna(oyun, b, c, cKazansin: false);
            Saniye(oyun, 5.1);
            Assert.That(Durum(oyun).Asama, Is.EqualTo(Asama.Gume));
        }
    }
}
