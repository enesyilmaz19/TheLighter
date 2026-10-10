using System.Collections.Generic;
using NUnit.Framework;
using static Cakmak.Kurallar.Tests.Kurulum;
using static Cakmak.Kurallar.Tests.TekrarYardim;

namespace Cakmak.Kurallar.Tests
{
    public class GorevOzetTestleri
    {
        // ---------------------------------------------------- Gizli görevler

        [Test]
        public void Gorevler_kapaliyken_gorev_yok()
        {
            var oyun = Yeni();
            Assert.That(Durum(oyun).Gorevin, Is.Null);
            Assert.That(oyun.Ozet().Gorevler, Is.Empty);
        }

        [Test]
        public void Herkese_ayni_zorlukta_kendi_gorevi_hedef_kendisi_degil()
        {
            for (int tohum = 1; tohum <= 50; tohum++)
            {
                var oyun = Yeni(kisi: 6, tohum: tohum, ayarla: x => x.GizliGorevler = true);
                var zorluklar = new HashSet<Zorluk>();
                foreach (var o in Durum(oyun).Oyuncular)
                {
                    var gorev = oyun.GorunumAl(o.Id).Gorevin;
                    Assert.That(gorev, Is.Not.Null);
                    zorluklar.Add(gorev.Zorluk);
                    if (gorev.Hedef.HasValue) Assert.That(gorev.Hedef.Value, Is.Not.EqualTo(o.Id));
                    Assert.That(gorev.Tur, Is.Not.EqualTo(GorevTuru.KaosTurundaKazan), "kaos kapalıyken kaos görevi yok");
                }
                Assert.That(zorluklar, Has.Count.EqualTo(1), "tohum " + tohum);
            }
        }

        [Test]
        public void Gorev_sadece_sahibinin_gorunumunde_kopyasini_degistirmek_bir_sey_degistirmez()
        {
            var oyun = Yeni(ayarla: x => x.GizliGorevler = true);
            var ilk = oyun.GorunumAl(P(1)).Gorevin;
            ilk.Adet = 999;
            Assert.That(oyun.GorunumAl(P(1)).Gorevin.Adet, Is.Not.EqualTo(999));
            // Görünümde başkasının görevi için alan yok; özet oyun sonunda hepsini açar.
            oyun.OyunuBitir();
            Assert.That(oyun.Ozet().Gorevler, Has.Count.EqualTo(5));
        }

        [Test]
        public void Gorev_sonuclari_herkese_acik_sayaclarla_tutarli()
        {
            for (int tohum = 1; tohum <= 80; tohum++)
            {
                var oyun = Yeni(kisi: 5, tohum: tohum, ayarla: x =>
                {
                    x.GizliGorevler = true;
                    x.KaosTurlari = true;
                    x.OneriSorulari = Havuz;
                    x.TurSayisi = 12;
                });
                for (int i = 0; Durum(oyun).Asama != Asama.OyunSonu; i++) TurOyna(oyun, i % 3 != 0);

                var ozet = oyun.Ozet();
                foreach (var s in ozet.Gorevler)
                {
                    OyuncuDurumu o = null;
                    foreach (var x in ozet.Oyuncular) if (x.Id == s.Kim) o = x;
                    switch (s.Gorev.Tur)
                    {
                        case GorevTuru.MiniOyunKazan: Assert.That(s.Basarili, Is.EqualTo(o.MiniOyunKazanma >= s.Gorev.Adet)); break;
                        case GorevTuru.HicGosterilme: Assert.That(s.Basarili, Is.EqualTo(o.Gosterilme == 0)); break;
                        case GorevTuru.KaosTurundaKazan: Assert.That(s.Basarili, Is.EqualTo(o.KaosKazanma >= 1)); break;
                        case GorevTuru.HicIfsaOlma:
                            if (o.SoruAlma == 0) Assert.That(s.Basarili, Is.False, "hiç B olmayan bu görevi geçemez");
                            break;
                    }
                }
            }
        }

        // ---------------------------------------------------- Özet

        [Test]
        public void Ozette_ifsa_olan_var_gume_giden_ve_fisilti_yok()
        {
            var oyun = Yeni(ayarla: x => x.TurSayisi = 0);
            Soru(oyun, "İfşa olan soru?", cKazansin: true);
            Soru(oyun, "Güme giden soru?", cKazansin: false);
            Soru(oyun, null, cKazansin: true); // fısıltı
            oyun.OyunuBitir();

            var ozet = oyun.Ozet();
            Assert.That(ozet.IfsaOlanlar, Has.Count.EqualTo(1));
            Assert.That(ozet.IfsaOlanlar[0].Soru, Is.EqualTo("İfşa olan soru?"));
            foreach (var k in ozet.IfsaOlanlar) Assert.That(k.Soru, Is.Not.EqualTo("Güme giden soru?"));
        }

