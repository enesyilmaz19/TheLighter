using System;
using System.Linq;
using Cakmak.Kurallar;
using NUnit.Framework;
using TheLighter.Ag;

namespace TheLighter.Testler
{
    /// <summary>
    /// Çevirici: docs/Protokol.md'deki örnek mesajlar doğru alanlara düşüyor mu, bozuk/eksik mesajda çökmüyor mu,
    /// giden mesajlar protokolün biçiminde mi.
    /// </summary>
    public class CeviriciTestleri
    {
        // ------------------------------------------------------------ Protokol.md örnekleri (birebir)

        const string HosgeldinOrnegi =
            "{ \"t\": \"hosgeldin\", \"sen\": \"p2\", \"anahtar\": \"32-karakterlik-rastgele-dize\", \"oda\": \"KXTPM\" }";

        const string LobiOrnegi =
            "{ \"t\": \"durum\", \"surum\": 3, \"oda\": \"KXTPM\", \"asama\": \"lobi\", \"kurucu\": \"p1\"," +
            "  \"oyuncular\": [ { \"id\": \"p1\", \"isim\": \"Enes\", \"renk\": 0, \"bagli\": true } ]," +
            "  \"ayarlar\": { \"mod\": \"normal\", \"miniOyun\": \"tkm\", \"turSayisi\": 15, \"soruSn\": 60, \"cevapSn\": 20, \"hamleSn\": 10," +
            "               \"ceza\": \"kapali\", \"gorevler\": false, \"kaos\": false, \"paketler\": [\"AileDostu\", \"Genel\", \"Okul\", \"TatilYolculuk\"] } }";

        const string BeklemeOdasiOrnegi =
            "{ \"t\": \"durum\", \"surum\": 58, \"oda\": \"KXTPM\", \"asama\": \"beklemeOdasi\", \"kurucu\": \"p1\"," +
            "  \"oyuncular\": [ { \"id\": \"p1\", \"isim\": \"Enes\", \"renk\": 0, \"bagli\": true } ]," +
            "  \"bekleyenler\": [ { \"id\": \"p6\", \"isim\": \"Ayşe\", \"renk\": 5, \"bagli\": true } ]," +
            "  \"tur\": 7, \"toplamTur\": 15, \"oyunBitti\": false }";

        const string OyunOrnegi =
            "{ \"t\": \"durum\", \"surum\": 57, \"oda\": \"KXTPM\", \"asama\": \"miniOyun\", \"tur\": 12, \"toplamTur\": 15, \"kalanMs\": 8000," +
            "  \"kurucu\": \"p1\", \"a\": \"p1\", \"b\": \"p2\", \"c\": \"p4\"," +
            "  \"oyuncular\": [ { \"id\": \"p1\", \"isim\": \"Enes\", \"renk\": 0, \"bagli\": true," +
            "                   \"havuzaEkledigi\": 0, \"gosterilme\": 1, \"soruAlma\": 2, \"soruSorma\": 3," +
            "                   \"ifsaEttirme\": 0, \"saklama\": 1, \"miniOyunKazanma\": 1," +
            "                   \"oynadigiMiniOyun\": 2, \"cezaCekme\": 1, \"yanma\": 0, \"bedelOdeme\": 0, \"bedelHakki\": 2, \"kaosKazanma\": 0," +
            "                   \"kisitlamalar\": [ { \"tur\": \"palyaco\", \"metin\": null, \"kalan\": 2 } ]," +
            "                   \"afk\": false, \"cikti\": false } ]," +
            "  \"secilebilir\": []," +
            "  \"miniOyun\": \"tkm\", \"bHamleYapti\": true, \"cHamleYapti\": false, \"beraberlik\": 0," +
            "  \"sonMiniOyun\": null," +
            "  \"kaos\": \"yok\", \"isi\": \"ilik\", \"kararVeren\": null," +
            "  \"oylayanlar\": [], \"oyKullanan\": 0, \"sonOylama\": null, \"bedelOdendi\": false, \"turCezalari\": []," +
            "  \"sana\": { \"soru\": \"Grupta sırrını en kolay kim açık eder?\", \"fisilti\": false," +
            "            \"atananSoru\": null, \"degistirmeHakki\": false, \"oneri\": null," +
            "            \"hamlen\": { \"oyun\": \"tkm\", \"secim\": \"tas\" }," +
            "            \"gorevin\": { \"tur\": \"kisiyiGoster\", \"hedef\": \"p3\", \"adet\": 2, \"zorluk\": \"orta\" }, \"oyVerdin\": false }," +
            "  \"ifsa\": null," +
            "  \"ozet\": null," +
            "  \"bekleme\": null," +
            "  \"bekleyenler\": [] }";

