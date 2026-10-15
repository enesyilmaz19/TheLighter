using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using static Cakmak.Kurallar.Tests.Kurulum;
using static Cakmak.Kurallar.Tests.GeceYardim;

namespace Cakmak.Kurallar.Tests
{
    /// <summary>Çakmak Gecesi'nin çakmağa özel rolleri (docs/Oyunlar.md 5): yetenekleri gündüz turuna bağlı.</summary>
    public class CakmakRolleriTestleri
    {
        static OyuncuId Diger(Oyun oyun, params OyuncuId[] haric) => Canlilar(oyun).First(x => !haric.Contains(x));

        static List<Olay> Dinle(Oyun oyun)
        {
            var olaylar = new List<Olay>();
            oyun.OlayOldu += olaylar.Add;
            return olaylar;
        }

        /// <summary>Sıradaki A <paramref name="haric"/>'ten biriyse onları karıştırmayan bir tur oynar: A başkası olur.</summary>
        static void ADisinda(Oyun oyun, params OyuncuId[] haric)
        {
            GunduzTurunaGel(oyun);
            var a = SiradakiA(oyun);
            if (!haric.Contains(a)) return;
            var b = Diger(oyun, haric.Append(a).ToArray());
            Tur(oyun, b, Diger(oyun, haric.Append(a).Append(b).ToArray()), cKazansin: true);
            GunduzTurunaGel(oyun);
            Assert.That(haric, Has.No.Member(SiradakiA(oyun)));
        }

        /// <summary><paramref name="kim"/>'in B ya da C olarak karıştığı bir tur (A o değil). B'yse C kazanır, C'yse <paramref name="cKazansin"/>.</summary>
        static OyuncuId KaristigiTur(Oyun oyun, OyuncuId kim, bool kimB, bool cKazansin)
        {
            ADisinda(oyun, kim);
            var a = SiradakiA(oyun);
            var diger = Diger(oyun, a, kim);
            if (kimB) Tur(oyun, kim, diger, cKazansin);
            else Tur(oyun, diger, kim, cKazansin);
            return a;
        }

        // ---------------------------------------------------- Masumlar

        [Test]
        public void Sorgucu_A_iken_muhurler_duello_kaybedilse_de_soru_ifsa()
        {
            var oyun = YeniGece(8, Rol.Sorgucu);
            var sorgucu = Tek(oyun, Rol.Sorgucu);
            var olaylar = Dinle(oyun);
            SiraVer(oyun, sorgucu);
            var b = Diger(oyun, sorgucu);
            Assert.That(oyun.GunduzYetenegi(b), Is.EqualTo(Sonuc.SiraSendeDegil));
            Assert.That(oyun.GunduzYetenegi(sorgucu), Is.EqualTo(Sonuc.Tamam));
            Assert.That(oyun.GunduzYetenegi(sorgucu), Is.EqualTo(Sonuc.HakKalmadi));
            olaylar.Clear();
            Tur(oyun, b, Diger(oyun, sorgucu, b), cKazansin: false);
            Assert.That(olaylar.Exists(o => o.Tur == OlayTuru.Ifsa), Is.True, "B kazandı ama mühürlü");
            Assert.That(oyun.GorunumAl(sorgucu).KalanHakkin, Is.Zero);
        }

