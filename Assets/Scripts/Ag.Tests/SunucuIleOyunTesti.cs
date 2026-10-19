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

        /// <summary>
        /// Takım Düellosu (tasks/g2-takim.md 4, 5): kurucu dağıtır (dengesizde "baslat" reddedilir), takım sohbeti sadece
        /// takıma düşer (iki takım, iki "pencere"), oyun 7 puanda ya da turlar bitince biter, özet tutarlı.
        /// </summary>
        [UnityTest]
        public IEnumerator DortBot_TakimDuellosu_DagitimTakimSohbetiVeSkor()
        {
            var botlar = new List<DenemeBotu>();
            for (int i = 0; i < 4; i++) botlar.Add(new DenemeBotu("Tk" + (i + 1), 4000 + i));
            var kurucu = botlar[0];
            var olaylar = new List<OlayMesaji>();
            var hatalar = new List<HataMesaji>();
            kurucu.Istemci.OlayGeldi += olaylar.Add;
            kurucu.Istemci.HataGeldi += hatalar.Add;
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
            yield return Bekle(() => botlar.All(b => b.Istemci.Sen.HasValue) && kurucu.Istemci.SonDurum?.Oyuncular.Count == 4, 10, "Katılma");
            var id = botlar.Select(b => b.Istemci.Sen.Value).ToList();

            // Dengesiz dağılım: lobi kabul eder ama "baslat" reddedilir.
            var dengesiz = new Dictionary<OyuncuId, int> { [id[0]] = 0, [id[1]] = 0, [id[2]] = 0, [id[3]] = 1 };
            kurucu.Istemci.Gonder(Cevirici.AyarDegistir(oyun: OyunTuru.Takim, ceza: CezaSeviyesi.Kapali, gorevler: false, kaos: false,
                soruSn: 10, cevapSn: 10, hamleSn: 5, miniOyun: MiniOyunTuru.Zar, turSayisi: 10, takimlar: dengesiz));
            yield return Bekle(() => botlar.All(b => b.Istemci.SonDurum?.Ayarlar?.Oyun == OyunTuru.Takim
                && b.Istemci.SonDurum.Ayarlar.Takimlar.Count == 4), 10, "Takım ayarı herkese");
            Assert.That(new TheLighter.Akis.TakimDagitimi(kurucu.Istemci.SonDurum.Ayarlar.Takimlar).Gecerli(id), Is.False, "ekran da uyarır");
            kurucu.Istemci.Gonder(Cevirici.Baslat());
            yield return Bekle(() => hatalar.Any(h => h.Kod == "GECERSIZ_GIRDI"), 10, "Dengesiz başlatın reddi");
            Assert.That(kurucu.Istemci.SonDurum.Yer, Is.EqualTo(OdaYeri.Lobi));

            // Kırmızı: 1. ve 3. bot, Mavi: 2. ve 4.
            var dagilim = new Dictionary<OyuncuId, int> { [id[0]] = 0, [id[1]] = 1, [id[2]] = 0, [id[3]] = 1 };
            kurucu.Istemci.Gonder(Cevirici.AyarDegistir(takimlar: dagilim));
            yield return Bekle(() => botlar.All(b => b.Istemci.SonDurum?.Ayarlar?.Takimlar.Count(kv => kv.Value == 1) == 2), 10, "Dengeli dağılım herkese");
            kurucu.Istemci.Gonder(Cevirici.Baslat());
            yield return Bekle(() => botlar.All(b => b.Istemci.SonDurum?.Yer == OdaYeri.Oyun && b.Istemci.SonDurum.Gorunum?.TakimPuanlari.Count == 2), 10, "Başlama");
            foreach (var o in kurucu.Istemci.SonDurum.Gorunum.Oyuncular)
                Assert.That(o.Takim, Is.EqualTo(dagilim[o.Id]), "kurucunun dağılımı oyunda");

            // Takım sohbeti: Kırmızı'nın yazdığı sadece Kırmızı'ya (yazan dahil), Mavi'nin sadece Mavi'ye.
            var takimYeri = SohbetYeri.Adli(SohbetKanali.Takim);
            Assert.That(botlar[0].Istemci.SohbetGonder(takimYeri, "kirmizi plan"), Is.True);
            Assert.That(botlar[1].Istemci.SohbetGonder(takimYeri, "mavi plan"), Is.True);
            yield return Bekle(() => botlar.All(b => b.Istemci.Sohbet.Mesajlar(takimYeri).Count >= 1), 10, "Takım mesajları");
            double bitisAni = saat.Elapsed.TotalSeconds + 1.5;
            yield return Bekle(() => saat.Elapsed.TotalSeconds >= bitisAni, 5, "Geç gelen mesaj beklemesi");
            for (int i = 0; i < 4; i++)
            {
                var gelen = botlar[i].Istemci.Sohbet.Mesajlar(takimYeri);
                Assert.That(gelen.Count, Is.EqualTo(1), botlar[i].Isim + " sadece kendi takımının mesajını alır");
                Assert.That(gelen[0].Metin, Is.EqualTo(i % 2 == 0 ? "kirmizi plan" : "mavi plan"));
                Assert.That(botlar[i].Istemci.Sohbet.Mesajlar(null), Is.Empty, "takım mesajı genele düşmez");
            }

            yield return Bekle(() => botlar.All(b => b.OyunBitti), 400, "Oyunun bitmesi");

            var t = kurucu.Istemci.SonDurum.Ozet?.Takim;
            Assert.That(t, Is.Not.Null, "özette Takım Düellosu");
            Assert.That(t.Puanlar.Count, Is.EqualTo(2));
            if (t.Kazanan.HasValue) Assert.That(t.Puanlar[t.Kazanan.Value], Is.GreaterThan(t.Puanlar[1 - t.Kazanan.Value]));
            else Assert.That(t.Puanlar[0], Is.EqualTo(t.Puanlar[1]));
            var puanOlaylari = olaylar.Where(o => o.Olay != null && (o.Olay.Tur == OlayTuru.DuelloPuani || o.Olay.Tur == OlayTuru.IfsaBonusu)).ToList();
            Assert.That(puanOlaylari.All(o => o.Olay.Takim.HasValue), Is.True, "puan olaylarında takım");
            Assert.That(puanOlaylari.Count, Is.EqualTo(t.Puanlar[0] + t.Puanlar[1]), "eşit takımlar: her puan bir olay");

            foreach (var b in botlar) b.Istemci.Ayril();
        }

        /// <summary>
        /// Casus (tasks/g2-casus.md 4, 5): iki oyun. İlki mekânla, casus "Mekânı biliyorum" der; ikincisi kelimeyle, tur bitince
        /// son oylama (yakalanırsa son şans). Hiçbir durumda casusa sır gelmez; özet ikisinde de dolu.
        /// </summary>
        [UnityTest]
        public IEnumerator DortBot_Casus_BiliyorumVeSonOylama()
        {
            var botlar = new List<DenemeBotu>();
            for (int i = 0; i < 4; i++) botlar.Add(new DenemeBotu("Cs" + (i + 1), 5000 + i));
            var kurucu = botlar[0];
            var olaylar = new List<OlayMesaji>();
            kurucu.Istemci.OlayGeldi += olaylar.Add;
            int casusaSirGeldi = 0;
            foreach (var b in botlar)
                b.Istemci.DurumGeldi += d =>
                {
                    var g = d.Gorunum;
                    if (g != null && g.Rolun == Rol.Casus && (g.Sirrin != null || g.MekanRolun != null)) casusaSirGeldi++;
                };
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

            // 1. oyun: mekân, casus ilk fırsatta "Mekânı biliyorum".
            foreach (var b in botlar) b.ErkenTahminEtsin = true;
            kurucu.Istemci.Gonder(Cevirici.AyarDegistir(oyun: OyunTuru.Casus, casus: CasusTuru.Mekan, mod: OyunModu.Normal,
                ceza: CezaSeviyesi.Kapali, gorevler: false, kaos: false, soruSn: 10, cevapSn: 10, hamleSn: 5,
                miniOyun: MiniOyunTuru.Zar, turSayisi: 4));
            yield return Bekle(() => botlar.All(b => b.Istemci.SonDurum?.Ayarlar?.Oyun == OyunTuru.Casus
                && b.Istemci.SonDurum.Ayarlar.TurSayisi == 4), 10, "Casus ayarı herkese");
            kurucu.Istemci.Gonder(Cevirici.Baslat());
            yield return Bekle(() => botlar.All(b => b.Istemci.SonDurum?.Yer == OdaYeri.Oyun), 10, "Başlama");
            Assert.That(botlar.Count(b => b.Istemci.SonDurum.Gorunum.Rolun == Rol.Casus), Is.EqualTo(1), "tek casus");
            Assert.That(botlar.All(b => b.Istemci.SonDurum.Gorunum.CasusListesi.Count > 0), Is.True, "liste herkeste");
            yield return Bekle(() => botlar.All(b => b.OyunBitti), 120, "1. oyunun bitmesi");
            var c1 = kurucu.Istemci.SonDurum.Ozet?.Casus;
            Assert.That(c1, Is.Not.Null, "özette Casus");
            Assert.That(c1.Neden, Is.EqualTo(CasusNedeni.Bildi).Or.EqualTo(CasusNedeni.YanlisTahmin), "Mekânı biliyorum ile bitti");
            Assert.That(c1.Tahmin, Is.Not.Null);
            Assert.That(olaylar.Any(o => o.Olay != null && o.Olay.Tur == OlayTuru.CasusTahminEtti), Is.True);

            // 2. oyun: kelime, erken tahmin yok; tur bitince son oylama.
            foreach (var b in botlar) b.ErkenTahminEtsin = false;
            kurucu.Istemci.Gonder(Cevirici.TekrarOyna());
            yield return Bekle(() => botlar.All(b => b.Istemci.SonDurum?.Yer == OdaYeri.Lobi), 10, "Lobiye dönüş");
            kurucu.Istemci.Gonder(Cevirici.AyarDegistir(casus: CasusTuru.Kelime));
            yield return Bekle(() => botlar.All(b => b.Istemci.SonDurum?.Ayarlar?.Casus == CasusTuru.Kelime), 10, "Kelime ayarı herkese");
            kurucu.Istemci.Gonder(Cevirici.Baslat());
            yield return Bekle(() => botlar.All(b => b.Istemci.SonDurum?.Yer == OdaYeri.Oyun && b.Istemci.SonDurum.Gorunum != null
                && b.Istemci.SonDurum.Gorunum.Asama != Asama.OyunSonu), 10, "2. oyunun başlaması");
            Assert.That(botlar.All(b => b.Istemci.SonDurum.Gorunum.CasusKategori != null), Is.True, "kelimede kategori herkeste");
            yield return Bekle(() => botlar.All(b => b.OyunBitti), 300, "2. oyunun bitmesi");
            var c2 = kurucu.Istemci.SonDurum.Ozet?.Casus;
            Assert.That(c2, Is.Not.Null);
            Assert.That(c2.Neden, Is.Not.EqualTo(CasusNedeni.Bildi).And.Not.EqualTo(CasusNedeni.YanlisTahmin), "erken tahmin yoktu");
            Assert.That(olaylar.Any(o => o.Olay != null && o.Olay.Tur == OlayTuru.SuclamaBasladi), Is.True, "son oylama oldu");

            Assert.That(casusaSirGeldi, Is.EqualTo(0), "casusa sır hiçbir durumda gelmez");
            foreach (var b in botlar) b.Istemci.Ayril();
        }

        /// <summary>
        /// Çakmak Gecesi (tasks/g2-cakmak-gecesi.md 4, 5): 8 bot, 12'den az olduğu için rol açık ve 1 kurban. İlk gece katil kanalı
        /// sadece katillere düşer, gece genel sohbet reddedilir; oyun bir tarafın kazanmasıyla biter, özette herkesin rolü.
        /// </summary>
        [UnityTest]
        public IEnumerator SekizBot_CakmakGecesi_KatilKanaliVeSonuc()
        {
            var botlar = new List<DenemeBotu>();
            for (int i = 0; i < 8; i++) botlar.Add(new DenemeBotu("Gc" + (i + 1), 6000 + i));
            var kurucu = botlar[0];
            var hatalar = new Dictionary<DenemeBotu, List<HataMesaji>>();
            foreach (var b in botlar)
            {
                var l = new List<HataMesaji>();
                hatalar[b] = l;
                b.Istemci.HataGeldi += l.Add;
            }
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
            for (int i = 1; i < 8; i++) botlar[i].Istemci.Katil(Adres, kurucu.Istemci.Oda, botlar[i].Isim);
            yield return Bekle(() => kurucu.Istemci.SonDurum?.Oyuncular.Count == 8, 10, "Katılma");

            kurucu.Istemci.Gonder(Cevirici.AyarDegistir(oyun: OyunTuru.CakmakGecesi, mod: OyunModu.Normal, ceza: CezaSeviyesi.Kapali,
                gorevler: false, kaos: false, soruSn: 10, cevapSn: 10, hamleSn: 5, miniOyun: MiniOyunTuru.Zar, rolAcilir: true, gecelikKurban: 1));
            yield return Bekle(() => botlar.All(b => b.Istemci.SonDurum?.Ayarlar?.Oyun == OyunTuru.CakmakGecesi), 10, "Çakmak Gecesi ayarı herkese");
            kurucu.Istemci.Gonder(Cevirici.Baslat());
            yield return Bekle(() => botlar.All(b => b.Istemci.SonDurum?.Yer == OdaYeri.Oyun && b.Istemci.SonDurum.Gorunum?.Rolun != Rol.Yok), 10, "Başlama");

            var katiller = botlar.Where(b => Oyun.TarafI(b.Istemci.SonDurum.Gorunum.Rolun) == Taraf.Katil).ToList();
            Assert.That(katiller.Count, Is.EqualTo(2), "8 kişide 2 katil");
            Assert.That(katiller.All(k => k.Istemci.SonDurum.Gorunum.Katiller.Count == 2), Is.True, "katiller birbirini bilir");
            Assert.That(botlar.Except(katiller).All(b => b.Istemci.SonDurum.Gorunum.Katiller.Count == 0), Is.True);

            // İlk gece: katil kanalı sadece katillere, genel sohbet herkese kapalı. Linç başlayınca botlar durur: linç süresi
            // dolar (kimse gitmez), gece botlar seçim yapmadan beklerken sohbet denenir.
            yield return Bekle(() => botlar.Any(b => b.Istemci.SonDurum?.Gorunum?.Asama == Asama.Linc) || botlar.All(b => b.OyunBitti), 300, "İlk linç");
            foreach (var b in botlar) b.Dursun = true;
            yield return Bekle(() => botlar.All(b => b.Istemci.SonDurum?.Gorunum?.Asama == Asama.Gece) || botlar.All(b => b.OyunBitti), 120, "İlk gece");
            if (!botlar.All(b => b.OyunBitti))
            {
                var katilYeri = SohbetYeri.Adli(SohbetKanali.Katil);
                Assert.That(katiller[0].Istemci.SohbetGonder(katilYeri, "bu gece Gc1"), Is.True);
                var masum = botlar.First(b => !katiller.Contains(b) && !b.Istemci.SonDurum.Gorunum.Oyuncular.First(o => o.Id == b.Istemci.Sen).Elendi);
                Assert.That(masum.Istemci.SohbetGonder(null, "uyuyamıyorum"), Is.True);
                yield return Bekle(() => katiller.All(k => k.Istemci.Sohbet.Mesajlar(katilYeri).Count == 1)
                    && hatalar[masum].Any(h => h.Kod == "GECERSIZ_GIRDI"), 10, "Katil kanalı ve gece genel reddi");
                Assert.That(botlar.Except(katiller).All(b => b.Istemci.Sohbet.Mesajlar(katilYeri).Count == 0), Is.True, "katil kanalı masuma düşmez");
                Assert.That(botlar.All(b => b.Istemci.Sohbet.Mesajlar(null).Count == 0), Is.True, "gece genel sohbet kimseye gitmez");
            }
            foreach (var b in botlar) b.Dursun = false;

            yield return Bekle(() => botlar.All(b => b.OyunBitti), 600, "Oyunun bitmesi");
            var s = kurucu.Istemci.SonDurum.Ozet?.Gece;
            Assert.That(s, Is.Not.Null, "özette Çakmak Gecesi");
            Assert.That(s.Kazanan.HasValue, Is.True, "bir taraf kazandı");
            Assert.That(s.Roller.Count, Is.EqualTo(8), "herkesin rolü özette");
            Assert.That(botlar.Sum(b => b.GeceSecti), Is.GreaterThan(0), "geceler oynandı");

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

        /// <summary>
        /// Rapor ve engelleme gerçek sunucuyla (tasks/g2-android.md 6): engellenenin genel mesajı ve DM'i gelmez, o yine seni görür;
        /// rapor <c>raporAlindi</c> ile döner; engel kaldırılınca mesajlar yine gelir.
        /// </summary>
        [UnityTest]
        public IEnumerator Rapor_Engelleme()
        {
            var a = new DenemeBotu("Engelleyen", 1);
            var b = new DenemeBotu("Engellenen", 2);
            var saat = Stopwatch.StartNew();
            double son = 0;
            var raporlar = new List<KeyValuePair<OyuncuId, bool>>();
            a.Istemci.RaporCevabi += (kim, alindi) => raporlar.Add(new KeyValuePair<OyuncuId, bool>(kim, alindi));

            IEnumerator Bekle(Func<bool> kosul, string ne, double sn = 10)
            {
                double bitis = saat.Elapsed.TotalSeconds + sn;
                while (!kosul())
                {
                    double g = saat.Elapsed.TotalSeconds - son;
                    son = saat.Elapsed.TotalSeconds;
                    a.Guncelle(g);
                    b.Guncelle(g);
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

            // a, b'yi engeller: liste sunucudan döner.
            Assert.That(a.Istemci.Engelle(bId), Is.True);
            yield return Bekle(() => a.Istemci.EngelliMi(bId), "Engel listesi");

            // b'nin genel mesajı ve DM'i a'ya gelmez (b kendi mesajını görür: sunucu yazana yollar).
            Assert.That(b.Istemci.SohbetGonder(null, "beni görüyor musun"), Is.True);
            yield return Bekle(() => b.Istemci.Sohbet.Mesajlar(null).Count == 1, "Engellenenin kendi mesajı");
            yield return Dur(1.2);
            Assert.That(b.Istemci.SohbetGonder(aId, "dm de gitmez"), Is.True);
            yield return Bekle(() => b.Istemci.Sohbet.Mesajlar(aId).Count == 1, "Engellenenin kendi DM'i");
            yield return Dur(1.0);
            Assert.That(a.Istemci.Sohbet.Mesajlar(null), Is.Empty, "engellenenin genel mesajı gelmez");
            Assert.That(a.Istemci.Sohbet.Mesajlar(bId), Is.Empty, "engellenenin DM'i gelmez");

            // Engelleyen yazmaya devam eder, engellenen onu görür.
            Assert.That(a.Istemci.SohbetGonder(null, "ben yazarım"), Is.True);
            yield return Bekle(() => b.Istemci.Sohbet.Mesajlar(null).Count == 2, "Engelleyenin mesajı engellenene");

            // Rapor: raporAlindi döner (sunucu diske yazamazsa RAPOR_YAZILAMADI: o zaman sunucunun günlüğüne bak).
            Assert.That(a.Istemci.Raporla(bId), Is.True);
            yield return Bekle(() => raporlar.Count == 1, "Rapor cevabı");
            Assert.That(raporlar[0].Key, Is.EqualTo(bId));
            Assert.That(raporlar[0].Value, Is.True, "RAPOR_YAZILAMADI: sunucu raporu diske yazamadı");

            // Engel kalkınca mesajlar yine gelir.
            yield return Dur(1.2);
            Assert.That(a.Istemci.Engelle(bId, engel: false), Is.True);
            yield return Bekle(() => a.Istemci.Engellenenler.Count == 0, "Engelin kalkması");
            yield return Dur(1.2);
            Assert.That(b.Istemci.SohbetGonder(null, "şimdi?"), Is.True);
            yield return Bekle(() => a.Istemci.Sohbet.Mesajlar(null).Any(m => m.Kim == bId), "Engel kalkınca mesaj");

            a.Istemci.Ayril();
            b.Istemci.Ayril();
        }
    }
}
