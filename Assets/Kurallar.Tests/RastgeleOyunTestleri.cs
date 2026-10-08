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

        /// <summary>Bir oyunu baştan sona oynar, olay kaydını döner.</summary>
        static string Oyna(int tohum)
        {
            var robot = new Random(tohum * 7919);
            int kisi = 4 + robot.Next(7);
            var mod = (OyunModu)robot.Next(3);
            var oyun = Yeni(kisi, tohum, x =>
            {
                x.Mod = mod;
                x.MiniOyun = MiniOyunlar[robot.Next(MiniOyunlar.Length)];
                x.TurSayisi = 3 + robot.Next(6);
                x.OneriSorulari = new[] { "Öneri A?", "Öneri B?" };
            });

            var kayit = new StringBuilder();
            oyun.OlayOldu += o => kayit.Append(o.Tur).Append(o.A).Append(o.B).Append(o.C).Append(o.Kim).Append(o.Soru).Append(';');

            string gizli = null;
            for (int adim = 0; adim < 5000; adim++)
            {
                var g = Durum(oyun);
                if (g.Asama == Asama.OyunSonu) return kayit.ToString();
                Degismezler(oyun, g, gizli, tohum);

                int zar = robot.Next(10);
                if (zar == 0) { Saniye(oyun, robot.Next(1, 30)); continue; }       // bekle (AFK)
                if (zar == 1) { YanlisKomut(oyun, g, robot); continue; }            // yanlış komut

                switch (g.Asama)
                {
                    case Asama.SoruToplama:
                        var yazan = P(1 + robot.Next(kisi));
                        oyun.HavuzaSoruEkle(yazan, "Robot sorusu " + robot.Next(100) + "?");
                        break;

                    case Asama.SoruSorma:
                        var a = g.A.Value;
                        var liste = oyun.GorunumAl(a).Secilebilir;
                        if (mod != OyunModu.Curcuna && robot.Next(4) == 0) oyun.OneriCek(a);
                        gizli = robot.Next(5) == 0 ? null : "Gizli " + tohum + "-" + adim + "?";
                        Assert.That(oyun.SoruSor(a, liste[robot.Next(liste.Count)], gizli), Is.EqualTo(Sonuc.Tamam));
                        if (mod == OyunModu.Curcuna) gizli = oyun.GorunumAl(Durum(oyun).B.Value).Soru;
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

                    case Asama.Ifsa:
                    case Asama.Gume:
                        Saniye(oyun, 6);
                        break;
                }
            }
            Assert.Fail($"tohum {tohum}: oyun 5000 adımda bitmedi, takıldı");
            return null;
        }

        static void Degismezler(Oyun oyun, Gorunum g, string gizli, int tohum)
        {
            string t = "tohum " + tohum + ", aşama " + g.Asama;

            if (g.Asama == Asama.SoruSorma)
                Assert.That(oyun.GorunumAl(g.A.Value).Secilebilir, Is.Not.Empty, t + ": A'nın seçeneği yok");
            if (g.Asama == Asama.CevapSecme)
                Assert.That(oyun.GorunumAl(g.B.Value).Secilebilir, Is.Not.Empty, t + ": B'nin seçeneği yok");

            if (g.Asama == Asama.CevapSecme || g.Asama == Asama.MiniOyun)
            {
                Assert.That(g.C == g.A || g.C == g.B, Is.False, t + ": C, A ya da B olamaz");
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
            switch (robot.Next(4))
            {
                case 0: oyun.SoruSor(kim, hedef, "?"); break;
                case 1: oyun.CevapSec(kim, hedef); break;
                case 2: oyun.MiniOyunHamlesi(kim, Hamle.ZarAt()); break;
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