        [Test]
        public void Hakem_gece_hazirlar_ertesi_gun_hedefin_ilk_duellosu_kararla_biter()
        {
            var oyun = YeniGece(8, Rol.Hakem);
            var hakem = Tek(oyun, Rol.Hakem);
            var masumlar = Sahipleri(oyun, Rol.Masum);
            var olaylar = Dinle(oyun);
            GeceyeGec(oyun);
            Assert.That(oyun.GeceSecimi(hakem, masumlar[0], null, ifsa: true), Is.EqualTo(Sonuc.Tamam));
            Gece(oyun);
            ADisinda(oyun, masumlar[0]);
            olaylar.Clear();
            KaristigiTur(oyun, masumlar[0], kimB: true, cKazansin: false);
            Assert.That(olaylar.Exists(o => o.Tur == OlayTuru.HakemKarari && o.B == masumlar[0]), Is.True);
            Assert.That(olaylar.Exists(o => o.Tur == OlayTuru.Ifsa), Is.True, "Hakem ifşa dedi");
            Assert.That(oyun.GorunumAl(hakem).KalanHakkin, Is.EqualTo(1));

            GeceyeGec(oyun);
            Assert.That(oyun.GeceSecimi(hakem, masumlar[1], null, ifsa: false), Is.EqualTo(Sonuc.Tamam));
            Gece(oyun);
            ADisinda(oyun, masumlar[1]);
            olaylar.Clear();
            KaristigiTur(oyun, masumlar[1], kimB: false, cKazansin: true);
            Assert.That(olaylar.Exists(o => o.Tur == OlayTuru.HakemKarari && o.C == masumlar[1]), Is.True);
            Assert.That(olaylar.Exists(o => o.Tur == OlayTuru.Gume), Is.True, "Hakem güme dedi");
            Assert.That(oyun.GorunumAl(hakem).KalanHakkin, Is.Zero);
        }

        [Test]
        public void Kollayici_bugun_sordugunu_o_gece_korur()
        {
            var oyun = YeniGece(8, Rol.Kollayici);
            var kollayici = Tek(oyun, Rol.Kollayici);
            var katiller = Sahipleri(oyun, Rol.Katil);
            var masum = Sahipleri(oyun, Rol.Masum)[0];
            SiraVer(oyun, kollayici, masum);
            Tur(oyun, masum, Diger(oyun, kollayici, masum), cKazansin: true);
            GeceyeGec(oyun);
            Gece(oyun, (katiller[0], masum, null), (katiller[1], masum, null));
            Assert.That(Kisi(oyun, masum).Elendi, Is.False);
        }

        [Test]
        public void Yuzlesmeci_C_olarak_kazaninca_B_ye_bakar_kaybedince_bakamaz()
        {
            var oyun = YeniGece(8, Rol.Yuzlesmeci);
            var yuz = Tek(oyun, Rol.Yuzlesmeci);
            var katil = Sahipleri(oyun, Rol.Katil)[0];
            var masum = Sahipleri(oyun, Rol.Masum)[0];
            ADisinda(oyun, yuz, katil);
            Tur(oyun, katil, yuz, cKazansin: true);
            Assert.That(oyun.GorunumAl(yuz).Bulgularin.Select(x => (x.Kim, x.Supheli)), Is.EqualTo(new[] { (katil, (bool?)true) }), "hemen");

            ADisinda(oyun, yuz, masum);
            Tur(oyun, masum, yuz, cKazansin: false);
            Assert.That(oyun.GorunumAl(yuz).Bulgularin.Count, Is.EqualTo(1), "kaybetti");
            ADisinda(oyun, yuz, masum);
            Tur(oyun, masum, yuz, cKazansin: true);
            Assert.That(oyun.GorunumAl(yuz).Bulgularin.Last().Supheli, Is.False);
        }

        [Test]
        public void Sezgici_B_olarak_gosterdigine_sabah_bakar()
        {
            var oyun = YeniGece(8, Rol.Sezgici);
            var sezgici = Tek(oyun, Rol.Sezgici);
            var katil = Sahipleri(oyun, Rol.Katil)[0];
            ADisinda(oyun, sezgici, katil);
            Tur(oyun, sezgici, katil, cKazansin: true);
            Assert.That(oyun.GorunumAl(sezgici).Bulgularin, Is.Empty, "sabah");
            GeceyeGec(oyun);
            Gece(oyun);
            Assert.That(oyun.GorunumAl(sezgici).Bulgularin.Any(x => x.Kim == katil && x.Supheli == true), Is.True);
        }

