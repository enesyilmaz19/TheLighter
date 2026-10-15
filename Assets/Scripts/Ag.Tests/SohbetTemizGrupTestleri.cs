using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cakmak.Kurallar;
using NUnit.Framework;
using TheLighter.Ag;

namespace TheLighter.Testler
{
    /// <summary>
    /// S5 kapanışı (docs/Protokol.md "Sohbet", "Temiz mod", "Grup paketi"): sohbet mesajları, UYGUNSUZ alanları,
    /// atılma nedenleri, lobideki grup paketi sayısı, grup paketinin parçalara bölünmesi ve yüklenmesi.
    /// </summary>
    public class SohbetTemizGrupTestleri
    {
        static readonly OyuncuId P1 = new OyuncuId(1);
        static readonly OyuncuId P3 = new OyuncuId(3);

        // ------------------------------------------------------------ Sohbet

        [Test]
        public void Sohbet_Genel_Cozulur()
        {
            var m = Cevirici.Coz("{ \"t\": \"sohbet\", \"id\": \"m41\", \"kimden\": \"p1\", \"kime\": null, \"metin\": \"selam millet\" }") as SohbetGelen;
            Assert.That(m, Is.Not.Null);
            Assert.That(m.Id, Is.EqualTo("m41"));
            Assert.That(m.Kimden, Is.EqualTo(P1));
            Assert.That(m.Kime, Is.Null);
            Assert.That(m.Metin, Is.EqualTo("selam millet"));
            Assert.That(m.ToString(), Does.Not.Contain("selam"), "mesaj metni günlüğe düşmesin");
        }

        [Test]
        public void Sohbet_Dm_Cozulur_BozukKimeAtilir_EksikAlanAtilir()
        {
            var m = (SohbetGelen)Cevirici.Coz("{\"t\":\"sohbet\",\"id\":\"m2\",\"kimden\":\"p1\",\"kime\":\"p3\",\"metin\":\"psst\"}");
            Assert.That(m.Kime, Is.EqualTo(P3));
            Assert.That(Cevirici.Coz("{\"t\":\"sohbet\",\"kimden\":\"p1\",\"kime\":\"x\",\"metin\":\"psst\"}"), Is.Null, "bozuk DM genele düşmez");
            Assert.That(Cevirici.Coz("{\"t\":\"sohbet\",\"kimden\":\"p1\"}"), Is.Null);
            Assert.That(Cevirici.Coz("{\"t\":\"sohbet\",\"metin\":\"a\"}"), Is.Null);
        }

        [Test]
        public void Sohbet_Giden_GenelKimeYazmaz_DmKimeYazar()
        {
            var g = JsonDeger.Coz(Cevirici.Sohbet(null, "selam"));
            Assert.That(g["t"].Metin, Is.EqualTo("sohbet"));
            Assert.That(g["kime"].Var, Is.False);
            Assert.That(g["metin"].Metin, Is.EqualTo("selam"));
            var d = JsonDeger.Coz(Cevirici.Sohbet(P3, "psst"));
            Assert.That(d["kime"].Metin, Is.EqualTo("p3"));
        }

        [Test]
        public void SohbetMesaji_KanalVeBenim()
        {
            // Genel, benimki (sunucu yazana da yollar)
            var a = OnlineIstemci.SohbetMesajiYap(new SohbetGelen { Kimden = P1, Metin = "x" }, P1);
            Assert.That(a.Kanal, Is.Null);
            Assert.That(a.Benim, Is.True);
            // DM bana geldi: kanal yazan
            var b = OnlineIstemci.SohbetMesajiYap(new SohbetGelen { Kimden = P3, Kime = P1, Metin = "x" }, P1);
            Assert.That(b.Kanal, Is.EqualTo(P3));
            Assert.That(b.Benim, Is.False);
            // DM'im döndü: kanal alan
            var c = OnlineIstemci.SohbetMesajiYap(new SohbetGelen { Kimden = P1, Kime = P3, Metin = "x" }, P1);
            Assert.That(c.Kanal, Is.EqualTo(P3));
            Assert.That(c.Benim, Is.True);
        }

