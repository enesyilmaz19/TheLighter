using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using static Cakmak.Kurallar.Tests.Kurulum;
using static Cakmak.Kurallar.Tests.GeceYardim;

namespace Cakmak.Kurallar.Tests
{
    /// <summary>Çakmak Gecesi (docs/Oyunlar.md, 5). Her test istemediği özel rolleri kapatır: oyundaki roller belli olur.</summary>
    public class CakmakGecesiTestleri
    {
        // ---------------------------------------------------- Dağılım ve gizlilik

        [Test]
        public void Dagilim_kisi_sayisina_gore_her_ozel_rol_en_fazla_bir_kez()
        {
            var tablo = new Dictionary<int, (int katil, int tarafsiz)>
            {
                [6] = (1, 0), [7] = (1, 0), [8] = (2, 1), [10] = (2, 1), [11] = (3, 1), [12] = (3, 1),
                [13] = (3, 2), [14] = (3, 2), [15] = (4, 2), [16] = (4, 2), [17] = (4, 3), [18] = (4, 3),
            };
            foreach (var x in tablo)
                for (int tohum = 1; tohum <= 5; tohum++)
                {
                    var oyun = Yeni(x.Key, tohum, a => a.Oyun = OyunTuru.CakmakGecesi);
                    oyun.OyunuBitir(); // özette herkesin gerçek rolü
                    var roller = oyun.Ozet().Gece.Roller.Select(r => r.Rol).ToList();
                    string t = x.Key + " kişi, tohum " + tohum;
                    Assert.That(roller.Count(r => Oyun.TarafI(r) == Taraf.Katil), Is.EqualTo(x.Value.katil), t);
                    Assert.That(roller.Count(r => Oyun.TarafI(r) == Taraf.Tarafsiz), Is.EqualTo(x.Value.tarafsiz), t);
                    int masum = x.Key - x.Value.katil - x.Value.tarafsiz;
                    Assert.That(roller.Count(r => r == Rol.Masum), Is.EqualTo(masum / 3), t + ": masumların üçte biri düz");
                    Assert.That(roller.Contains(Rol.Koruyucu) && roller.Contains(Rol.Dedektif), Is.True, t);
                    foreach (var r in roller.Where(r => r != Rol.Masum && r != Rol.Katil))
                        Assert.That(roller.Count(y => y == r), Is.EqualTo(1), t + ": iki " + r);
                }
        }

        [Test]
        public void Herkes_kendi_rolunu_katiller_birbirini_ve_rollerini_gorur()
        {
            var oyun = Yeni(12, 3, a => a.Oyun = OyunTuru.CakmakGecesi);
            var katiller = Kisiler(oyun).Where(o => Oyun.TarafI(oyun.GorunumAl(o.Id).Rolun) == Taraf.Katil).ToList();
            Assert.That(katiller.Count, Is.EqualTo(3));
            foreach (var o in Kisiler(oyun))
            {
                var g = oyun.GorunumAl(o.Id);
                Assert.That(g.Oyun, Is.EqualTo(OyunTuru.CakmakGecesi));
                Assert.That(g.ToplamTur, Is.Zero);
                Assert.That(g.Gun, Is.EqualTo(1));
                if (Oyun.TarafI(g.Rolun) != Taraf.Katil) { Assert.That(g.Katiller, Is.Empty); continue; }
                Assert.That(g.Katiller.Select(k => k.Kim), Is.EquivalentTo(katiller.Select(k => k.Id)));
                Assert.That(g.Katiller.All(k => k.Rol == oyun.GorunumAl(k.Kim).Rolun), Is.True, "rolleriyle");
            }
            Assert.That(Kisiler(oyun).All(o => o.AcikRol == null), Is.True);
            Assert.That(oyun.Ozet().Gece, Is.Null, "oyun bitmeden roller yok");
        }

        [Test]
        public void Kapatilan_roller_cikmaz_hepsi_kapaliysa_duz_roller()
        {
            var oyun = YeniGece(18);
            oyun.OyunuBitir();
            var roller = oyun.Ozet().Gece.Roller.Select(r => r.Rol).ToList();
            Assert.That(roller.Count(r => r == Rol.Katil), Is.EqualTo(4));
            Assert.That(roller.Count(r => r == Rol.Masum), Is.EqualTo(14), "tarafsızların yeri de Masum'a kaldı");
        }

        [Test]
        public void Ayarlar_ve_kisi_siniri()
        {
            Sonuc Kur(int kisi, Action<Ayarlar> ayarla, OyunTuru tur = OyunTuru.CakmakGecesi)
            {
                var ayar = new Ayarlar { Oyun = tur };
                ayarla?.Invoke(ayar);
                return Oyun.Kur(ayar, Oyuncular(kisi), 1, out _);
            }
            Assert.That(Kur(5, null), Is.EqualTo(Sonuc.OyuncuSayisiGecersiz));
            Assert.That(Kur(Oyun.GeceEnFazlaOyuncu, null), Is.EqualTo(Sonuc.Tamam));
            Assert.That(Kur(Oyun.GeceEnFazlaOyuncu + 1, null), Is.EqualTo(Sonuc.OyuncuSayisiGecersiz));
            Assert.That(Kur(11, null, OyunTuru.Cakmak), Is.EqualTo(Sonuc.OyuncuSayisiGecersiz), "diğer oyunlar 10'da");
            Assert.That(Kur(6, a => a.GizliGorevler = true), Is.EqualTo(Sonuc.GecersizGirdi));
            Assert.That(Kur(6, a => a.KaosTurlari = true), Is.EqualTo(Sonuc.GecersizGirdi));
            Assert.That(Kur(6, a => a.GecelikKurban = 0), Is.EqualTo(Sonuc.GecersizGirdi));
            Assert.That(Kur(6, a => a.GecelikKurban = Oyun.EnFazlaGecelikKurban + 1), Is.EqualTo(Sonuc.GecersizGirdi));
            Assert.That(Kur(6, a => a.KapaliRoller = new[] { Rol.Masum }), Is.EqualTo(Sonuc.GecersizGirdi));
            Assert.That(Kur(6, a => a.KapaliRoller = new[] { Rol.Kostebek }), Is.EqualTo(Sonuc.GecersizGirdi));
            Assert.That(Kur(6, a => { a.Mod = OyunModu.Curcuna; a.CezaSeviyesi = CezaSeviyesi.Cesur; }), Is.EqualTo(Sonuc.Tamam));
        }

