using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using static Cakmak.Kurallar.Tests.Kurulum;

namespace Cakmak.Kurallar.Tests
{
    /// <summary>
    /// Rastgele oynayan bir robot: bazen doğru hamle, bazen bekleme, bazen yanlış komut.
    /// Her adımda değişmezler kontrol edilir. Elle yazılmış senaryoların kaçırdığını yakalar.
    /// </summary>
    public class RastgeleOyunTestleri
    {
        static readonly MiniOyunTuru[] MiniOyunlar =
            { MiniOyunTuru.Tkm, MiniOyunTuru.TekCift, MiniOyunTuru.Zar, MiniOyunTuru.Reaksiyon, MiniOyunTuru.Karisik };

        [Test]
        public void Bin_rastgele_oyunda_hicbir_degismez_bozulmaz()
        {
            for (int tohum = 1; tohum <= 1000; tohum++)
                Oyna(tohum);
        }

        [Test]
        public void Uc_yuz_rastgele_kostebek_oyununda_hicbir_degismez_bozulmaz()
        {
            for (int tohum = 1; tohum <= 300; tohum++)
                Oyna(tohum, OyunTuru.Kostebek);
        }

        [Test]
        public void Uc_yuz_rastgele_takim_oyununda_hicbir_degismez_bozulmaz()
        {
            for (int tohum = 1; tohum <= 300; tohum++)
                Oyna(tohum, OyunTuru.Takim);
        }

        [Test]
        public void Uc_yuz_rastgele_rus_ruleti_oyununda_hicbir_degismez_bozulmaz()
        {
            for (int tohum = 1; tohum <= 300; tohum++)
                Oyna(tohum, OyunTuru.RusRuleti);
        }

        [Test]
        public void Uc_yuz_rastgele_casus_oyununda_hicbir_degismez_bozulmaz()
        {
            for (int tohum = 1; tohum <= 300; tohum++)
                Oyna(tohum, OyunTuru.Casus);
        }

        [Test]
        public void Ayni_tohum_ayni_oyunu_uretir()
        {
            for (int tohum = 1; tohum <= 50; tohum++)
                Assert.That(Oyna(tohum), Is.EqualTo(Oyna(tohum)), "tohum " + tohum);
        }

        [Test]
        public void Farkli_tohum_farkli_oyun_uretir()
        {
            Assert.That(Oyna(1), Is.Not.EqualTo(Oyna(2)));
        }

