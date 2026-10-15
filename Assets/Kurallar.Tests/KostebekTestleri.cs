using System;
using System.Collections.Generic;
using NUnit.Framework;
using static Cakmak.Kurallar.Tests.Kurulum;

namespace Cakmak.Kurallar.Tests
{
    /// <summary>Köstebek (docs/Oyunlar.md, 1).</summary>
    public class KostebekTestleri
    {
        static Oyun YeniKostebek(int kisi = 5, int tohum = 1, Action<Ayarlar> ayarla = null) =>
            Yeni(kisi, tohum, a =>
            {
                a.Oyun = OyunTuru.Kostebek;
                ayarla?.Invoke(a);
            });

        static OyuncuId KostebekKim(Oyun oyun, int kisi = 5)
        {
            var bulunan = new List<OyuncuId>();
            for (int i = 1; i <= kisi; i++)
                if (oyun.GorunumAl(P(i)).Rolun == Rol.Kostebek) bulunan.Add(P(i));
            Assert.That(bulunan.Count, Is.EqualTo(1), "tam bir köstebek");
            return bulunan[0];
        }

        // ---------------------------------------------------- Kurulum ve gizlilik

        [Test]
        public void Tam_bir_kostebek_rolunu_sadece_kendisi_gorur_herkesin_gizli_gorevi_var()
        {
            for (int tohum = 1; tohum <= 30; tohum++)
            {
                var oyun = YeniKostebek(6, tohum); // GizliGorevler kapalı: Köstebek'te yine de herkeste görev
                var k = KostebekKim(oyun, 6);
                for (int i = 1; i <= 6; i++)
                {
                    var g = oyun.GorunumAl(P(i));
                    Assert.That(g.Oyun, Is.EqualTo(OyunTuru.Kostebek));
                    Assert.That(g.Gorevin, Is.Not.Null);
                    Assert.That(g.SuclamaHakkin, Is.True);
                }
                var gorev = oyun.GorunumAl(k).Gorevin;
                Assert.That(gorev.Zorluk, Is.EqualTo(Zorluk.Zor));
                Assert.That(gorev.Tur != GorevTuru.HicIfsaOlma && gorev.Tur != GorevTuru.HicGosterilme && gorev.Tur != GorevTuru.HicCezaCekme,
                    "köstebek görevi oyun ortasında bitebilmeli: " + gorev.Tur);
                Assert.That(oyun.Ozet().Kostebek, Is.Null, "oyun bitmeden özette köstebek yok");
            }
        }

        [Test]
        public void Kostebekte_kaos_kurulamaz()
        {
            var ayar = new Ayarlar { Oyun = OyunTuru.Kostebek, KaosTurlari = true };
            Assert.That(Oyun.Kur(ayar, Oyuncular(5), 1, out _), Is.EqualTo(Sonuc.GecersizGirdi));
        }

        [Test]
        public void Cakmak_oyununda_rol_ve_suclama_yok()
        {
            var oyun = Yeni();
            for (int i = 1; i <= 5; i++)
            {
                var g = oyun.GorunumAl(P(i));
                Assert.That(g.Rolun, Is.EqualTo(Rol.Yok));
                Assert.That(g.SuclamaHakkin, Is.False);
                Assert.That(g.Gorevin, Is.Null, "Çakmak'ta görev ayara bağlı");
            }
            Assert.That(oyun.Sucla(P(1)), Is.EqualTo(Sonuc.YanlisAsama));
            Assert.That(oyun.OyunuBitir(), Is.EqualTo(Sonuc.Tamam));
            Assert.That(Durum(oyun).Asama, Is.EqualTo(Asama.OyunSonu), "Çakmak'ta bitir hemen bitirir");
            Assert.That(oyun.Ozet().Kostebek, Is.Null);
        }

        // ---------------------------------------------------- Suçlama

