using System.Collections.Generic;
using NUnit.Framework;
using static Cakmak.Kurallar.Tests.Kurulum;

namespace Cakmak.Kurallar.Tests
{
    public class TurAkisiTestleri
    {
        // ---------------------------------------------------- Seçim kuralları

        [Test]
        public void A_kendini_secemez_ve_listede_kendisi_yok()
        {
            var oyun = Yeni();
            var a = SiradakiA(oyun);
            Assert.That(oyun.GorunumAl(a).Secilebilir, Has.No.Member(a));
            Assert.That(oyun.GorunumAl(a).Secilebilir, Has.Count.EqualTo(4));
            Assert.That(oyun.SoruSor(a, a, "Soru?"), Is.EqualTo(Sonuc.Secilemez));
        }

        [Test]
        public void B_ne_kendini_ne_A_yi_secebilir()
        {
            var oyun = Yeni();
            var a = SiradakiA(oyun);
            var b = oyun.GorunumAl(a).Secilebilir[0];
            oyun.SoruSor(a, b, "Soru?");

            var liste = oyun.GorunumAl(b).Secilebilir;
            Assert.That(liste, Has.No.Member(a));
            Assert.That(liste, Has.No.Member(b));
            Assert.That(liste, Has.Count.EqualTo(3));
            Assert.That(oyun.CevapSec(b, a), Is.EqualTo(Sonuc.Secilemez));
            Assert.That(oyun.CevapSec(b, b), Is.EqualTo(Sonuc.Secilemez));
        }

        [Test]
        public void Dort_kiside_B_nin_iki_secenegi_var()
        {
            var oyun = Yeni(kisi: 4);
            var a = SiradakiA(oyun);
            var b = oyun.GorunumAl(a).Secilebilir[0];
            oyun.SoruSor(a, b, "Soru?");
            Assert.That(oyun.GorunumAl(b).Secilebilir, Has.Count.EqualTo(2));
        }

        [Test]
        public void Secim_listesi_sadece_sececek_kisiye_gelir()
        {
            var oyun = Yeni();
            var a = SiradakiA(oyun);
            foreach (var o in Durum(oyun).Oyuncular)
                if (o.Id != a) Assert.That(oyun.GorunumAl(o.Id).Secilebilir, Is.Empty);
        }

        [Test]
        public void Sirasi_olmayan_komut_veremez()
        {
            var oyun = Yeni();
            var a = SiradakiA(oyun);
            var baskasi = oyun.GorunumAl(a).Secilebilir[0];
            var hedef = oyun.GorunumAl(a).Secilebilir[1];
            Assert.That(oyun.SoruSor(baskasi, hedef, "Soru?"), Is.EqualTo(Sonuc.SiraSendeDegil));
        }

        [Test]
        public void Yanlis_asamada_komut_reddedilir()
        {
            var oyun = Yeni();
            var a = SiradakiA(oyun);
            var b = oyun.GorunumAl(a).Secilebilir[0];
            Assert.That(oyun.CevapSec(b, a), Is.EqualTo(Sonuc.YanlisAsama));
            Assert.That(oyun.MiniOyunHamlesi(b, Hamle.TasKagitMakas(TkmSecim.Tas)), Is.EqualTo(Sonuc.YanlisAsama));
        }

        // ---------------------------------------------------- Soru metni

        [Test]
        public void Bos_soru_reddedilir()
        {
            var oyun = Yeni();
            var a = SiradakiA(oyun);
            var b = oyun.GorunumAl(a).Secilebilir[0];
            Assert.That(oyun.SoruSor(a, b, "   "), Is.EqualTo(Sonuc.GecersizGirdi));
        }

        [Test]
        public void Soru_140_karakteri_gecemez()
        {
            var oyun = Yeni();
            var a = SiradakiA(oyun);
            var b = oyun.GorunumAl(a).Secilebilir[0];
            Assert.That(oyun.SoruSor(a, b, new string('x', 141)), Is.EqualTo(Sonuc.GecersizGirdi));
            Assert.That(oyun.SoruSor(a, b, new string('x', 140)), Is.EqualTo(Sonuc.Tamam));
        }

