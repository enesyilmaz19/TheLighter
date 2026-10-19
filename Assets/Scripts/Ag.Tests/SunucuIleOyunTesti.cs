using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Cakmak.Kurallar;
using NUnit.Framework;
using TheLighter.Ag;
using UnityEngine.TestTools;

namespace TheLighter.Testler
{
    /// <summary>
    /// Gerçek sunucuyla uçtan uca: 4 bot oda kurar, katılır, oyunu baştan sona oynar (S4 bitti ölçütü, ekransız).
    /// Yerel sunucu gerekir: repo kökünde <c>dotnet run --project Sunucu/Cakmak.Sunucu</c>. Sunucu yoksa test atlanır
    /// (Explicit: "Run All" ile koşmaz, Test Runner'da elle seçilir).
    /// </summary>
    [Explicit("Yerel sunucu gerekir: dotnet run --project Sunucu/Cakmak.Sunucu")]
    [Category("Sunucu")]
    public class SunucuIleOyunTesti
    {
        static readonly Uri Adres = new Uri("ws://localhost:5000/ws");

        [UnityTest]
        public IEnumerator DortBot_CezaGorevKaosAcik_OyunBaştanSonaBiter()
        {
            var botlar = new List<DenemeBotu>();
            for (int i = 0; i < 4; i++) botlar.Add(new DenemeBotu("Bot" + (i + 1), 1000 + i));
            var kurucu = botlar[0];
            var saat = Stopwatch.StartNew();
            double son = 0;

            double Gecen()
            {
                double simdi = saat.Elapsed.TotalSeconds;
                double fark = simdi - son;
                son = simdi;
                return fark;
            }

            IEnumerator Bekle(Func<bool> kosul, double sn, string ne)
            {
                double bitis = saat.Elapsed.TotalSeconds + sn;
                while (!kosul())
                {
                    double g = Gecen();
                    foreach (var b in botlar) b.Guncelle(g);
                    if (saat.Elapsed.TotalSeconds > bitis)
                        Assert.Fail(ne + " olmadı. Bot durumları: " + string.Join(", ", botlar.Select(b =>
                            b.Isim + "=" + b.Istemci.Durum + "/" + (b.Istemci.SonDurum?.ToString() ?? "-") + (b.SonHata != null ? " hata:" + b.SonHata : ""))));
                    yield return null;
                }
            }

            kurucu.Istemci.OdaKur(Adres, kurucu.Isim);
            string ayrilma = null;
            kurucu.Istemci.Ayrildi += n => ayrilma = n;
            yield return Bekle(() => kurucu.Istemci.Durum == BaglantiDurumu.Odada || ayrilma != null, 10, "Oda kurma");
            if (ayrilma == OnlineIstemci.NedenBaglanamadi) Assert.Ignore("Yerel sunucu çalışmıyor (" + Adres + ").");
            Assert.That(ayrilma, Is.Null, "kurucu odadan çıktı");
            string oda = kurucu.Istemci.Oda;
            Assert.That(oda, Has.Length.EqualTo(5));

            for (int i = 1; i < 4; i++) botlar[i].Istemci.Katil(Adres, oda.ToLowerInvariant(), botlar[i].Isim);
            yield return Bekle(() => kurucu.Istemci.SonDurum != null && kurucu.Istemci.SonDurum.Oyuncular.Count == 4, 10, "4 oyuncunun lobiye girmesi");

            // Bütün S3 özellikleri açık, kısa oyun: kaos 5. turda gelsin diye 6 tur.
            kurucu.Istemci.Gonder(Cevirici.AyarDegistir(mod: OyunModu.Normal, miniOyun: MiniOyunTuru.Karisik, turSayisi: 6,
                soruSn: 10, cevapSn: 10, hamleSn: 5, ceza: CezaSeviyesi.Cesur, gorevler: true, kaos: true));
            // Bütün botlar aynı durumu işlemiş olsun: kurucu almışken 4. bot henüz işlememiş olabiliyor (yarış).
            yield return Bekle(() => botlar.All(b => b.Istemci.SonDurum?.Ayarlar?.TurSayisi == 6 && b.Istemci.SonDurum.Ayarlar.Kaos), 10, "Ayarların herkese gelmesi");
            Assert.That(botlar[3].Istemci.SonDurum.Ayarlar.Ceza, Is.EqualTo(CezaSeviyesi.Cesur), "ayarlar herkese gider");

            kurucu.Istemci.Gonder(Cevirici.Baslat());
            yield return Bekle(() => botlar.All(b => b.Istemci.SonDurum?.Yer == OdaYeri.Oyun), 10, "Oyunun başlaması");

            // Gizli görev sadece sahibine: herkesin kendi görevi var, başkasınınki gelmez (sunucu filtreler).
            foreach (var b in botlar) Assert.That(b.Istemci.SonDurum.Gorunum.Gorevin, Is.Not.Null, b.Isim + " görevini almadı");

            yield return Bekle(() => botlar.All(b => b.OyunBitti), 240, "Oyunun bitmesi");

            var ozet = kurucu.Istemci.SonDurum.Ozet;
            Assert.That(ozet, Is.Not.Null, "oyun sonunda özet gelir");
            Assert.That(ozet.OynananTur, Is.GreaterThanOrEqualTo(1));
            Assert.That(ozet.Gorevler.Count, Is.EqualTo(4), "görevler oyun sonunda açılır");
            Assert.That(botlar.Sum(b => b.YolladigiKomut), Is.GreaterThan(10));
            Assert.DoesNotThrow(() => GrupKaydi.Isle(new GrupKaydi { Ad = "Botlar" }, ozet), "grup hafızası özetten yazılabilir");

            // Tekrar oyna: herkes lobiye döner.
            kurucu.Istemci.Gonder(Cevirici.TekrarOyna());
            yield return Bekle(() => botlar.All(b => b.Istemci.SonDurum?.Yer == OdaYeri.Lobi), 10, "Lobiye dönüş");

            foreach (var b in botlar) b.Istemci.Ayril();
        }

