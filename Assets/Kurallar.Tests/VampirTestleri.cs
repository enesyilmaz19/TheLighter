using System;
using System.Collections.Generic;
using NUnit.Framework;
using static Cakmak.Kurallar.Tests.Kurulum;

namespace Cakmak.Kurallar.Tests
{
    /// <summary>Vampir Köylü (docs/Oyunlar.md, 5).</summary>
    public class VampirTestleri
    {
        static Oyun YeniVampir(int kisi = 6, int tohum = 1, Action<Ayarlar> ayarla = null) =>
            Yeni(kisi, tohum, a =>
            {
                a.Oyun = OyunTuru.Vampir;
                ayarla?.Invoke(a);
            });

        static List<OyuncuId> Sahipleri(Oyun oyun, Rol rol)
        {
            var liste = new List<OyuncuId>();
            foreach (var o in Durum(oyun).Oyuncular)
                if (oyun.GorunumAl(o.Id).Rolun == rol) liste.Add(o.Id);
            return liste;
        }

        static OyuncuId Tek(Oyun oyun, Rol rol) => Sahipleri(oyun, rol)[0];

        static List<OyuncuDurumu> Kisiler(Oyun oyun) => new List<OyuncuDurumu>(Durum(oyun).Oyuncular);

        static OyuncuDurumu Kisi(Oyun oyun, OyuncuId id) => Kisiler(oyun).Find(o => o.Id == id);

        /// <summary>Gündüzün kalan turlarını oynar (C kazanır), linç başlayınca durur.</summary>
        static int GunduzuBitir(Oyun oyun)
        {
            int tur = 0;
            while (Durum(oyun).Asama != Asama.Linc)
            {
                Assert.That(Durum(oyun).Asama, Is.EqualTo(Asama.SoruSorma));
                SorVeSec(oyun, "Sence vampir kim?", out _, out var b, out var c);
                TkmOyna(oyun, b, c, cKazansin: true);
                Saniye(oyun, 7);
                tur++;
            }
            return tur;
        }

        /// <summary>Linçte herkes <paramref name="hedef"/>'e oy verir, hedef kendisi başkasına.</summary>
        static void Linc(Oyun oyun, OyuncuId hedef)
        {
            var oylayanlar = new List<OyuncuId>(Durum(oyun).Oylayanlar);
            foreach (var kim in oylayanlar)
                Assert.That(oyun.LincOyu(kim, kim == hedef ? Ilk(oylayanlar, hedef) : hedef), Is.EqualTo(Sonuc.Tamam));
        }

        /// <summary>Gece: vampirler kurbanı, doktor korunanı, diğerleri ilk uygun kişiyi seçer.</summary>
        static void Gece(Oyun oyun, OyuncuId kurban, OyuncuId? korunan = null)
        {
            foreach (var kim in new List<OyuncuId>(Durum(oyun).Oylayanlar))
            {
                var g = oyun.GorunumAl(kim);
                var hedef = g.Rolun == Rol.Vampir ? kurban
                    : g.Rolun == Rol.Doktor && korunan.HasValue ? korunan.Value
                    : Ilk(g.Secilebilir, kurban); // doktor kurbanı rastlantıyla korumasın
                Assert.That(oyun.GeceSecimi(kim, hedef), Is.EqualTo(Sonuc.Tamam), g.Rolun.ToString());
            }
        }

        // ---------------------------------------------------- Kurulum ve gizlilik

        [Test]
        public void Roller_kisi_sayisina_gore_herkes_sadece_kendi_rolunu_vampirler_birbirini_gorur()
        {
            foreach (var (kisi, vampir) in new[] { (6, 1), (7, 1), (8, 2), (10, 2) })
                for (int tohum = 1; tohum <= 10; tohum++)
                {
                    var oyun = YeniVampir(kisi, tohum);
                    var vampirler = Sahipleri(oyun, Rol.Vampir);
                    Assert.That(vampirler.Count, Is.EqualTo(vampir));
                    Assert.That(Sahipleri(oyun, Rol.Doktor).Count, Is.EqualTo(1));
                    Assert.That(Sahipleri(oyun, Rol.Gozcu).Count, Is.EqualTo(1));
                    Assert.That(Sahipleri(oyun, Rol.Koylu).Count, Is.EqualTo(kisi - vampir - 2));
                    for (int i = 1; i <= kisi; i++)
                    {
                        var g = oyun.GorunumAl(P(i));
                        Assert.That(g.Vampirler, Is.EqualTo(g.Rolun == Rol.Vampir ? vampirler : new List<OyuncuId>()));
                        Assert.That(new List<OyuncuDurumu>(g.Oyuncular).TrueForAll(o => o.AcikRol == null), Is.True);
                        Assert.That(g.ToplamTur, Is.Zero);
                        Assert.That(g.Gun, Is.EqualTo(1));
                    }
                    Assert.That(oyun.Ozet().Vampir, Is.Null);
                }
        }

