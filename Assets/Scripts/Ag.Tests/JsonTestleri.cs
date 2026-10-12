using System.Linq;
using System.Text;
using NUnit.Framework;
using TheLighter.Ag;

namespace TheLighter.Testler
{
    public class JsonTestleri
    {
        [Test]
        public void IcIceNesneVeDizi()
        {
            var d = JsonDeger.Coz(" { \"a\" : [ 1 , -2.5 , true , false , null , \"x\" ] , \"b\" : { \"c\" : { } , \"d\" : [ ] } } ");
            Assert.That(d.Tur, Is.EqualTo(JsonTuru.Nesne));
            Assert.That(d["a"].Elemanlar.Count, Is.EqualTo(6));
            Assert.That(d["a"][0].Tamsayi, Is.EqualTo(1));
            Assert.That(d["a"][1].Tamsayi, Is.Null); // ondalıklı
            Assert.That(d["a"][2].Mantik, Is.True);
            Assert.That(d["a"][3].Mantik, Is.False);
            Assert.That(d["a"][4].Var, Is.False);
            Assert.That(d["a"][5].Metin, Is.EqualTo("x"));
            Assert.That(d["b"]["c"].Tur, Is.EqualTo(JsonTuru.Nesne));
            Assert.That(d["b"]["d"].Elemanlar, Is.Empty);
        }

        [Test]
        public void OlmayanAlanVeYanlisTip_HicAtmaz()
        {
            var d = JsonDeger.Coz("{\"s\":\"metin\",\"n\":5}");
            Assert.That(d["yok"].Var, Is.False);
            Assert.That(d["yok"]["daha"][3].Metin, Is.Null);
            Assert.That(d["s"].Tamsayi, Is.Null);
            Assert.That(d["n"].Metin, Is.Null);
            Assert.That(d["n"].Elemanlar, Is.Empty);
            Assert.That(d["n"].TamsayiYa(9), Is.EqualTo(5));
            Assert.That(d["s"].TamsayiYa(9), Is.EqualTo(9));
        }

        [Test]
        public void KacisKarakterleriVeTurkce()
        {
            var d = JsonDeger.Coz("{\"m\":\"a\\\"b\\\\c\\/d\\n\\t\\u00e7\\u011f\\ud83d\\ude00 şİ\"}");
            Assert.That(d["m"].Metin, Is.EqualTo("a\"b\\c/d\n\tçğ😀 şİ"));
        }

        [Test]
        public void YazilaniOkuyunca_Ayni()
        {
            string tuhaf = "\"tırnak\" \\ ters\n satır\t sekme \u0001 kontrol 😀 ğüşiöç <size=500>";
            string json = new JsonNesne().Ekle("m", tuhaf).Ekle("n", -42).Ekle("b", true).Ekle("yok", null)
                .Ekle("l", new[] { "a", "b\"" }).Ekle("o", new JsonNesne().Ekle("x", 1)).ToString();
            var d = JsonDeger.Coz(json);
            Assert.That(d["m"].Metin, Is.EqualTo(tuhaf));
            Assert.That(d["n"].Tamsayi, Is.EqualTo(-42));
            Assert.That(d["b"].Mantik, Is.True);
            Assert.That(d["yok"].Var, Is.False);
            Assert.That(d["l"].Elemanlar.Select(x => x.Metin), Is.EqualTo(new[] { "a", "b\"" }));
            Assert.That(d["o"]["x"].Tamsayi, Is.EqualTo(1));
        }

        [TestCase("")]
        [TestCase("{")]
        [TestCase("{\"a\":}")]
        [TestCase("{\"a\":1,}")]
        [TestCase("{a:1}")]
        [TestCase("[1 2]")]
        [TestCase("\"kapanmamış")]
        [TestCase("{\"a\":tru}")]
        [TestCase("{\"a\":\"\\x\"}")]
        [TestCase("{\"a\":\"\\u12\"}")]
        [TestCase("{\"a\":1} fazla")]
        [TestCase("{\"a\":--1}")]
        [TestCase("{\"a\":\"satır\nkırık\"}")]
        public void BozukJson_DeneFalse(string json)
        {
            Assert.That(JsonDeger.Dene(json, out var d), Is.False);
            Assert.That(d.Var, Is.False);
        }

        [Test]
        public void CokDerinJson_YiginiTasirmaz()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < 10000; i++) sb.Append('[');
            Assert.That(JsonDeger.Dene(sb.ToString(), out _), Is.False);
        }

        [Test]
        public void AyniAlanIkiKez_SonuncusuGecerli()
        {
            Assert.That(JsonDeger.Coz("{\"a\":1,\"a\":2}")["a"].Tamsayi, Is.EqualTo(2));
        }
    }
}
