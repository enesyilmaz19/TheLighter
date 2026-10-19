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
    /// Çakmak Gecesi online (tasks/g2-cakmak-gecesi.md 3, 4): çevirici bütün yeni alanlar (bulgular dahil), linç ve gece aşamaları,
    /// olaylar, ozet.gece, lobide yeni ayarlar, giden komutlar; akış linç/gece ekranlarını sadece seçecek olana açar; sohbet: gece
    /// genel kapalı, ölü ve susturulan genele yazamaz, katil ve ölüler sekmeleri sadece ilgililerde.
    /// </summary>
    public class GeceOnlineTestleri
    {
        static readonly OyuncuId P1 = new OyuncuId(1), P2 = new OyuncuId(2), P3 = new OyuncuId(3), P4 = new OyuncuId(4),
            P5 = new OyuncuId(5), P6 = new OyuncuId(6);

        static string Oyuncular(string p3Ek = "", string p6Ek = "") =>
            "\"oyuncular\":[" +
            "{\"id\":\"p1\",\"isim\":\"Enes\",\"renk\":0,\"bagli\":true}," +
            "{\"id\":\"p2\",\"isim\":\"Ayşe\",\"renk\":1,\"bagli\":true}," +
            "{\"id\":\"p3\",\"isim\":\"Can\",\"renk\":2,\"bagli\":true" + p3Ek + "}," +
            "{\"id\":\"p4\",\"isim\":\"Deniz\",\"renk\":3,\"bagli\":true}," +
            "{\"id\":\"p5\",\"isim\":\"Elif\",\"renk\":4,\"bagli\":true}," +
            "{\"id\":\"p6\",\"isim\":\"Umut\",\"renk\":5,\"bagli\":true" + p6Ek + "}]";

        static string Durum(string asama, string sana = "{}", string ek = "", string p3Ek = "", string p6Ek = "", int surum = 5) =>
            "{\"t\":\"durum\",\"surum\":" + surum + ",\"oda\":\"KXTPM\",\"asama\":\"" + asama + "\",\"tur\":7,\"toplamTur\":0,\"kalanMs\":60000," +
            "\"kurucu\":\"p1\"," + Oyuncular(p3Ek, p6Ek) + ",\"oyun\":\"cakmakGecesi\",\"gun\":2,\"kalanGunduzTuru\":1," +
            "\"sana\":" + sana + ek + "}";

        const string KatilSana =
            "{\"rol\":\"patron\",\"katiller\":[{\"kim\":\"p2\",\"rol\":\"patron\"},{\"kim\":\"p5\",\"rol\":\"susturucu\"}]," +
            "\"katilSecimleri\":[{\"kim\":\"p5\",\"hedef\":\"p1\"}],\"bulgularin\":[],\"kalanHakkin\":null,\"cellatHedefin\":null," +
            "\"bombalilar\":[],\"kulakSorusu\":null}";

        // ------------------------------------------------------------ Çevirici

        [Test]
        public void Durum_HerkeseAcikAlanlar()
        {
            var g = ((DurumMesaji)Cevirici.Coz(Durum("soruSorma", "{\"rol\":\"masum\"}", "",
                ",\"elendi\":true,\"acikRol\":\"dedektif\"", ",\"susturuldu\":true"), P1)).Gorunum;
            Assert.That(g.Oyun, Is.EqualTo(OyunTuru.CakmakGecesi));
            Assert.That(g.Gun, Is.EqualTo(2));
            Assert.That(g.KalanGunduzTuru, Is.EqualTo(1));
            Assert.That(g.ToplamTur, Is.EqualTo(0));
            var p3 = g.Oyuncular.First(o => o.Id == P3);
            Assert.That(p3.Elendi, Is.True);
            Assert.That(p3.AcikRol, Is.EqualTo(Rol.Dedektif));
            Assert.That(g.Oyuncular.First(o => o.Id == P6).Susturuldu, Is.True);
            Assert.That(g.Rolun, Is.EqualTo(Rol.Masum));
            Assert.That(g.Katiller, Is.Empty);
        }

        [Test]
        public void Durum_KatilinGordukleri()
        {
            var g = ((DurumMesaji)Cevirici.Coz(Durum("gece", KatilSana,
                ",\"oylayanlar\":[\"p1\",\"p2\",\"p4\",\"p5\",\"p6\"],\"oyKullanan\":1,\"secilebilir\":[\"p1\",\"p4\",\"p6\"],\"ikinciSecilebilir\":[\"p4\"]"), P2)).Gorunum;
            Assert.That(g.Asama, Is.EqualTo(Asama.Gece));
            Assert.That(g.Rolun, Is.EqualTo(Rol.Patron));
            Assert.That(g.Katiller.Select(k => k.Kim), Is.EqualTo(new[] { P2, P5 }));
            Assert.That(g.Katiller[1].Rol, Is.EqualTo(Rol.Susturucu));
            Assert.That(g.KatilSecimleri.Single().Kim, Is.EqualTo(P5));
            Assert.That(g.KatilSecimleri.Single().Hedef, Is.EqualTo(P1));
            Assert.That(g.Secilebilir, Is.EqualTo(new[] { P1, P4, P6 }));
            Assert.That(g.IkinciSecilebilir, Is.EqualTo(new[] { P4 }));
            Assert.That(g.KalanHakkin, Is.Null);
        }

        [Test]
        public void Durum_Bulgular_HaklarHedefBombaKulak()
        {
            const string sana = "{\"rol\":\"falci\",\"kalanHakkin\":1,\"cellatHedefin\":\"p4\",\"bombalilar\":[\"p1\",\"p6\"],\"kulakSorusu\":\"Kim en çok yalan söyler?\"," +
                "\"bulgularin\":[" +
                "{\"gun\":1,\"kim\":\"p3\",\"supheli\":true,\"rol\":null,\"ziyaretciler\":[],\"grup\":[],\"kotuVar\":null}," +
                "{\"gun\":1,\"kim\":\"p4\",\"supheli\":null,\"rol\":\"vampir\",\"ziyaretciler\":[],\"grup\":[],\"kotuVar\":null}," +
                "{\"gun\":1,\"kim\":\"p5\",\"supheli\":null,\"rol\":null,\"ziyaretciler\":[\"p2\",\"p6\"],\"grup\":[],\"kotuVar\":null}," +
                "{\"gun\":1,\"kim\":\"p6\",\"supheli\":null,\"rol\":null,\"ziyaretciler\":[],\"grup\":[\"p6\",\"p1\",\"p3\"],\"kotuVar\":false}]}";
            var g = ((DurumMesaji)Cevirici.Coz(Durum("gume", sana), P2)).Gorunum;
            Assert.That(g.KalanHakkin, Is.EqualTo(1));
            Assert.That(g.CellatHedefin, Is.EqualTo(P4));
            Assert.That(g.Bombalilar, Is.EqualTo(new[] { P1, P6 }));
            Assert.That(g.KulakSorusu, Is.EqualTo("Kim en çok yalan söyler?"));
            var b = g.Bulgularin;
            Assert.That(b.Count, Is.EqualTo(4));
            Assert.That(b[0].Supheli, Is.True);
            Assert.That(b[0].Rol, Is.Null);
            Assert.That(b[1].Rol, Is.EqualTo(Rol.Vampir));
            Assert.That(b[1].Supheli, Is.Null, "null kalır (Dedektif doldurmadı)");
            Assert.That(b[2].Ziyaretciler, Is.EqualTo(new[] { P2, P6 }));
            Assert.That(b[3].Grup, Is.EqualTo(new[] { P6, P1, P3 }));
            Assert.That(b[3].KotuVar, Is.False);
            Assert.That(b[3].Gun, Is.EqualTo(1));
        }

        [Test]
        public void Olaylar_LincGeceHakem()
        {
            foreach (var ad in new[] { "lincBasladi", "lincBitti", "suikast", "geceBasladi", "geceBitti", "hakemKarari", "duelloSonduruldu" })
                Assert.That(Cevirici.Coz("{\"t\":\"olay\",\"olay\":\"" + ad + "\",\"kim\":null}"), Is.Not.Null, ad);
            var l = (OlayMesaji)Cevirici.Coz("{\"t\":\"olay\",\"olay\":\"lincBitti\",\"kim\":\"p3\"}");
            Assert.That(l.Olay.Tur, Is.EqualTo(OlayTuru.LincBitti));
            Assert.That(l.Kim, Is.EqualTo(P3));
            var h = (OlayMesaji)Cevirici.Coz("{\"t\":\"olay\",\"olay\":\"hakemKarari\",\"b\":\"p2\",\"c\":\"p4\",\"kim\":null}");
            Assert.That(h.Olay.B, Is.EqualTo(P2));
            Assert.That(h.Olay.C, Is.EqualTo(P4));
            Assert.That(h.Kim, Is.Null, "kimin yaptığı yok");
        }

        [Test]
        public void Ozet_Gece()
        {
            var d = (DurumMesaji)Cevirici.Coz(Durum("oyunSonu", "{}",
                ",\"ozet\":{\"oynananTur\":9,\"gece\":{\"kazanan\":\"katiller\",\"kazananlar\":[\"p2\",\"p5\"]," +
                "\"roller\":[{\"kim\":\"p1\",\"rol\":\"deli\"},{\"kim\":\"p2\",\"rol\":\"patron\"}]}}"), P1);
            var s = d.Ozet.Gece;
            Assert.That(s.Kazanan, Is.EqualTo(GeceKazanan.Katiller));
            Assert.That(s.Kazananlar, Is.EqualTo(new[] { P2, P5 }));
            Assert.That(s.Roller.First(r => r.Kim == P1).Rol, Is.EqualTo(Rol.Deli), "Deli oyun sonunda Deli");
            var yok = (DurumMesaji)Cevirici.Coz(Durum("oyunSonu", "{}", ",\"ozet\":{\"gece\":{\"kazanan\":null,\"kazananlar\":[],\"roller\":[]}}"), P1);
            Assert.That(yok.Ozet.Gece.Kazanan, Is.Null, "kurucu bitirdi");
            var baska = (DurumMesaji)Cevirici.Coz(Durum("oyunSonu", "{}", ",\"ozet\":{\"gece\":null}"), P1);
            Assert.That(baska.Ozet.Gece, Is.Null);
        }

        [Test]
        public void Lobi_YeniAyarlar_VeGiden()
        {
            var d = (DurumMesaji)Cevirici.Coz("{\"t\":\"durum\",\"surum\":1,\"oda\":\"KXTPM\",\"asama\":\"lobi\",\"kurucu\":\"p1\",\"oyuncular\":[]," +
                "\"ayarlar\":{\"oyun\":\"cakmakGecesi\",\"rolAcilir\":false,\"gecelikKurban\":2,\"kapaliRoller\":[\"palyaco\",\"seriKatil\",\"uzayli\"]}}");
            Assert.That(d.Ayarlar.Oyun, Is.EqualTo(OyunTuru.CakmakGecesi));
            Assert.That(d.Ayarlar.RolAcilir, Is.False);
            Assert.That(d.Ayarlar.GecelikKurban, Is.EqualTo(2));
            Assert.That(d.Ayarlar.KapaliRoller, Is.EqualTo(new[] { Rol.Palyaco, Rol.SeriKatil }), "tanınmayan rol atlanır");

            var a = JsonDeger.Coz(Cevirici.AyarDegistir(oyun: OyunTuru.CakmakGecesi, rolAcilir: false, gecelikKurban: 3,
                kapaliRoller: new[] { Rol.Vampir, Rol.HayattaKalan }));
            Assert.That(a["oyun"].Metin, Is.EqualTo("cakmakGecesi"));
            Assert.That(a["rolAcilir"].MantikYa(true), Is.False);
            Assert.That(a["gecelikKurban"].Tamsayi, Is.EqualTo(3));
            Assert.That(a["kapaliRoller"].Elemanlar.Select(x => x.Metin), Is.EqualTo(new[] { "vampir", "hayattaKalan" }));
            Assert.That(JsonDeger.Coz(Cevirici.AyarDegistir(kaos: false))["kapaliRoller"].Var, Is.False, "değişmeyen alan gitmez");
        }

        [Test]
        public void Giden_LincGeceYetenekSuikast()
        {
            var l = JsonDeger.Coz(Cevirici.LincOyu(7, P3));
            Assert.That(l["komut"].Metin, Is.EqualTo("lincOyu"));
            Assert.That(l["hedef"].Metin, Is.EqualTo("p3"));

            var pas = JsonDeger.Coz(Cevirici.GeceSecimi(7, null));
            Assert.That(pas["komut"].Metin, Is.EqualTo("geceSecimi"));
            Assert.That(pas["hedef"].Var, Is.False, "hedefsiz = pas");
            var katil = JsonDeger.Coz(Cevirici.GeceSecimi(7, P1, P4));
            Assert.That(katil["hedef"].Metin, Is.EqualTo("p1"));
            Assert.That(katil["ikinci"].Metin, Is.EqualTo("p4"));
            Assert.That(katil["ifsa"].Var, Is.False);
            var hakem = JsonDeger.Coz(Cevirici.GeceSecimi(7, P4, null, false));
            Assert.That(hakem["ifsa"].MantikYa(true), Is.False);

            Assert.That(JsonDeger.Coz(Cevirici.Yetenek(7))["komut"].Metin, Is.EqualTo("yetenek"));
            Assert.That(JsonDeger.Coz(Cevirici.Yetenek(7))["hedef"].Var, Is.False);
            Assert.That(JsonDeger.Coz(Cevirici.Yetenek(7, P5))["hedef"].Metin, Is.EqualTo("p5"));

            var s = JsonDeger.Coz(Cevirici.Suikast(7, P4, Rol.SeriKatil));
            Assert.That(s["komut"].Metin, Is.EqualTo("suikast"));
            Assert.That(s["hedef"].Metin, Is.EqualTo("p4"));
            Assert.That(s["rol"].Metin, Is.EqualTo("seriKatil"));
        }

        // ------------------------------------------------------------ Sohbet izni

        static DurumMesaji D(string asama, string sana = "{\"rol\":\"masum\"}", string p3Ek = "", string p6Ek = "") =>
            (DurumMesaji)Cevirici.Coz(Durum(asama, sana, "", p3Ek, p6Ek), P1);

        [Test]
        public void Sohbet_GeceGenelKapali_OluVeSusturulanYazamaz()
        {
            Assert.That(SohbetIzni.YazamazNedeni(D("soruSorma"), P1, SohbetYeri.Genel), Is.Null, "gündüz canlı yazar");
            Assert.That(SohbetIzni.YazamazNedeni(D("gece"), P1, SohbetYeri.Genel), Is.EqualTo("sohbet.yazamaz.gece"));
            Assert.That(SohbetIzni.YazamazNedeni(D("gece"), P1, SohbetYeri.DmIle(P2)), Is.EqualTo("sohbet.yazamaz.gece"), "DM de");
            Assert.That(SohbetIzni.YazamazNedeni(D("soruSorma", p3Ek: ",\"elendi\":true"), P3, SohbetYeri.Genel), Is.EqualTo("sohbet.yazamaz.olu"));
            Assert.That(SohbetIzni.YazamazNedeni(D("linc", p6Ek: ",\"susturuldu\":true"), P6, SohbetYeri.Genel), Is.EqualTo("sohbet.yazamaz.susturuldu"));
            Assert.That(SohbetIzni.YazamazNedeni(D("oyunSonu"), P3, SohbetYeri.Genel), Is.Null, "oyun bitince herkes");

            // Başka oyunlarda kısıt yok.
            var cakmak = (DurumMesaji)Cevirici.Coz(Durum("soruSorma").Replace("\"oyun\":\"cakmakGecesi\"", "\"oyun\":\"cakmak\""), P1);
            Assert.That(SohbetIzni.YazamazNedeni(cakmak, P1, SohbetYeri.Genel), Is.Null);
        }

        [Test]
        public void Sohbet_KatilVeOluSekmeleriSadeceIlgililerde()
        {
            var katil = (DurumMesaji)Cevirici.Coz(Durum("gece", KatilSana), P2);
            Assert.That(SohbetIzni.AdliKanallar(katil, P2), Is.EqualTo(new[] { SohbetKanali.Katil }));
            Assert.That(SohbetIzni.YazamazNedeni(katil, P2, SohbetYeri.Adli(SohbetKanali.Katil)), Is.Null, "katil gece yazar");
            var gunduzKatil = (DurumMesaji)Cevirici.Coz(Durum("soruSorma", KatilSana), P2);
            Assert.That(SohbetIzni.YazamazNedeni(gunduzKatil, P2, SohbetYeri.Adli(SohbetKanali.Katil)), Is.EqualTo("sohbet.yazamaz.katil"));

            Assert.That(SohbetIzni.AdliKanallar(D("gece"), P1), Is.Empty, "masumda katil sekmesi yok");
            var olu = (DurumMesaji)Cevirici.Coz(Durum("gece", "{\"rol\":\"masum\"}", "", ",\"elendi\":true"), P3);
            Assert.That(SohbetIzni.AdliKanallar(olu, P3), Is.EqualTo(new[] { SohbetKanali.Oluler }));
            Assert.That(SohbetIzni.YazamazNedeni(olu, P3, SohbetYeri.Adli(SohbetKanali.Oluler)), Is.Null, "ölüler kendi aralarında her an");
            Assert.That(SohbetIzni.AdliKanallar(D("oyunSonu", KatilSana), P2), Is.Empty, "oyun bitince kanal yok");
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
            akis = new OnlineAkis(istemci, new Ayarlar { Oyun = OyunTuru.CakmakGecesi });
            akis.GorevGordum();
        }

        void Kare()
        {
            istemci.Guncelle(TimeSpan.Zero);
            akis?.Ilerle(TimeSpan.Zero);
        }

        JsonDeger SonGiden => JsonDeger.Coz(tasima.Giden.Last());

        [Test]
        public void Linc_OyVerecekseEkran_OluyseBekler()
        {
            Kur("p2");
            tasima.Mesaj(Durum("linc", "{\"rol\":\"masum\",\"oyVerdin\":false}", ",\"oylayanlar\":[\"p1\",\"p2\",\"p4\"],\"secilebilir\":[\"p1\",\"p4\"]"));
            Kare();
            Assert.That(akis.Ekran.Tur, Is.EqualTo(EkranTuru.Linc));
            Assert.That(akis.LincOyu(P4), Is.EqualTo(Sonuc.Tamam));
            Kare();
            Assert.That(SonGiden["komut"].Metin, Is.EqualTo("lincOyu"));
            Assert.That(SonGiden["hedef"].Metin, Is.EqualTo("p4"));

            Kur("p3");
            tasima.Mesaj(Durum("linc", "{\"rol\":\"masum\"}", ",\"oylayanlar\":[\"p1\",\"p2\",\"p4\"]", ",\"elendi\":true"));
            Kare();
            Assert.That(akis.Ekran.Tur, Is.EqualTo(EkranTuru.Bekleniyor), "ölü oy vermez, izler");
        }

        [Test]
        public void Gece_SecimKomutu_PasVeIkinci()
        {
            Kur("p2");
            tasima.Mesaj(Durum("gece", KatilSana, ",\"oylayanlar\":[\"p1\",\"p2\",\"p4\",\"p5\",\"p6\"],\"secilebilir\":[\"p1\",\"p4\",\"p6\"],\"ikinciSecilebilir\":[\"p4\"]"));
            Kare();
            Assert.That(akis.Ekran.Tur, Is.EqualTo(EkranTuru.Gece));
            Assert.That(akis.Ekran.Kime, Is.EqualTo(P2));
            Assert.That(akis.GeceSecimi(P1, P4), Is.EqualTo(Sonuc.Tamam));
            Kare();
            Assert.That(SonGiden["komut"].Metin, Is.EqualTo("geceSecimi"));
            Assert.That(SonGiden["hedef"].Metin, Is.EqualTo("p1"));
            Assert.That(SonGiden["ikinci"].Metin, Is.EqualTo("p4"));

            tasima.Mesaj(Durum("gece", KatilSana.Replace("\"rol\":\"patron\",", "\"rol\":\"patron\",\"oyVerdin\":true,"),
                ",\"oylayanlar\":[\"p1\",\"p2\",\"p4\",\"p5\",\"p6\"],\"oyKullanan\":1", surum: 6));
            Kare();
            Assert.That(akis.Ekran.Tur, Is.EqualTo(EkranTuru.Bekleniyor), "seçtikten sonra bekler");
        }

        [Test]
        public void GunduzYetenegi_VeSuikast_KomutGider()
        {
            Kur("p1");
            tasima.Mesaj(Durum("soruSorma", "{\"rol\":\"kuklaci\",\"kalanHakkin\":1}"));
            Kare();
            Assert.That(akis.GunduzYetenegi(P5), Is.EqualTo(Sonuc.Tamam));
            Kare();
            Assert.That(SonGiden["komut"].Metin, Is.EqualTo("yetenek"));
            Assert.That(SonGiden["hedef"].Metin, Is.EqualTo("p5"));

            Assert.That(akis.Suikast(P4, Rol.Vampir), Is.EqualTo(Sonuc.Tamam));
            Kare();
            Assert.That(SonGiden["komut"].Metin, Is.EqualTo("suikast"));
            Assert.That(SonGiden["rol"].Metin, Is.EqualTo("vampir"));
        }
    }
}
