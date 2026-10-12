using System.Collections.Generic;
using NUnit.Framework;
using static Cakmak.Kurallar.Tests.Kurulum;
using static Cakmak.Kurallar.Tests.TekrarYardim;

namespace Cakmak.Kurallar.Tests
{
    /// <summary>S5: kopma, AFK, oyundan çıkma (Ana Plan 6.1).</summary>
    public class KopmaTestleri
    {
        static List<OyuncuId> Liste(IReadOnlyList<OyuncuId> l) => new List<OyuncuId>(l);

        static OyuncuId Baska(Oyun oyun, params OyuncuId[] haric)
        {
            foreach (var o in Durum(oyun).Oyuncular)
                if (System.Array.IndexOf(haric, o.Id) < 0) return o.Id;
            throw new AssertionException("başka oyuncu yok");
        }

        // ---------------------------------------------------- Kopma

        [Test]
        public void Kopuk_oyuncu_secilemez_donunce_secilebilir()
        {
            var oyun = Yeni();
            var a = SiradakiA(oyun);
            var x = Baska(oyun, a);
            oyun.OyuncuKoptu(x);
            Assert.That(Liste(oyun.GorunumAl(a).Secilebilir), Has.No.Member(x));
            Assert.That(oyun.SoruSor(a, x, "Kim?"), Is.EqualTo(Sonuc.Secilemez));

            oyun.OyuncuDondu(x);
            Assert.That(Liste(oyun.GorunumAl(a).Secilebilir), Has.Member(x));
            Assert.That(oyun.SoruSor(a, x, "Kim?"), Is.EqualTo(Sonuc.Tamam));
        }

        [Test]
        public void Sirasi_gelen_kopuk_oyuncu_atlanir_sure_beklenmez()
        {
            var oyun = Yeni(ayarla: x => x.TurSayisi = 0);
            var olaylar = new List<Olay>();
            oyun.OlayOldu += olaylar.Add;
            SorVeSec(oyun, "Kim?", out _, out var b, out var c);
            oyun.OyuncuKoptu(c); // sıra C'ye geçecekti
            TkmOyna(oyun, b, c, cKazansin: true);
            Saniye(oyun, 6);

            var g = Durum(oyun);
            Assert.That(g.Asama, Is.EqualTo(Asama.SoruSorma));
            Assert.That(g.A, Is.Not.EqualTo(c));
            Assert.That(olaylar.Exists(o => o.Tur == OlayTuru.SiraAtlandi && o.Kim == c), Is.True);
        }

        [Test]
        public void Turun_ortasinda_kopanin_suresi_isler()
        {
            var oyun = Yeni();
            var a = SiradakiA(oyun);
            var b = oyun.GorunumAl(a).Secilebilir[0];
            oyun.SoruSor(a, b, "Kim?");
            oyun.OyuncuKoptu(b);
            Assert.That(Durum(oyun).Asama, Is.EqualTo(Asama.CevapSecme), "kopunca hemen bitmez, yeri tutulur");
            Saniye(oyun, 20.1);
            Assert.That(Durum(oyun).Tur, Is.EqualTo(2), "süresi doldu, tur geçti");
        }

        [Test]
        public void Aktif_kimse_kalmasa_da_oyun_takilmaz()
        {
            var oyun = Yeni(kisi: 4, ayarla: x => x.TurSayisi = 0);
            foreach (var o in Durum(oyun).Oyuncular) oyun.OyuncuKoptu(o.Id);
            for (int i = 0; i < 20; i++) Saniye(oyun, 61);
            Assert.That(Durum(oyun).Tur, Is.GreaterThan(10), "sıra dönmeye devam eder (online'da sunucu oyunu durdurur)");
        }

        // ---------------------------------------------------- AFK

        /// <summary>A, x'e sorar; x cevap seçmez (süre dolar). Sonraki turda sıra A ve x dışında birine geçer.</summary>
        static void BIkenSureDoldur(Oyun oyun, OyuncuId x)
        {
            var a = SiradakiA(oyun);
            Assert.That(a, Is.Not.EqualTo(x));
            Assert.That(oyun.SoruSor(a, x, "Kim?"), Is.EqualTo(Sonuc.Tamam));
            Saniye(oyun, 20.1);
        }