        // ------------------------------------------------------------ Temiz mod

        [Test]
        public void Uygunsuz_IhlalVeSinir()
        {
            var h = (HataMesaji)Cevirici.Coz("{ \"t\": \"hata\", \"kod\": \"UYGUNSUZ\", \"ihlal\": 1, \"sinir\": 3 }");
            Assert.That(h.Sonuc, Is.EqualTo(Sonuc.Uygunsuz));
            Assert.That(h.Ihlal, Is.EqualTo(1));
            Assert.That(h.Sinir, Is.EqualTo(3));
        }

        [Test]
        public void Atildin_Neden()
        {
            var k = (HataMesaji)Cevirici.Coz("{\"t\":\"hata\",\"kod\":\"ATILDIN\",\"neden\":\"temiz\"}");
            Assert.That(k.Neden, Is.EqualTo("temiz"));
            var y = (HataMesaji)Cevirici.Coz("{\"t\":\"hata\",\"kod\":\"ATILDIN\"}");
            Assert.That(y.Neden, Is.Null);

            Assert.That(OnlineIstemci.AtilmaNedeni("kurucu"), Is.EqualTo("ATILDIN.kurucu"));
            Assert.That(OnlineIstemci.AtilmaNedeni("temiz"), Is.EqualTo("ATILDIN.temiz"));
            Assert.That(OnlineIstemci.AtilmaNedeni(null), Is.EqualTo("ATILDIN"));
            Assert.That(OnlineIstemci.AtilmaNedeni("bilinmeyen"), Is.EqualTo("ATILDIN"));
        }

        [Test]
        public void AtildiOlayi_Neden()
        {
            var o = (OlayMesaji)Cevirici.Coz("{\"t\":\"olay\",\"olay\":\"atildi\",\"kim\":\"p3\",\"neden\":\"kurucu\"}");
            Assert.That(o.Atildi, Is.True);
            Assert.That(o.Kim, Is.EqualTo(P3));
            Assert.That(o.Neden, Is.EqualTo("kurucu"));
        }

        // ------------------------------------------------------------ Grup paketi

        const string Lobi =
            "{\"t\":\"durum\",\"surum\":4,\"oda\":\"KXTPM\",\"asama\":\"lobi\",\"kurucu\":\"p1\"," +
            "\"oyuncular\":[{\"id\":\"p1\",\"isim\":\"Enes\",\"renk\":0,\"bagli\":true}],\"ayarlar\":{},\"grupPaketi\":37}";

        [Test]
        public void Lobi_GrupPaketiSayisi()
        {
            var d = (DurumMesaji)Cevirici.Coz(Lobi);
            Assert.That(d.GrupPaketi, Is.EqualTo(37));
            var bos = (DurumMesaji)Cevirici.Coz(Lobi.Replace(",\"grupPaketi\":37", ""));
            Assert.That(bos.GrupPaketi, Is.EqualTo(0));
        }

        static List<string> Sorular(int n, int uzunluk)
        {
            var l = new List<string>();
            for (int i = 0; i < n; i++)
            {
                // Kaçış isteyen karakterler ve çok baytlı harfler: sınır JSON'un UTF-8 boyutuyla ölçülmeli.
                string bas = i + " \"ğüşıöç\\ ";
                l.Add((bas + new string('ş', uzunluk)).Substring(0, uzunluk));
            }
            return l;
        }