        [Test]
        public void Sinayici_A_iken_sordugu_B_ye_sabah_bakar()
        {
            var oyun = YeniGece(8, Rol.Sinayici);
            var sinayici = Tek(oyun, Rol.Sinayici);
            var katil = Sahipleri(oyun, Rol.Katil)[0];
            SiraVer(oyun, sinayici, katil);
            Assert.That(oyun.GunduzYetenegi(katil), Is.EqualTo(Sonuc.SiraSendeDegil));
            Assert.That(oyun.GunduzYetenegi(sinayici), Is.EqualTo(Sonuc.Tamam), "soruyu sormadan önce");
            Assert.That(oyun.GunduzYetenegi(sinayici), Is.EqualTo(Sonuc.HakKalmadi), "turda bir kez");
            Tur(oyun, katil, Diger(oyun, sinayici, katil), cKazansin: true);
            Assert.That(oyun.GorunumAl(sinayici).KalanHakkin, Is.EqualTo(1));
            GeceyeGec(oyun);
            Gece(oyun);
            Assert.That(oyun.GorunumAl(sinayici).Bulgularin.Single().Kim, Is.EqualTo(katil));
            Assert.That(oyun.GorunumAl(sinayici).Bulgularin.Single().Supheli, Is.True);
        }

        [Test]
        public void Siper_C_oldugu_gecenin_saldirilarindan_kurtulur()
        {
            var oyun = YeniGece(8, Rol.Siper);
            var siper = Tek(oyun, Rol.Siper);
            var katiller = Sahipleri(oyun, Rol.Katil);
            KaristigiTur(oyun, siper, kimB: false, cKazansin: true);
            GeceyeGec(oyun);
            Gece(oyun, (katiller[0], siper, null), (katiller[1], siper, null));
            Assert.That(Kisi(oyun, siper).Elendi, Is.False);
            Assert.That(oyun.GorunumAl(siper).KalanHakkin, Is.EqualTo(1));

            // Bugün C değil: korunmaz.
            while (Durum(oyun).Asama != Asama.Linc)
            {
                var a = SiradakiA(oyun);
                var b = Diger(oyun, a);
                Tur(oyun, b, Diger(oyun, a, b, siper), cKazansin: true);
            }
            Saniye(oyun, 61);
            Gece(oyun, (katiller[0], siper, null), (katiller[1], siper, null));
            Assert.That(Kisi(oyun, siper).Elendi, Is.True);
        }

        [Test]
        public void Sonduren_hazirladigi_duello_soner_soru_gume_ceza_yok_sabah_A_ya_bakar()
        {
            var oyun = YeniGece(8, new[] { Rol.Sondurucu }, a => a.CezaSeviyesi = CezaSeviyesi.Hafif);
            var sondurucu = Tek(oyun, Rol.Sondurucu);
            var masum = Sahipleri(oyun, Rol.Masum)[0];
            var olaylar = Dinle(oyun);
            GeceyeGec(oyun);
            Gece(oyun, (sondurucu, masum, null));
            ADisinda(oyun, masum);
            olaylar.Clear();
            var a = KaristigiTur(oyun, masum, kimB: false, cKazansin: true);
            Assert.That(olaylar.Exists(o => o.Tur == OlayTuru.DuelloSonduruldu), Is.True);
            Assert.That(olaylar.Exists(o => o.Tur == OlayTuru.Gume), Is.True);
            Assert.That(olaylar.Exists(o => o.Tur == OlayTuru.Ifsa || o.Tur == OlayTuru.CezaCekildi), Is.False, "ceza açık ama kimse çekmez");
            Assert.That(oyun.GorunumAl(sondurucu).KalanHakkin, Is.Zero);
            GeceyeGec(oyun);
            Gece(oyun);
            Assert.That(oyun.GorunumAl(sondurucu).Bulgularin.Single().Kim, Is.EqualTo(a));
        }

        // ---------------------------------------------------- Katiller

        [Test]
        public void Kundakci_onu_bugun_gosteren_B_yi_o_gece_yakar()
        {
            var oyun = YeniGece(8, Rol.Kundakci);
            var kundakci = Tek(oyun, Rol.Kundakci);
            var masum = Sahipleri(oyun, Rol.Masum)[0];
            ADisinda(oyun, kundakci, masum);
            Tur(oyun, masum, kundakci, cKazansin: true);
            GeceyeGec(oyun);
            Assert.That(oyun.GorunumAl(kundakci).IkinciSecilebilir, Has.Member(masum));
            Gece(oyun, (kundakci, null, masum));
            Assert.That(Kisi(oyun, masum).Elendi, Is.True);
            Assert.That(oyun.GorunumAl(kundakci).KalanHakkin, Is.Zero);
        }

