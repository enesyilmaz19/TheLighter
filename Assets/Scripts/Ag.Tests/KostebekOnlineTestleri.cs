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
    /// Köstebek online (tasks/g2-kostebek.md 3, 5): çevirici yeni alanları, "suclama" aşamasını, olayları ve
    /// ozet.kostebek'i okuyor mu; akış oylama ekranını açıyor, komutları protokol biçiminde yolluyor mu.
    /// </summary>
    public class KostebekOnlineTestleri
    {
        static readonly OyuncuId P1 = new OyuncuId(1), P2 = new OyuncuId(2), P3 = new OyuncuId(3);

        // ------------------------------------------------------------ Çevirici

        static string Durum(string asama, string ek = "", string sana = "{}", int surum = 5) =>
            "{\"t\":\"durum\",\"surum\":" + surum + ",\"oda\":\"KXTPM\",\"asama\":\"" + asama + "\",\"tur\":6,\"toplamTur\":15,\"kalanMs\":60000," +
            "\"kurucu\":\"p1\",\"a\":\"p1\",\"oyuncular\":[" +
            "{\"id\":\"p1\",\"isim\":\"Enes\",\"renk\":0,\"bagli\":true}," +
            "{\"id\":\"p2\",\"isim\":\"Ayşe\",\"renk\":1,\"bagli\":true}," +
            "{\"id\":\"p3\",\"isim\":\"Can\",\"renk\":2,\"bagli\":true}," +
            "{\"id\":\"p4\",\"isim\":\"Deniz\",\"renk\":3,\"bagli\":true}]," +
            "\"oyun\":\"kostebek\",\"sana\":" + sana + ek + "}";

        [Test]
        public void Durum_KostebekAlanlari()
        {
            var d = (DurumMesaji)Cevirici.Coz(Durum("suclama",
                ",\"supheliler\":[\"p3\"],\"sonSuclama\":true,\"oylayanlar\":[\"p1\",\"p2\",\"p3\",\"p4\"],\"oyKullanan\":2",
                "{\"rol\":\"kostebek\",\"suclamaHakki\":true,\"oyVerdin\":true}"), P2);
            var g = d.Gorunum;
            Assert.That(g.Asama, Is.EqualTo(Asama.Suclama));
            Assert.That(g.Oyun, Is.EqualTo(OyunTuru.Kostebek));
            Assert.That(g.Supheliler, Is.EqualTo(new[] { P3 }));
            Assert.That(g.SonSuclama, Is.True);
            Assert.That(g.Rolun, Is.EqualTo(Rol.Kostebek));
            Assert.That(g.SuclamaHakkin, Is.True);
            Assert.That(g.OyVerdin, Is.True);
            Assert.That(g.OyKullanan, Is.EqualTo(2));
        }

        [Test]
        public void Durum_RolYoksaYok_OyunYoksaCakmak()
        {
            var d = (DurumMesaji)Cevirici.Coz(Durum("soruSorma", "", "{\"rol\":null}").Replace(",\"oyun\":\"kostebek\"", ""), P2);
            Assert.That(d.Gorunum.Rolun, Is.EqualTo(Rol.Yok));
            Assert.That(d.Gorunum.Oyun, Is.EqualTo(OyunTuru.Cakmak));
            Assert.That(d.Gorunum.Supheliler, Is.Empty);
        }

        [Test]
        public void Olaylar_SuclamaBasladiVeBitti()
        {
            var b = (OlayMesaji)Cevirici.Coz("{\"t\":\"olay\",\"olay\":\"suclamaBasladi\",\"kim\":\"p2\"}");
            Assert.That(b.Olay.Tur, Is.EqualTo(OlayTuru.SuclamaBasladi));
            Assert.That(b.Kim, Is.EqualTo(P2));
            var bitti = (OlayMesaji)Cevirici.Coz("{\"t\":\"olay\",\"olay\":\"suclamaBitti\",\"kim\":null}");
            Assert.That(bitti.Olay.Tur, Is.EqualTo(OlayTuru.SuclamaBitti));
            Assert.That(bitti.Kim, Is.Null, "eşitlikte kimse");
        }

        [Test]
        public void Ozet_Kostebek()
        {
            var d = (DurumMesaji)Cevirici.Coz(Durum("oyunSonu",
                ",\"ozet\":{\"oynananTur\":7,\"kostebek\":{\"kim\":\"p3\",\"kazanan\":\"kostebek\",\"neden\":\"gorevBitti\"}}"), P1);
            var k = d.Ozet.Kostebek;
            Assert.That(k, Is.Not.Null);
            Assert.That(k.Kostebek, Is.EqualTo(P3));
            Assert.That(k.Kazanan, Is.EqualTo(KostebekKazanan.Kostebek));
            Assert.That(k.Neden, Is.EqualTo(KostebekNedeni.GorevBitti));

            var cakmak = (DurumMesaji)Cevirici.Coz(Durum("oyunSonu", ",\"ozet\":{\"oynananTur\":7,\"kostebek\":null}"), P1);
            Assert.That(cakmak.Ozet.Kostebek, Is.Null);
        }

        [Test]
        public void Lobi_OyunAyari_VeGiden()
        {
            var d = (DurumMesaji)Cevirici.Coz("{\"t\":\"durum\",\"surum\":1,\"oda\":\"KXTPM\",\"asama\":\"lobi\",\"kurucu\":\"p1\"," +
                "\"oyuncular\":[],\"ayarlar\":{\"oyun\":\"kostebek\",\"kaos\":false}}");
            Assert.That(d.Ayarlar.Oyun, Is.EqualTo(OyunTuru.Kostebek));

            var a = JsonDeger.Coz(Cevirici.AyarDegistir(oyun: OyunTuru.Kostebek, kaos: false));
            Assert.That(a["oyun"].Metin, Is.EqualTo("kostebek"));
            Assert.That(a["kaos"].MantikYa(true), Is.False);
            Assert.That(JsonDeger.Coz(Cevirici.AyarDegistir(mod: OyunModu.Temiz))["oyun"].Var, Is.False, "değişmeyen alan gitmez");

            var s = JsonDeger.Coz(Cevirici.Sucla(6));
            Assert.That(s["komut"].Metin, Is.EqualTo("sucla"));
            Assert.That(s["tur"].Tamsayi, Is.EqualTo(6));
            var o = JsonDeger.Coz(Cevirici.SuclamaOyu(6, P3));
            Assert.That(o["komut"].Metin, Is.EqualTo("suclamaOyu"));
            Assert.That(o["hedef"].Metin, Is.EqualTo("p3"));
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

        [SetUp]
        public void Kur()
        {
            tasima = new Sahte();
            istemci = new OnlineIstemci(() => tasima, null);
            istemci.Katil(new Uri("ws://localhost:5000/ws"), "KXTPM", "Ayşe");
            tasima.Acil();
            tasima.Mesaj("{\"t\":\"hosgeldin\",\"sen\":\"p2\",\"anahtar\":\"k\",\"oda\":\"KXTPM\"}");
            Kare();
            akis = new OnlineAkis(istemci, new Ayarlar { Oyun = OyunTuru.Kostebek });
        }

        void Kare()
        {
            istemci.Guncelle(TimeSpan.Zero);
            akis?.Ilerle(TimeSpan.Zero);
        }

        JsonDeger SonGiden => JsonDeger.Coz(tasima.Giden.Last());

        [Test]
        public void Suclama_OyVermediysem_OyEkrani_VerdiysemBekle()
        {
            tasima.Mesaj(Durum("suclama", ",\"oylayanlar\":[\"p1\",\"p2\",\"p3\",\"p4\"]", "{\"oyVerdin\":false}", 5));
            Kare();
            Assert.That(akis.Ekran.Tur, Is.EqualTo(EkranTuru.Suclama));
            Assert.That(akis.Ekran.Kime, Is.EqualTo(P2));

            Assert.That(akis.SuclamaOyu(P3), Is.EqualTo(Sonuc.Tamam));
            Kare();
            Assert.That(SonGiden["komut"].Metin, Is.EqualTo("suclamaOyu"));
            Assert.That(SonGiden["hedef"].Metin, Is.EqualTo("p3"));
            Assert.That(SonGiden["tur"].Tamsayi, Is.EqualTo(6));

            tasima.Mesaj(Durum("suclama", ",\"oylayanlar\":[\"p1\",\"p2\",\"p3\",\"p4\"],\"oyKullanan\":1", "{\"oyVerdin\":true}", 6));
            Kare();
            Assert.That(akis.Ekran.Tur, Is.EqualTo(EkranTuru.Bekleniyor));
        }

        [Test]
        public void ErkenSuclama_HakkimVarsaSadeceBen_KomutGider()
        {
            tasima.Mesaj(Durum("soruSorma", "", "{\"suclamaHakki\":true}", 5));
            Kare();
            Assert.That(akis.SuclamaHakkiOlanlar, Is.EqualTo(new[] { P2 }), "online'da sadece kendi hakkım");
            Assert.That(akis.Sucla(P1), Is.EqualTo(Sonuc.Tamam));
            Kare();
            Assert.That(SonGiden["komut"].Metin, Is.EqualTo("sucla"));

            tasima.Mesaj(Durum("soruSorma", "", "{\"suclamaHakki\":false}", 6));
            Kare();
            Assert.That(akis.SuclamaHakkiOlanlar, Is.Empty);
        }
    }
}
