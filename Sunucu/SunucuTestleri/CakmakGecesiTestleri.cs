using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using Cakmak.Kurallar;
using NUnit.Framework;

namespace SunucuTestleri
{
    /// <summary>Protokol.md, "Vampir Köylü" (docs/Oyunlar.md, 5). Bütün kurallar kural testlerinde; burada mesajlar, gizlilik, kanallar.</summary>
    public class VampirTestleri : SunucuTesti
    {
        async Task<TestIstemci[]> VampirBaslat(int n)
        {
            var o = await OdaKur(n);
            await o[0].Gonder(new { t = "ayarlar", oyun = "vampir" });
            foreach (var x in o) await x.Bekle("durum", d => (string)d["ayarlar"]["oyun"] == "vampir");
            await o[0].Gonder(new { t = "baslat" });
            foreach (var x in o) await x.Bekle("durum", d => (string)d["asama"] == "soruSorma");
            return o;
        }

        static List<TestIstemci> Rol(TestIstemci[] o, string rol) => o.Where(x => (string)x.SonDurum["sana"]["rol"] == rol).ToList();

        static int Tur(TestIstemci x) => (int)x.SonDurum["tur"];

        static Task Komut(TestIstemci x, string komut, string hedef) =>
            x.Gonder(new { t = "komut", tur = Tur(x), komut, hedef });

        /// <summary>Bir çakmak turu (C kazanır); herkes sıradaki turu ya da linci görene kadar.</summary>
        static async Task TurOyna(TestIstemci[] o)
        {
            int tur = Tur(o[0]);
            var a = Kim(o, (string)o[0].SonDurum["a"]);
            var b = Kim(o, (string)a.SonDurum["secilebilir"][0]);
            await a.Gonder(new { t = "komut", tur, komut = "soruSor", hedef = b.Sen, metin = "Sence vampir kim?" });
            await b.Bekle("durum", m => (string)m["asama"] == "cevapSecme" && (int)m["tur"] == tur);
            var c = Kim(o, (string)b.SonDurum["secilebilir"][0]);
            await b.Gonder(new { t = "komut", tur, komut = "cevapSec", hedef = c.Sen });
            await c.Bekle("durum", m => (string)m["asama"] == "miniOyun" && (int)m["tur"] == tur);
            await b.Gonder(new { t = "komut", tur, komut = "hamle", hamle = new { oyun = "tkm", secim = "tas" } });
            await c.Gonder(new { t = "komut", tur, komut = "hamle", hamle = new { oyun = "tkm", secim = "kagit" } });
            foreach (var x in o)
                await x.Bekle("durum", m => (int)m["tur"] > tur || (string)m["asama"] == "linc", TimeSpan.FromSeconds(10));
        }

        static List<string> VampirSorulari([CallerFilePath] string buDosya = "") =>
            Paket.Ayristir(File.ReadAllText(Path.Combine(Path.GetDirectoryName(buDosya), "..", "..", "Assets", "Icerik", "VampirSorulari.csv"))).ToList();

        [Test]
        public async Task Rolu_sadece_sahibi_vampirler_birbirini_oneriler_vampir_sorulari()
        {
            var o = await VampirBaslat(8);
            var vampirler = Rol(o, "vampir");
            Assert.That(vampirler.Count, Is.EqualTo(2));
            Assert.That(Rol(o, "doktor").Count + Rol(o, "gozcu").Count + Rol(o, "koylu").Count, Is.EqualTo(6));
            foreach (var x in o)
            {
                var d = x.SonDurum;
                Assert.That((int)d["gun"], Is.EqualTo(1));
                Assert.That((int)d["toplamTur"], Is.Zero);
                if (vampirler.Contains(x))
                {
                    Assert.That(Idler(d["sana"]["vampirler"]), Is.EquivalentTo(vampirler.Select(v => v.Sen)));
                    continue;
                }
                Assert.That(d["sana"]["vampirler"].AsArray(), Is.Empty);
                Assert.That(string.Join("\n", x.Hepsi), Does.Not.Contain("\"rol\":\"vampir\""), "vampirin kim olduğu sızdı");
            }

            var a = Kim(o, (string)o[0].SonDurum["a"]);
            await a.Gonder(new { t = "komut", tur = 1, komut = "oneriCek" });
            var oneri = (string)(await a.Bekle("durum", m => m["sana"]["oneri"] != null))["sana"]["oneri"];
            Assert.That(VampirSorulari(), Has.Member(oneri), "Vampir'de öneri suçlama sorularından");
        }