        [Test]
        public void Her_5_turda_oylama_suresi_dolunca_A_turu_bastan()
        {
            var oyun = YeniKostebek();
            var k = KostebekKim(oyun);
            for (int i = 0; i < Oyun.SuclamaAraligi; i++)
            {
                Assert.That(Durum(oyun).Asama, Is.EqualTo(Asama.SoruSorma), "tur " + (i + 1));
                OnsuzTur(oyun, k);
            }
            var d = Durum(oyun);
            Assert.That(d.Asama, Is.EqualTo(Asama.Suclama));
            Assert.That(d.Tur, Is.EqualTo(6));
            Assert.That(d.Oylayanlar.Count, Is.EqualTo(5), "A ve B dahil herkes");
            Assert.That(d.SonSuclama, Is.False);
            var a = d.A.Value;

            Saniye(oyun, 61);
            d = Durum(oyun);
            Assert.That(d.Asama, Is.EqualTo(Asama.SoruSorma));
            Assert.That(d.Tur, Is.EqualTo(6));
            Assert.That(d.A, Is.EqualTo(a));
            Assert.That(d.Supheliler, Is.Empty, "hiç oy yok: kimse suçlanmadı");
        }

        [Test]
        public void Kostebek_yakalanirsa_grup_kazanir()
        {
            var oyun = YeniKostebek();
            var k = KostebekKim(oyun);
            var olaylar = new List<Olay>();
            oyun.OlayOldu += olaylar.Add;
            Assert.That(oyun.Sucla(P(1)), Is.EqualTo(Sonuc.Tamam));
            Assert.That(olaylar.Exists(o => o.Tur == OlayTuru.SuclamaBasladi && o.Kim == P(1)), Is.True);

            HerkesOyVerir(oyun, kim => kim == k ? Ilk(new[] { P(1), P(2) }, k) : k);
            Assert.That(Durum(oyun).Asama, Is.EqualTo(Asama.OyunSonu));
            Assert.That(olaylar.Exists(o => o.Tur == OlayTuru.SuclamaBitti && o.Kim == k), Is.True);
            var sonuc = oyun.Ozet().Kostebek;
            Assert.That(sonuc.Kostebek, Is.EqualTo(k));
            Assert.That(sonuc.Kazanan, Is.EqualTo(KostebekKazanan.Grup));
            Assert.That(sonuc.Neden, Is.EqualTo(KostebekNedeni.Yakalandi));
            Assert.That(oyun.Ozet().Gorevler.Count, Is.EqualTo(5), "oyun sonunda herkesin görevi açılır, köstebeğinki dahil");
        }

        [Test]
        public void Yanlis_kisi_supheli_olur_oyun_A_turuyla_tam_sureden_surer()
        {
            var oyun = YeniKostebek();
            var k = KostebekKim(oyun);
            var a = SiradakiA(oyun);
            var masum = Ilk(new[] { P(1), P(2), P(3) }, k);
            Assert.That(oyun.Sucla(P(4)), Is.EqualTo(Sonuc.Tamam));
            HerkesOyVerir(oyun, kim => kim == masum ? Ilk(new[] { P(1), P(2), P(3) }, masum) : masum);

            var d = Durum(oyun);
            Assert.That(d.Asama, Is.EqualTo(Asama.SoruSorma));
            Assert.That(d.A, Is.EqualTo(a));
            Assert.That(d.Tur, Is.EqualTo(1));
            Assert.That(d.KalanMs, Is.EqualTo(60000));
            Assert.That(d.Supheliler, Is.EqualTo(new[] { masum }));
        }

        [Test]
        public void Esitlikte_kimse_suclanmaz()
        {
            var oyun = YeniKostebek(4);
            KostebekKim(oyun, 4);
            var olaylar = new List<Olay>();
            oyun.OlayOldu += olaylar.Add;
            Assert.That(oyun.Sucla(P(1)), Is.EqualTo(Sonuc.Tamam));
            // p1→p2, p2→p1, p3→p4, p4→p3: herkes 1 oy
            HerkesOyVerir(oyun, kim => P(kim.Deger % 2 == 1 ? kim.Deger + 1 : kim.Deger - 1), 4);
            Assert.That(Durum(oyun).Asama, Is.EqualTo(Asama.SoruSorma));
            Assert.That(olaylar.Exists(o => o.Tur == OlayTuru.SuclamaBitti && o.Kim == null), Is.True);
            Assert.That(Durum(oyun).Supheliler, Is.Empty);
        }