        [Test]
        public void GrupPaketi_Parcalar_SiniriGecmez_SiraKorunur_IlkiIlk()
        {
            var sorular = Sorular(500, 140);
            var parcalar = Cevirici.GrupPaketiParcalari(sorular);
            Assert.That(parcalar.Count, Is.GreaterThan(1));

            var toplanan = new List<string>();
            for (int i = 0; i < parcalar.Count; i++)
            {
                Assert.That(Encoding.UTF8.GetByteCount(parcalar[i]), Is.LessThanOrEqualTo(Cevirici.GrupPaketiParcaBayt), "parça " + i);
                var j = JsonDeger.Coz(parcalar[i]);
                Assert.That(j["t"].Metin, Is.EqualTo("grupPaketi"));
                Assert.That(j["ilk"].MantikYa(false), Is.EqualTo(i == 0), "sadece ilk parça ilk:true");
                Assert.That(j["sorular"].Elemanlar.Count, Is.GreaterThan(0));
                foreach (var s in j["sorular"].Elemanlar) toplanan.Add(s.Metin);
            }
            Assert.That(toplanan, Is.EqualTo(sorular), "hepsi, sırasıyla, bir kez");
        }

        [Test]
        public void GrupPaketi_BosUzunTekrarAtlanir_EnFazla500()
        {
            var sorular = new List<string> { "Kim en çok uyur?", "  ", null, "kim en çok uyur?", new string('a', 141), " Kim geç kalır? " };
            var p = Cevirici.GrupPaketiParcalari(sorular);
            Assert.That(p.Count, Is.EqualTo(1));
            var liste = JsonDeger.Coz(p[0])["sorular"].Elemanlar.Select(x => x.Metin).ToList();
            Assert.That(liste, Is.EqualTo(new[] { "Kim en çok uyur?", "Kim geç kalır?" }));

            var cok = Cevirici.GrupPaketiParcalari(Enumerable.Range(0, 700).Select(i => "Soru " + i));
            int toplam = cok.Sum(x => JsonDeger.Coz(x)["sorular"].Elemanlar.Count);
            Assert.That(toplam, Is.EqualTo(Cevirici.GrupPaketiEnFazla));

            Assert.That(Cevirici.GrupPaketiParcalari(new string[0]), Is.Empty);
        }

        [Test]
        public void Yukleyici_SonrakiParca_DurumGelinceGider_BitinceBitti()
        {
            var giden = new List<string>();
            var y = new GrupPaketiYukleyici(Sorular(200, 140), j => { giden.Add(j); return true; });
            Assert.That(y.Toplam, Is.GreaterThan(2));
            y.Basla(10);
            Assert.That(giden.Count, Is.EqualTo(1));
            Assert.That(JsonDeger.Coz(giden[0])["ilk"].MantikYa(false), Is.True);

            y.DurumGeldi(10);
            Assert.That(giden.Count, Is.EqualTo(1), "eski durum: bekler");
            y.DurumGeldi(11);
            Assert.That(giden.Count, Is.EqualTo(2));
            Assert.That(JsonDeger.Coz(giden[1])["ilk"].Var, Is.False);

            // Durum gelmezse süre dolunca yine gider.
            y.Ilerle(GrupPaketiYukleyici.BeklemeSn + 0.1);
            Assert.That(giden.Count, Is.EqualTo(3));

            int surum = 12;
            while (y.Durum == GrupPaketiYukleyici.Hal.Yukleniyor) y.DurumGeldi(++surum);
            Assert.That(y.Durum, Is.EqualTo(GrupPaketiYukleyici.Hal.Bitti));
            Assert.That(giden.Count, Is.EqualTo(y.Toplam));
            Assert.That(y.Biten, Is.EqualTo(y.Toplam));
        }

        [Test]
        public void Yukleyici_HataGelinceDurur()
        {
            var giden = new List<string>();
            var y = new GrupPaketiYukleyici(Sorular(200, 140), j => { giden.Add(j); return true; });
            y.Basla(1);
            y.HataGeldi("GECERSIZ_GIRDI");
            Assert.That(y.Durum, Is.EqualTo(GrupPaketiYukleyici.Hal.Hata));
            Assert.That(y.HataKodu, Is.EqualTo("GECERSIZ_GIRDI"));
            y.DurumGeldi(5);
            y.Ilerle(10);
            Assert.That(giden.Count, Is.EqualTo(1), "hatadan sonra parça gitmez");
        }
    }
}
