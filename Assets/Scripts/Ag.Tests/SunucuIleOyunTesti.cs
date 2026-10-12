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
            yield return Bekle(() => kurucu.Istemci.SonDurum.Ayarlar?.TurSayisi == 6 && kurucu.Istemci.SonDurum.Ayarlar.Kaos, 10, "Ayarların gelmesi");
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
    }
}