        [Test]
        public void En_az_6_kisi_gorev_ve_kaos_olmaz()
        {
            Assert.That(Oyun.Kur(new Ayarlar { Oyun = OyunTuru.Vampir }, Oyuncular(5), 1, out _), Is.EqualTo(Sonuc.OyuncuSayisiGecersiz));
            Assert.That(Oyun.Kur(new Ayarlar { Oyun = OyunTuru.Vampir, GizliGorevler = true }, Oyuncular(6), 1, out _), Is.EqualTo(Sonuc.GecersizGirdi));
            Assert.That(Oyun.Kur(new Ayarlar { Oyun = OyunTuru.Vampir, KaosTurlari = true }, Oyuncular(6), 1, out _), Is.EqualTo(Sonuc.GecersizGirdi));
            YeniVampir(ayarla: a => { a.Mod = OyunModu.Curcuna; a.CezaSeviyesi = CezaSeviyesi.Cesur; });
        }

        // ---------------------------------------------------- Gündüz ve linç

        [Test]
        public void Gunduz_canlilarin_yarisi_kadar_tur_tur_sayisi_ayari_yok_sayilir()
        {
            var oyun = YeniVampir(7, ayarla: a => a.TurSayisi = 1);
            var olaylar = new List<Olay>();
            oyun.OlayOldu += olaylar.Add;
            Assert.That(Durum(oyun).KalanGunduzTuru, Is.EqualTo(2), "bu tur hariç");
            Assert.That(GunduzuBitir(oyun), Is.EqualTo(3));
            Assert.That(olaylar.Exists(o => o.Tur == OlayTuru.LincBasladi), Is.True);
            var d = Durum(oyun);
            Assert.That(d.Oylayanlar.Count, Is.EqualTo(7));
            Assert.That(d.B, Is.Null);
            Assert.That(d.Secilebilir.Count, Is.EqualTo(6), "kendisi dışında herkes");
        }

        [Test]
        public void Linc_cogunlukla_elenir_rol_acilir_gece_baslar_kurallar()
        {
            var oyun = YeniVampir();
            var koylu = Tek(oyun, Rol.Koylu);
            var olaylar = new List<Olay>();
            oyun.OlayOldu += olaylar.Add;
            GunduzuBitir(oyun);
            Assert.That(oyun.LincOyu(koylu, koylu), Is.EqualTo(Sonuc.Secilemez), "kendine oy yok");
            Assert.That(oyun.GeceSecimi(koylu, P(1)), Is.EqualTo(Sonuc.YanlisAsama));
            var baska = Ilk(Durum(oyun).Oylayanlar, koylu);
            Assert.That(oyun.LincOyu(baska, koylu), Is.EqualTo(Sonuc.Tamam));
            Assert.That(oyun.LincOyu(baska, koylu), Is.EqualTo(Sonuc.HakKalmadi));
            Assert.That(Durum(oyun).OyKullanan, Is.EqualTo(1));
            foreach (var kim in Durum(oyun).Oylayanlar)
                if (kim != baska) oyun.LincOyu(kim, kim == koylu ? baska : koylu);

            Assert.That(olaylar.Exists(o => o.Tur == OlayTuru.LincBitti && o.Kim == koylu), Is.True);
            Assert.That(Kisi(oyun, koylu).Elendi, Is.True);
            Assert.That(Kisi(oyun, koylu).AcikRol, Is.EqualTo(Rol.Koylu));
            var d = Durum(oyun);
            Assert.That(d.Asama, Is.EqualTo(Asama.Gece));
            Assert.That(olaylar.Exists(o => o.Tur == OlayTuru.GeceBasladi), Is.True);
            Assert.That(d.Oylayanlar, Has.No.Member(koylu), "ölü seçmez");
            Assert.That(oyun.GeceSecimi(koylu, baska), Is.EqualTo(Sonuc.SiraSendeDegil));
        }