        [Test]
        public void Null_metin_fisilti_sayilir()
        {
            var oyun = Yeni();
            var a = SiradakiA(oyun);
            var b = oyun.GorunumAl(a).Secilebilir[0];
            Assert.That(oyun.SoruSor(a, b, null), Is.EqualTo(Sonuc.Tamam));
            var gB = oyun.GorunumAl(b);
            Assert.That(gB.Fisilti, Is.True);
            Assert.That(gB.Soru, Is.Null);
        }

        // ---------------------------------------------------- Gizlilik

        [Test]
        public void Soru_metni_sadece_B_nin_gorunumunde()
        {
            const string gizli = "Grupta en güvenilmez kim?";
            var oyun = Yeni();
            var a = SiradakiA(oyun);
            var b = oyun.GorunumAl(a).Secilebilir[0];
            oyun.SoruSor(a, b, gizli);

            // CevapSecme aşaması
            HerkesiKontrolEt(oyun, b, gizli);

            // MiniOyun aşaması
            var c = oyun.GorunumAl(b).Secilebilir[0];
            oyun.CevapSec(b, c);
            HerkesiKontrolEt(oyun, b, gizli);
        }

        static void HerkesiKontrolEt(Oyun oyun, OyuncuId b, string gizli)
        {
            foreach (var o in Durum(oyun).Oyuncular)
            {
                var g = oyun.GorunumAl(o.Id);
                if (o.Id == b) Assert.That(g.Soru, Is.EqualTo(gizli), "B soruyu görmeli");
                else
                {
                    Assert.That(g.Soru, Is.Null, $"{o.Id} soruyu görmemeli");
                    Assert.That(g.IfsaSoru, Is.Null, $"{o.Id} soruyu görmemeli");
                }
            }
        }

        [Test]
        public void Soru_olayinda_metin_yok()
        {
            var oyun = Yeni();
            var olaylar = new List<Olay>();
            oyun.OlayOldu += olaylar.Add;
            SorVeSec(oyun, "Gizli soru?", out _, out _, out _);
            foreach (var olay in olaylar) Assert.That(olay.Soru, Is.Null, olay.Tur.ToString());
        }

        [Test]
        public void C_kazanirsa_soru_herkese_ifsa_olur()
        {
            const string soru = "Kiminle tatile gitmezsin?";
            var oyun = Yeni();
            var olaylar = new List<Olay>();
            oyun.OlayOldu += olaylar.Add;
            SorVeSec(oyun, soru, out var a, out var b, out var c);
            TkmOyna(oyun, b, c, cKazansin: true);

            foreach (var o in Durum(oyun).Oyuncular)
            {
                var g = oyun.GorunumAl(o.Id);
                Assert.That(g.Asama, Is.EqualTo(Asama.Ifsa));
                Assert.That(g.IfsaSoru, Is.EqualTo(soru));
            }
            var ifsa = olaylar.Find(x => x.Tur == OlayTuru.Ifsa);
            Assert.That(ifsa.Soru, Is.EqualTo(soru));
            Assert.That(ifsa.A, Is.EqualTo(a));
            Assert.That(ifsa.C, Is.EqualTo(c));
        }

        [Test]
        public void B_kazanirsa_soru_gume_gider_ve_hicbir_yerde_kalmaz()
        {
            const string soru = "Bu asla görünmemeli?";
            var oyun = Yeni();
            var olaylar = new List<Olay>();
            oyun.OlayOldu += olaylar.Add;
            SorVeSec(oyun, soru, out _, out var b, out var c);
            TkmOyna(oyun, b, c, cKazansin: false);

            // Güme aşamasında, sonraki turda ve oyun sonunda kimse görmüyor
            for (int adim = 0; adim < 3; adim++)
            {
                foreach (var o in Durum(oyun).Oyuncular)
                {
                    var g = oyun.GorunumAl(o.Id);
                    Assert.That(g.Soru, Is.Not.EqualTo(soru));
                    Assert.That(g.IfsaSoru, Is.Not.EqualTo(soru));
                }
                if (adim == 0) Saniye(oyun, 6);       // sonraki tura
                else if (adim == 1) oyun.OyunuBitir();
            }
            foreach (var olay in olaylar) Assert.That(olay.Soru, Is.Not.EqualTo(soru), olay.Tur.ToString());
        }