        // ---------------------------------------------------- Gündüz ve linç

        [Test]
        public void Gunduz_canlilarin_yarisi_kadar_tur_tur_sayisi_ayari_yok_sayilir()
        {
            var oyun = YeniGece(7, new Rol[0], a => a.TurSayisi = 1);
            Assert.That(Durum(oyun).KalanGunduzTuru, Is.EqualTo(2), "bu tur hariç");
            Assert.That(GunduzuBitir(oyun), Is.EqualTo(3));
            var d = Durum(oyun);
            Assert.That(d.Oylayanlar.Count, Is.EqualTo(7));
            Assert.That(d.B, Is.Null);
            Assert.That(d.Secilebilir.Count, Is.EqualTo(6), "kendisi dışında herkes");
        }

        [Test]
        public void Linc_cogunlukla_elenir_rol_acilir_esitlikte_kimse_sonra_gece()
        {
            var oyun = YeniGece(6);
            var olaylar = new List<Olay>();
            oyun.OlayOldu += olaylar.Add;
            var masum = Sahipleri(oyun, Rol.Masum)[0];
            GunduzuBitir(oyun);
            Assert.That(oyun.LincOyu(masum, masum), Is.EqualTo(Sonuc.Secilemez));
            Linc(oyun, masum);
            Assert.That(olaylar.Exists(o => o.Tur == OlayTuru.LincBitti && o.Kim == masum), Is.True);
            Assert.That(Kisi(oyun, masum).AcikRol, Is.EqualTo(Rol.Masum));
            Assert.That(Durum(oyun).Asama, Is.EqualTo(Asama.Gece));
            Assert.That(Durum(oyun).Oylayanlar, Has.No.Member(masum));

            oyun = YeniGece(6);
            GunduzuBitir(oyun);
            Assert.That(oyun.LincOyu(P(1), P(2)), Is.EqualTo(Sonuc.Tamam));
            Assert.That(oyun.LincOyu(P(1), P(3)), Is.EqualTo(Sonuc.HakKalmadi));
            Assert.That(oyun.LincOyu(P(2), P(1)), Is.EqualTo(Sonuc.Tamam));
            Saniye(oyun, 61);
            Assert.That(Durum(oyun).Asama, Is.EqualTo(Asama.Gece));
            Assert.That(Kisiler(oyun).All(o => !o.Elendi), Is.True, "eşitlik");
        }

        [Test]
        public void Rol_acilmazsa_elenenin_rolu_gizli_oyun_sonunda_acik()
        {
            var oyun = YeniGece(6, new Rol[0], a => a.RolAcilir = false);
            var masum = Sahipleri(oyun, Rol.Masum)[0];
            GunduzuBitir(oyun);
            Linc(oyun, masum);
            Assert.That(Kisi(oyun, masum).Elendi, Is.True);
            Assert.That(Kisi(oyun, masum).AcikRol, Is.Null);
            oyun.OyunuBitir();
            Assert.That(oyun.Ozet().Gece.Roller.Single(r => r.Kim == masum).Rol, Is.EqualTo(Rol.Masum));
        }

        // ---------------------------------------------------- Gece: katiller ve koruma

        [Test]
        public void Gecelik_kurban_ayari_iki_katil_iki_kisi_oldurur()
        {
            foreach (var (ayar, beklenen) in new[] { (1, 1), (2, 2), (3, 2) }) // en fazla canlı katil sayısı
            {
                var oyun = YeniGece(8, new Rol[0], a => a.GecelikKurban = ayar);
                var katiller = Sahipleri(oyun, Rol.Katil);
                var masumlar = Sahipleri(oyun, Rol.Masum);
                var olaylar = new List<Olay>();
                oyun.OlayOldu += olaylar.Add;
                GeceyeGec(oyun);
                Gece(oyun, (katiller[0], masumlar[0], null), (katiller[1], masumlar[1], null));
                Assert.That(Olenler(olaylar).Count, Is.EqualTo(beklenen), "ayar " + ayar);
                Assert.That(Olenler(olaylar).All(x => x == masumlar[0] || x == masumlar[1]), Is.True);
                Assert.That(olaylar.Last(o => o.Tur != OlayTuru.TurBasladi && o.Tur != OlayTuru.KaosBasladi).Tur, Is.EqualTo(OlayTuru.GeceBitti));
                Assert.That(Durum(oyun).Gun, Is.EqualTo(2));
            }
        }

