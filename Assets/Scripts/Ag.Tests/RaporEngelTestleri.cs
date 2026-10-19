using System;
using System.Collections.Generic;
using System.Linq;
using Cakmak.Kurallar;
using NUnit.Framework;
using TheLighter.Ag;

namespace TheLighter.Testler
{
    /// <summary>
    /// Rapor ve engelleme (docs/Protokol.md, "Rapor ve engelleme"; tasks/g2-android.md 6): çevirici (<c>rapor</c>,
    /// <c>engelle</c>, <c>raporAlindi</c>, <c>engeller</c>) ve oturum (engel listesi, raporun cevabı).
    /// </summary>
    public class RaporEngelTestleri
    {
        // ------------------------------------------------------------ Çevirici

        [Test]
        public void Coz_RaporAlindi()
        {
            var r = Cevirici.Coz("{ \"t\": \"raporAlindi\", \"kim\": \"p3\" }") as RaporAlindi;
            Assert.That(r, Is.Not.Null);
            Assert.That(r.Kim, Is.EqualTo(new OyuncuId(3)));
            Assert.That(Cevirici.Coz("{ \"t\": \"raporAlindi\" }"), Is.Null, "kim zorunlu");
            Assert.That(Cevirici.Coz("{ \"t\": \"raporAlindi\", \"kim\": \"x\" }"), Is.Null);
        }

        [Test]
        public void Coz_Engeller_TamListe_BozukKimlikAtlanir()
        {
            var e = Cevirici.Coz("{ \"t\": \"engeller\", \"kimler\": [\"p3\", \"bozuk\", \"p5\", \"p3\"] }") as EngellerMesaji;
            Assert.That(e, Is.Not.Null);
            Assert.That(e.Kimler, Is.EqualTo(new[] { new OyuncuId(3), new OyuncuId(5) }));

            var bos = Cevirici.Coz("{ \"t\": \"engeller\", \"kimler\": [] }") as EngellerMesaji;
            Assert.That(bos, Is.Not.Null, "boş liste = kimse engelli değil");
            Assert.That(bos.Kimler, Is.Empty);
            Assert.That(Cevirici.Coz("{ \"t\": \"engeller\" }"), Is.Null, "liste zorunlu");
        }

        [Test]
        public void Giden_RaporVeEngelle()
        {
            var r = JsonDeger.Coz(Cevirici.Rapor(new OyuncuId(3)));
            Assert.That(r["t"].Metin, Is.EqualTo("rapor"));
            Assert.That(r["kim"].Metin, Is.EqualTo("p3"));

            var e = JsonDeger.Coz(Cevirici.Engelle(new OyuncuId(4)));
            Assert.That(e["t"].Metin, Is.EqualTo("engelle"));
            Assert.That(e["kim"].Metin, Is.EqualTo("p4"));
            Assert.That(e["engel"].Var, Is.False, "varsayılan true: yollanmaz");

            var k = JsonDeger.Coz(Cevirici.Engelle(new OyuncuId(4), engel: false));
            Assert.That(k["engel"].Tur, Is.EqualTo(JsonTuru.Mantik));
            Assert.That(k["engel"].Mantik, Is.False);
        }

        [Test]
        public void RaporYazilamadi_SunucuKodu()
        {
            CollectionAssert.Contains(Cevirici.SunucuKodlari, "RAPOR_YAZILAMADI");
            Assert.That(Cevirici.SonucKodu("RAPOR_YAZILAMADI"), Is.Null);
        }

        // ------------------------------------------------------------ Oturum

        sealed class SahteTasima : ITasima
        {
            public readonly Queue<TasimaOlayi> Gelen = new Queue<TasimaOlayi>();
            public readonly List<string> Giden = new List<string>();
            public bool Acik;

            public void Ac(Uri adres) { }
            public bool Gonder(string json)
            {
                if (!Acik) return false;
                Giden.Add(json);
                return true;
            }
            public void Kapat() { Acik = false; }
            public bool Al(out TasimaOlayi olay)
            {
                if (Gelen.Count > 0) { olay = Gelen.Dequeue(); return true; }
                olay = default;
                return false;
            }

            public void Acil() { Acik = true; Gelen.Enqueue(new TasimaOlayi { Tur = TasimaOlayTuru.Acildi }); }
            public void Mesaj(string json) => Gelen.Enqueue(new TasimaOlayi { Tur = TasimaOlayTuru.Mesaj, Metin = json });
            public void Kop() { Acik = false; Gelen.Enqueue(new TasimaOlayi { Tur = TasimaOlayTuru.Kapandi, Metin = "test" }); }
            public JsonDeger Son => JsonDeger.Coz(Giden.Last());
        }

        static readonly Uri Adres = new Uri("ws://localhost:5000/ws");