        static void Soru(Oyun oyun, string metin, bool cKazansin)
        {
            var a = GercekA(oyun);
            var b = oyun.GorunumAl(a).Secilebilir[0];
            oyun.SoruSor(a, b, metin);
            var c = oyun.GorunumAl(b).Secilebilir[0];
            oyun.CevapSec(b, c);
            TkmOyna(oyun, b, c, cKazansin);
            Saniye(oyun, 6);
        }

        static int? AcikSayac(UnvanTuru tur, OyuncuDurumu o)
        {
            switch (tur)
            {
                case UnvanTuru.Miknatis: return o.Gosterilme;
                case UnvanTuru.Gammaz: return o.IfsaEttirme;
                case UnvanTuru.SirKupu: return o.Saklama;
                case UnvanTuru.HedefTahtasi: return o.SoruAlma;
                case UnvanTuru.SoruMakinesi: return o.SoruSorma;
                case UnvanTuru.Sansli: return o.MiniOyunKazanma;
                case UnvanTuru.AtesleOynayan: return o.Yanma;
                case UnvanTuru.Bedelci: return o.BedelOdeme;
                case UnvanTuru.Cezakes: return o.CezaCekme;
                case UnvanTuru.KaosCanavari: return o.KaosKazanma;
                default: return null; // gizli izlerden hesaplananlar
            }
        }

        [Test]
        public void Unvanlar_en_fazla_uc_tane_ve_her_birinin_tek_sahibi_var()
        {
            int kontrol = 0;
            for (int tohum = 1; tohum <= 200; tohum++)
            {
                var oyun = Yeni(kisi: 4 + tohum % 4, tohum: tohum, ayarla: x =>
                {
                    x.TurSayisi = 15;
                    x.CezaSeviyesi = CezaSeviyesi.Cesur;
                    x.KaosTurlari = true;
                    x.OneriSorulari = Havuz;
                    x.SesliCezalar = Sesli;
                });
                for (int i = 0; Durum(oyun).Asama != Asama.OyunSonu; i++) TurOyna(oyun, (i + tohum) % 3 != 0);
                var ozet = oyun.Ozet();
                Assert.That(ozet.Unvanlar.Count, Is.LessThanOrEqualTo(3));
                var turler = new HashSet<UnvanTuru>();
                foreach (var u in ozet.Unvanlar)
                {
                    Assert.That(turler.Add(u.Tur), Is.True, "aynı unvan iki kez");
                    foreach (var o in ozet.Oyuncular)
                    {
                        var sayac = AcikSayac(u.Tur, o);
                        if (!sayac.HasValue) continue;
                        kontrol++;
                        if (o.Id == u.Kim) Assert.That(sayac.Value, Is.EqualTo(u.Deger), u.Tur + ": değer");
                        else Assert.That(sayac.Value, Is.LessThan(u.Deger), $"tohum {tohum}, {u.Tur}: eşitlikte unvan verilmez");
                    }
                }
            }
            Assert.That(kontrol, Is.GreaterThan(100), "test gerçekten unvan kontrol etti");
        }

        [Test]
        public void Esitlikte_unvan_verilmez()
        {
            // 4 tur, seçimler elle: X ve Z ikişer kez gösteriliyor, Mıknatıs berabere.
            var oyun = Yeni(kisi: 4, tohum: 1, ayarla: x => x.TurSayisi = 0);
            var a1 = GercekA(oyun);
            var digerleri = new List<OyuncuId>();
            foreach (var o in Durum(oyun).Oyuncular) if (o.Id != a1) digerleri.Add(o.Id);
            OyuncuId x1 = digerleri[0], z = digerleri[1], y = digerleri[2];

            void Tur(OyuncuId a, OyuncuId b, OyuncuId c)
            {
                Assert.That(GercekA(oyun), Is.EqualTo(a));
                Assert.That(oyun.SoruSor(a, b, "Soru?"), Is.EqualTo(Sonuc.Tamam));
                Assert.That(oyun.CevapSec(b, c), Is.EqualTo(Sonuc.Tamam));
                TkmOyna(oyun, b, c, cKazansin: false);
                Saniye(oyun, 6);
            }
            Tur(a1, y, x1);
            Tur(x1, a1, z);
            Tur(z, y, x1);
            Tur(x1, a1, z);
            oyun.OyunuBitir();

            var ozet = oyun.Ozet();
            Assert.That(TekrarYardim.Oyuncu(oyun, x1).Gosterilme, Is.EqualTo(2));
            Assert.That(TekrarYardim.Oyuncu(oyun, z).Gosterilme, Is.EqualTo(2));
            foreach (var u in ozet.Unvanlar) Assert.That(u.Tur, Is.Not.EqualTo(UnvanTuru.Miknatis), "berabere Mıknatıs verilmez");
        }