        const string OlayOrnegi =
            "{ \"t\": \"olay\", \"olay\": \"ifsa\", \"a\": \"p1\", \"b\": \"p2\", \"c\": \"p4\", \"kim\": null, \"soru\": \"…\", \"ceza\": null, \"kaos\": null }";

        const string OzetOrnegi =
            "{ \"oynananTur\": 15," +
            "  \"unvanlar\": [ { \"tur\": \"miknatis\", \"kim\": \"p3\", \"deger\": 6 } ]," +
            "  \"gorevler\": [ { \"kim\": \"p1\", \"gorev\": { \"tur\": \"kisiyiGoster\", \"hedef\": \"p3\", \"adet\": 2, \"zorluk\": \"orta\" }, \"basarili\": true } ]," +
            "  \"ifsaOlanlar\": [ { \"soru\": \"…\", \"a\": \"p1\", \"b\": \"p2\", \"c\": \"p4\" } ]," +
            "  \"curcunaSorulari\": [ \"…\" ] }";

        [Test]
        public void Hosgeldin_AlanlarDoluyor_AnahtarYaziyaDusmuyor()
        {
            var h = (Hosgeldin)Cevirici.Coz(HosgeldinOrnegi);
            Assert.That(h.Sen, Is.EqualTo(new OyuncuId(2)));
            Assert.That(h.Anahtar, Is.EqualTo("32-karakterlik-rastgele-dize"));
            Assert.That(h.Oda, Is.EqualTo("KXTPM"));
            Assert.That(h.ToString(), Does.Not.Contain(h.Anahtar));
        }

        [Test]
        public void Lobi_OyuncularVeAyarlar()
        {
            var d = (DurumMesaji)Cevirici.Coz(LobiOrnegi);
            Assert.That(d.Yer, Is.EqualTo(OdaYeri.Lobi));
            Assert.That(d.Surum, Is.EqualTo(3));
            Assert.That(d.Oda, Is.EqualTo("KXTPM"));
            Assert.That(d.Kurucu, Is.EqualTo(new OyuncuId(1)));
            Assert.That(d.Oyuncular.Single().Isim, Is.EqualTo("Enes"));
            Assert.That(d.Oyuncular.Single().Bagli, Is.True);
            Assert.That(d.Gorunum, Is.Null);

            var a = d.Ayarlar;
            Assert.That(a.Mod, Is.EqualTo(OyunModu.Normal));
            Assert.That(a.MiniOyun, Is.EqualTo(MiniOyunTuru.Tkm));
            Assert.That(a.TurSayisi, Is.EqualTo(15));
            Assert.That(a.SoruSn, Is.EqualTo(60));
            Assert.That(a.CevapSn, Is.EqualTo(20));
            Assert.That(a.HamleSn, Is.EqualTo(10));
            Assert.That(a.Ceza, Is.EqualTo(CezaSeviyesi.Kapali));
            Assert.That(a.Gorevler, Is.False);
            Assert.That(a.Kaos, Is.False);
            Assert.That(a.Paketler, Is.EqualTo(new[] { "AileDostu", "Genel", "Okul", "TatilYolculuk" }));
        }

        [Test]
        public void BeklemeOdasi_SadeceHerkeseAcikBilgi()
        {
            var d = (DurumMesaji)Cevirici.Coz(BeklemeOdasiOrnegi);
            Assert.That(d.Yer, Is.EqualTo(OdaYeri.BeklemeOdasi));
            Assert.That(d.Tur, Is.EqualTo(7));
            Assert.That(d.ToplamTur, Is.EqualTo(15));
            Assert.That(d.OyunBitti, Is.False);
            Assert.That(d.Bekleyenler.Single().Isim, Is.EqualTo("Ayşe"));
            Assert.That(d.Bekleyenler.Single().Renk, Is.EqualTo(5));
            Assert.That(d.Gorunum, Is.Null);
        }