        [Test]
        public void Katil_katili_secemez_ikinci_hedef_sadece_yetenekli_katilde()
        {
            var oyun = YeniGece(8);
            var katiller = Sahipleri(oyun, Rol.Katil);
            var masum = Sahipleri(oyun, Rol.Masum)[0];
            GeceyeGec(oyun);
            Assert.That(oyun.GeceSecimi(katiller[0], katiller[1]), Is.EqualTo(Sonuc.Secilemez));
            Assert.That(oyun.GeceSecimi(katiller[0], masum, masum), Is.EqualTo(Sonuc.Secilemez), "düz Katil'in yeteneği yok");
            Assert.That(oyun.GeceSecimi(masum, masum), Is.EqualTo(Sonuc.Secilemez), "kendini seçemez");
            Assert.That(oyun.GorunumAl(masum).Katiller, Is.Empty);
            Assert.That(oyun.GeceSecimi(katiller[0], masum), Is.EqualTo(Sonuc.Tamam));
            Assert.That(oyun.GeceSecimi(katiller[0], Sahipleri(oyun, Rol.Masum)[1]), Is.EqualTo(Sonuc.HakKalmadi), "gece bir seçim");
            Assert.That(oyun.GorunumAl(katiller[1]).KatilSecimleri.Single().Hedef, Is.EqualTo(masum), "katiller birbirinin oyunu görür");
            Assert.That(oyun.GorunumAl(masum).KatilSecimleri, Is.Empty);
        }

        [Test]
        public void Koruyucu_kurbani_kurtarir_kendini_oyunda_bir_kez()
        {
            var oyun = YeniGece(6, Rol.Koruyucu);
            var katil = Tek(oyun, Rol.Katil);
            var koruyucu = Tek(oyun, Rol.Koruyucu);
            var masum = Sahipleri(oyun, Rol.Masum)[0];
            var olaylar = new List<Olay>();
            oyun.OlayOldu += olaylar.Add;
            GeceyeGec(oyun);
            Assert.That(oyun.GorunumAl(koruyucu).KalanHakkin, Is.EqualTo(1));
            Gece(oyun, (katil, masum, null), (koruyucu, masum, null));
            Assert.That(Olenler(olaylar), Is.Empty);

            GeceyeGec(oyun);
            Assert.That(oyun.GorunumAl(koruyucu).Secilebilir, Has.Member(koruyucu));
            Gece(oyun, (katil, koruyucu, null), (koruyucu, koruyucu, null));
            Assert.That(Kisi(oyun, koruyucu).Elendi, Is.False);
            Assert.That(oyun.GorunumAl(koruyucu).KalanHakkin, Is.Zero);
            GeceyeGec(oyun);
            Assert.That(oyun.GeceSecimi(koruyucu, koruyucu), Is.EqualTo(Sonuc.Secilemez));
        }

        // ---------------------------------------------------- Bakışlar

        [Test]
        public void Dedektif_katili_supheli_Patronu_temiz_Tuzakcinin_isaretledigini_supheli_gorur()
        {
            var oyun = YeniGece(11, Rol.Dedektif, Rol.Patron, Rol.Tuzakci);
            var dedektif = Tek(oyun, Rol.Dedektif);
            var patron = Tek(oyun, Rol.Patron);
            var tuzakci = Tek(oyun, Rol.Tuzakci);
            var katil = Tek(oyun, Rol.Katil);
            var masum = Sahipleri(oyun, Rol.Masum)[0];

            GeceyeGec(oyun);
            Gece(oyun, (dedektif, patron, null));
            GeceyeGec(oyun);
            Gece(oyun, (dedektif, katil, null));
            GeceyeGec(oyun);
            Gece(oyun, (dedektif, masum, null), (tuzakci, null, masum));
            GeceyeGec(oyun);
            Gece(oyun, (dedektif, masum, null));

            var b = oyun.GorunumAl(dedektif).Bulgularin;
            Assert.That(b.Select(x => (x.Gun, x.Kim, x.Supheli)), Is.EqualTo(new (int, OyuncuId, bool?)[]
                { (1, patron, false), (2, katil, true), (3, masum, true), (4, masum, false) }));
            Assert.That(Kisiler(oyun).Where(o => o.Id != dedektif).All(o => !oyun.GorunumAl(o.Id).Bulgularin.Any()), Is.True, "sadece sahibine");
        }

        [Test]
        public void Casus_tam_rolu_Bekci_ugrayanlari_gorur()
        {
            var oyun = YeniGece(8, Rol.Casus, Rol.Bekci, Rol.Koruyucu);
            var casus = Tek(oyun, Rol.Casus);
            var katil = Tek(oyun, Rol.Katil);
            var bekci = Tek(oyun, Rol.Bekci);
            var koruyucu = Tek(oyun, Rol.Koruyucu);
            var masumlar = Sahipleri(oyun, Rol.Masum);
            GeceyeGec(oyun);
            // Katil masumlar[0]'a oy verir (kurban), Koruyucu onu korur, bir Masum da onu "düşünür" (uğramaz).
            Gece(oyun, (katil, masumlar[0], null), (casus, null, koruyucu), (koruyucu, masumlar[0], null),
                (masumlar[1], masumlar[0], null), (bekci, masumlar[0], null));
            Assert.That(oyun.GorunumAl(casus).Bulgularin.Single().Rol, Is.EqualTo(Rol.Koruyucu));
            Assert.That(oyun.GorunumAl(bekci).Bulgularin.Single().Ziyaretciler, Is.EquivalentTo(new[] { katil, koruyucu }));
            Assert.That(Kisi(oyun, masumlar[0]).Elendi, Is.False, "korundu");
        }