        [Test]
        public async Task Ayarlar_6_kisiden_az_baslamaz_rol_acilmasi_ayari_gecersiz_komutlar()
        {
            var o = await OdaKur(5);
            await o[0].Gonder(new { t = "ayarlar", oyun = "vampir", gorevler = true });
            await o[0].Hata("GECERSIZ_GIRDI");
            await o[0].Gonder(new { t = "ayarlar", oyun = "vampir", rolAcilir = false });
            await o[1].Bekle("durum", m => (string)m["ayarlar"]["oyun"] == "vampir" && !(bool)m["ayarlar"]["rolAcilir"]);
            await o[0].Gonder(new { t = "baslat" });
            await o[0].Hata("OYUNCU_SAYISI_GECERSIZ");

            await o[0].Gonder(new { t = "ayarlar", oyun = "cakmak" });
            await o[0].Bekle("durum", m => (string)m["ayarlar"]["oyun"] == "cakmak");
            await o[0].Gonder(new { t = "baslat" });
            await o[0].Bekle("durum", m => (string)m["asama"] == "soruSorma");
            await o[0].Gonder(new { t = "komut", tur = 1, komut = "lincOyu", hedef = "p2" });
            await o[0].Hata("YANLIS_ASAMA");
            await o[0].Gonder("{\"t\":\"komut\",\"tur\":1,\"komut\":\"geceSecimi\"}");
            await o[0].Hata("GECERSIZ_MESAJ");
            await o[0].Gonder(new { t = "sohbet", kime = "oluler", metin = "merhaba" });
            await o[0].Hata("GECERSIZ_GIRDI");
        }

