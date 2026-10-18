using System;
using System.Collections.Generic;
using System.Linq;
using Cakmak.Kurallar;
using NUnit.Framework;
using TheLighter.Ag;
using TheLighter.Akis;

namespace TheLighter.Testler
{
    /// <summary>
    /// Casus online (tasks/g2-casus.md 3, 4): çevirici yeni alanları, "casusTahmini" aşaması, ozet.casus, lobide ayarlar.casus;
    /// akış rol ekranını başta bir kez açıyor, casus olmayanın tahmini hiç gitmiyor, casusunki protokol biçiminde gidiyor.
    /// </summary>
    public class CasusOnlineTestleri
    {
        static readonly OyuncuId P1 = new OyuncuId(1), P2 = new OyuncuId(2), P3 = new OyuncuId(3);

        static string Durum(string asama, string sana = "{}", string ek = "", int surum = 5) =>
            "{\"t\":\"durum\",\"surum\":" + surum + ",\"oda\":\"KXTPM\",\"asama\":\"" + asama + "\",\"tur\":3,\"toplamTur\":15,\"kalanMs\":30000," +
            "\"kurucu\":\"p1\",\"a\":\"p1\",\"oyuncular\":[" +
            "{\"id\":\"p1\",\"isim\":\"Enes\",\"renk\":0,\"bagli\":true}," +
            "{\"id\":\"p2\",\"isim\":\"Ayşe\",\"renk\":1,\"bagli\":true}," +
            "{\"id\":\"p3\",\"isim\":\"Can\",\"renk\":2,\"bagli\":true}," +
            "{\"id\":\"p4\",\"isim\":\"Deniz\",\"renk\":3,\"bagli\":true}]," +
            "\"oyun\":\"casus\",\"casusListesi\":[\"Uçak\",\"Hastane\",\"Okul\"],\"casusKategori\":null,\"yakalanan\":null," +
            "\"sana\":" + sana + ek + "}";

        const string Masum = "{\"sir\":\"Uçak\",\"mekanRolu\":\"Hostes\",\"rol\":null}";
        const string CasusSana = "{\"sir\":null,\"mekanRolu\":null,\"rol\":\"casus\"}";

        // ------------------------------------------------------------ Çevirici

        [Test]
        public void Durum_MasumunSirriVeRolu_ListeHerkeste()
        {
            var g = ((DurumMesaji)Cevirici.Coz(Durum("soruSorma", Masum), P2)).Gorunum;
            Assert.That(g.Oyun, Is.EqualTo(OyunTuru.Casus));
            Assert.That(g.Sirrin, Is.EqualTo("Uçak"));
            Assert.That(g.MekanRolun, Is.EqualTo("Hostes"));
            Assert.That(g.Rolun, Is.EqualTo(Rol.Yok));
            Assert.That(g.CasusListesi, Is.EqualTo(new[] { "Uçak", "Hastane", "Okul" }));
            Assert.That(g.CasusKategori, Is.Null);
            Assert.That(g.Yakalanan, Is.Null);
        }

        [Test]
        public void Durum_CasusaSirYok_KelimedeKategori()
        {
            var g = ((DurumMesaji)Cevirici.Coz(Durum("soruSorma", CasusSana).Replace("\"casusKategori\":null", "\"casusKategori\":\"Hayvanlar\""), P3)).Gorunum;
            Assert.That(g.Rolun, Is.EqualTo(Rol.Casus));
            Assert.That(g.Sirrin, Is.Null);
            Assert.That(g.MekanRolun, Is.Null);
            Assert.That(g.CasusKategori, Is.EqualTo("Hayvanlar"));
        }

        [Test]
        public void Durum_SonSansAsamasi_Yakalanan()
        {
            var g = ((DurumMesaji)Cevirici.Coz(Durum("casusTahmini", Masum).Replace("\"yakalanan\":null", "\"yakalanan\":\"p3\""), P2)).Gorunum;
            Assert.That(g.Asama, Is.EqualTo(Asama.CasusTahmini));
            Assert.That(g.Yakalanan, Is.EqualTo(P3));
        }