        [Test]
        public void Ust_uste_iki_kez_suresini_dolduran_AFK_olur_atlanir_buradayim_deyince_kalkar()
        {
            var oyun = Yeni(ayarla: x => x.TurSayisi = 0);
            var olaylar = new List<Olay>();
            oyun.OlayOldu += olaylar.Add;
            var x = Baska(oyun, SiradakiA(oyun));
            BIkenSureDoldur(oyun, x);
            Assert.That(TekrarYardim.Oyuncu(oyun, x).Afk, Is.False, "bir kez yetmez");
            BIkenSureDoldur(oyun, x);

            Assert.That(TekrarYardim.Oyuncu(oyun, x).Afk, Is.True);
            Assert.That(olaylar.Exists(o => o.Tur == OlayTuru.Afk && o.Kim == x), Is.True);
            Assert.That(Liste(oyun.GorunumAl(SiradakiA(oyun)).Secilebilir), Has.No.Member(x));

            oyun.Buradayim(x);
            Assert.That(TekrarYardim.Oyuncu(oyun, x).Afk, Is.False);
            Assert.That(Liste(oyun.GorunumAl(SiradakiA(oyun)).Secilebilir), Has.Member(x));
        }

        [Test]
        public void Arada_buradayim_diyen_AFK_olmaz()
        {
            var oyun = Yeni(ayarla: x => x.TurSayisi = 0);
            var x = Baska(oyun, SiradakiA(oyun));
            BIkenSureDoldur(oyun, x);
            oyun.Buradayim(x);
            BIkenSureDoldur(oyun, x);
            Assert.That(TekrarYardim.Oyuncu(oyun, x).Afk, Is.False, "üst üste değil");
        }

        [Test]
        public void Elden_elede_AFK_yok()
        {
            var oyun = Yeni(ayarla: x => { x.TurSayisi = 0; x.TekCihaz = true; });
            var x = Baska(oyun, SiradakiA(oyun));
            BIkenSureDoldur(oyun, x);
            BIkenSureDoldur(oyun, x);
            Assert.That(TekrarYardim.Oyuncu(oyun, x).Afk, Is.False);
        }

        [Test]
        public void Soran_gizlide_A_nin_suresi_AFK_icin_sayilmaz()
        {
            // Sayılsaydı A'nın AFK olması gizli A'yı ele verirdi.
            var oyun = KaosaGetir(KaosKurali.SoranGizli);
            var a = GercekA(oyun);
            Saniye(oyun, 60.1);
            BIkenSureDoldur(oyun, a);
            Assert.That(TekrarYardim.Oyuncu(oyun, a).Afk, Is.False);
        }

        // ---------------------------------------------------- Çıkış

        [Test]
        public void Sormadan_cikan_A_nin_turu_hemen_yanar()
        {
            var oyun = Yeni(ayarla: x => x.TurSayisi = 0);
            var olaylar = new List<Olay>();
            oyun.OlayOldu += olaylar.Add;
            var a = SiradakiA(oyun);
            Assert.That(oyun.OyuncuCikti(a), Is.EqualTo(Sonuc.Tamam));

            var g = Durum(oyun);
            Assert.That(g.Tur, Is.EqualTo(2));
            Assert.That(g.A, Is.Not.EqualTo(a));
            Assert.That(TekrarYardim.Oyuncu(oyun, a).Cikti, Is.True);
            Assert.That(olaylar.Exists(o => o.Tur == OlayTuru.OyuncuCikti && o.Kim == a), Is.True);
            Assert.That(Liste(oyun.GorunumAl(g.A.Value).Secilebilir), Has.No.Member(a), "bir daha seçilmez");
        }