        [Test]
        public async Task Gunduz_linc_gece_sabah_kanallar_ve_gizlilik()
        {
            var o = await VampirBaslat(6);
            var vampir = Rol(o, "vampir").Single();
            var doktor = Rol(o, "doktor").Single();
            var gozcu = Rol(o, "gozcu").Single();
            var koylular = Rol(o, "koylu");

            // Gündüz: 6 canlı, 3 tur, sonra linç.
            await koylular[0].Gonder(new { t = "sohbet", metin = "gunduz" });
            await koylular[1].Bekle("sohbet", s => (string)s["metin"] == "gunduz");
            for (int i = 0; i < 3; i++) await TurOyna(o);
            await o[0].Bekle("olay", m => (string)m["olay"] == "lincBasladi");
            Assert.That(Idler(o[0].SonDurum["oylayanlar"]).Count, Is.EqualTo(6));

            var olu = koylular[0];
            foreach (var x in o) await Komut(x, "lincOyu", (x == olu ? koylular[1] : olu).Sen);
            var linc = await o[1].Bekle("olay", m => (string)m["olay"] == "lincBitti");
            Assert.That((string)linc["kim"], Is.EqualTo(olu.Sen));
            var d = await o[1].Bekle("durum", m => (string)m["asama"] == "gece");
            Assert.That((bool)Oyuncu(d, olu.Sen)["elendi"], Is.True);
            Assert.That((string)Oyuncu(d, olu.Sen)["acikRol"], Is.EqualTo("koylu"));
            foreach (var x in o.Where(x => x != o[1])) await x.Bekle("durum", m => (string)m["asama"] == "gece");

            // Gece: genel sohbet kapalı, ölü genele yazamaz; vampir kanalı vampire, ölüler kanalı ölüye.
            await koylular[1].Gonder(new { t = "sohbet", metin = "gece" });
            await koylular[1].Hata("GECERSIZ_GIRDI");
            await olu.Gonder(new { t = "sohbet", kime = koylular[1].Sen, metin = "ipucu" });
            await olu.Hata("GECERSIZ_GIRDI");
            var vampirMetni = GizliMetin("Vampir");
            await vampir.Gonder(new { t = "sohbet", kime = "vampir", metin = vampirMetni });
            Assert.That((string)(await vampir.Bekle("sohbet", s => (string)s["metin"] == vampirMetni))["kime"], Is.EqualTo("vampir"));
            await doktor.Gonder(new { t = "sohbet", kime = "vampir", metin = "ben de" });
            await doktor.Hata("GECERSIZ_GIRDI");
            var oluMetni = GizliMetin("Olu");
            await olu.Gonder(new { t = "sohbet", kime = "oluler", metin = oluMetni });
            await olu.Bekle("sohbet", s => (string)s["metin"] == oluMetni);

            // Seçimler: vampir köylüyü, doktor kendini, gözcü vampire bakar, köylüler birini düşünür.
            var kurban = koylular[1];
            await Komut(vampir, "geceSecimi", kurban.Sen);
            Assert.That((string)(await vampir.Bekle("durum", m => m["sana"]["vampirSecimleri"].AsArray().Count == 1))["sana"]["vampirSecimleri"][0]["hedef"],
                Is.EqualTo(kurban.Sen));
            await Komut(doktor, "geceSecimi", doktor.Sen);
            await Komut(gozcu, "geceSecimi", vampir.Sen);
            var bilgi = (await gozcu.Bekle("durum", m => m["sana"]["gozcuBilgilerin"].AsArray().Count == 1))["sana"]["gozcuBilgilerin"][0];
            Assert.That(((string)bilgi["kim"], (bool)bilgi["vampir"]), Is.EqualTo((vampir.Sen, true)));
            foreach (var x in koylular.Skip(1)) await Komut(x, "geceSecimi", (string)x.SonDurum["secilebilir"][0]);

            var sabah = await o[2].Bekle("olay", m => (string)m["olay"] == "geceBitti");
            Assert.That((string)sabah["kim"], Is.EqualTo(kurban.Sen));
            d = await o[2].Bekle("durum", m => (int)m["gun"] == 2);
            Assert.That((string)d["asama"], Is.EqualTo("soruSorma"));
            Assert.That((bool)Oyuncu(d, kurban.Sen)["elendi"], Is.True);
            Assert.That((bool)doktor.SonDurum["sana"]["kendiniKoruyabilir"], Is.False);

            // Gizlilik: vampirin seçimi ve gözcünün bilgisi başkasına hiç gitmedi.
            foreach (var x in o.Where(x => x != vampir))
                Assert.That(string.Join("\n", x.Hepsi), Does.Not.Contain("\"vampirSecimleri\":[{"), x.Sen);
            foreach (var x in o.Where(x => x != gozcu))
                Assert.That(string.Join("\n", x.Hepsi), Does.Not.Contain("\"gozcuBilgilerin\":[{"), x.Sen);
            foreach (var x in o.Where(x => x != vampir))
                Assert.That(string.Join("\n", x.Hepsi), Does.Not.Contain(vampirMetni), "vampir kanalı sızdı");
            foreach (var x in o.Where(x => x != olu && x != kurban))
                Assert.That(string.Join("\n", x.Hepsi), Does.Not.Contain(oluMetni), "ölüler kanalı canlılara sızdı");

            // Kurucu bitirir: kazanan yok, herkesin rolü özette.
            await o[0].Gonder(new { t = "bitir" });
            var son = await o[3].Bekle("durum", m => (string)m["asama"] == "oyunSonu");
            Assert.That(son["ozet"]["vampir"]["kazanan"], Is.Null);
            Assert.That(son["ozet"]["vampir"]["roller"].AsArray().Count, Is.EqualTo(6));
        }
    }
}