        [Test]
        public void Erken_suclama_hakki_bir_kez_ve_sadece_soru_sormadan_once()
        {
            var oyun = YeniKostebek();
            var k = KostebekKim(oyun);
            Assert.That(oyun.Sucla(P(2)), Is.EqualTo(Sonuc.Tamam));
            Assert.That(oyun.Sucla(P(3)), Is.EqualTo(Sonuc.YanlisAsama), "oylama sürerken");
            Saniye(oyun, 61);
            Assert.That(oyun.GorunumAl(P(2)).SuclamaHakkin, Is.False);
            Assert.That(oyun.Sucla(P(2)), Is.EqualTo(Sonuc.HakKalmadi));

            var a = SiradakiA(oyun);
            var b = Ilk(oyun.GorunumAl(a).Secilebilir, k);
            Assert.That(oyun.SoruSor(a, b, "Kim?"), Is.EqualTo(Sonuc.Tamam));
            Assert.That(oyun.Sucla(P(3)), Is.EqualTo(Sonuc.YanlisAsama), "düello ortası kesilmez");
            Assert.That(oyun.GorunumAl(P(3)).SuclamaHakkin, Is.True, "kullanılamayan hak yanmaz");
        }

        [Test]
        public void Oy_kurallari()
        {
            var oyun = YeniKostebek();
            Assert.That(oyun.SuclamaOyu(P(1), P(2)), Is.EqualTo(Sonuc.YanlisAsama));
            Assert.That(oyun.OyuncuCikti(Ilk(new[] { P(4), P(5) }, KostebekKim(oyun))), Is.EqualTo(Sonuc.Tamam));
            var cikan = Durum(oyun).Oyuncular;
            OyuncuId cikanId = default;
            foreach (var o in cikan) if (o.Cikti) cikanId = o.Id;

            Assert.That(oyun.Sucla(P(1)), Is.EqualTo(Sonuc.Tamam));
            Assert.That(oyun.SuclamaOyu(P(1), P(1)), Is.EqualTo(Sonuc.Secilemez), "kendini suçlayamaz");
            Assert.That(oyun.SuclamaOyu(P(1), cikanId), Is.EqualTo(Sonuc.Secilemez), "oyundan çıkan suçlanmaz");
            Assert.That(oyun.SuclamaOyu(cikanId, P(1)), Is.EqualTo(Sonuc.SiraSendeDegil), "oyundan çıkan oy vermez");
            Assert.That(oyun.SuclamaOyu(P(1), P(2)), Is.EqualTo(Sonuc.Tamam));
            Assert.That(oyun.SuclamaOyu(P(1), P(3)), Is.EqualTo(Sonuc.HakKalmadi));
            Assert.That(Durum(oyun).Oylayanlar.Count, Is.EqualTo(4));
            Assert.That(oyun.GorunumAl(P(1)).OyVerdin, Is.True);
            Assert.That(Durum(oyun).OyKullanan, Is.EqualTo(1));
        }

        // ---------------------------------------------------- Kazanma

        [Test]
        public void Gorevini_bitiren_kostebek_o_turun_sonunda_kazanir()
        {
            var oyun = GoreviBitmisKostebek(); // görev bu turda bitti, sonuç yine gösteriliyor
            Saniye(oyun, 7);
            Assert.That(Durum(oyun).Asama, Is.EqualTo(Asama.OyunSonu));
            var sonuc = oyun.Ozet().Kostebek;
            Assert.That(sonuc.Kazanan, Is.EqualTo(KostebekKazanan.Kostebek));
            Assert.That(sonuc.Neden, Is.EqualTo(KostebekNedeni.GorevBitti));
        }