        [Test]
        public void Dinleyici_gume_giden_soruyu_duyar()
        {
            var oyun = YeniGece(8, Rol.Dinleyici);
            var dinleyici = Tek(oyun, Rol.Dinleyici);
            var a = SiradakiA(oyun);
            var b = Diger(oyun, a, dinleyici);
            var c = Diger(oyun, a, dinleyici, b);
            Assert.That(oyun.SoruSor(a, b, "Gizli suçlama kim?"), Is.EqualTo(Sonuc.Tamam));
            Assert.That(oyun.CevapSec(b, c), Is.EqualTo(Sonuc.Tamam));
            TkmOyna(oyun, b, c, cKazansin: false);
            Assert.That(oyun.GorunumAl(dinleyici).KulakSorusu, Is.EqualTo("Gizli suçlama kim?"));
        }

        [Test]
        public void Fitnecinin_turunda_gosterilen_o_gece_Dedektife_supheli()
        {
            var oyun = YeniGece(8, Rol.Fitneci, Rol.Dedektif);
            var fitneci = Tek(oyun, Rol.Fitneci);
            var dedektif = Tek(oyun, Rol.Dedektif);
            var masumlar = Sahipleri(oyun, Rol.Masum);
            SiraVer(oyun, fitneci, masumlar[0], masumlar[1]);
            Tur(oyun, masumlar[0], masumlar[1], cKazansin: true);
            GeceyeGec(oyun);
            Gece(oyun, (dedektif, masumlar[1], null));
            Assert.That(oyun.GorunumAl(dedektif).Bulgularin.Single().Supheli, Is.True);
        }

        [Test]
        public void Kuklaci_C_yi_B_nin_yerine_secer()
        {
            var oyun = YeniGece(8, Rol.Kuklaci);
            var kuklaci = Tek(oyun, Rol.Kuklaci);
            var masumlar = Sahipleri(oyun, Rol.Masum);
            SiraVer(oyun, kuklaci, masumlar[0], masumlar[1]);
            Assert.That(oyun.GunduzYetenegi(kuklaci), Is.EqualTo(Sonuc.GecersizGirdi));
            Assert.That(oyun.GunduzYetenegi(kuklaci, kuklaci), Is.EqualTo(Sonuc.Secilemez));
            Assert.That(oyun.GunduzYetenegi(kuklaci, masumlar[1]), Is.EqualTo(Sonuc.Tamam));
            Assert.That(oyun.SoruSor(kuklaci, masumlar[0], "Kim?"), Is.EqualTo(Sonuc.Tamam));
            Assert.That(Durum(oyun).Asama, Is.EqualTo(Asama.MiniOyun));
            Assert.That(Durum(oyun).C, Is.EqualTo(masumlar[1]));
            Assert.That(oyun.GorunumAl(kuklaci).KalanHakkin, Is.Zero);
        }

        [Test]
        public void Kiskirticinin_gosterdigi_lince_iki_oyla_baslar()
        {
            var oyun = YeniGece(8, Rol.Kiskirtici);
            var kiskirtici = Tek(oyun, Rol.Kiskirtici);
            ADisinda(oyun, kiskirtici);
            var a = SiradakiA(oyun);
            var masumlar = Sahipleri(oyun, Rol.Masum).Where(x => x != a).ToList();
            Assert.That(oyun.SoruSor(a, kiskirtici, "Kim?"), Is.EqualTo(Sonuc.Tamam));
            Assert.That(oyun.GunduzYetenegi(kiskirtici), Is.EqualTo(Sonuc.Tamam), "C'yi seçmeden önce");
            Assert.That(oyun.CevapSec(kiskirtici, masumlar[0]), Is.EqualTo(Sonuc.Tamam));
            TkmOyna(oyun, kiskirtici, masumlar[0], cKazansin: true);
            Saniye(oyun, 7);
            Assert.That(oyun.GorunumAl(kiskirtici).KalanHakkin, Is.Zero);
            GunduzuBitir(oyun);
            Assert.That(oyun.LincOyu(masumlar[1], masumlar[2]), Is.EqualTo(Sonuc.Tamam));
            Saniye(oyun, 61);
            Assert.That(Kisi(oyun, masumlar[0]).Elendi, Is.True, "2–1");
        }