        [Test]
        public void Gorunumu_degistirmek_oyunu_degistirmez()
        {
            var oyun = Yeni();
            SorVeSec(oyun, "Soru?", out _, out var b, out var c);
            TkmOyna(oyun, b, c, cKazansin: true);

            var ilk = Durum(oyun);
            ilk.SonMiniOyun.CKazandi = false;
            ilk.Oyuncular[0].Gosterilme = 99;

            var sonraki = Durum(oyun);
            Assert.That(sonraki.SonMiniOyun.CKazandi, Is.True);
            Assert.That(sonraki.Oyuncular[0].Gosterilme, Is.Not.EqualTo(99));
        }

        // ---------------------------------------------------- Sıra ve tur

        [TestCase(true)]
        [TestCase(false)]
        public void Sonuc_ne_olursa_olsun_sira_C_ye_gecer(bool cKazansin)
        {
            var oyun = Yeni();
            SorVeSec(oyun, "Soru?", out _, out var b, out var c);
            TkmOyna(oyun, b, c, cKazansin);
            Saniye(oyun, 6);

            var g = Durum(oyun);
            Assert.That(g.Asama, Is.EqualTo(Asama.SoruSorma));
            Assert.That(g.A, Is.EqualTo(c));
            Assert.That(g.Tur, Is.EqualTo(2));
        }

        [Test]
        public void Sonuc_ekrani_6_saniye_surer()
        {
            var oyun = Yeni();
            SorVeSec(oyun, "Soru?", out _, out var b, out var c);
            TkmOyna(oyun, b, c, cKazansin: true);
            Saniye(oyun, 5.9);
            Assert.That(Durum(oyun).Asama, Is.EqualTo(Asama.Ifsa));
            Saniye(oyun, 0.2);
            Assert.That(Durum(oyun).Asama, Is.EqualTo(Asama.SoruSorma));
        }

        [Test]
        public void Tur_sayisi_dolunca_oyun_biter()
        {
            var oyun = Yeni(ayarla: x => x.TurSayisi = 2);
            for (int i = 0; i < 2; i++)
            {
                SorVeSec(oyun, "Soru?", out _, out var b, out var c);
                TkmOyna(oyun, b, c, cKazansin: true);
                Saniye(oyun, 6);
            }
            var g = Durum(oyun);
            Assert.That(g.Asama, Is.EqualTo(Asama.OyunSonu));
            Assert.That(g.Tur, Is.EqualTo(2));
        }

        [Test]
        public void Sinirsiz_oyun_kurucu_bitirene_kadar_surer()
        {
            var oyun = Yeni(ayarla: x => x.TurSayisi = 0);
            for (int i = 0; i < 30; i++)
            {
                SorVeSec(oyun, "Soru?", out _, out var b, out var c);
                TkmOyna(oyun, b, c, cKazansin: i % 2 == 0);
                Saniye(oyun, 6);
            }
            Assert.That(Durum(oyun).Asama, Is.EqualTo(Asama.SoruSorma));
            Assert.That(oyun.OyunuBitir(), Is.EqualTo(Sonuc.Tamam));
            Assert.That(Durum(oyun).Asama, Is.EqualTo(Asama.OyunSonu));
            Assert.That(oyun.OyunuBitir(), Is.EqualTo(Sonuc.YanlisAsama));
        }

        [Test]
        public void Sayaclar_dogru_sayar()
        {
            var oyun = Yeni();
            SorVeSec(oyun, "Soru?", out var a, out var b, out var c);
            TkmOyna(oyun, b, c, cKazansin: true);

            var oyuncular = Durum(oyun).Oyuncular;
            OyuncuDurumu O(OyuncuId id)
            {
                foreach (var x in oyuncular) if (x.Id == id) return x;
                return null;
            }
            Assert.That(O(a).SoruSorma, Is.EqualTo(1));
            Assert.That(O(b).SoruAlma, Is.EqualTo(1));
            Assert.That(O(c).Gosterilme, Is.EqualTo(1));
            Assert.That(O(c).IfsaEttirme, Is.EqualTo(1));
            Assert.That(O(c).MiniOyunKazanma, Is.EqualTo(1));
            Assert.That(O(b).Saklama, Is.EqualTo(0));
        }
    }
}