        [Test]
        public void Casus_Deliyi_Deli_olarak_gorur()
        {
            var oyun = YeniGece(6, Rol.Casus, Rol.Deli);
            var casus = Tek(oyun, Rol.Casus);
            var deli = Kisiler(oyun).Single(o => o.Id != casus && oyun.GorunumAl(o.Id).Rolun != Rol.Masum).Id;
            GeceyeGec(oyun);
            Gece(oyun, (casus, null, deli));
            Assert.That(oyun.GorunumAl(casus).Bulgularin.Single().Rol, Is.EqualTo(Rol.Deli));
        }

        [Test]
        public void Falci_grubu_canlilarin_ucte_biri_secilen_dahil_hakki_sinirli()
        {
            var oyun = YeniGece(6, Rol.Falci);
            var falci = Tek(oyun, Rol.Falci);
            var katil = Tek(oyun, Rol.Katil);
            Assert.That(oyun.GorunumAl(falci).KalanHakkin, Is.EqualTo(2));
            GeceyeGec(oyun);
            Gece(oyun, (falci, katil, null));
            var bulgu = oyun.GorunumAl(falci).Bulgularin.Single();
            Assert.That(bulgu.Grup.Count, Is.EqualTo(3), "6 canlı: en az 3");
            Assert.That(bulgu.Grup, Has.Member(katil));
            Assert.That(bulgu.Grup, Has.No.Member(falci));
            Assert.That(bulgu.KotuVar, Is.True);
            GeceyeGec(oyun);
            Gece(oyun, (falci, Sahipleri(oyun, Rol.Masum)[0], null));
            Assert.That(oyun.GorunumAl(falci).KalanHakkin, Is.Zero);
            GeceyeGec(oyun);
            Assert.That(oyun.GorunumAl(falci).Secilebilir, Is.Empty);
            Assert.That(oyun.GeceSecimi(falci, katil), Is.EqualTo(Sonuc.Secilemez));

            var buyuk = YeniGece(15, Rol.Falci);
            Assert.That(buyuk.GorunumAl(Tek(buyuk, Rol.Falci)).KalanHakkin, Is.EqualTo(3), "12+ kişide 3 hak");
            GeceyeGec(buyuk);
            Gece(buyuk, (Tek(buyuk, Rol.Falci), Sahipleri(buyuk, Rol.Katil)[0], null));
            Assert.That(buyuk.GorunumAl(Tek(buyuk, Rol.Falci)).Bulgularin.Single().Grup.Count, Is.EqualTo(5), "15 canlının üçte biri");
        }

        [Test]
        public void Deli_kendini_sandigi_rolde_gorur_korumasi_islemez_sonda_Deli_acilir()
        {
            for (int tohum = 1; tohum <= 40; tohum++)
            {
                var oyun = YeniGece(6, new[] { Rol.Deli }, null, tohum);
                var deli = Kisiler(oyun).Single(o => oyun.GorunumAl(o.Id).Rolun != Rol.Masum && oyun.GorunumAl(o.Id).Rolun != Rol.Katil).Id;
                var sandigi = oyun.GorunumAl(deli).Rolun;
                Assert.That(new[] { Rol.Koruyucu, Rol.Dedektif, Rol.Bekci, Rol.Falci }, Has.Member(sandigi));
                if (sandigi != Rol.Koruyucu) continue;

                var katil = Tek(oyun, Rol.Katil);
                var masum = Sahipleri(oyun, Rol.Masum)[0];
                Assert.That(oyun.GorunumAl(deli).KalanHakkin, Is.EqualTo(1), "Koruyucu gibi görünür");
                GeceyeGec(oyun);
                Gece(oyun, (katil, masum, null), (deli, masum, null));
                Assert.That(Kisi(oyun, masum).Elendi, Is.True, "Deli'nin koruması işlemez");
                oyun.OyunuBitir();
                Assert.That(oyun.Ozet().Gece.Roller.Single(r => r.Kim == deli).Rol, Is.EqualTo(Rol.Deli));
                return;
            }
            Assert.Fail("Koruyucu sanan Deli çıkmadı");
        }

        // ---------------------------------------------------- Öldüren masumlar

        [Test]
        public void Serif_katili_vurursa_yasar_masumu_vurursa_kendisi_de_olur()
        {
            var oyun = YeniGece(8, Rol.Serif);
            var serif = Tek(oyun, Rol.Serif);
            var katiller = Sahipleri(oyun, Rol.Katil);
            var masum = Sahipleri(oyun, Rol.Masum)[0];
            GeceyeGec(oyun);
            Gece(oyun, (serif, katiller[0], null));
            Assert.That(Kisi(oyun, katiller[0]).Elendi, Is.True);
            Assert.That(Kisi(oyun, serif).Elendi, Is.False);
            GeceyeGec(oyun);
            Gece(oyun, (serif, masum, null));
            Assert.That(Kisi(oyun, masum).Elendi && Kisi(oyun, serif).Elendi, Is.True);
        }

        [Test]
        public void Kanunsuz_oyunda_bir_kursun()
        {
            var oyun = YeniGece(8, Rol.Kanunsuz);
            var kanunsuz = Tek(oyun, Rol.Kanunsuz);
            var katiller = Sahipleri(oyun, Rol.Katil);
            GeceyeGec(oyun);
            Gece(oyun, (kanunsuz, katiller[0], null));
            Assert.That(Kisi(oyun, katiller[0]).Elendi, Is.True);
            GeceyeGec(oyun);
            Assert.That(oyun.GorunumAl(kanunsuz).KalanHakkin, Is.Zero);
            Assert.That(oyun.GeceSecimi(kanunsuz, katiller[1]), Is.EqualTo(Sonuc.Secilemez));
        }