        [Test]
        public void Oyun_GorunumAlanlariDogruYere()
        {
            var d = (DurumMesaji)Cevirici.Coz(OyunOrnegi, new OyuncuId(2));
            Assert.That(d.Yer, Is.EqualTo(OdaYeri.Oyun));
            var g = d.Gorunum;
            Assert.That(g.Sen, Is.EqualTo(new OyuncuId(2)));
            Assert.That(g.Asama, Is.EqualTo(Asama.MiniOyun));
            Assert.That(g.Tur, Is.EqualTo(12));
            Assert.That(g.ToplamTur, Is.EqualTo(15));
            Assert.That(g.KalanMs, Is.EqualTo(8000));
            Assert.That(g.A, Is.EqualTo(new OyuncuId(1)));
            Assert.That(g.B, Is.EqualTo(new OyuncuId(2)));
            Assert.That(g.C, Is.EqualTo(new OyuncuId(4)));
            Assert.That(g.Secilebilir, Is.Empty);
            Assert.That(g.MiniOyun, Is.EqualTo(MiniOyunTuru.Tkm));
            Assert.That(g.BHamleYapti, Is.True);
            Assert.That(g.CHamleYapti, Is.False);
            Assert.That(g.SonMiniOyun, Is.Null);
            Assert.That(g.Kaos, Is.EqualTo(KaosKurali.Yok));
            Assert.That(g.Isi, Is.EqualTo(IsiSeviyesi.Ilik));
            Assert.That(g.KararVeren, Is.Null);
            Assert.That(g.SonOylama, Is.Null);

            Assert.That(g.Soru, Is.EqualTo("Grupta sırrını en kolay kim açık eder?"));
            Assert.That(g.Hamlen.HasValue && g.Hamlen.Value.Tur == MiniOyunTuru.Tkm && g.Hamlen.Value.Tkm == TkmSecim.Tas);
            Assert.That(g.Gorevin.Tur, Is.EqualTo(GorevTuru.KisiyiGoster));
            Assert.That(g.Gorevin.Hedef, Is.EqualTo(new OyuncuId(3)));
            Assert.That(g.Gorevin.Adet, Is.EqualTo(2));
            Assert.That(g.Gorevin.Zorluk, Is.EqualTo(Zorluk.Orta));

            var o = g.Oyuncular.Single();
            Assert.That(o.Isim, Is.EqualTo("Enes"));
            Assert.That(o.Gosterilme, Is.EqualTo(1));
            Assert.That(o.SoruAlma, Is.EqualTo(2));
            Assert.That(o.SoruSorma, Is.EqualTo(3));
            Assert.That(o.Saklama, Is.EqualTo(1));
            Assert.That(o.MiniOyunKazanma, Is.EqualTo(1));
            Assert.That(o.OynadigiMiniOyun, Is.EqualTo(2));
            Assert.That(o.CezaCekme, Is.EqualTo(1));
            Assert.That(o.BedelHakki, Is.EqualTo(2));
            Assert.That(o.Kisitlamalar.Single().Tur, Is.EqualTo(CezaTuru.Palyaco));
            Assert.That(o.Kisitlamalar.Single().Kalan, Is.EqualTo(2));
            Assert.That(o.Afk, Is.False);

            Assert.That(d.Oyuncular.Single().Bagli, Is.True);
            Assert.That(d.BeklemeKalanMs, Is.Null);
            Assert.That(d.Ozet, Is.Null);
        }