        [Test]
        public void Linc_esitlikte_kimse_elenmez_suresi_dolunca_oy_vermeyen_saymaz()
        {
            var oyun = YeniVampir();
            GunduzuBitir(oyun);
            Assert.That(oyun.LincOyu(P(1), P(2)), Is.EqualTo(Sonuc.Tamam));
            Assert.That(oyun.LincOyu(P(2), P(1)), Is.EqualTo(Sonuc.Tamam));
            Saniye(oyun, 61);
            Assert.That(Durum(oyun).Asama, Is.EqualTo(Asama.Gece));
            Assert.That(Kisiler(oyun).TrueForAll(o => !o.Elendi), Is.True);
        }

        [Test]
        public void Rol_acilmazsa_elenenin_rolu_oyun_sonuna_kadar_gizli()
        {
            var oyun = YeniVampir(ayarla: a => a.RolAcilir = false);
            var vampir = Tek(oyun, Rol.Vampir);
            var koylu = Tek(oyun, Rol.Koylu);
            GunduzuBitir(oyun);
            Linc(oyun, koylu);
            Assert.That(Kisi(oyun, koylu).Elendi, Is.True);
            Assert.That(Kisi(oyun, koylu).AcikRol, Is.Null);
            Gece(oyun, Ilk(Sahipleri(oyun, Rol.Koylu), koylu));
            Assert.That(Kisiler(oyun).TrueForAll(o => o.AcikRol == null), Is.True, "gece ölen de gizli");
            Assert.That(oyun.OyunuBitir(), Is.EqualTo(Sonuc.Tamam));
            var roller = oyun.Ozet().Vampir.Roller;
            Assert.That(roller.Count, Is.EqualTo(6));
            Assert.That(((List<OyuncuRolu>)roller).Find(r => r.Kim == vampir).Rol, Is.EqualTo(Rol.Vampir), "oyun sonunda herkesin rolü açık");
        }

        // ---------------------------------------------------- Gece

        [Test]
        public void Gece_vampirin_kurbani_olur_sabah_yeni_gun_baslar()
        {
            var oyun = YeniVampir(7);
            var kurban = Tek(oyun, Rol.Koylu);
            var olaylar = new List<Olay>();
            oyun.OlayOldu += olaylar.Add;
            GunduzuBitir(oyun);
            Saniye(oyun, 61); // linçte kimse oy vermedi
            Gece(oyun, kurban);

            Assert.That(olaylar.Exists(o => o.Tur == OlayTuru.GeceBitti && o.Kim == kurban), Is.True);
            Assert.That(Kisi(oyun, kurban).Elendi, Is.True);
            Assert.That(Kisi(oyun, kurban).AcikRol, Is.EqualTo(Rol.Koylu));
            var d = Durum(oyun);
            Assert.That(d.Asama, Is.EqualTo(Asama.SoruSorma));
            Assert.That(d.Gun, Is.EqualTo(2));
            Assert.That(d.KalanGunduzTuru, Is.EqualTo(6 / 2 - 1), "6 canlı: 3 tur, bu tur hariç 2");
            Assert.That(d.A, Is.Not.EqualTo(kurban));
            Assert.That(GunduzuBitir(oyun), Is.EqualTo(3));
        }

