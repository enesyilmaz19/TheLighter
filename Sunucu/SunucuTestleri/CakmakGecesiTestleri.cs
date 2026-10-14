using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using Cakmak.Kurallar;
using NUnit.Framework;

namespace SunucuTestleri
{
    /// <summary>Protokol.md, "Çakmak Gecesi" (docs/Oyunlar.md, 5). Bütün kurallar kural testlerinde; burada mesajlar, gizlilik, kanallar.</summary>
    public class CakmakGecesiTestleri : SunucuTesti
    {
        /// <summary>Sadece <paramref name="acik"/> özel rolleri açık bir Çakmak Gecesi başlatır.</summary>
        async Task<TestIstemci[]> GeceBaslat(int n, params string[] acik)
        {
            var o = await OdaKur(n);
            var kapali = Oyun.GeceRolleri.Where(r => r != Rol.Masum && r != Rol.Katil).Select(Ad).Where(r => !acik.Contains(r)).ToArray();
            await o[0].Gonder(new { t = "ayarlar", oyun = "cakmakGecesi", kapaliRoller = kapali });
            foreach (var x in o) await x.Bekle("durum", d => (string)d["ayarlar"]["oyun"] == "cakmakGecesi");
            await o[0].Gonder(new { t = "baslat" });
            foreach (var x in o) await x.Bekle("durum", d => (string)d["asama"] == "soruSorma");
            return o;
        }

        /// <summary>Protokoldeki adı: "SeriKatil" → "seriKatil".</summary>
        static string Ad(Rol r) => char.ToLowerInvariant(r.ToString()[0]) + r.ToString().Substring(1);

        static List<TestIstemci> RolSahipleri(TestIstemci[] o, string rol) => o.Where(x => (string)x.SonDurum["sana"]["rol"] == rol).ToList();

        static int Tur(TestIstemci x) => (int)x.SonDurum["tur"];

        /// <summary>Bir çakmak turu (C kazanır); herkes sıradaki turu ya da linci görene kadar.</summary>
        static async Task TurOyna(TestIstemci[] o)
        {
            int tur = Tur(o[0]);
            var a = Kim(o, (string)o[0].SonDurum["a"]);
            var b = Kim(o, (string)a.SonDurum["secilebilir"][0]);
            await a.Gonder(new { t = "komut", tur, komut = "soruSor", hedef = b.Sen, metin = "Sence katil kim?" });
            await b.Bekle("durum", m => (string)m["asama"] == "cevapSecme" && (int)m["tur"] == tur);
            var c = Kim(o, (string)b.SonDurum["secilebilir"][0]);
            await b.Gonder(new { t = "komut", tur, komut = "cevapSec", hedef = c.Sen });
            await c.Bekle("durum", m => (string)m["asama"] == "miniOyun" && (int)m["tur"] == tur);
            await b.Gonder(new { t = "komut", tur, komut = "hamle", hamle = new { oyun = "tkm", secim = "tas" } });
            await c.Gonder(new { t = "komut", tur, komut = "hamle", hamle = new { oyun = "tkm", secim = "kagit" } });
            foreach (var x in o)
                await x.Bekle("durum", m => (int)m["tur"] > tur || (string)m["asama"] == "linc", TimeSpan.FromSeconds(10));
        }

        static List<string> GeceSorulari([CallerFilePath] string buDosya = "") =>
            Paket.Ayristir(File.ReadAllText(Path.Combine(Path.GetDirectoryName(buDosya), "..", "..", "Assets", "Icerik", "GeceSorulari.csv"))).ToList();

        [Test]
        public async Task Rolu_sadece_sahibi_katiller_birbirini_rolleriyle_oneriler_gece_sorulari()
        {
            var o = await GeceBaslat(8, "patron", "casus");
            var katiller = o.Where(x => (string)x.SonDurum["sana"]["rol"] is "patron" or "casus").ToList();
            Assert.That(katiller.Count, Is.EqualTo(2));
            foreach (var x in o)
            {
                var d = x.SonDurum;
                Assert.That((string)d["oyun"], Is.EqualTo("cakmakGecesi"));
                Assert.That((int)d["gun"], Is.EqualTo(1));
                if (katiller.Contains(x))
                {
                    Assert.That(d["sana"]["katiller"].AsArray().Select(k => ((string)k["kim"], (string)k["rol"])),
                        Is.EquivalentTo(katiller.Select(k => (k.Sen, (string)k.SonDurum["sana"]["rol"]))));
                    continue;
                }
                Assert.That(d["sana"]["katiller"].AsArray(), Is.Empty);
                Assert.That(string.Join("\n", x.Hepsi), Does.Not.Contain("\"rol\":\"patron\"").And.Not.Contain("\"rol\":\"casus\""), "katil sızdı");
            }

            var a = Kim(o, (string)o[0].SonDurum["a"]);
            await a.Gonder(new { t = "komut", tur = 1, komut = "oneriCek" });
            var oneri = (string)(await a.Bekle("durum", m => m["sana"]["oneri"] != null))["sana"]["oneri"];
            Assert.That(GeceSorulari(), Has.Member(oneri), "öneriler Çakmak Gecesi sorularından");
        }