        [Test]
        public void Olaylar_CasusYakalandiVeTahminEtti()
        {
            var y = (OlayMesaji)Cevirici.Coz("{\"t\":\"olay\",\"olay\":\"casusYakalandi\",\"kim\":\"p3\"}");
            Assert.That(y.Olay.Tur, Is.EqualTo(OlayTuru.CasusYakalandi));
            Assert.That(y.Kim, Is.EqualTo(P3));
            var t = (OlayMesaji)Cevirici.Coz("{\"t\":\"olay\",\"olay\":\"casusTahminEtti\",\"kim\":\"p3\"}");
            Assert.That(t.Olay.Tur, Is.EqualTo(OlayTuru.CasusTahminEtti));
        }

        [Test]
        public void Ozet_Casus()
        {
            var d = (DurumMesaji)Cevirici.Coz(Durum("oyunSonu", "{}",
                ",\"ozet\":{\"oynananTur\":6,\"casus\":{\"kim\":\"p3\",\"kazanan\":\"grup\",\"neden\":\"yakalandi\",\"sir\":\"Uçak\",\"tahmin\":\"Okul\"}}"), P1);
            var c = d.Ozet.Casus;
            Assert.That(c, Is.Not.Null);
            Assert.That(c.Casus, Is.EqualTo(P3));
            Assert.That(c.Kazanan, Is.EqualTo(CasusKazanan.Grup));
            Assert.That(c.Neden, Is.EqualTo(CasusNedeni.Yakalandi));
            Assert.That(c.Sir, Is.EqualTo("Uçak"));
            Assert.That(c.Tahmin, Is.EqualTo("Okul"));

            var bildi = (DurumMesaji)Cevirici.Coz(Durum("oyunSonu", "{}",
                ",\"ozet\":{\"casus\":{\"kim\":\"p3\",\"kazanan\":\"casus\",\"neden\":\"sonSanstaBildi\",\"sir\":\"Uçak\",\"tahmin\":\"Uçak\"}}"), P1);
            Assert.That(bildi.Ozet.Casus.Neden, Is.EqualTo(CasusNedeni.SonSanstaBildi));
            var tahminsiz = (DurumMesaji)Cevirici.Coz(Durum("oyunSonu", "{}",
                ",\"ozet\":{\"casus\":{\"kim\":\"p3\",\"kazanan\":\"casus\",\"neden\":\"yakalanmadi\",\"sir\":\"Uçak\",\"tahmin\":null}}"), P1);
            Assert.That(tahminsiz.Ozet.Casus.Tahmin, Is.Null);
            var yok = (DurumMesaji)Cevirici.Coz(Durum("oyunSonu", "{}", ",\"ozet\":{\"casus\":null}"), P1);
            Assert.That(yok.Ozet.Casus, Is.Null);
        }

        [Test]
        public void Lobi_CasusTuru_VeGiden()
        {
            var d = (DurumMesaji)Cevirici.Coz("{\"t\":\"durum\",\"surum\":1,\"oda\":\"KXTPM\",\"asama\":\"lobi\",\"kurucu\":\"p1\"," +
                "\"oyuncular\":[],\"ayarlar\":{\"oyun\":\"casus\",\"casus\":\"kelime\"}}");
            Assert.That(d.Ayarlar.Oyun, Is.EqualTo(OyunTuru.Casus));
            Assert.That(d.Ayarlar.Casus, Is.EqualTo(CasusTuru.Kelime));
            var varsayilan = (DurumMesaji)Cevirici.Coz("{\"t\":\"durum\",\"surum\":1,\"oda\":\"KXTPM\",\"asama\":\"lobi\",\"kurucu\":\"p1\"," +
                "\"oyuncular\":[],\"ayarlar\":{\"oyun\":\"casus\"}}");
            Assert.That(varsayilan.Ayarlar.Casus, Is.EqualTo(CasusTuru.Mekan));

            var a = JsonDeger.Coz(Cevirici.AyarDegistir(oyun: OyunTuru.Casus, casus: CasusTuru.Kelime));
            Assert.That(a["casus"].Metin, Is.EqualTo("kelime"));
            Assert.That(JsonDeger.Coz(Cevirici.AyarDegistir(kaos: false))["casus"].Var, Is.False, "değişmeyen alan gitmez");

            var t = JsonDeger.Coz(Cevirici.CasusTahmini(7, "Uçak"));
            Assert.That(t["komut"].Metin, Is.EqualTo("casusTahmini"));
            Assert.That(t["tur"].Tamsayi, Is.EqualTo(7));
            Assert.That(t["tahmin"].Metin, Is.EqualTo("Uçak"));
        }

        // ------------------------------------------------------------ Akış