        [Test]
        public void Oyun_IfsaSonucuOylamaCezalarBekleme()
        {
            string json = OyunOrnegi
                .Replace("\"asama\": \"miniOyun\"", "\"asama\": \"ifsa\"")
                .Replace("\"sonMiniOyun\": null",
                    "\"sonMiniOyun\": { \"oyun\": \"tekCift\", \"bHamle\": { \"oyun\": \"tekCift\", \"parmak\": 3, \"tek\": true }," +
                    " \"cHamle\": { \"oyun\": \"tekCift\", \"parmak\": 2, \"tek\": false }, \"bZar\": 0, \"cZar\": 0," +
                    " \"zarlaKarar\": false, \"sureDoldu\": false, \"cKazandi\": true, \"katlandi\": true }")
                .Replace("\"sonOylama\": null", "\"sonOylama\": { \"ifsa\": 2, \"gume\": 1 }")
                .Replace("\"turCezalari\": []", "\"turCezalari\": [ { \"kim\": \"p2\", \"ceza\": { \"tur\": \"sesli\", \"metin\": \"Şarkı söyle\", \"kalan\": 0 }, \"neden\": \"yandi\" } ]")
                .Replace("\"ifsa\": null", "\"ifsa\": { \"soru\": \"Kim?\", \"fisilti\": false }")
                .Replace("\"bekleme\": null", "\"bekleme\": { \"kalanMs\": 95000 }")
                .Replace("\"kaos\": \"yok\"", "\"kaos\": \"grupKarari\"");

            var d = (DurumMesaji)Cevirici.Coz(json, new OyuncuId(1));
            var g = d.Gorunum;
            Assert.That(g.Asama, Is.EqualTo(Asama.Ifsa));
            Assert.That(g.IfsaSoru, Is.EqualTo("Kim?"));
            Assert.That(g.Kaos, Is.EqualTo(KaosKurali.GrupKarari));
            Assert.That(g.SonOylama.Ifsa, Is.EqualTo(2));
            Assert.That(g.SonOylama.Gume, Is.EqualTo(1));
            var s = g.SonMiniOyun;
            Assert.That(s.Tur, Is.EqualTo(MiniOyunTuru.TekCift));
            Assert.That(s.BHamle.Value.Sayi, Is.EqualTo(3));
            Assert.That(s.BHamle.Value.TekDiyor, Is.True);
            Assert.That(s.CHamle.Value.Sayi, Is.EqualTo(2));
            Assert.That(s.CKazandi, Is.True);
            Assert.That(s.Katlandi, Is.True);
            var c = g.TurCezalari.Single();
            Assert.That(c.Kim, Is.EqualTo(new OyuncuId(2)));
            Assert.That(c.Ceza.Tur, Is.EqualTo(CezaTuru.Sesli));
            Assert.That(c.Ceza.Metin, Is.EqualTo("Şarkı söyle"));
            Assert.That(c.Neden, Is.EqualTo(CekimNedeni.Yandi));
            Assert.That(d.BeklemeKalanMs, Is.EqualTo(95000));
        }

        [Test]
        public void OyunSonu_OzetSayaclariOyunculardanAlir()
        {
            string json = OyunOrnegi
                .Replace("\"asama\": \"miniOyun\"", "\"asama\": \"oyunSonu\"")
                .Replace("\"ozet\": null", "\"ozet\": " + OzetOrnegi);

            var d = (DurumMesaji)Cevirici.Coz(json);
            var o = d.Ozet;
            Assert.That(o, Is.Not.Null);
            Assert.That(o.OynananTur, Is.EqualTo(15));
            Assert.That(o.Unvanlar.Single().Tur, Is.EqualTo(UnvanTuru.Miknatis));
            Assert.That(o.Unvanlar.Single().Kim, Is.EqualTo(new OyuncuId(3)));
            Assert.That(o.Unvanlar.Single().Deger, Is.EqualTo(6));
            Assert.That(o.Gorevler.Single().Basarili, Is.True);
            Assert.That(o.Gorevler.Single().Gorev.Hedef, Is.EqualTo(new OyuncuId(3)));
            Assert.That(o.IfsaOlanlar.Single().C, Is.EqualTo(new OyuncuId(4)));
            Assert.That(o.CurcunaSorulari.Single(), Is.EqualTo("…"));
            Assert.That(o.Oyuncular, Is.SameAs(d.Gorunum.Oyuncular));
            // Grup hafızası bu özetten yazılabiliyor.
            Assert.DoesNotThrow(() => GrupKaydi.Isle(new GrupKaydi { Ad = "Test" }, o));
        }