        [Test]
        public void Doktor_kurbani_korursa_kimse_olmez_kendini_oyunda_bir_kez()
        {
            var oyun = YeniVampir();
            var kurban = Tek(oyun, Rol.Koylu);
            var doktor = Tek(oyun, Rol.Doktor);
            var vampir = Tek(oyun, Rol.Vampir);
            var olaylar = new List<Olay>();
            oyun.OlayOldu += olaylar.Add;
            GunduzuBitir(oyun);
            Saniye(oyun, 61);
            Assert.That(oyun.GeceSecimi(vampir, vampir), Is.EqualTo(Sonuc.Secilemez), "vampir vampiri seçemez");
            Gece(oyun, kurban, korunan: kurban);
            Assert.That(olaylar.Exists(o => o.Tur == OlayTuru.GeceBitti && o.Kim == null), Is.True);
            Assert.That(Kisiler(oyun).TrueForAll(o => !o.Elendi), Is.True);

            Assert.That(oyun.GorunumAl(doktor).KendiniKoruyabilir, Is.True);
            GunduzuBitir(oyun);
            Saniye(oyun, 61);
            Assert.That(oyun.GorunumAl(doktor).Secilebilir, Has.Member(doktor));
            Gece(oyun, doktor, korunan: doktor);
            Assert.That(Kisi(oyun, doktor).Elendi, Is.False, "kendini korudu");
            Assert.That(oyun.GorunumAl(doktor).KendiniKoruyabilir, Is.False);
            GunduzuBitir(oyun);
            Saniye(oyun, 61);
            Assert.That(oyun.GorunumAl(doktor).Secilebilir, Has.No.Member(doktor));
            Assert.That(oyun.GeceSecimi(doktor, doktor), Is.EqualTo(Sonuc.Secilemez));
        }

        [Test]
        public void Gozcu_baktigini_hemen_ve_sadece_kendisi_ogrenir_son_secen_olsa_da()
        {
            var oyun = YeniVampir();
            var gozcu = Tek(oyun, Rol.Gozcu);
            var vampir = Tek(oyun, Rol.Vampir);
            GunduzuBitir(oyun);
            Saniye(oyun, 61);
            // Herkes seçer, gözcü en son: gece onun seçimiyle biter.
            foreach (var kim in new List<OyuncuId>(Durum(oyun).Oylayanlar))
                if (kim != gozcu) oyun.GeceSecimi(kim, oyun.GorunumAl(kim).Secilebilir[0]);
            Assert.That(Durum(oyun).Asama, Is.EqualTo(Asama.Gece));
            Assert.That(oyun.GeceSecimi(gozcu, vampir), Is.EqualTo(Sonuc.Tamam));
            Assert.That(Durum(oyun).Asama, Is.Not.EqualTo(Asama.Gece));

            var bilgi = oyun.GorunumAl(gozcu).GozcuBilgilerin;
            Assert.That(bilgi.Count, Is.EqualTo(1));
            Assert.That((bilgi[0].Kim, bilgi[0].Vampir), Is.EqualTo((vampir, true)));
            foreach (var o in Durum(oyun).Oyuncular)
                if (o.Id != gozcu) Assert.That(oyun.GorunumAl(o.Id).GozcuBilgilerin, Is.Empty);
        }

        [Test]
        public void Iki_vampir_birbirinin_secimini_gorur_digerleri_gormez()
        {
            var oyun = YeniVampir(8);
            var vampirler = Sahipleri(oyun, Rol.Vampir);
            var kurban = Tek(oyun, Rol.Koylu);
            GunduzuBitir(oyun);
            Saniye(oyun, 61);
            Assert.That(oyun.GeceSecimi(vampirler[0], kurban), Is.EqualTo(Sonuc.Tamam));
            var gordugu = oyun.GorunumAl(vampirler[1]).VampirSecimleri;
            Assert.That(gordugu.Count, Is.EqualTo(1));
            Assert.That((gordugu[0].Kim, gordugu[0].Hedef), Is.EqualTo((vampirler[0], kurban)));
            Assert.That(oyun.GorunumAl(kurban).VampirSecimleri, Is.Empty);
        }

        [Test]
        public void Iki_vampir_farkli_secerse_ikisinden_biri_olur()
        {
            for (int tohum = 1; tohum <= 10; tohum++)
            {
                var oyun = YeniVampir(8, tohum);
                var vampirler = Sahipleri(oyun, Rol.Vampir);
                var koylular = Sahipleri(oyun, Rol.Koylu);
                GunduzuBitir(oyun);
                Saniye(oyun, 61);
                foreach (var kim in new List<OyuncuId>(Durum(oyun).Oylayanlar))
                {
                    var hedef = kim == vampirler[0] ? koylular[0] : kim == vampirler[1] ? koylular[1] : Ilk(oyun.GorunumAl(kim).Secilebilir, koylular[0], koylular[1]);
                    Assert.That(oyun.GeceSecimi(kim, hedef), Is.EqualTo(Sonuc.Tamam));
                }
                Assert.That(Kisi(oyun, koylular[0]).Elendi ^ Kisi(oyun, koylular[1]).Elendi, Is.True, "tohum " + tohum);
            }
        }