        /// <summary>"3 farklı kişiyi göster" görevli bir oyunda köstebeği 3 kez B yapar; son turun sonuç ekranında bırakır.</summary>
        static Oyun GoreviBitmisKostebek()
        {
            Oyun oyun = null;
            OyuncuId k = default;
            for (int tohum = 1; tohum <= 500 && oyun == null; tohum++)
            {
                var o = YeniKostebek(5, tohum);
                var kk = KostebekKim(o);
                if (o.GorunumAl(kk).Gorevin.Tur == GorevTuru.FarkliKisiGoster) { oyun = o; k = kk; }
            }
            Assert.That(oyun, Is.Not.Null);
            Assert.That(oyun.GorunumAl(k).Gorevin.Adet, Is.EqualTo(3));
            if (SiradakiA(oyun) == k) OnsuzTur(oyun, k);
            var gosterilen = new List<OyuncuId>();
            for (int i = 0; i < 3; i++)
            {
                var a = SiradakiA(oyun);
                Assert.That(oyun.SoruSor(a, k, "Kim?"), Is.EqualTo(Sonuc.Tamam));
                var c = Ilk(oyun.GorunumAl(k).Secilebilir, gosterilen.ToArray());
                gosterilen.Add(c);
                Assert.That(oyun.CevapSec(k, c), Is.EqualTo(Sonuc.Tamam));
                TkmOyna(oyun, k, c, cKazansin: true);
                if (i < 2) Saniye(oyun, 7);
            }
            Assert.That(Durum(oyun).Asama, Is.EqualTo(Asama.Ifsa));
            return oyun;
        }

        [Test]
        public void Gorevi_bitmisken_kurucu_bitirirse_oylama_olmaz_kostebek_kazanir()
        {
            var oyun = GoreviBitmisKostebek();
            Assert.That(oyun.OyunuBitir(), Is.EqualTo(Sonuc.Tamam));
            Assert.That(Durum(oyun).Asama, Is.EqualTo(Asama.OyunSonu), "görevini bitirmiş köstebek oylamayla yakalanamaz");
            Assert.That(oyun.Ozet().Kostebek.Neden, Is.EqualTo(KostebekNedeni.GorevBitti));
        }

        [Test]
        public void Gorevi_bitmisken_oyun_zorla_biterse_neden_gorev()
        {
            var oyun = GoreviBitmisKostebek();
            Assert.That(oyun.OyunuBitir(zorla: true), Is.EqualTo(Sonuc.Tamam));
            Assert.That(oyun.Ozet().Kostebek.Neden, Is.EqualTo(KostebekNedeni.GorevBitti));
        }

        [Test]
        public void Tur_sayisi_bitince_son_oylama_yakalanmazsa_kostebek_kazanir()
        {
            var oyun = YeniKostebek(ayarla: a => a.TurSayisi = 2);
            var k = KostebekKim(oyun);
            OnsuzTur(oyun, k);
            OnsuzTur(oyun, k);
            var d = Durum(oyun);
            Assert.That(d.Asama, Is.EqualTo(Asama.Suclama));
            Assert.That(d.SonSuclama, Is.True);
            Assert.That(d.Tur, Is.EqualTo(2));
            Assert.That(oyun.Ozet().IfsaOlanlar, Is.Empty, "oyun bitmedi");

            Saniye(oyun, 61);
            Assert.That(Durum(oyun).Asama, Is.EqualTo(Asama.OyunSonu));
            var ozet = oyun.Ozet();
            Assert.That(ozet.Kostebek.Kazanan, Is.EqualTo(KostebekKazanan.Kostebek));
            Assert.That(ozet.Kostebek.Neden, Is.EqualTo(KostebekNedeni.Yakalanmadi));
            Assert.That(ozet.IfsaOlanlar.Count, Is.EqualTo(2), "ifşa olan sorular son oylamadan sonra da kayıtlı");
        }

        [Test]
        public void Curcunada_son_oylamadan_sonra_ifsa_olan_soru_grup_paketine_kalir()
        {
            var oyun = YeniKostebek(4, ayarla: a => { a.Mod = OyunModu.Curcuna; a.TurSayisi = 1; });
            for (int i = 1; i <= 4; i++)
                for (int j = 1; j <= Oyun.KisiBasiHavuzSorusu; j++)
                    Assert.That(oyun.HavuzaSoruEkle(P(i), $"Soru {i}-{j}?"), Is.EqualTo(Sonuc.Tamam));
            var k = KostebekKim(oyun, 4);
            var a = SiradakiA(oyun);
            var b = Ilk(oyun.GorunumAl(a).Secilebilir, k);
            Assert.That(oyun.SoruSor(a, b, null), Is.EqualTo(Sonuc.Tamam));
            var soru = oyun.GorunumAl(b).Soru;
            var c = Ilk(oyun.GorunumAl(b).Secilebilir, k);
            Assert.That(oyun.CevapSec(b, c), Is.EqualTo(Sonuc.Tamam));
            TkmOyna(oyun, b, c, cKazansin: true);
            Saniye(oyun, 7);
            Assert.That(Durum(oyun).Asama, Is.EqualTo(Asama.Suclama));

            Saniye(oyun, 61);
            Assert.That(Durum(oyun).Asama, Is.EqualTo(Asama.OyunSonu));
            Assert.That(oyun.Ozet().CurcunaSorulari, Has.Member(soru), "ifşa olan soru güme sayılmasın");
        }