        /// <summary>Bir oyunu baştan sona oynar, olay kaydını döner. Köstebek dalı Çakmak oyunlarının rastgeleliğini değiştirmez.</summary>
        static string Oyna(int tohum, OyunTuru tur = OyunTuru.Cakmak)
        {
            bool kostebek = tur == OyunTuru.Kostebek;
            bool rusRuleti = tur == OyunTuru.RusRuleti;
            bool takim = tur == OyunTuru.Takim;
            bool casus = tur == OyunTuru.Casus;
            var robot = new Random(tohum * 7919);
            int kisi = 4 + robot.Next(7);
            var mod = (OyunModu)robot.Next(3);
            bool s3 = robot.Next(2) == 0;
            var oyun = Kur(kisi, tohum, x =>
            {
                x.Mod = mod;
                x.MiniOyun = MiniOyunlar[robot.Next(MiniOyunlar.Length)];
                x.TurSayisi = s3 ? 6 + robot.Next(10) : 3 + robot.Next(6);
                x.OneriSorulari = new[] { "Öneri A?", "Öneri B?" };
                if (!s3) return;
                // S3: cezalar, görevler, kaos turları rastgele açılır.
                x.CezaSeviyesi = (CezaSeviyesi)robot.Next(mod == OyunModu.Temiz ? 2 : 3);
                x.GizliGorevler = robot.Next(2) == 0;
                x.KaosTurlari = robot.Next(3) != 0;
                x.TekCihaz = robot.Next(2) == 0;
                x.SesliCezalar = new[] { "Sesli ceza 1", "Sesli ceza 2" };
            }, tur, robot);

            var kayit = new StringBuilder();
            int olaySayisi = 0;
            oyun.OlayOldu += o => olaySayisi++;
            // Takım: puan olaylarının toplamı (başlangıç puanı hariç), takıma göre.
            var olayPuani = new int[Oyun.TakimSayisi];
            var baslangic = takim ? new List<int>(Durum(oyun).TakimPuanlari) : null;
            oyun.OlayOldu += o => { if (o.Tur == OlayTuru.DuelloPuani || o.Tur == OlayTuru.IfsaBonusu) olayPuani[o.Takim.Value]++; };
            // Zaman ilerleyince aşama değiştiyse en az bir olay yayılmalı: sunucu yeni durumu olaydan sonra yollar,
            // olaysız değişen aşama online'da kimsenin ekranına gitmez.
            void Bekle(int sn)
            {
                var once = Durum(oyun).Asama;
                int olayOnce = olaySayisi;
                Saniye(oyun, sn);
                if (Durum(oyun).Asama != once)
                    Assert.That(olaySayisi, Is.GreaterThan(olayOnce), $"tohum {tohum}: {once} → {Durum(oyun).Asama} olaysız");
            }
            oyun.OlayOldu += o => kayit.Append(o.Tur).Append(o.A).Append(o.B).Append(o.C).Append(o.Kim).Append(o.Soru)
                .Append(o.Ceza?.Tur).Append(o.Ceza?.Metin).Append(o.Kaos).Append(';');

            string gizli = null;
            for (int adim = 0; adim < 5000; adim++)
            {
                var g = Durum(oyun);
                if (g.Asama == Asama.OyunSonu)
                {
                    // Aynı tohum aynı özeti de vermeli: unvanlar, görevler, ifşalar.
                    var ozet = oyun.Ozet();
                    foreach (var u in ozet.Unvanlar) kayit.Append(u.Tur).Append(u.Kim).Append(u.Deger).Append(',');
                    foreach (var gs in ozet.Gorevler) kayit.Append(gs.Kim).Append(gs.Gorev.Tur).Append(gs.Gorev.Hedef).Append(gs.Basarili).Append(',');
                    foreach (var k in ozet.IfsaOlanlar) kayit.Append(k.Soru).Append(',');
                    if (kostebek) KostebekSonu(oyun, ozet, tohum);
                    if (rusRuleti) RusRuletiSonu(ozet, g, tohum);
                    if (takim) TakimSonu(ozet, tohum);
                    if (casus) CasusSonu(oyun, ozet, tohum);
                    return kayit.ToString();
                }
                Degismezler(oyun, g, gizli, tohum);
                if (kostebek) KostebekDegismezleri(oyun, g, tohum);
                if (rusRuleti) RusRuletiDegismezleri(g, tohum);
                if (takim) TakimDegismezleri(g, baslangic, olayPuani, tohum);
                if (casus) CasusDegismezleri(oyun, g, tohum);

                int zar = robot.Next(10);
                if (zar == 0) { Bekle(robot.Next(1, 30)); continue; }               // bekle (AFK)
                if (zar == 1) { YanlisKomut(oyun, g, robot); continue; }            // yanlış komut
                if (kostebek && zar == 2 && g.Asama == Asama.SoruSorma)            // erken suçlama
                {
                    oyun.Sucla(g.Oyuncular[robot.Next(g.Oyuncular.Count)].Id);
                    continue;
                }
                if (casus && zar == 2 && robot.Next(10) == 0)                         // "Mekânı biliyorum" (herkes basabilir)
                {
                    oyun.CasusTahmini(g.Oyuncular[robot.Next(g.Oyuncular.Count)].Id, g.CasusListesi[robot.Next(g.CasusListesi.Count)]);
                    continue;
                }

                switch (g.Asama)
                {
                    case Asama.SoruToplama:
                        var yazan = P(1 + robot.Next(kisi));
                        oyun.HavuzaSoruEkle(yazan, "Robot sorusu " + robot.Next(100) + "?");
                        break;

                    case Asama.SoruSorma:
                        var a = TekrarYardim.GercekA(oyun);
                        var liste = oyun.GorunumAl(a).Secilebilir;
                        if (mod != OyunModu.Curcuna && robot.Next(4) == 0) oyun.OneriCek(a);
                        var yazilan = robot.Next(5) == 0 ? null : "Gizli " + tohum + "-" + adim + "?";
                        Assert.That(oyun.SoruSor(a, liste[robot.Next(liste.Count)], yazilan), Is.EqualTo(Sonuc.Tamam));
                        // Atanan soru zorunlu olabilir (Curcuna, Kör Soru, havuzdan sor cezası): B'nin gördüğü esas.
                        gizli = oyun.GorunumAl(Durum(oyun).B.Value).Soru;
                        break;

                    case Asama.CevapSecme:
                        var b = g.B.Value;
                        var secenek = oyun.GorunumAl(b).Secilebilir;
                        Assert.That(oyun.CevapSec(b, secenek[robot.Next(secenek.Count)]), Is.EqualTo(Sonuc.Tamam));
                        break;

                    case Asama.MiniOyun:
                        var oynayan = robot.Next(2) == 0 ? g.B.Value : g.C.Value;
                        oyun.MiniOyunHamlesi(oynayan, RastgeleHamle(g.MiniOyun, robot));
                        break;

                    case Asama.Oylama:
                        var oylayan = g.Oylayanlar[robot.Next(g.Oylayanlar.Count)];
                        oyun.Oy(oylayan, robot.Next(2) == 0);
                        break;

                    case Asama.IkiyeKatla:
                        if (robot.Next(2) == 0) oyun.IkiyeKatla(g.KararVeren.Value); else oyun.KarariGec(g.KararVeren.Value);
                        break;

                    case Asama.Bedel:
                        if (robot.Next(2) == 0) oyun.BedelOde(g.KararVeren.Value); else oyun.KarariGec(g.KararVeren.Value);
                        break;

                    case Asama.Ifsa:
                    case Asama.Gume:
                        Bekle(6);
                        break;

                    case Asama.Suclama:
                        var suclayan = g.Oylayanlar[robot.Next(g.Oylayanlar.Count)];
                        oyun.SuclamaOyu(suclayan, g.Oyuncular[robot.Next(g.Oyuncular.Count)].Id); // geçersiz de olabilir
                        break;

                    case Asama.CasusTahmini:
                        if (robot.Next(2) == 0) Bekle(robot.Next(1, Oyun.CasusTahminiSn + 5));
                        else oyun.CasusTahmini(g.Yakalanan.Value, g.CasusListesi[robot.Next(g.CasusListesi.Count)]);
                        break;

                    case Asama.Tetik:
                        var ceken = g.TetikCeken.Value;
                        if (robot.Next(3) == 0) oyun.TetikDevret(ceken, g.Oyuncular[robot.Next(g.Oyuncular.Count)].Id); // geçersiz de olabilir
                        else oyun.TetikCek(ceken);
                        break;
                }
            }
            Assert.Fail($"tohum {tohum}: oyun 5000 adımda bitmedi, takıldı");
            return null;
        }