        [Test]
        public void Curcuna_sorulari_ozette_oneriden_tamamlananlar_yok()
        {
            var oyun = Yeni(ayarla: x => { x.Mod = OyunModu.Curcuna; x.OneriSorulari = Havuz; });
            oyun.HavuzaSoruEkle(P(1), "Curcuna yazılan kim?");
            Saniye(oyun, 90.1);
            var ozet = oyun.Ozet();
            Assert.That(ozet.CurcunaSorulari, Is.EqualTo(new[] { "Curcuna yazılan kim?" }));
        }

        // ---------------------------------------------------- Grup kaydı

        static OyunOzeti BirOyun(int tohum, params bool[] sonuclar)
        {
            var oyun = Yeni(kisi: 4, tohum: tohum, ayarla: x => x.TurSayisi = 0);
            int i = 0;
            foreach (var cKazansin in sonuclar) { Soru(oyun, "Soru " + tohum + "-" + i++ + " kim?", cKazansin); }
            oyun.OyunuBitir();
            return oyun.Ozet();
        }

        [Test]
        public void Grup_kaydi_ilk_oyunda_olusur_ifsa_olanlar_efsane_ve_grup_paketine_girer()
        {
            var ozet = BirOyun(1, true, false, true);
            var kayit = GrupKaydi.Isle(null, ozet);
            Assert.That(kayit.OyunSayisi, Is.EqualTo(1));
            Assert.That(kayit.Uyeler, Has.Count.EqualTo(4));
            Assert.That(kayit.Efsaneler, Has.Count.EqualTo(2));
            Assert.That(kayit.Efsaneler, Has.No.Member("Soru 1-1 kim?"), "güme giden yazılmaz");
            Assert.That(kayit.GrupPaketi, Is.EquivalentTo(kayit.Efsaneler));
        }

        [Test]
        public void Grup_kaydi_ikinci_oyunda_birikir_eski_kayit_degismez()
        {
            var ilk = GrupKaydi.Isle(null, BirOyun(1, true, true));
            var ilkEfsane = ilk.Efsaneler.Count;
            var ikinci = GrupKaydi.Isle(ilk, BirOyun(2, true));

            Assert.That(ikinci.OyunSayisi, Is.EqualTo(2));
            Assert.That(ikinci.Uyeler, Has.Count.EqualTo(4), "aynı isimler aynı kişi");
            Assert.That(ikinci.Uyeler[0].Oyun, Is.EqualTo(2));
            Assert.That(ikinci.Efsaneler.Count, Is.EqualTo(ilkEfsane + 1));
            Assert.That(ilk.OyunSayisi, Is.EqualTo(1), "eski kayıt değişmez");
            Assert.That(ilk.Efsaneler.Count, Is.EqualTo(ilkEfsane));
        }

        [Test]
        public void Grup_kaydi_isimleri_buyuk_kucuk_harf_farki_gozetmeden_eslestirir_unvan_serisi_tutar()
        {
            var ozet = new OyunOzeti
            {
                Oyuncular = new[] { new OyuncuDurumu { Id = P(1), Isim = "Ayşe", Gosterilme = 5 } },
                Unvanlar = new[] { new Unvan { Tur = UnvanTuru.Miknatis, Kim = P(1), Deger = 5 } },
            };
            var k1 = GrupKaydi.Isle(null, ozet);
            ozet.Oyuncular = new[] { new OyuncuDurumu { Id = P(3), Isim = "AYŞE", Gosterilme = 2 } };
            ozet.Unvanlar = new[] { new Unvan { Tur = UnvanTuru.Miknatis, Kim = P(3), Deger = 2 } };
            var k2 = GrupKaydi.Isle(k1, ozet);

            Assert.That(k2.Uyeler, Has.Count.EqualTo(1));
            var ayse = k2.Uyeler[0];
            Assert.That(ayse.RekorGosterilme, Is.EqualTo(5));
            Assert.That(ayse.ToplamGosterilme, Is.EqualTo(7));
            Assert.That(ayse.Seriler, Has.Count.EqualTo(1));
            Assert.That(ayse.Seriler[0].Seri, Is.EqualTo(2), "üst üste 2. kez Mıknatıs");
        }

        [Test]
        public void Grup_paketi_tekrarsiz_ve_sinirli()
        {
            var ozet = new OyunOzeti
            {
                CurcunaSorulari = new[] { "Aynı soru kim?", "aynı SORU kim?" },
            };
            var kayit = GrupKaydi.Isle(null, ozet);
            Assert.That(kayit.GrupPaketi, Has.Count.EqualTo(1));

            var cok = new List<string>();
            for (int i = 0; i < GrupKaydi.GrupPaketiSiniri + 50; i++) cok.Add("Soru " + i + " kim?");
            kayit = GrupKaydi.Isle(kayit, new OyunOzeti { CurcunaSorulari = cok });
            Assert.That(kayit.GrupPaketi, Has.Count.EqualTo(GrupKaydi.GrupPaketiSiniri));
        }
    }
}