        [Test]
        public void Secmeden_cikan_B_nin_sorusu_gume_gider()
        {
            var oyun = Yeni(ayarla: x => x.TurSayisi = 0);
            var a = SiradakiA(oyun);
            var b = oyun.GorunumAl(a).Secilebilir[0];
            oyun.SoruSor(a, b, "Gizli kalacak soru kim?");
            oyun.OyuncuCikti(b);
            Assert.That(Durum(oyun).Tur, Is.EqualTo(2));
            oyun.OyunuBitir();
            Assert.That(oyun.Ozet().IfsaOlanlar, Is.Empty);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Mini_oyundan_kacan_kaybeder(bool cKacar)
        {
            var oyun = Yeni();
            SorVeSec(oyun, "Kim?", out _, out var b, out var c);
            oyun.MiniOyunHamlesi(cKacar ? c : b, Hamle.TasKagitMakas(TkmSecim.Tas));
            oyun.OyuncuCikti(cKacar ? c : b);
            var g = Durum(oyun);
            Assert.That(g.Asama, Is.EqualTo(cKacar ? Asama.Gume : Asama.Ifsa));
            Assert.That(g.SonMiniOyun.CKazandi, Is.EqualTo(!cKacar));
        }

        [Test]
        public void Oy_vermeden_cikan_oylamadan_duser_kalanlar_verince_sonuc_cikar()
        {
            var oyun = KaosaGetir(KaosKurali.GrupKarari);
            SorSec(oyun, "Oylanan soru kim?", out _, out _, out _);
            var oylayanlar = Liste(Durum(oyun).Oylayanlar);
            oyun.Oy(oylayanlar[0], true);
            oyun.Oy(oylayanlar[1], true);
            oyun.OyuncuCikti(oylayanlar[2]);
            Assert.That(Durum(oyun).Asama, Is.EqualTo(Asama.Ifsa));
        }

        [Test]
        public void Soran_gizlide_cikan_A_nin_turu_hemen_bitmez()
        {
            // Hemen bitseydi turun bitişinden gizli A'nın kim olduğu anlaşılırdı.
            var oyun = KaosaGetir(KaosKurali.SoranGizli);
            var a = GercekA(oyun);
            int tur = Durum(oyun).Tur;
            oyun.OyuncuCikti(a);
            Assert.That(Durum(oyun).Asama, Is.EqualTo(Asama.SoruSorma));
            Assert.That(Durum(oyun).Tur, Is.EqualTo(tur));
            Saniye(oyun, 60.1);
            Assert.That(Durum(oyun).Tur, Is.EqualTo(tur + 1));
        }

        [Test]
        public void Curcunada_cikanin_yazmadigi_soru_toplamayi_bekletmez()
        {
            var oyun = Yeni(kisi: 5, ayarla: x => { x.Mod = OyunModu.Curcuna; x.OneriSorulari = Havuz; });
            for (int i = 1; i <= 4; i++)
            {
                oyun.HavuzaSoruEkle(P(i), "Soru " + i + "a kim?");
                oyun.HavuzaSoruEkle(P(i), "Soru " + i + "b kim?");
            }
            Assert.That(Durum(oyun).Asama, Is.EqualTo(Asama.SoruToplama));
            oyun.OyuncuCikti(P(5));
            Assert.That(Durum(oyun).Asama, Is.EqualTo(Asama.SoruSorma));
            Assert.That(oyun.HavuzaSoruEkle(P(5), "Geç kim?"), Is.EqualTo(Sonuc.YanlisAsama));
        }

        [Test]
        public void Bilinmeyen_oyuncu_ve_oyun_sonu()
        {
            var oyun = Yeni();
            Assert.That(oyun.OyuncuKoptu(P(99)), Is.EqualTo(Sonuc.GecersizGirdi));
            Assert.That(oyun.OyuncuCikti(P(99)), Is.EqualTo(Sonuc.GecersizGirdi));
            oyun.OyunuBitir();
            Assert.That(oyun.OyuncuCikti(P(1)), Is.EqualTo(Sonuc.Tamam));
            Assert.That(TekrarYardim.Oyuncu(oyun, P(1)).Cikti, Is.False, "oyun bitince çıkış bir şey değiştirmez");
        }
    }
}