        /// <summary>Kurulum: S3 ayarları rastgele, oyunun kapattıkları kapalı (Oyun.AyarlarUyumlu).</summary>
        static Oyun Kur(int kisi, int tohum, Action<Ayarlar> ayarla, OyunTuru tur, Random robot) =>
            Yeni(kisi, tohum, x =>
            {
                ayarla(x);
                x.Oyun = tur;
                if (tur == OyunTuru.Kostebek) x.KaosTurlari = false;
                if (tur == OyunTuru.Takim) x.GizliGorevler = false;
                if (tur == OyunTuru.Casus)
                {
                    if (x.Mod == OyunModu.Curcuna) x.Mod = OyunModu.Normal;
                    x.GizliGorevler = false;
                    x.KaosTurlari = false;
                    x.Casus = (CasusTuru)robot.Next(2);
                }
                if (tur != OyunTuru.RusRuleti) return;
                x.CezaSeviyesi = CezaSeviyesi.Kapali;
                x.GizliGorevler = false;
                x.KaosTurlari = false;
                x.Can = 1 + robot.Next(Oyun.EnFazlaCan);
            });

        /// <summary>Elenen oynamaz; final sadece 3 kişi kalınca; tetik elenmemiş birinde.</summary>
        static void RusRuletiDegismezleri(Gorunum g, int tohum)
        {
            string t = "tohum " + tohum + ", aşama " + g.Asama;
            int kalan = 0;
            foreach (var o in g.Oyuncular)
            {
                if (!o.Elendi && !o.Cikti) kalan++;
                if (!o.Elendi) continue;
                Assert.That(o.Can, Is.EqualTo(0), t);
                Assert.That(g.B == o.Id || g.C == o.Id || g.TetikCeken == o.Id, Is.False, t + ": elenen oynuyor");
            }
            Assert.That(kalan, Is.GreaterThanOrEqualTo(2), t + ": oyun bitmeliydi");
            if (g.Final) Assert.That(kalan, Is.LessThanOrEqualTo(Oyun.FinalSiniri), t + ": erken final");
            else Assert.That(kalan, Is.GreaterThan(Oyun.FinalSiniri), t + ": final başlamadı");
        }