        [Test]
        public void Kulak_gume_giden_soruyu_sadece_gume_ekraninda_duyar()
        {
            var oyun = YeniGece(6, Rol.Kulak);
            var kulak = Tek(oyun, Rol.Kulak);
            SorVeSec(oyun, "Gizli suçlama kim?", out var a, out var b, out var c);
            TkmOyna(oyun, b, c, cKazansin: false);
            Assert.That(Durum(oyun).Asama, Is.EqualTo(Asama.Gume));
            Assert.That(oyun.GorunumAl(kulak).KulakSorusu, Is.EqualTo("Gizli suçlama kim?"));
            foreach (var o in Kisiler(oyun).Where(o => o.Id != kulak))
                Assert.That(oyun.GorunumAl(o.Id).KulakSorusu, Is.Null);
            Saniye(oyun, 7);
            Assert.That(oyun.GorunumAl(kulak).KulakSorusu, Is.Null, "ekran bitince silinir");
            Assert.That(oyun.Ozet().IfsaOlanlar, Is.Empty);

            SorVeSec(oyun, "Açık soru kim?", out _, out b, out c);
            TkmOyna(oyun, b, c, cKazansin: true);
            Assert.That(oyun.GorunumAl(kulak).KulakSorusu, Is.Null, "ifşa olan Kulak'a ayrıca gelmez");
        }

        // ---------------------------------------------------- Katil yetenekleri

        [Test]
        public void Susturulan_ertesi_gun_yazamaz_oy_veremez_herkes_gorur()
        {
            var oyun = YeniGece(6, Rol.Susturucu);
            var susturucu = Tek(oyun, Rol.Susturucu);
            var masum = Sahipleri(oyun, Rol.Masum)[0];
            GeceyeGec(oyun);
            Gece(oyun, (susturucu, null, masum));
            Assert.That(Kisi(oyun, masum).Susturuldu, Is.True);
            Assert.That(oyun.SohbeteYazabilir(masum), Is.False);
            GunduzuBitir(oyun);
            Assert.That(Durum(oyun).Oylayanlar, Has.No.Member(masum));
            Assert.That(oyun.LincOyu(masum, susturucu), Is.EqualTo(Sonuc.SiraSendeDegil));
            Saniye(oyun, 61);
            Assert.That(Kisi(oyun, masum).Susturuldu, Is.False, "susturma o günün");
            Assert.That(Durum(oyun).Oylayanlar, Has.Member(masum), "gece seçer");
        }

        [Test]
        public void Palyaconun_kuklasinin_linc_oyu_Palyaconun_oyuna_sayilir()
        {
            var oyun = YeniGece(8, Rol.Palyaco);
            var palyaco = Tek(oyun, Rol.Palyaco);
            var masumlar = Sahipleri(oyun, Rol.Masum);
            GeceyeGec(oyun);
            Gece(oyun, (palyaco, null, masumlar[0]));
            GunduzuBitir(oyun);
            Assert.That(oyun.LincOyu(palyaco, masumlar[1]), Is.EqualTo(Sonuc.Tamam));
            Assert.That(oyun.LincOyu(masumlar[0], masumlar[2]), Is.EqualTo(Sonuc.Tamam));
            Saniye(oyun, 61);
            Assert.That(Kisi(oyun, masumlar[1]).Elendi, Is.True, "kuklanın oyu da masumlar[1]'e: 2–0");
        }

        [Test]
        public void Temizlikcinin_temizledigi_olunun_rolu_acilmaz()
        {
            var oyun = YeniGece(6, Rol.Temizlikci);
            var temizlikci = Tek(oyun, Rol.Temizlikci);
            var masum = Sahipleri(oyun, Rol.Masum)[0];
            Assert.That(oyun.GorunumAl(temizlikci).KalanHakkin, Is.EqualTo(2));
            GeceyeGec(oyun);
            Gece(oyun, (temizlikci, masum, masum));
            Assert.That(Kisi(oyun, masum).Elendi, Is.True);
            Assert.That(Kisi(oyun, masum).AcikRol, Is.Null, "rol açılması açık ama temizlendi");
            Assert.That(oyun.GorunumAl(temizlikci).KalanHakkin, Is.EqualTo(1));
        }