        [Test]
        public void Bitir_once_son_oylama_ikincisi_reddedilir_zorla_hemen_bitirir()
        {
            var oyun = YeniKostebek();
            Assert.That(oyun.OyunuBitir(), Is.EqualTo(Sonuc.Tamam));
            Assert.That(Durum(oyun).Asama, Is.EqualTo(Asama.Suclama));
            Assert.That(Durum(oyun).SonSuclama, Is.True);
            Assert.That(oyun.OyunuBitir(), Is.EqualTo(Sonuc.YanlisAsama), "kurucu köstebekse ikinci bitirle kazanamasın");

            Assert.That(oyun.OyunuBitir(zorla: true), Is.EqualTo(Sonuc.Tamam));
            Assert.That(Durum(oyun).Asama, Is.EqualTo(Asama.OyunSonu));
            Assert.That(oyun.Ozet().Kostebek.Neden, Is.EqualTo(KostebekNedeni.Yakalanmadi));
        }

        [Test]
        public void Kostebek_oyundan_cikarsa_grup_kazanir()
        {
            var oyun = YeniKostebek();
            var k = KostebekKim(oyun);
            Assert.That(oyun.OyuncuCikti(k), Is.EqualTo(Sonuc.Tamam));
            Assert.That(Durum(oyun).Asama, Is.EqualTo(Asama.OyunSonu));
            Assert.That(oyun.Ozet().Kostebek.Kazanan, Is.EqualTo(KostebekKazanan.Grup));
            Assert.That(oyun.Ozet().Kostebek.Neden, Is.EqualTo(KostebekNedeni.Kacti));
        }

        [Test]
        public void Oylamada_oy_vermeyen_cikarsa_oylama_kalanlarla_biter()
        {
            var oyun = YeniKostebek();
            var k = KostebekKim(oyun);
            var vermeyecek = Ilk(new[] { P(5), P(4) }, k);
            Assert.That(oyun.Sucla(P(1)), Is.EqualTo(Sonuc.Tamam));
            for (int i = 1; i <= 5; i++)
                if (P(i) != vermeyecek) Assert.That(oyun.SuclamaOyu(P(i), P(i) == k ? vermeyecek : k), Is.EqualTo(Sonuc.Tamam));
            Assert.That(Durum(oyun).Asama, Is.EqualTo(Asama.Suclama));

            Assert.That(oyun.OyuncuCikti(vermeyecek), Is.EqualTo(Sonuc.Tamam));
            Assert.That(Durum(oyun).Asama, Is.EqualTo(Asama.OyunSonu));
            Assert.That(oyun.Ozet().Kostebek.Neden, Is.EqualTo(KostebekNedeni.Yakalandi));
        }

        [Test]
        public void Oylama_sirasinda_A_cikarsa_turu_yanar()
        {
            var oyun = YeniKostebek();
            var k = KostebekKim(oyun);
            if (SiradakiA(oyun) == k) OnsuzTur(oyun, k);
            var a = SiradakiA(oyun);
            int tur = Durum(oyun).Tur;
            Assert.That(oyun.Sucla(P(1) == a ? P(2) : P(1)), Is.EqualTo(Sonuc.Tamam));
            Assert.That(oyun.OyuncuCikti(a), Is.EqualTo(Sonuc.Tamam));
            Saniye(oyun, 61);

            var d = Durum(oyun);
            Assert.That(d.Asama, Is.EqualTo(Asama.SoruSorma));
            Assert.That(d.Tur, Is.EqualTo(tur + 1));
            Assert.That(d.A, Is.Not.EqualTo(a));
        }
    }
}