        /// <summary>Herkesin takımı var; puan = başlangıç + puan olayları.</summary>
        static void TakimDegismezleri(Gorunum g, List<int> baslangic, int[] olayPuani, int tohum)
        {
            string t = "tohum " + tohum + ", aşama " + g.Asama;
            foreach (var o in g.Oyuncular) Assert.That(o.Takim == 0 || o.Takim == 1, Is.True, t);
            for (int i = 0; i < Oyun.TakimSayisi; i++)
                Assert.That(g.TakimPuanlari[i], Is.EqualTo(baslangic[i] + olayPuani[i]), t + ": puan olaysız değişti");
        }

        static void TakimSonu(OyunOzeti ozet, int tohum)
        {
            var p = ozet.Takim.Puanlar;
            int? beklenen = p[0] > p[1] ? 0 : p[1] > p[0] ? 1 : (int?)null;
            Assert.That(ozet.Takim.Kazanan, Is.EqualTo(beklenen), "tohum " + tohum);
        }

        static void RusRuletiSonu(OyunOzeti ozet, Gorunum g, int tohum)
        {
            Assert.That(ozet.RusRuleti, Is.Not.Null, "tohum " + tohum);
            Assert.That(ozet.RusRuleti.Kazanan, Is.Not.Null, "tohum " + tohum + ": son kalan yok");
            foreach (var o in g.Oyuncular)
                if (o.Id == ozet.RusRuleti.Kazanan) Assert.That(o.Elendi, Is.False, "tohum " + tohum + ": kazanan elenmiş");
        }

        /// <summary>Köstebek sadece kendi görünümünde, hiç şüpheli olmaz (köstebek suçlanırsa oyun biter).</summary>
        static void KostebekDegismezleri(Oyun oyun, Gorunum g, int tohum)
        {
            string t = "tohum " + tohum + ", aşama " + g.Asama;
            int sayi = 0;
            OyuncuId k = default;
            foreach (var o in g.Oyuncular)
                if (oyun.GorunumAl(o.Id).Rolun == Rol.Kostebek) { sayi++; k = o.Id; }
            Assert.That(sayi, Is.EqualTo(1), t + ": tam bir köstebek");
            Assert.That(g.Supheliler, Has.No.Member(k), t + ": köstebek şüpheli listesinde ama oyun sürüyor");
            Assert.That(oyun.Ozet().Kostebek, Is.Null, t + ": oyun bitmeden köstebek açıldı");
        }