        [Test]
        public void Gece_suresi_dolar_vampir_secmediyse_kimse_olmez()
        {
            var oyun = YeniVampir();
            GunduzuBitir(oyun);
            Saniye(oyun, 61);
            Assert.That(Durum(oyun).Asama, Is.EqualTo(Asama.Gece));
            Saniye(oyun, 61);
            Assert.That(Durum(oyun).Asama, Is.EqualTo(Asama.SoruSorma));
            Assert.That(Kisiler(oyun).TrueForAll(o => !o.Elendi), Is.True);
        }

        [Test]
        public void Elden_elede_her_secim_sureyi_bastan_baslatir()
        {
            var oyun = YeniVampir(ayarla: a => a.TekCihaz = true);
            GunduzuBitir(oyun);
            Saniye(oyun, 61);
            Saniye(oyun, 50);
            var kim = Durum(oyun).Oylayanlar[0];
            Assert.That(oyun.GeceSecimi(kim, oyun.GorunumAl(kim).Secilebilir[0]), Is.EqualTo(Sonuc.Tamam));
            Saniye(oyun, 50);
            Assert.That(Durum(oyun).Asama, Is.EqualTo(Asama.Gece));
        }

        // ---------------------------------------------------- Kazanma

        [Test]
        public void Vampir_lince_giderse_koylu_kazanir_ozette_herkesin_rolu()
        {
            var oyun = YeniVampir();
            var vampir = Tek(oyun, Rol.Vampir);
            GunduzuBitir(oyun);
            Linc(oyun, vampir);
            Assert.That(Durum(oyun).Asama, Is.EqualTo(Asama.OyunSonu));
            var s = oyun.Ozet().Vampir;
            Assert.That(s.Kazanan, Is.EqualTo(VampirKazanan.Koylu));
            Assert.That(s.Roller.Count, Is.EqualTo(6));
        }

        [Test]
        public void Vampirler_sayica_esitlenince_kazanir_cikan_sayilmaz()
        {
            var oyun = YeniVampir();
            var koylular = Sahipleri(oyun, Rol.Koylu);
            koylular.Add(Tek(oyun, Rol.Doktor));
            for (int i = 0; i < 3; i++) oyun.OyuncuCikti(koylular[i]);
            Assert.That(Durum(oyun).Asama, Is.Not.EqualTo(Asama.OyunSonu), "1 vampire 2 köylü");
            oyun.OyuncuCikti(koylular[3]);
            Assert.That(oyun.Ozet().Vampir.Kazanan, Is.EqualTo(VampirKazanan.Vampir));
        }

        [Test]
        public void Vampir_cikarsa_koylu_kazanir_kurucu_bitirirse_kazanan_yok()
        {
            var oyun = YeniVampir();
            oyun.OyuncuCikti(Tek(oyun, Rol.Vampir));
            Assert.That(oyun.Ozet().Vampir.Kazanan, Is.EqualTo(VampirKazanan.Koylu));

            oyun = YeniVampir();
            Assert.That(oyun.OyunuBitir(), Is.EqualTo(Sonuc.Tamam));
            Assert.That(oyun.Ozet().Vampir.Kazanan, Is.Null);
        }

        [Test]
        public void Canlilar_4un_altina_dusunce_cakmak_turu_yok_sadece_linc_ve_gece()
        {
            var oyun = YeniVampir();
            var koylular = Sahipleri(oyun, Rol.Koylu);
            oyun.OyuncuCikti(koylular[0]);
            oyun.OyuncuCikti(koylular[1]);
            oyun.OyuncuCikti(koylular[2]); // 3 canlı: vampir, doktor, gözcü
            var d = Durum(oyun);
            Assert.That(d.Asama, Is.EqualTo(Asama.SoruSorma), "elindeki tur sürer");
            SorVeSec(oyun, "Kim?", out _, out var b, out var c);
            TkmOyna(oyun, b, c, cKazansin: true);
            Saniye(oyun, 7);
            Assert.That(Durum(oyun).Asama, Is.EqualTo(Asama.Linc), "kalan gündüz turları atlandı");
            Saniye(oyun, 61);
            Saniye(oyun, 61); // gece, kimse seçmedi
            Assert.That(Durum(oyun).Asama, Is.EqualTo(Asama.Linc), "yeni gün de doğrudan linç");
            Assert.That(Durum(oyun).Gun, Is.EqualTo(2));
        }