        [Test]
        public void Olay_KuralOlayiVeSunucununAtildiOlayi()
        {
            var o = (OlayMesaji)Cevirici.Coz(OlayOrnegi);
            Assert.That(o.Atildi, Is.False);
            Assert.That(o.Olay.Tur, Is.EqualTo(OlayTuru.Ifsa));
            Assert.That(o.Olay.A, Is.EqualTo(new OyuncuId(1)));
            Assert.That(o.Olay.Soru, Is.EqualTo("…"));
            Assert.That(o.Olay.Kim, Is.Null);

            var kaos = (OlayMesaji)Cevirici.Coz(OlayOrnegi.Replace("\"olay\": \"ifsa\"", "\"olay\": \"kaosBasladi\"").Replace("\"kaos\": null", "\"kaos\": \"tersDunya\""));
            Assert.That(kaos.Olay.Kaos, Is.EqualTo(KaosKurali.TersDunya));

            var ceza = (OlayMesaji)Cevirici.Coz("{\"t\":\"olay\",\"olay\":\"cezaCekildi\",\"kim\":\"p3\",\"ceza\":{\"tur\":\"palyaco\",\"metin\":null,\"kalan\":2}}");
            Assert.That(ceza.Olay.Ceza.Tur, Is.EqualTo(CezaTuru.Palyaco));
            Assert.That(ceza.Kim, Is.EqualTo(new OyuncuId(3)));

            var atildi = (OlayMesaji)Cevirici.Coz("{ \"t\": \"olay\", \"olay\": \"atildi\", \"a\": null, \"b\": null, \"c\": null, \"kim\": \"p4\", \"soru\": null, \"ceza\": null, \"kaos\": null }");
            Assert.That(atildi.Atildi, Is.True);
            Assert.That(atildi.Olay, Is.Null);
            Assert.That(atildi.Kim, Is.EqualTo(new OyuncuId(4)));
        }

        [Test]
        public void Hata_KuralKoduSonucaSunucuKoduKendisi()
        {
            var h = (HataMesaji)Cevirici.Coz("{ \"t\": \"hata\", \"kod\": \"SIRA_SENDE_DEGIL\" }");
            Assert.That(h.Kod, Is.EqualTo("SIRA_SENDE_DEGIL"));
            Assert.That(h.Sonuc, Is.EqualTo(Sonuc.SiraSendeDegil));

            var s = (HataMesaji)Cevirici.Coz("{ \"t\": \"hata\", \"kod\": \"ODA_YOK\" }");
            Assert.That(s.Kod, Is.EqualTo("ODA_YOK"));
            Assert.That(s.Sonuc, Is.Null);
        }

        [Test]
        public void HataKodu_HerSonucIcinSunucununBicimi_GidipGeliyor()
        {
            foreach (Sonuc s in Enum.GetValues(typeof(Sonuc)))
            {
                string kod = Cevirici.HataKodu(s);
                Assert.That(kod, Does.Match("^[A-Z]+(_[A-Z]+)*$"), s.ToString());
                Assert.That(Cevirici.SonucKodu(kod), Is.EqualTo(s), kod);
            }
            Assert.That(Cevirici.HataKodu(Sonuc.OyuncuSayisiGecersiz), Is.EqualTo("OYUNCU_SAYISI_GECERSIZ"));
        }

        // ------------------------------------------------------------ Bozuk / eksik / yeni

        [TestCase(null)]
        [TestCase("")]
        [TestCase("çöp")]
        [TestCase("[1,2,3]")]
        [TestCase("{\"t\":\"durum\"")]                       // yarım
        [TestCase("{\"t\":\"bilinmeyen\"}")]
        [TestCase("{\"kod\":\"ODA_YOK\"}")]                  // t yok
        [TestCase("{\"t\":\"hosgeldin\",\"sen\":\"p2\"}")]   // anahtar yok
        [TestCase("{\"t\":\"hosgeldin\",\"sen\":\"x\",\"anahtar\":\"a\",\"oda\":\"KXTPM\"}")]
        [TestCase("{\"t\":\"durum\",\"asama\":\"lobi\"}")]   // surum yok
        [TestCase("{\"t\":\"durum\",\"surum\":\"3\",\"asama\":\"lobi\"}")] // yanlış tip
        [TestCase("{\"t\":\"durum\",\"surum\":3,\"asama\":\"uzayda\"}")]
        [TestCase("{\"t\":\"olay\",\"olay\":\"depremOldu\"}")]
        [TestCase("{\"t\":\"hata\"}")]
        public void BozukMesaj_NullDoner_Atmaz(string json)
        {
            Assert.That(Cevirici.Coz(json), Is.Null);
        }

        [Test]
        public void EksikAlanliOyunDurumu_VarsayilanlaCozulur()
        {
            var d = (DurumMesaji)Cevirici.Coz("{\"t\":\"durum\",\"surum\":9,\"asama\":\"soruSorma\"}");
            Assert.That(d.Gorunum.Asama, Is.EqualTo(Asama.SoruSorma));
            Assert.That(d.Gorunum.Oyuncular, Is.Empty);
            Assert.That(d.Gorunum.Secilebilir, Is.Empty);
            Assert.That(d.Gorunum.Gorevin, Is.Null);
            Assert.That(d.Gorunum.Isi, Is.Null);
        }