        static void KostebekSonu(Oyun oyun, OyunOzeti ozet, int tohum)
        {
            Assert.That(ozet.Kostebek, Is.Not.Null, "tohum " + tohum);
            Assert.That(oyun.GorunumAl(ozet.Kostebek.Kostebek).Rolun, Is.EqualTo(Rol.Kostebek), "tohum " + tohum);
            if (ozet.Kostebek.Neden != KostebekNedeni.GorevBitti) return;
            foreach (var gs in ozet.Gorevler)
                if (gs.Kim == ozet.Kostebek.Kostebek) Assert.That(gs.Basarili, Is.True, "tohum " + tohum + ": görevi bitmeden kazandı");
        }

        /// <summary>Tam bir casus, sırrı bilmez, diğerleri aynı sırrı bilir; casus şüpheli olmaz; son şansta herkes görür.</summary>
        static void CasusDegismezleri(Oyun oyun, Gorunum g, int tohum)
        {
            string t = "tohum " + tohum + ", aşama " + g.Asama;
            var casuslar = new List<OyuncuId>();
            var sirlar = new HashSet<string>();
            foreach (var o in g.Oyuncular)
            {
                var x = oyun.GorunumAl(o.Id);
                if (x.Rolun == Rol.Casus) { casuslar.Add(o.Id); Assert.That(x.Sirrin, Is.Null, t + ": casus sırrı gördü"); }
                else sirlar.Add(x.Sirrin);
                Assert.That(x.Yakalanan.HasValue, Is.EqualTo(g.Asama == Asama.CasusTahmini), t + ": yakalanan herkese, sadece son şansta");
            }
            Assert.That(casuslar.Count, Is.EqualTo(1), t + ": tam bir casus");
            Assert.That(sirlar.Count, Is.EqualTo(1), t + ": herkes aynı sırrı bilmeli");
            Assert.That(g.CasusListesi, Has.Member(new List<string>(sirlar)[0]), t);
            Assert.That(g.Supheliler, Has.No.Member(casuslar[0]), t + ": casus şüpheli listesinde ama oyun sürüyor");
            if (g.Asama == Asama.CasusTahmini) Assert.That(g.Yakalanan, Is.EqualTo(casuslar[0]), t);
            Assert.That(oyun.Ozet().Casus, Is.Null, t + ": oyun bitmeden casus açıldı");
        }

        static void CasusSonu(Oyun oyun, OyunOzeti ozet, int tohum)
        {
            var s = ozet.Casus;
            Assert.That(s, Is.Not.Null, "tohum " + tohum);
            Assert.That(oyun.GorunumAl(s.Casus).Rolun, Is.EqualTo(Rol.Casus), "tohum " + tohum);
            bool bildi = s.Neden == CasusNedeni.Bildi || s.Neden == CasusNedeni.SonSanstaBildi;
            Assert.That(s.Kazanan == CasusKazanan.Casus, Is.EqualTo(bildi || s.Neden == CasusNedeni.Yakalanmadi), "tohum " + tohum);
            if (s.Tahmin != null) Assert.That(s.Tahmin == s.Sir, Is.EqualTo(bildi), "tohum " + tohum);
            else Assert.That(bildi || s.Neden == CasusNedeni.YanlisTahmin, Is.False, "tohum " + tohum + ": tahminsiz tahmin sonucu");
        }