        [Test]
        public void Gece_secmeden_cikan_beklenmez()
        {
            var oyun = YeniVampir(7);
            var koylu = Tek(oyun, Rol.Koylu);
            GunduzuBitir(oyun);
            Saniye(oyun, 61);
            foreach (var kim in new List<OyuncuId>(Durum(oyun).Oylayanlar))
                if (kim != koylu) oyun.GeceSecimi(kim, oyun.GorunumAl(kim).Secilebilir[0]);
            Assert.That(Durum(oyun).Asama, Is.EqualTo(Asama.Gece));
            oyun.OyuncuCikti(koylu);
            Assert.That(Durum(oyun).Asama, Is.Not.EqualTo(Asama.Gece));
        }

        // ---------------------------------------------------- Sohbet kuralları

        [Test]
        public void Sohbet_gece_ve_oluler_genele_yazamaz_vampir_kanali_gece_oluler_kanali_olulere()
        {
            var oyun = YeniVampir(8);
            var vampirler = Sahipleri(oyun, Rol.Vampir);
            var koylu = Tek(oyun, Rol.Koylu);
            Assert.That(oyun.SohbeteYazabilir(koylu), Is.True);
            Assert.That(oyun.KanalAlicilari(vampirler[0], SohbetKanali.Vampir), Is.Null, "gündüz vampir kanalı kapalı");
            Assert.That(oyun.KanalAlicilari(koylu, SohbetKanali.Takim), Is.Null);
            GunduzuBitir(oyun);
            Linc(oyun, koylu);
            Assert.That(Durum(oyun).Asama, Is.EqualTo(Asama.Gece));

            Assert.That(oyun.SohbeteYazabilir(vampirler[0]), Is.False, "gece genel kapalı");
            Assert.That(oyun.SohbeteYazabilir(koylu), Is.False, "ölü genele yazamaz");
            Assert.That(oyun.KanalAlicilari(vampirler[1], SohbetKanali.Vampir), Is.EquivalentTo(vampirler));
            Assert.That(oyun.KanalAlicilari(Tek(oyun, Rol.Doktor), SohbetKanali.Vampir), Is.Null);
            Assert.That(oyun.KanalAlicilari(koylu, SohbetKanali.Oluler), Is.EqualTo(new[] { koylu }));
            Assert.That(oyun.KanalAlicilari(vampirler[0], SohbetKanali.Oluler), Is.Null, "canlı ölüler kanalına yazamaz");

            Saniye(oyun, 61); // gece kimse seçmedi, sabah oldu
            Assert.That(Durum(oyun).Asama, Is.EqualTo(Asama.SoruSorma));
            Assert.That(oyun.SohbeteYazabilir(vampirler[0]), Is.True, "gündüz canlı yazar");
            Assert.That(oyun.SohbeteYazabilir(koylu), Is.False, "gündüz de ölü yazamaz");

            oyun.OyunuBitir();
            Assert.That(oyun.SohbeteYazabilir(koylu), Is.True, "oyun bitince herkes");
        }

        [Test]
        public void Diger_oyunlarda_sohbet_hep_acik_vampir_alanlari_bos()
        {
            var oyun = Yeni();
            Assert.That(oyun.SohbeteYazabilir(P(1)), Is.True);
            Assert.That(oyun.KanalAlicilari(P(1), SohbetKanali.Oluler), Is.Null);
            Assert.That(oyun.LincOyu(P(1), P(2)), Is.EqualTo(Sonuc.YanlisAsama));
            Assert.That(oyun.GeceSecimi(P(1), P(2)), Is.EqualTo(Sonuc.YanlisAsama));
            var g = Durum(oyun);
            Assert.That(g.Gun, Is.Zero);
            Assert.That(g.Rolun, Is.EqualTo(Rol.Yok));
            oyun.OyunuBitir();
            Assert.That(oyun.Ozet().Vampir, Is.Null);
        }
    }
}