        [Test]
        public async Task Ayarlar_gecersizleri_ve_kisi_siniri()
        {
            var o = await OdaKur(5);
            await o[0].Gonder(new { t = "ayarlar", oyun = "cakmakGecesi", gorevler = true });
            await o[0].Hata("GECERSIZ_GIRDI");
            await o[0].Gonder(new { t = "ayarlar", oyun = "cakmakGecesi", gecelikKurban = 4 });
            await o[0].Hata("GECERSIZ_GIRDI");
            await o[0].Gonder(new { t = "ayarlar", oyun = "cakmakGecesi", kapaliRoller = new[] { "masum" } });
            await o[0].Hata("GECERSIZ_GIRDI");
            await o[0].Gonder(new { t = "ayarlar", oyun = "cakmakGecesi", kapaliRoller = new[] { "ejderha" } });
            await o[0].Hata("GECERSIZ_MESAJ");
            await o[0].Gonder(new { t = "ayarlar", oyun = "cakmakGecesi", rolAcilir = false, gecelikKurban = 2, kapaliRoller = new[] { "palyaco", "deli" } });
            var d = await o[1].Bekle("durum", m => (string)m["ayarlar"]["oyun"] == "cakmakGecesi");
            Assert.That((bool)d["ayarlar"]["rolAcilir"], Is.False);
            Assert.That((int)d["ayarlar"]["gecelikKurban"], Is.EqualTo(2));
            Assert.That(Idler(d["ayarlar"]["kapaliRoller"]), Is.EqualTo(new[] { "palyaco", "deli" }));
            await o[0].Gonder(new { t = "baslat" });
            await o[0].Hata("OYUNCU_SAYISI_GECERSIZ");

            await o[0].Gonder(new { t = "ayarlar", oyun = "cakmak" });
            await o[0].Bekle("durum", m => (string)m["ayarlar"]["oyun"] == "cakmak");
            await o[0].Gonder(new { t = "baslat" });
            await o[0].Bekle("durum", m => (string)m["asama"] == "soruSorma");
            await o[0].Gonder(new { t = "komut", tur = 1, komut = "lincOyu", hedef = "p2" });
            await o[0].Hata("YANLIS_ASAMA");
            await o[0].Gonder(new { t = "komut", tur = 1, komut = "geceSecimi" });
            await o[0].Hata("YANLIS_ASAMA");
            await o[0].Gonder("{\"t\":\"komut\",\"tur\":1,\"komut\":\"suikast\",\"hedef\":\"p2\"}");
            await o[0].Hata("GECERSIZ_MESAJ");
            await o[0].Gonder(new { t = "sohbet", kime = "oluler", metin = "merhaba" });
            await o[0].Hata("GECERSIZ_GIRDI");
        }

        [Test]
        public async Task Oda_18_kisilik_diger_oyunlar_10_dan_fazlayla_baslamaz()
        {
            var o = await OdaKur(11);
            await o[0].Gonder(new { t = "baslat" });
            await o[0].Hata("OYUNCU_SAYISI_GECERSIZ");
            for (int i = 11; i < Oyun.GeceEnFazlaOyuncu; i++) await Katil(o[0].Oda, "Oyuncu" + (i + 1));
            var fazla = await Baglan();
            await fazla.Gonder(new { t = "katil", oda = o[0].Oda, isim = "Fazla", anahtar = (string)null });
            await fazla.Hata("ODA_DOLU");
        }