        static void Degismezler(Oyun oyun, Gorunum g, string gizli, int tohum)
        {
            string t = "tohum " + tohum + ", aşama " + g.Asama;

            var gercekA = g.Asama == Asama.SoruToplama ? (OyuncuId?)null : TekrarYardim.GercekA(oyun);
            if (g.Asama == Asama.SoruSorma)
                Assert.That(oyun.GorunumAl(gercekA.Value).Secilebilir, Is.Not.Empty, t + ": A'nın seçeneği yok");

            // S3 gizliliği
            if (g.Kaos == KaosKurali.SoranGizli)
                foreach (var o in g.Oyuncular)
                    if (o.Id != gercekA) Assert.That(oyun.GorunumAl(o.Id).A, Is.Null, t + ": Soran Gizli'de A göründü");
            if (g.Kaos == KaosKurali.KorSoru && g.Asama == Asama.SoruSorma)
                Assert.That(oyun.GorunumAl(gercekA.Value).AtananSoru, Is.Null, t + ": Kör Soru'da A soruyu gördü");
            if (g.Kaos == KaosKurali.KorSoru && gizli != null && gercekA.HasValue && g.Asama != Asama.Ifsa)
                Assert.That(oyun.GorunumAl(gercekA.Value).Soru, Is.Not.EqualTo(gizli), t + ": Kör Soru'da A soruyu gördü");
            if (g.Asama == Asama.CevapSecme)
                Assert.That(oyun.GorunumAl(g.B.Value).Secilebilir, Is.Not.Empty, t + ": B'nin seçeneği yok");

            if (g.Asama == Asama.CevapSecme || g.Asama == Asama.MiniOyun || g.Asama == Asama.Oylama
                || g.Asama == Asama.IkiyeKatla || g.Asama == Asama.Bedel)
            {
                Assert.That(g.C == g.B || (g.Kaos != KaosKurali.SoranGizli && g.C == gercekA), Is.False, t + ": C, A ya da B olamaz");
                foreach (var o in g.Oyuncular)
                    if (o.Id != g.B && gizli != null)
                        Assert.That(oyun.GorunumAl(o.Id).Soru, Is.Null, t + ": soru B dışına sızdı");
            }

            if (g.Asama == Asama.Gume && gizli != null)
                foreach (var o in g.Oyuncular)
                {
                    var og = oyun.GorunumAl(o.Id);
                    Assert.That(og.Soru, Is.Not.EqualTo(gizli), t + ": güme giden soru görünüyor");
                    Assert.That(og.IfsaSoru, Is.Not.EqualTo(gizli), t + ": güme giden soru görünüyor");
                }
        }

        static void YanlisKomut(Oyun oyun, Gorunum g, Random robot)
        {
            var kim = g.Oyuncular[robot.Next(g.Oyuncular.Count)].Id;
            var hedef = g.Oyuncular[robot.Next(g.Oyuncular.Count)].Id;
            switch (robot.Next(8))
            {
                case 0: oyun.SoruSor(kim, hedef, "?"); break;
                case 1: oyun.CevapSec(kim, hedef); break;
                case 2: oyun.MiniOyunHamlesi(kim, Hamle.ZarAt()); break;
                case 3: oyun.Oy(kim, robot.Next(2) == 0); break;
                case 4: oyun.IkiyeKatla(kim); break;
                case 5: oyun.BedelOde(kim); break;
                case 6: oyun.KarariGec(kim); break;
                default: oyun.HavuzaSoruEkle(kim, ""); break;
            }
        }

        static Hamle RastgeleHamle(MiniOyunTuru tur, Random robot)
        {
            switch (tur)
            {
                case MiniOyunTuru.Tkm: return Hamle.TasKagitMakas((TkmSecim)robot.Next(3));
                case MiniOyunTuru.TekCift: return Hamle.TekCift(1 + robot.Next(5), robot.Next(2) == 0);
                case MiniOyunTuru.Reaksiyon: return Hamle.Reaksiyon(robot.Next(-50, 600));
                default: return Hamle.ZarAt();
            }
        }
    }
}