        [Test]
        public void Suikastci_bilirse_hedef_bilemezse_kendisi_olur_bir_kez_ve_sadece_lincte()
        {
            var oyun = YeniGece(8, Rol.Suikastci);
            var suikastci = Tek(oyun, Rol.Suikastci);
            var masumlar = Sahipleri(oyun, Rol.Masum);
            var olaylar = new List<Olay>();
            oyun.OlayOldu += olaylar.Add;
            Assert.That(oyun.Suikast(suikastci, masumlar[0], Rol.Masum), Is.EqualTo(Sonuc.YanlisAsama), "gündüz turunda değil");
            GunduzuBitir(oyun);
            Assert.That(oyun.LincOyu(masumlar[1], masumlar[0]), Is.EqualTo(Sonuc.Tamam));
            Assert.That(oyun.Suikast(masumlar[1], masumlar[0], Rol.Masum), Is.EqualTo(Sonuc.SiraSendeDegil));
            Assert.That(oyun.Suikast(suikastci, masumlar[0], Rol.Kostebek), Is.EqualTo(Sonuc.GecersizGirdi));
            Assert.That(oyun.Suikast(suikastci, masumlar[0], Rol.Masum), Is.EqualTo(Sonuc.Tamam));
            Assert.That(olaylar.Exists(o => o.Tur == OlayTuru.Suikast && o.Kim == masumlar[0]), Is.True);
            Assert.That(oyun.GorunumAl(masumlar[1]).OyVerdin, Is.False, "ölüye verilen oy düştü, yeniden verir");
            Assert.That(Durum(oyun).Oylayanlar, Has.No.Member(masumlar[0]));
            Assert.That(oyun.Suikast(suikastci, masumlar[2], Rol.Masum), Is.EqualTo(Sonuc.HakKalmadi));

            oyun = YeniGece(8, Rol.Suikastci);
            suikastci = Tek(oyun, Rol.Suikastci);
            GunduzuBitir(oyun);
            Assert.That(oyun.Suikast(suikastci, Sahipleri(oyun, Rol.Masum)[0], Rol.Koruyucu), Is.EqualTo(Sonuc.Tamam));
            Assert.That(Kisi(oyun, suikastci).Elendi, Is.True, "bilemedi");
        }

        [Test]
        public void Vampirin_isirdigi_masum_sabah_katil_olur_korunan_olmaz()
        {
            var oyun = YeniGece(6, Rol.Vampir);
            var vampir = Tek(oyun, Rol.Vampir);
            var masum = Sahipleri(oyun, Rol.Masum)[0];
            GeceyeGec(oyun);
            Gece(oyun, (vampir, null, masum));
            Assert.That(oyun.GorunumAl(masum).Rolun, Is.EqualTo(Rol.Katil));
            Assert.That(oyun.GorunumAl(vampir).Katiller.Select(k => k.Kim), Is.EquivalentTo(new[] { vampir, masum }));
            Assert.That(oyun.GorunumAl(vampir).KalanHakkin, Is.Zero);
            GeceyeGec(oyun);
            Assert.That(oyun.GorunumAl(vampir).IkinciSecilebilir, Is.Empty, "hakkı bitti");

            oyun = YeniGece(6, Rol.Vampir, Rol.Koruyucu);
            vampir = Tek(oyun, Rol.Vampir);
            masum = Sahipleri(oyun, Rol.Masum)[0];
            GeceyeGec(oyun);
            Gece(oyun, (vampir, null, masum), (Tek(oyun, Rol.Koruyucu), masum, null));
            Assert.That(oyun.GorunumAl(masum).Rolun, Is.EqualTo(Rol.Masum));
        }

        // ---------------------------------------------------- Tarafsızlar

        [Test]
        public void Soytari_linc_edilirse_kazanir_oyun_biter()
        {
            var oyun = YeniGece(8, Rol.Soytari);
            var soytari = Tek(oyun, Rol.Soytari);
            GunduzuBitir(oyun);
            Linc(oyun, soytari);
            var s = oyun.Ozet().Gece;
            Assert.That(s.Kazanan, Is.EqualTo(GeceKazanan.Soytari));
            Assert.That(s.Kazananlar, Is.EqualTo(new[] { soytari }));
        }

        [Test]
        public void Cellat_hedefi_lince_giderse_kazanir_gece_olurse_Soytari_olur()
        {
            var oyun = YeniGece(8, Rol.Cellat);
            var cellat = Tek(oyun, Rol.Cellat);
            var hedef = oyun.GorunumAl(cellat).CellatHedefin.Value;
            Assert.That(Oyun.TarafI(oyun.GorunumAl(hedef).Rolun), Is.EqualTo(Taraf.Masum));
            Assert.That(Kisiler(oyun).Where(o => o.Id != cellat).All(o => oyun.GorunumAl(o.Id).CellatHedefin == null), Is.True);
            GunduzuBitir(oyun);
            Linc(oyun, hedef);
            var s = oyun.Ozet().Gece;
            Assert.That((s.Kazanan.Value, s.Kazananlar.Single()), Is.EqualTo((GeceKazanan.Cellat, cellat)));
            Assert.That(s.Roller.Single(r => r.Kim == cellat).Rol, Is.EqualTo(Rol.Cellat));

            oyun = YeniGece(8, Rol.Cellat);
            cellat = Tek(oyun, Rol.Cellat);
            hedef = oyun.GorunumAl(cellat).CellatHedefin.Value;
            GeceyeGec(oyun);
            Gece(oyun, (Sahipleri(oyun, Rol.Katil)[0], hedef, null), (Sahipleri(oyun, Rol.Katil)[1], hedef, null));
            Assert.That(oyun.GorunumAl(cellat).Rolun, Is.EqualTo(Rol.Soytari));
            Assert.That(oyun.GorunumAl(cellat).CellatHedefin, Is.Null);
        }

        [Test]
        public void Seri_Katil_oldurdugunun_rolu_gizli_katillerin_ilk_saldirisindan_kurtulur()
        {
            var oyun = YeniGece(8, Rol.SeriKatil);
            var sk = Tek(oyun, Rol.SeriKatil);
            var katiller = Sahipleri(oyun, Rol.Katil);
            var masum = Sahipleri(oyun, Rol.Masum)[0];
            Assert.That(oyun.GorunumAl(sk).KalanHakkin, Is.EqualTo(1));
            GeceyeGec(oyun);
            Gece(oyun, (sk, masum, null), (katiller[0], sk, null), (katiller[1], sk, null));
            Assert.That(Kisi(oyun, masum).Elendi, Is.True);
            Assert.That(Kisi(oyun, masum).AcikRol, Is.Null, "Seri Katil'in kurbanı gizli");
            Assert.That(Kisi(oyun, sk).Elendi, Is.False, "kaçtı");
            Assert.That(oyun.GorunumAl(sk).KalanHakkin, Is.Zero);
            GeceyeGec(oyun);
            Gece(oyun, (katiller[0], sk, null), (katiller[1], sk, null));
            Assert.That(Kisi(oyun, sk).Elendi, Is.True);
        }