        // ---------------------------------------------------- Tarafsızlar

        [Test]
        public void Kibritci_bugun_sordugunu_gece_yakar_son_kalirsa_kazanir()
        {
            var oyun = YeniGece(8, Rol.Kibritci);
            var kibritci = Tek(oyun, Rol.Kibritci);
            var masum = Sahipleri(oyun, Rol.Masum)[0];
            SiraVer(oyun, kibritci, masum);
            Tur(oyun, masum, Diger(oyun, kibritci, masum), cKazansin: true);
            GeceyeGec(oyun);
            Assert.That(oyun.GorunumAl(kibritci).Secilebilir, Has.Member(masum));
            Assert.That(oyun.GorunumAl(kibritci).Secilebilir.Count, Is.LessThan(7), "sadece bugün sorduklarından");
            Gece(oyun, (kibritci, masum, null));
            Assert.That(Kisi(oyun, masum).Elendi, Is.True);

            oyun = YeniGece(8, Rol.Kibritci);
            kibritci = Tek(oyun, Rol.Kibritci);
            foreach (var k in Sahipleri(oyun, Rol.Katil)) oyun.OyuncuCikti(k);
            foreach (var m in Sahipleri(oyun, Rol.Masum).Skip(1)) oyun.OyuncuCikti(m);
            var s = oyun.Ozet().Gece;
            Assert.That((s.Kazanan, s.Kazananlar.Single()), Is.EqualTo((GeceKazanan.Kibritci, kibritci)));
        }

        /// <summary><paramref name="kim"/>'i <paramref name="kez"/> kez B ya da C yapar, sonra katiller çıkar: masumlar kazanır.</summary>
        static CakmakGecesiSonucu Oyna(Rol rol, int kez, bool kimB, bool cKazansin)
        {
            var oyun = YeniGece(8, rol);
            var kim = Tek(oyun, rol);
            for (int i = 0; i < kez; i++) KaristigiTur(oyun, kim, kimB, cKazansin);
            foreach (var k in Sahipleri(oyun, Rol.Katil)) oyun.OyuncuCikti(k);
            var s = oyun.Ozet().Gece;
            Assert.That(s.Kazanan, Is.EqualTo(GeceKazanan.Masumlar));
            return s;
        }

        static bool Kazandi(CakmakGecesiSonucu s, Rol rol) => s.Kazananlar.Contains(s.Roller.Single(r => r.Rol == rol).Kim);

        [Test]
        public void Gozde_dort_kez_gosterilirse_kazananlara_eklenir()
        {
            Assert.That(Kazandi(Oyna(Rol.Gozde, 4, kimB: false, cKazansin: false), Rol.Gozde), Is.True);
            Assert.That(Kazandi(Oyna(Rol.Gozde, 3, kimB: false, cKazansin: false), Rol.Gozde), Is.False);
        }

        [Test]
        public void Duellocu_uc_duello_kazanirsa_kazananlara_eklenir()
        {
            Assert.That(Kazandi(Oyna(Rol.Duellocu, 3, kimB: false, cKazansin: true), Rol.Duellocu), Is.True);
            Assert.That(Kazandi(Oyna(Rol.Duellocu, 2, kimB: false, cKazansin: true), Rol.Duellocu), Is.False);
            Assert.That(Kazandi(Oyna(Rol.Duellocu, 3, kimB: false, cKazansin: false), Rol.Duellocu), Is.False, "kaybetti");
        }

        [Test]
        public void Sir_Kupu_uc_kez_B_olup_hic_ifsa_olmazsa_kazananlara_eklenir()
        {
            // B'yse C kazanır demek ifşa: güme için B kazanmalı (C kaybeder).
            Assert.That(Kazandi(Oyna(Rol.SirKupu, 3, kimB: true, cKazansin: false), Rol.SirKupu), Is.True);
            Assert.That(Kazandi(Oyna(Rol.SirKupu, 2, kimB: true, cKazansin: false), Rol.SirKupu), Is.False);
            Assert.That(Kazandi(Oyna(Rol.SirKupu, 3, kimB: true, cKazansin: true), Rol.SirKupu), Is.False, "ifşa oldu");
        }
    }
}