        /// <summary>
        /// Köstebek gerçek sunucuyla (tasks/g2-kostebek.md 6): bir bot erken suçlar, botlar oy verir, 6. turda aralıklı oylama
        /// ya da son oylama; oyun köstebek açılışıyla biter. Köstebeğin kim olduğu oyun bitmeden kimsenin durumunda yok.
        /// </summary>
        [UnityTest]
        public IEnumerator DortBot_Kostebek_SuclamaVeAcilis()
        {
            var botlar = new List<DenemeBotu>();
            for (int i = 0; i < 4; i++) botlar.Add(new DenemeBotu("Kos" + (i + 1), 2000 + i));
            var kurucu = botlar[0];
            botlar[2].ErkenSuclasin = true;
            var olaylar = new List<OlayMesaji>();
            kurucu.Istemci.OlayGeldi += olaylar.Add;
            var saat = Stopwatch.StartNew();
            double son = 0;

            IEnumerator Bekle(Func<bool> kosul, double sn, string ne)
            {
                double bitis = saat.Elapsed.TotalSeconds + sn;
                while (!kosul())
                {
                    double g = saat.Elapsed.TotalSeconds - son;
                    son = saat.Elapsed.TotalSeconds;
                    foreach (var b in botlar) b.Guncelle(g);
                    if (kurucu.Istemci.Durum == BaglantiDurumu.Kapali) Assert.Ignore("Yerel sunucu çalışmıyor.");
                    if (saat.Elapsed.TotalSeconds > bitis)
                        Assert.Fail(ne + " olmadı: " + string.Join(", ", botlar.Select(b => b.Isim + "=" + (b.Istemci.SonDurum?.ToString() ?? "-") + " " + b.SonHata)));
                    yield return null;
                }
            }

            kurucu.Istemci.OdaKur(Adres, kurucu.Isim);
            yield return Bekle(() => kurucu.Istemci.Durum == BaglantiDurumu.Odada, 10, "Oda kurma");
            for (int i = 1; i < 4; i++) botlar[i].Istemci.Katil(Adres, kurucu.Istemci.Oda, botlar[i].Isim);
            yield return Bekle(() => kurucu.Istemci.SonDurum?.Oyuncular.Count == 4, 10, "Katılma");

            kurucu.Istemci.Gonder(Cevirici.AyarDegistir(oyun: OyunTuru.Kostebek, kaos: false, turSayisi: 6,
                soruSn: 10, cevapSn: 10, hamleSn: 5, miniOyun: MiniOyunTuru.Zar));
            yield return Bekle(() => botlar.All(b => b.Istemci.SonDurum?.Ayarlar?.Oyun == OyunTuru.Kostebek && b.Istemci.SonDurum.Ayarlar.TurSayisi == 6),
                10, "Köstebek ayarı");
            kurucu.Istemci.Gonder(Cevirici.Baslat());
            yield return Bekle(() => botlar.All(b => b.Istemci.SonDurum?.Yer == OdaYeri.Oyun), 10, "Başlama");

            // Rol sadece köstebeğin kendi durumunda; görev herkeste (ayar kapalı olsa da).
            Assert.That(botlar.Count(b => b.Istemci.SonDurum.Gorunum.Rolun == Rol.Kostebek), Is.EqualTo(1));
            foreach (var b in botlar) Assert.That(b.Istemci.SonDurum.Gorunum.Gorevin, Is.Not.Null, b.Isim + " görevsiz");

            yield return Bekle(() => botlar.All(b => b.OyunBitti), 300, "Oyunun bitmesi");
            // Erken suçlama (Kim dolu) olduysa oylaması da bitmeli; köstebek görevini erkenden bitirdiyse hiç olmayabilir.
            if (olaylar.Any(o => o.Olay != null && o.Olay.Tur == OlayTuru.SuclamaBasladi))
                Assert.That(olaylar.Any(o => o.Olay != null && o.Olay.Tur == OlayTuru.SuclamaBitti), Is.True, "başlayan oylama bitti");

            var k = kurucu.Istemci.SonDurum.Ozet?.Kostebek;
            Assert.That(k, Is.Not.Null, "köstebek açılışı özette");
            var kostebekBot = botlar.First(b => b.Istemci.Sen == k.Kostebek);
            Assert.That(kostebekBot, Is.Not.Null);

            foreach (var b in botlar) b.Istemci.Ayril();
        }