        [Test]
        public void Hirsiz_rolu_sabah_alir_calinan_duz_role_duser()
        {
            var oyun = YeniGece(8, Rol.Hirsiz, Rol.Koruyucu);
            var hirsiz = Tek(oyun, Rol.Hirsiz);
            var koruyucu = Tek(oyun, Rol.Koruyucu);
            GeceyeGec(oyun);
            Gece(oyun, (hirsiz, koruyucu, null));
            Assert.That(oyun.GorunumAl(hirsiz).Rolun, Is.EqualTo(Rol.Koruyucu));
            Assert.That(oyun.GorunumAl(hirsiz).KalanHakkin, Is.EqualTo(1));
            Assert.That(oyun.GorunumAl(koruyucu).Rolun, Is.EqualTo(Rol.Masum));
        }

        [Test]
        public void Bombaci_bomba_koyar_kendini_secince_patlatir()
        {
            var oyun = YeniGece(8, Rol.Bombaci);
            var bombaci = Tek(oyun, Rol.Bombaci);
            var masumlar = Sahipleri(oyun, Rol.Masum);
            GeceyeGec(oyun);
            Assert.That(oyun.GorunumAl(bombaci).Secilebilir, Has.No.Member(bombaci), "bomba yokken patlatamaz");
            Gece(oyun, (bombaci, masumlar[0], null));
            GeceyeGec(oyun);
            Gece(oyun, (bombaci, masumlar[1], null));
            Assert.That(oyun.GorunumAl(bombaci).Bombalilar, Is.EquivalentTo(new[] { masumlar[0], masumlar[1] }));
            Assert.That(oyun.GorunumAl(masumlar[0]).Bombalilar, Is.Empty);
            Assert.That(Kisiler(oyun).All(o => !o.Elendi), Is.True, "bomba koymak öldürmez");
            GeceyeGec(oyun);
            Gece(oyun, (bombaci, bombaci, null));
            Assert.That(Kisi(oyun, masumlar[0]).Elendi && Kisi(oyun, masumlar[1]).Elendi, Is.True);
            Assert.That(oyun.GorunumAl(bombaci).Bombalilar, Is.Empty);
        }

        // ---------------------------------------------------- Kazanma

        [Test]
        public void Masumlar_katiller_seri_katil_berabere_ve_hayatta_kalan()
        {
            // Katil linçe gider: masumlar (ölüler dahil) kazanır, hayattaki Hayatta Kalan da.
            var oyun = YeniGece(8, Rol.HayattaKalan);
            var katiller = Sahipleri(oyun, Rol.Katil);
            var hk = Tek(oyun, Rol.HayattaKalan);
            oyun.OyuncuCikti(katiller[0]);
            GunduzuBitir(oyun);
            Linc(oyun, katiller[1]);
            var s = oyun.Ozet().Gece;
            Assert.That(s.Kazanan, Is.EqualTo(GeceKazanan.Masumlar));
            Assert.That(s.Kazananlar, Is.EquivalentTo(Sahipleri(oyun, Rol.Masum).Append(hk)));

            // Masumlar çıkar: katiller sayıca eşitlenince kazanır.
            oyun = YeniGece(6);
            var masumlar = Sahipleri(oyun, Rol.Masum);
            for (int i = 0; i < 3; i++) oyun.OyuncuCikti(masumlar[i]);
            Assert.That(Durum(oyun).Asama, Is.Not.EqualTo(Asama.OyunSonu), "1 katile 2 masum");
            oyun.OyuncuCikti(masumlar[3]);
            Assert.That(oyun.Ozet().Gece.Kazanan, Is.EqualTo(GeceKazanan.Katiller));

            // Seri Katil ve bir masum kaldı: Seri Katil kazanır. Katil ve Seri Katil kaldı: berabere.
            oyun = YeniGece(8, Rol.SeriKatil);
            var sk = Tek(oyun, Rol.SeriKatil);
            foreach (var k in Sahipleri(oyun, Rol.Katil)) oyun.OyuncuCikti(k);
            foreach (var m in Sahipleri(oyun, Rol.Masum).Skip(1)) oyun.OyuncuCikti(m);
            s = oyun.Ozet().Gece;
            Assert.That((s.Kazanan.Value, s.Kazananlar.Single()), Is.EqualTo((GeceKazanan.SeriKatil, sk)));

            oyun = YeniGece(8, Rol.SeriKatil);
            oyun.OyuncuCikti(Sahipleri(oyun, Rol.Katil)[0]);
            foreach (var m in Sahipleri(oyun, Rol.Masum)) oyun.OyuncuCikti(m);
            s = oyun.Ozet().Gece;
            Assert.That(s.Kazanan, Is.EqualTo(GeceKazanan.Berabere));
            Assert.That(s.Kazananlar, Is.Empty);

            oyun = YeniGece(6);
            oyun.OyunuBitir();
            Assert.That(oyun.Ozet().Gece.Kazanan, Is.Null, "kurucu bitirdi");
        }