        [Test]
        public async Task Suikast_linc_gece_isirma_katiller_kazanir_kanallar_ve_gizlilik()
        {
            // 8 kişi: Suikastçı, Vampir, Koruyucu, Dedektif, 4 Masum.
            var o = await GeceBaslat(8, "suikastci", "vampir", "koruyucu", "dedektif");
            var suikastci = RolSahipleri(o, "suikastci").Single();
            var vampir = RolSahipleri(o, "vampir").Single();
            var koruyucu = RolSahipleri(o, "koruyucu").Single();
            var dedektif = RolSahipleri(o, "dedektif").Single();
            var masumlar = RolSahipleri(o, "masum");
            Assert.That(masumlar.Count, Is.EqualTo(4));

            for (int i = 0; i < 4; i++) await TurOyna(o);
            await o[0].Bekle("olay", m => (string)m["olay"] == "lincBasladi");

            // Linç: Suikastçı masumlar[0]'ın rolünü bilir, o ölür; sonra masumlar[1] linç edilir.
            await suikastci.Gonder(new { t = "komut", tur = Tur(suikastci), komut = "suikast", hedef = masumlar[0].Sen, rol = "masum" });
            var suikast = await o[1].Bekle("olay", m => (string)m["olay"] == "suikast");
            Assert.That((string)suikast["kim"], Is.EqualTo(masumlar[0].Sen));
            foreach (var x in o.Where(x => x != masumlar[0]))
                await x.Gonder(new { t = "komut", tur = Tur(x), komut = "lincOyu", hedef = (x == masumlar[1] ? masumlar[2] : masumlar[1]).Sen });
            Assert.That((string)(await o[1].Bekle("olay", m => (string)m["olay"] == "lincBitti"))["kim"], Is.EqualTo(masumlar[1].Sen));
            foreach (var x in o) await x.Bekle("durum", m => (string)m["asama"] == "gece");

            // Gece sohbeti: genel kapalı, ölü genele yazamaz; katil kanalı katillere, ölüler kanalı ölülere.
            await koruyucu.Gonder(new { t = "sohbet", metin = "gece" });
            await koruyucu.Hata("GECERSIZ_GIRDI");
            await masumlar[1].Gonder(new { t = "sohbet", kime = koruyucu.Sen, metin = "ipucu" });
            await masumlar[1].Hata("GECERSIZ_GIRDI");
            var katilMetni = GizliMetin("Katil");
            await vampir.Gonder(new { t = "sohbet", kime = "katil", metin = katilMetni });
            Assert.That((string)(await suikastci.Bekle("sohbet", s => (string)s["metin"] == katilMetni))["kime"], Is.EqualTo("katil"));
            await dedektif.Gonder(new { t = "sohbet", kime = "katil", metin = "ben de" });
            await dedektif.Hata("GECERSIZ_GIRDI");
            var oluMetni = GizliMetin("Olu");
            await masumlar[1].Gonder(new { t = "sohbet", kime = "oluler", metin = oluMetni });
            await masumlar[0].Bekle("sohbet", s => (string)s["metin"] == oluMetni);

            // Seçimler: Vampir masumlar[2]'ye oy verir ve masumlar[3]'ü ısırır; Suikastçı da masumlar[2]; Koruyucu kendini; Dedektif Vampir'e bakar.
            await vampir.Gonder(new { t = "komut", tur = Tur(vampir), komut = "geceSecimi", hedef = masumlar[2].Sen, ikinci = masumlar[3].Sen });
            var secimler = (await suikastci.Bekle("durum", m => m["sana"]["katilSecimleri"].AsArray().Count == 1))["sana"]["katilSecimleri"];
            Assert.That((string)secimler[0]["hedef"], Is.EqualTo(masumlar[2].Sen));
            await suikastci.Gonder(new { t = "komut", tur = Tur(suikastci), komut = "geceSecimi", hedef = masumlar[2].Sen });
            await koruyucu.Gonder(new { t = "komut", tur = Tur(koruyucu), komut = "geceSecimi", hedef = koruyucu.Sen });
            await dedektif.Gonder(new { t = "komut", tur = Tur(dedektif), komut = "geceSecimi", hedef = vampir.Sen });
            foreach (var x in masumlar.Skip(2)) await x.Gonder(new { t = "komut", tur = Tur(x), komut = "geceSecimi" }); // pas

            Assert.That((string)(await o[1].Bekle("olay", m => (string)m["olay"] == "elendi"))["kim"], Is.EqualTo(masumlar[2].Sen));
            await o[1].Bekle("olay", m => (string)m["olay"] == "geceBitti");
            // Isırılan katil oldu: 3 katile 2 masum, katiller kazandı.
            var son = await o[1].Bekle("durum", m => (string)m["asama"] == "oyunSonu");
            var s = son["ozet"]["gece"];
            Assert.That((string)s["kazanan"], Is.EqualTo("katiller"));
            Assert.That(Idler(s["kazananlar"]), Is.EquivalentTo(new[] { suikastci.Sen, vampir.Sen, masumlar[3].Sen }));
            Assert.That(s["roller"].AsArray().Count, Is.EqualTo(8));
            var bulgu = (await dedektif.Bekle("durum", m => (string)m["asama"] == "oyunSonu"))["sana"]["bulgularin"][0];
            Assert.That(((string)bulgu["kim"], (bool)bulgu["supheli"]), Is.EqualTo((vampir.Sen, true)));

            // Gizlilik: katil kanalı, katil oyları ve Dedektif'in bulgusu başkasına gitmedi; ölüler kanalı canlılara gitmedi.
            foreach (var x in o.Where(x => x != vampir && x != suikastci))
            {
                Assert.That(string.Join("\n", x.Hepsi), Does.Not.Contain(katilMetni), x.Sen);
                Assert.That(string.Join("\n", x.Hepsi), Does.Not.Contain("\"katilSecimleri\":[{"), x.Sen);
            }
            foreach (var x in o.Where(x => x != dedektif))
                Assert.That(string.Join("\n", x.Hepsi), Does.Not.Contain("\"bulgularin\":[{"), x.Sen);
            foreach (var x in o.Where(x => x != masumlar[0] && x != masumlar[1] && x != masumlar[2]))
                Assert.That(string.Join("\n", x.Hepsi), Does.Not.Contain(oluMetni), x.Sen);
        }
    }
}