        [Test]
        public void YanlisTipliAlanlar_Atlanir()
        {
            string json = OyunOrnegi
                .Replace("\"kalanMs\": 8000", "\"kalanMs\": \"çok\"")
                .Replace("\"secilebilir\": []", "\"secilebilir\": [\"p3\", 5, null, \"zz\"]")
                .Replace("\"kisitlamalar\": [ { \"tur\": \"palyaco\", \"metin\": null, \"kalan\": 2 } ]", "\"kisitlamalar\": \"yok\"")
                .Replace("\"isi\": \"ilik\"", "\"isi\": 7");
            var g = ((DurumMesaji)Cevirici.Coz(json)).Gorunum;
            Assert.That(g.KalanMs, Is.EqualTo(0));
            Assert.That(g.Secilebilir, Is.EqualTo(new[] { new OyuncuId(3) }));
            Assert.That(g.Oyuncular.Single().Kisitlamalar, Is.Empty);
            Assert.That(g.Isi, Is.Null);
        }

        [Test]
        public void BilinmeyenYeniDegerler_MesajiDusurmez()
        {
            // Sunucu istemciden önce güncellenirse: yeni kaos, yeni ceza, yeni alan.
            string json = OyunOrnegi
                .Replace("\"kaos\": \"yok\"", "\"kaos\": \"yepyeniKaos\"")
                .Replace("\"tur\": \"palyaco\"", "\"tur\": \"yepyeniCeza\"")
                .Replace("\"bekleyenler\": []", "\"bekleyenler\": [], \"gelecekAlan\": { \"x\": [1, {\"y\": true}] }");
            var d = (DurumMesaji)Cevirici.Coz(json);
            Assert.That(d, Is.Not.Null);
            Assert.That(d.Gorunum.Kaos, Is.EqualTo(KaosKurali.Yok));
            Assert.That(d.Gorunum.Oyuncular.Single().Kisitlamalar, Is.Empty);
        }

        // ------------------------------------------------------------ Giden