        /// <summary>
        /// Rus Ruleti gerçek sunucuyla (tasks/g2-rus-ruleti.md 5): 4 bot, can 1. Düelloyu kaybeden tetiği çeker ya da devreder,
        /// 3 kişide final, son kalan kazanır; özet kazananı ve elenme sırasını getirir.
        /// </summary>
        [UnityTest]
        public IEnumerator DortBot_RusRuleti_SonKalanKazanir()
        {
            var botlar = new List<DenemeBotu>();
            for (int i = 0; i < 4; i++) botlar.Add(new DenemeBotu("Rr" + (i + 1), 3000 + i));
            var kurucu = botlar[0];
            var olaylar = new List<OlayMesaji>();
            kurucu.Istemci.OlayGeldi += olaylar.Add;
            var saat = Stopwatch.StartNew();
            double son = 0;

            IEnumerator Bekle(Func<bool> kosul, double sn, string ne)
            {
                double bitis = saat.Elapsed.TotalSeconds + sn;
                while (!kosul())
                {
                    double g = saat.Elapsed.TotalSeconds - son;
                    son = saat.Elapsed.TotalSeconds;
                    foreach (var b in botlar) b.Guncelle(g);
                    if (kurucu.Istemci.Durum == BaglantiDurumu.Kapali) Assert.Ignore("Yerel sunucu çalışmıyor.");
                    if (saat.Elapsed.TotalSeconds > bitis)
                        Assert.Fail(ne + " olmadı: " + string.Join(", ", botlar.Select(b => b.Isim + "=" + (b.Istemci.SonDurum?.ToString() ?? "-") + " " + b.SonHata)));
                    yield return null;
                }
            }

            kurucu.Istemci.OdaKur(Adres, kurucu.Isim);
            yield return Bekle(() => kurucu.Istemci.Durum == BaglantiDurumu.Odada, 10, "Oda kurma");
            for (int i = 1; i < 4; i++) botlar[i].Istemci.Katil(Adres, kurucu.Istemci.Oda, botlar[i].Isim);
            yield return Bekle(() => kurucu.Istemci.SonDurum?.Oyuncular.Count == 4, 10, "Katılma");

            kurucu.Istemci.Gonder(Cevirici.AyarDegistir(oyun: OyunTuru.RusRuleti, can: 1, ceza: CezaSeviyesi.Kapali, gorevler: false, kaos: false,
                soruSn: 10, cevapSn: 10, hamleSn: 5, miniOyun: MiniOyunTuru.Zar));
            yield return Bekle(() => botlar.All(b => b.Istemci.SonDurum?.Ayarlar?.Oyun == OyunTuru.RusRuleti), 10, "Rus Ruleti ayarı");
            kurucu.Istemci.Gonder(Cevirici.Baslat());
            yield return Bekle(() => botlar.All(b => b.Istemci.SonDurum?.Yer == OdaYeri.Oyun), 10, "Başlama");

            yield return Bekle(() => botlar.All(b => b.OyunBitti), 400, "Oyunun bitmesi");

            var r = kurucu.Istemci.SonDurum.Ozet?.RusRuleti;
            Assert.That(r, Is.Not.Null, "özette Rus Ruleti");
            Assert.That(r.Kazanan.HasValue, Is.True, "son kalan kazanır");
            Assert.That(r.ElenmeSirasi.Count, Is.EqualTo(3));
            Assert.That(olaylar.Any(o => o.Olay != null && o.Olay.Tur == OlayTuru.FinalBasladi), Is.True, "3 kişide final");
            Assert.That(olaylar.Count(o => o.Olay != null && o.Olay.Tur == OlayTuru.TetikPatladi), Is.EqualTo(3), "can 1: her patlama bir eleme");

            foreach (var b in botlar) b.Istemci.Ayril();
        }