        List<SahteTasima> tasimalar;
        OnlineIstemci istemci;
        List<(OyuncuId kim, bool alindi)> cevaplar;
        List<HataMesaji> hatalar;

        SahteTasima T => tasimalar.Last();

        [SetUp]
        public void Kur()
        {
            tasimalar = new List<SahteTasima>();
            istemci = new OnlineIstemci(() => { var t = new SahteTasima(); tasimalar.Add(t); return t; }, null);
            cevaplar = new List<(OyuncuId, bool)>();
            hatalar = new List<HataMesaji>();
            istemci.RaporCevabi += (k, a) => cevaplar.Add((k, a));
            istemci.HataGeldi += hatalar.Add;
        }

        void Kare() => istemci.Guncelle(TimeSpan.FromSeconds(0.016));

        void OdayaGir()
        {
            istemci.OdaKur(Adres, "Enes");
            T.Acil();
            Kare();
            T.Mesaj("{\"t\":\"hosgeldin\",\"sen\":\"p1\",\"anahtar\":\"A\",\"oda\":\"KXTPM\"}");
            T.Mesaj("{\"t\":\"durum\",\"surum\":1,\"oda\":\"KXTPM\",\"asama\":\"lobi\",\"kurucu\":\"p1\"," +
                    "\"oyuncular\":[{\"id\":\"p1\",\"isim\":\"Enes\",\"renk\":0,\"bagli\":true}],\"ayarlar\":{}}");
            Kare();
            Assert.That(istemci.Durum, Is.EqualTo(BaglantiDurumu.Odada));
        }

        [Test]
        public void Engelle_ListeSunucudan_SurumArtar()
        {
            OdayaGir();
            int s0 = istemci.EngelSurumu;
            Assert.That(istemci.Engelle(new OyuncuId(3)), Is.True);
            Kare();
            Assert.That(T.Son["t"].Metin, Is.EqualTo("engelle"));
            Assert.That(istemci.EngelliMi(new OyuncuId(3)), Is.False, "cevap gelmeden liste değişmez");

            T.Mesaj("{\"t\":\"engeller\",\"kimler\":[\"p3\"]}");
            Kare();
            Assert.That(istemci.EngelliMi(new OyuncuId(3)), Is.True);
            Assert.That(istemci.EngelSurumu, Is.GreaterThan(s0));

            istemci.Engelle(new OyuncuId(3), engel: false);
            Kare();
            Assert.That(T.Son["engel"].Mantik, Is.False);
            T.Mesaj("{\"t\":\"engeller\",\"kimler\":[]}");
            Kare();
            Assert.That(istemci.Engellenenler, Is.Empty, "engel kaldırıldı");
        }

        [Test]
        public void Engeller_KopupDonunceKalir_AyrilincaSilinir()
        {
            OdayaGir();
            T.Mesaj("{\"t\":\"engeller\",\"kimler\":[\"p2\"]}");
            Kare();
            T.Kop();
            Kare();
            Assert.That(istemci.EngelliMi(new OyuncuId(2)), Is.True, "kopup dönerken korunur");
            istemci.Ayril();
            Assert.That(istemci.Engellenenler, Is.Empty, "oda boyunca: odadan çıkınca biter");
        }

        [Test]
        public void Rapor_AlindiCevabi()
        {
            OdayaGir();
            Assert.That(istemci.Raporla(new OyuncuId(2)), Is.True);
            Kare();
            Assert.That(T.Son["t"].Metin, Is.EqualTo("rapor"));
            Assert.That(T.Son["kim"].Metin, Is.EqualTo("p2"));
            T.Mesaj("{\"t\":\"raporAlindi\",\"kim\":\"p2\"}");
            Kare();
            Assert.That(cevaplar, Is.EqualTo(new[] { (new OyuncuId(2), true) }));
            Assert.That(hatalar, Is.Empty);
        }

        [Test]
        public void Rapor_YazilamadiCevabi_EkranaGenelHataGitmez()
        {
            OdayaGir();
            istemci.Raporla(new OyuncuId(2));
            Kare();
            T.Mesaj("{\"t\":\"hata\",\"kod\":\"RAPOR_YAZILAMADI\"}");
            Kare();
            Assert.That(cevaplar, Is.EqualTo(new[] { (new OyuncuId(2), false) }));
            Assert.That(hatalar, Is.Empty, "raporlama ekranı kendisi söyler");
            Assert.That(istemci.Durum, Is.EqualTo(BaglantiDurumu.Odada));
        }

        [Test]
        public void OdadaDegilken_RaporVeEngelYollanmaz()
        {
            Assert.That(istemci.Raporla(new OyuncuId(2)), Is.False);
            Assert.That(istemci.Engelle(new OyuncuId(2)), Is.False);
        }
    }
}