        [Test]
        public void Giden_GirisMesajlari()
        {
            var k = JsonDeger.Coz(Cevirici.OdaKur("Enes"));
            Assert.That(k["t"].Metin, Is.EqualTo("odaKur"));
            Assert.That(k["isim"].Metin, Is.EqualTo("Enes"));

            var ilk = JsonDeger.Coz(Cevirici.Katil("KXTPM", "Ayşe", null));
            Assert.That(ilk["t"].Metin, Is.EqualTo("katil"));
            Assert.That(ilk["oda"].Metin, Is.EqualTo("KXTPM"));
            Assert.That(ilk["isim"].Metin, Is.EqualTo("Ayşe"));
            Assert.That(ilk["anahtar"].Tur, Is.EqualTo(JsonTuru.Yok));
            Assert.That(Cevirici.Katil("KXTPM", "Ayşe", null), Does.Contain("\"anahtar\":null"));

            var don = JsonDeger.Coz(Cevirici.Katil("KXTPM", "Ayşe", "abc"));
            Assert.That(don["anahtar"].Metin, Is.EqualTo("abc"));

            // Protokol sürümü ikisinde de (docs/Protokol.md, "Sürüm").
            Assert.That(k["surum"].Tamsayi, Is.EqualTo(Cevirici.ProtokolSurumu));
            Assert.That(ilk["surum"].Tamsayi, Is.EqualTo(Cevirici.ProtokolSurumu));
            Assert.That(don["surum"].Tamsayi, Is.EqualTo(Cevirici.ProtokolSurumu));
            Assert.That(Cevirici.ProtokolSurumu, Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public void Giden_AyarlarSadeceVerilenAlanlar()
        {
            var a = JsonDeger.Coz(Cevirici.AyarDegistir(ceza: CezaSeviyesi.Cesur, kaos: true));
            Assert.That(a["t"].Metin, Is.EqualTo("ayarlar"));
            Assert.That(a["ceza"].Metin, Is.EqualTo("cesur"));
            Assert.That(a["kaos"].Mantik, Is.True);
            Assert.That(a["mod"].Var, Is.False);
            Assert.That(a["paketler"].Var, Is.False);

            var p = JsonDeger.Coz(Cevirici.AyarDegistir(mod: OyunModu.Curcuna, miniOyun: MiniOyunTuru.TekCift, turSayisi: 0, paketler: new[] { "Genel", "Okul" }));
            Assert.That(p["mod"].Metin, Is.EqualTo("curcuna"));
            Assert.That(p["miniOyun"].Metin, Is.EqualTo("tekCift"));
            Assert.That(p["turSayisi"].Tamsayi, Is.EqualTo(0));
            Assert.That(p["paketler"].Elemanlar.Select(x => x.Metin), Is.EqualTo(new[] { "Genel", "Okul" }));
        }

        [Test]
        public void Giden_KomutlarTurVeAlanlarla()
        {
            var s = JsonDeger.Coz(Cevirici.SoruSor(4, new OyuncuId(3), "Kim \"en\" çok\\uyur?\n"));
            Assert.That(s["t"].Metin, Is.EqualTo("komut"));
            Assert.That(s["tur"].Tamsayi, Is.EqualTo(4));
            Assert.That(s["komut"].Metin, Is.EqualTo("soruSor"));
            Assert.That(s["hedef"].Metin, Is.EqualTo("p3"));
            Assert.That(s["metin"].Metin, Is.EqualTo("Kim \"en\" çok\\uyur?\n"));

            Assert.That(JsonDeger.Coz(Cevirici.CevapSec(4, new OyuncuId(2)))["komut"].Metin, Is.EqualTo("cevapSec"));
            Assert.That(JsonDeger.Coz(Cevirici.Oy(4, false))["ifsa"].Mantik, Is.False);
            Assert.That(JsonDeger.Coz(Cevirici.HavuzaEkle(0, "x"))["tur"].Tamsayi, Is.EqualTo(0));
            Assert.That(JsonDeger.Coz(Cevirici.BedelOde(5))["komut"].Metin, Is.EqualTo("bedelOde"));
            Assert.That(JsonDeger.Coz(Cevirici.KarariGec(5))["komut"].Metin, Is.EqualTo("karariGec"));
            Assert.That(JsonDeger.Coz(Cevirici.IkiyeKatla(5))["komut"].Metin, Is.EqualTo("ikiyeKatla"));
            Assert.That(JsonDeger.Coz(Cevirici.OneriCek(5))["komut"].Metin, Is.EqualTo("oneriCek"));
            Assert.That(JsonDeger.Coz(Cevirici.SoruyuDegistir(5))["komut"].Metin, Is.EqualTo("soruyuDegistir"));
            Assert.That(JsonDeger.Coz(Cevirici.At(new OyuncuId(7)))["kim"].Metin, Is.EqualTo("p7"));
            Assert.That(JsonDeger.Coz(Cevirici.Buradayim())["t"].Metin, Is.EqualTo("buradayim"));
            Assert.That(JsonDeger.Coz(Cevirici.TekrarOyna())["t"].Metin, Is.EqualTo("tekrarOyna"));
            Assert.That(JsonDeger.Coz(Cevirici.Bitir())["t"].Metin, Is.EqualTo("bitir"));
            Assert.That(JsonDeger.Coz(Cevirici.Baslat())["t"].Metin, Is.EqualTo("baslat"));
        }

        [Test]
        public void Giden_HamleBicimi_GeriCozulunceAyniHamle()
        {
            var hamleler = new[] { Hamle.TasKagitMakas(TkmSecim.Makas), Hamle.TekCift(4, true), Hamle.TekCift(1), Hamle.ZarAt() };
            foreach (var h in hamleler)
            {
                var m = JsonDeger.Coz(Cevirici.HamleYap(9, h));
                Assert.That(m["komut"].Metin, Is.EqualTo("hamle"));
                var geri = Cevirici.HamleCoz(m["hamle"]).Value;
                Assert.That(geri.Tur, Is.EqualTo(h.Tur));
                Assert.That(geri.Tkm, Is.EqualTo(h.Tkm));
                Assert.That(geri.Sayi, Is.EqualTo(h.Sayi));
                Assert.That(geri.TekDiyor, Is.EqualTo(h.TekDiyor));
            }
            Assert.That(JsonDeger.Coz(Cevirici.HamleYap(9, Hamle.TasKagitMakas(TkmSecim.Tas)))["hamle"]["secim"].Metin, Is.EqualTo("tas"));
        }
    }
}