        [UnityTest]
        public IEnumerator UygulamayaDonunce_YenidenBaglanir_AyniIsimleOdada()
        {
            // Lobide tek kişi kopunca oda silinir: odayı ayakta tutan ikinci biri var.
            var a = new DenemeBotu("Kalan", 1);
            var b = new DenemeBotu("Donen", 2);
            var saat = Stopwatch.StartNew();
            double son = 0;

            IEnumerator Bekle(Func<bool> kosul, string ne)
            {
                double bitis = saat.Elapsed.TotalSeconds + 10;
                while (!kosul())
                {
                    double g = saat.Elapsed.TotalSeconds - son;
                    son = saat.Elapsed.TotalSeconds;
                    a.Guncelle(g);
                    b.Guncelle(g);
                    if (a.Istemci.Durum == BaglantiDurumu.Kapali) Assert.Ignore("Yerel sunucu çalışmıyor.");
                    if (saat.Elapsed.TotalSeconds > bitis) Assert.Fail(ne + " olmadı: " + b.Istemci.Durum + " " + b.SonHata);
                    yield return null;
                }
            }

            a.Istemci.OdaKur(Adres, a.Isim);
            yield return Bekle(() => a.Istemci.Durum == BaglantiDurumu.Odada, "Oda kurma");
            b.Istemci.Katil(Adres, a.Istemci.Oda, b.Isim);
            yield return Bekle(() => b.Istemci.Durum == BaglantiDurumu.Odada, "Katılma");

            // Telefon arka plana gidip dönmüş gibi: bağlantı kapanır, anahtar + aynı isimle yeniden girilir.
            b.Istemci.UygulamaDondu();
            Assert.That(b.Istemci.Durum, Is.EqualTo(BaglantiDurumu.YenidenBaglaniyor));
            yield return Bekle(() => b.Istemci.Durum == BaglantiDurumu.Odada, "Yeniden bağlanma");
            yield return Bekle(() => a.Istemci.SonDurum != null && a.Istemci.SonDurum.Oyuncular.Count == 2
                && a.Istemci.SonDurum.Oyuncular.Exists(o => o.Isim == "Donen"), "Odada iki kişi");

            a.Istemci.Ayril();
            b.Istemci.Ayril();
        }