        sealed class Sahte : ITasima
        {
            public readonly Queue<TasimaOlayi> Gelen = new Queue<TasimaOlayi>();
            public readonly List<string> Giden = new List<string>();
            bool acik;
            public void Ac(Uri adres) { }
            public bool Gonder(string json) { if (!acik) return false; Giden.Add(json); return true; }
            public void Kapat() { acik = false; }
            public bool Al(out TasimaOlayi o)
            {
                if (Gelen.Count > 0) { o = Gelen.Dequeue(); return true; }
                o = default;
                return false;
            }
            public void Acil() { acik = true; Gelen.Enqueue(new TasimaOlayi { Tur = TasimaOlayTuru.Acildi }); }
            public void Mesaj(string j) => Gelen.Enqueue(new TasimaOlayi { Tur = TasimaOlayTuru.Mesaj, Metin = j });
        }

        Sahte tasima;
        OnlineIstemci istemci;
        OnlineAkis akis;

        void Kur(string sen)
        {
            tasima = new Sahte();
            istemci = new OnlineIstemci(() => tasima, null);
            istemci.Katil(new Uri("ws://localhost:5000/ws"), "KXTPM", "Ayşe");
            tasima.Acil();
            tasima.Mesaj("{\"t\":\"hosgeldin\",\"sen\":\"" + sen + "\",\"anahtar\":\"k\",\"oda\":\"KXTPM\"}");
            Kare();
            akis = new OnlineAkis(istemci, new Ayarlar { Oyun = OyunTuru.Casus });
        }

        void Kare()
        {
            istemci.Guncelle(TimeSpan.Zero);
            akis?.Ilerle(TimeSpan.Zero);
        }

        int CasusKomutu => tasima.Giden.Count(j => j.Contains("casusTahmini"));

        [Test]
        public void RolEkrani_BastaBirKez_MasumaDaCasusaDa()
        {
            Kur("p2");
            tasima.Mesaj(Durum("soruSorma", Masum));
            Kare();
            Assert.That(akis.Ekran.Tur, Is.EqualTo(EkranTuru.GorevGoster), "rol perdesi başta");
            akis.GorevGordum();
            Assert.That(akis.Ekran.Tur, Is.Not.EqualTo(EkranTuru.GorevGoster));

            Kur("p3");
            tasima.Mesaj(Durum("soruSorma", CasusSana));
            Kare();
            Assert.That(akis.Ekran.Tur, Is.EqualTo(EkranTuru.GorevGoster), "casusa da aynı perde");
        }

        [Test]
        public void MasumBilirimDerse_HicbirSeyGitmez()
        {
            Kur("p2");
            tasima.Mesaj(Durum("soruSorma", Masum));
            Kare();
            Assert.That(akis.CasusTahmini(P2, "Uçak"), Is.EqualTo(Sonuc.Tamam), "ekran herkeste aynı kapanır");
            Kare();
            Assert.That(CasusKomutu, Is.EqualTo(0), "casus olmayanın tahmini sunucuya gitmez");
        }

        [Test]
        public void CasusBilirimDerse_KomutGider_BaskasiAdinaGitmez()
        {
            Kur("p3");
            tasima.Mesaj(Durum("soruSorma", CasusSana));
            Kare();
            Assert.That(akis.CasusTahmini(P2, "Okul"), Is.EqualTo(Sonuc.SiraSendeDegil), "online'da sadece kendi adıma");
            Assert.That(akis.CasusTahmini(P3, "Okul"), Is.EqualTo(Sonuc.Tamam));
            Kare();
            var son = JsonDeger.Coz(tasima.Giden.Last());
            Assert.That(son["komut"].Metin, Is.EqualTo("casusTahmini"));
            Assert.That(son["tahmin"].Metin, Is.EqualTo("Okul"));
            Assert.That(son["tur"].Tamsayi, Is.EqualTo(3));
        }

        [Test]
        public void SonSans_HerkesIzler_KimeCasus()
        {
            Kur("p2");
            akis.GorevGordum();
            tasima.Mesaj(Durum("casusTahmini", Masum).Replace("\"yakalanan\":null", "\"yakalanan\":\"p3\""));
            Kare();
            akis.GorevGordum();
            Assert.That(akis.Ekran.Tur, Is.EqualTo(EkranTuru.CasusTahmini));
            Assert.That(akis.Ekran.Kime, Is.EqualTo(P3));
        }
    }
}