        [Test]
        public void Hirsiz_calmadan_oyun_biterse_kaybeder()
        {
            var oyun = YeniGece(8, Rol.Hirsiz);
            var hirsiz = Tek(oyun, Rol.Hirsiz);
            foreach (var k in Sahipleri(oyun, Rol.Katil)) oyun.OyuncuCikti(k);
            var s = oyun.Ozet().Gece;
            Assert.That(s.Kazanan, Is.EqualTo(GeceKazanan.Masumlar));
            Assert.That(s.Kazananlar, Has.No.Member(hirsiz));
        }

        // ---------------------------------------------------- Süreler, çıkış, sohbet

        [Test]
        public void Gece_suresi_dolar_secmeyen_pas_cikan_beklenmez_elden_elede_sure_bastan()
        {
            var oyun = YeniGece(6);
            GeceyeGec(oyun);
            Saniye(oyun, 61);
            Assert.That(Durum(oyun).Asama, Is.EqualTo(Asama.SoruSorma));
            Assert.That(Kisiler(oyun).All(o => !o.Elendi), Is.True);

            oyun = YeniGece(7);
            var masum = Sahipleri(oyun, Rol.Masum)[0];
            GeceyeGec(oyun);
            foreach (var kim in Durum(oyun).Oylayanlar.ToList())
                if (kim != masum) oyun.GeceSecimi(kim, null);
            Assert.That(Durum(oyun).Asama, Is.EqualTo(Asama.Gece));
            oyun.OyuncuCikti(masum);
            Assert.That(Durum(oyun).Asama, Is.Not.EqualTo(Asama.Gece));

            oyun = YeniGece(6, new Rol[0], a => a.TekCihaz = true);
            GeceyeGec(oyun);
            Saniye(oyun, 50);
            oyun.GeceSecimi(Durum(oyun).Oylayanlar[0], null);
            Saniye(oyun, 50);
            Assert.That(Durum(oyun).Asama, Is.EqualTo(Asama.Gece));
        }

        [Test]
        public void Canlilar_4un_altina_dusunce_cakmak_turu_yok()
        {
            var oyun = YeniGece(6);
            var masumlar = Sahipleri(oyun, Rol.Masum).Where(m => m != SiradakiA(oyun)).ToList(); // A çıkarsa tur yanar
            for (int i = 0; i < 3; i++) oyun.OyuncuCikti(masumlar[i]); // 3 canlı: katil ve iki masum
            SorVeSec(oyun, "Kim?", out _, out var b, out var c);
            TkmOyna(oyun, b, c, cKazansin: true);
            Saniye(oyun, 7);
            Assert.That(Durum(oyun).Asama, Is.EqualTo(Asama.Linc), "kalan gündüz turları atlandı");
            Saniye(oyun, 61);
            Saniye(oyun, 61);
            Assert.That(Durum(oyun).Asama, Is.EqualTo(Asama.Linc), "yeni gün de doğrudan linç");
        }

        [Test]
        public void Sohbet_katil_kanali_gece_oluler_kanali_susturulan_ve_gece_genele_yazamaz()
        {
            var oyun = YeniGece(8);
            var katiller = Sahipleri(oyun, Rol.Katil);
            var masumlar = Sahipleri(oyun, Rol.Masum);
            Assert.That(oyun.KanalAlicilari(katiller[0], SohbetKanali.Katil), Is.Null, "gündüz kapalı");
            GunduzuBitir(oyun);
            Linc(oyun, masumlar[0]);
            Assert.That(oyun.SohbeteYazabilir(katiller[0]), Is.False, "gece genel kapalı");
            Assert.That(oyun.KanalAlicilari(katiller[1], SohbetKanali.Katil), Is.EquivalentTo(katiller));
            Assert.That(oyun.KanalAlicilari(masumlar[1], SohbetKanali.Katil), Is.Null);
            Assert.That(oyun.KanalAlicilari(masumlar[0], SohbetKanali.Oluler), Is.EqualTo(new[] { masumlar[0] }));
            Assert.That(oyun.KanalAlicilari(masumlar[1], SohbetKanali.Oluler), Is.Null);
            Saniye(oyun, 61);
            Assert.That(oyun.SohbeteYazabilir(masumlar[1]), Is.True, "gündüz canlı yazar");
            Assert.That(oyun.SohbeteYazabilir(masumlar[0]), Is.False, "ölü yazamaz");
            oyun.OyunuBitir();
            Assert.That(oyun.SohbeteYazabilir(masumlar[0]), Is.True, "oyun bitince herkes");
        }

        [Test]
        public void Diger_oyunlarda_gece_alanlari_bos()
        {
            var oyun = Yeni();
            Assert.That(oyun.SohbeteYazabilir(P(1)), Is.True);
            Assert.That(oyun.KanalAlicilari(P(1), SohbetKanali.Oluler), Is.Null);
            Assert.That(oyun.LincOyu(P(1), P(2)), Is.EqualTo(Sonuc.YanlisAsama));
            Assert.That(oyun.GeceSecimi(P(1), P(2)), Is.EqualTo(Sonuc.YanlisAsama));
            Assert.That(oyun.Suikast(P(1), P(2), Rol.Masum), Is.EqualTo(Sonuc.YanlisAsama));
            var g = Durum(oyun);
            Assert.That(g.Gun, Is.Zero);
            Assert.That(g.Rolun, Is.EqualTo(Rol.Yok));
            Assert.That(g.KalanHakkin, Is.Null);
            oyun.OyunuBitir();
            Assert.That(oyun.Ozet().Gece, Is.Null);
        }
    }
}