        /// <summary>
        /// S5 kapanışı gerçek sunucuyla: sohbet ve DM dönüşü, grup paketinin parça parça yüklenip seçilmesi,
        /// Temiz modda UYGUNSUZ (kimseye gitmez) ve 3. ihlalde atılma.
        /// </summary>
        [UnityTest]
        public IEnumerator Sohbet_GrupPaketi_TemizAtma()
        {
            var a = new DenemeBotu("Kurucu", 1);
            var b = new DenemeBotu("Yazan", 2);
            var saat = Stopwatch.StartNew();
            double son = 0;
            GrupPaketiYukleyici yukleyici = null;
            var bHatalari = new List<HataMesaji>();
            var aOlaylari = new List<OlayMesaji>();
            string bAyrilma = null;
            b.Istemci.HataGeldi += bHatalari.Add;
            b.Istemci.Ayrildi += n => bAyrilma = n;
            a.Istemci.OlayGeldi += aOlaylari.Add;

            IEnumerator Bekle(Func<bool> kosul, string ne, double sn = 10)
            {
                double bitis = saat.Elapsed.TotalSeconds + sn;
                while (!kosul())
                {
                    double g = saat.Elapsed.TotalSeconds - son;
                    son = saat.Elapsed.TotalSeconds;
                    a.Guncelle(g);
                    b.Guncelle(g);
                    if (yukleyici != null)
                    {
                        if (a.Istemci.SonDurum != null) yukleyici.DurumGeldi(a.Istemci.SonDurum.Surum);
                        yukleyici.Ilerle(g);
                    }
                    if (a.Istemci.Durum == BaglantiDurumu.Kapali) Assert.Ignore("Yerel sunucu çalışmıyor.");
                    if (saat.Elapsed.TotalSeconds > bitis) Assert.Fail(ne + " olmadı. a:" + a.SonHata + " b:" + b.SonHata);
                    yield return null;
                }
            }

            IEnumerator Dur(double sn)
            {
                double bitis = saat.Elapsed.TotalSeconds + sn;
                yield return Bekle(() => saat.Elapsed.TotalSeconds >= bitis, "Bekleme", sn + 5);
            }

            a.Istemci.OdaKur(Adres, a.Isim);
            yield return Bekle(() => a.Istemci.Durum == BaglantiDurumu.Odada, "Oda kurma");
            b.Istemci.Katil(Adres, a.Istemci.Oda, b.Isim);
            yield return Bekle(() => b.Istemci.Durum == BaglantiDurumu.Odada && a.Istemci.SonDurum?.Oyuncular.Count == 2, "Katılma");
            var aId = a.Istemci.Sen.Value;
            var bId = b.Istemci.Sen.Value;

            // Genel: yazana da döner.
            Assert.That(a.Istemci.SohbetGonder(null, "selam millet"), Is.True);
            yield return Bekle(() => a.Istemci.Sohbet.Mesajlar(null).Count == 1 && b.Istemci.Sohbet.Mesajlar(null).Count == 1, "Genel sohbet");
            Assert.That(a.Istemci.Sohbet.Mesajlar(null)[0].Benim, Is.True);
            Assert.That(b.Istemci.Sohbet.Mesajlar(null)[0].Benim, Is.False);
            Assert.That(b.Istemci.Sohbet.Mesajlar(null)[0].Kim, Is.EqualTo(aId));

            // DM: kanal karşıdaki kişi.
            Assert.That(b.Istemci.SohbetGonder(aId, "psst"), Is.True);
            yield return Bekle(() => a.Istemci.Sohbet.Mesajlar(bId).Count == 1 && b.Istemci.Sohbet.Mesajlar(aId).Count == 1, "DM");
            Assert.That(b.Istemci.Sohbet.Mesajlar(aId)[0].Benim, Is.True);
            Assert.That(a.Istemci.Sohbet.Mesajlar(null).Count, Is.EqualTo(1), "DM genele düşmez");

            // Grup paketi: uzun sorularla birden çok parça.
            var sorular = new List<string>();
            for (int i = 0; i < 120; i++) sorular.Add("Grupta en çok kim " + i + ". kez geç kalır ve bunu nasıl açıklar, uzun bir soru olsun diye ğüşıöç ekliyorum?");
            yukleyici = new GrupPaketiYukleyici(sorular, a.Istemci.Gonder);
            Assert.That(yukleyici.Toplam, Is.GreaterThan(1));
            yukleyici.Basla(a.Istemci.SonDurum.Surum);
            yield return Bekle(() => yukleyici.Durum != GrupPaketiYukleyici.Hal.Yukleniyor, "Grup paketinin yüklenmesi", 30);
            Assert.That(yukleyici.Durum, Is.EqualTo(GrupPaketiYukleyici.Hal.Bitti), "hata: " + yukleyici.HataKodu);
            yukleyici = null;
            yield return Bekle(() => b.Istemci.SonDurum.GrupPaketi == 120, "Herkesin 120 soruyu görmesi");

            var paketler = new List<string>(a.Istemci.SonDurum.Ayarlar.Paketler) { "@bizim" };
            a.Istemci.Gonder(Cevirici.AyarDegistir(paketler: paketler));
            yield return Bekle(() => b.Istemci.SonDurum.Ayarlar.Paketler.Contains("@bizim"), "@bizim seçimi");

            // Temiz: uygunsuz sohbet kimseye gitmez, sadece yazana UYGUNSUZ; 3. seferde atılır.
            a.Istemci.Gonder(Cevirici.AyarDegistir(mod: OyunModu.Temiz));
            yield return Bekle(() => b.Istemci.SonDurum.Ayarlar.Mod == OyunModu.Temiz, "Temiz mod");
            int genelOnce = a.Istemci.Sohbet.Mesajlar(null).Count;
            for (int i = 1; i <= 3; i++)
            {
                yield return Dur(1.2);
                Assert.That(b.Istemci.SohbetGonder(null, "siktir git"), Is.True);
                if (i < 3)
                {
                    int n = i;
                    yield return Bekle(() => bHatalari.Count(h => h.Kod == "UYGUNSUZ") == n, n + ". UYGUNSUZ");
                    var h0 = bHatalari.Last();
                    Assert.That(h0.Ihlal, Is.EqualTo(n));
                    Assert.That(h0.Sinir, Is.EqualTo(3));
                }
            }
            yield return Bekle(() => bAyrilma != null, "Temizden atılma");
            Assert.That(bAyrilma, Is.EqualTo("ATILDIN.temiz"));
            yield return Bekle(() => aOlaylari.Any(o => o.Atildi), "Atıldı olayı");
            Assert.That(aOlaylari.First(o => o.Atildi).Neden, Is.EqualTo("temiz"));
            Assert.That(a.Istemci.Sohbet.Mesajlar(null).Count, Is.EqualTo(genelOnce), "uygunsuz mesaj kimseye gitmedi");

            a.Istemci.Ayril();
        }
    }
}
