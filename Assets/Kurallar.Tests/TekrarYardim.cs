using System;
using NUnit.Framework;
using static Cakmak.Kurallar.Tests.Kurulum;

namespace Cakmak.Kurallar.Tests
{
    /// <summary>S3 testlerinin ortak yardımcıları.</summary>
    static class TekrarYardim
    {
        public static readonly string[] Havuz = { "Havuz sorusu 1 kim?", "Havuz sorusu 2 kim?", "Havuz sorusu 3 kim?" };
        public static readonly string[] Sesli = { "Hayvan taklidi yap", "Bir şarkı söyle" };

        /// <summary>A'yı her oyuncunun görünümüne bakarak bulur (Soran Gizli turunda A sadece kendi görünümünde).</summary>
        public static OyuncuId GercekA(Oyun oyun)
        {
            foreach (var o in Durum(oyun).Oyuncular)
                if (oyun.GorunumAl(o.Id).A == o.Id) return o.Id;
            throw new AssertionException("A bulunamadı");
        }

        /// <summary>Bekleyen İkiye Katla / Bedel kararını "hayır" diye geçer.</summary>
        public static void KarariGec(Oyun oyun)
        {
            var g = Durum(oyun);
            if (g.Asama == Asama.IkiyeKatla || g.Asama == Asama.Bedel)
                Assert.That(oyun.KarariGec(g.KararVeren.Value), Is.EqualTo(Sonuc.Tamam));
        }

        /// <summary>Bir turu sonuna kadar oynar (TKM, kararlar "hayır"), sonra 6 sn bekleyip sıradaki tura geçer.</summary>
        public static void TurOyna(Oyun oyun, bool cKazansin)
        {
            var a = GercekA(oyun);
            var b = oyun.GorunumAl(a).Secilebilir[0];
            Assert.That(oyun.SoruSor(a, b, "Soru?"), Is.EqualTo(Sonuc.Tamam));
            var c = oyun.GorunumAl(b).Secilebilir[0];
            Assert.That(oyun.CevapSec(b, c), Is.EqualTo(Sonuc.Tamam));
            var asama = Durum(oyun).Asama;
            if (asama == Asama.Oylama)
                foreach (var v in Durum(oyun).Oylayanlar) oyun.Oy(v, cKazansin);
            else if (asama == Asama.MiniOyun)
                MiniOyunOyna(oyun, b, c, cKazansin);
            // Başka aşamaya geçtiyse ("sorun ifşa" cezası oylamayı atladı) oynanacak bir şey yok.
            KarariGec(oyun);
            KarariGec(oyun);
            Saniye(oyun, 6);
        }

        /// <summary>Bu turun mini oyununu oynar. TKM, Tek-Çift ve Reaksiyon'da kazanan seçilebilir; Zar şansa kalır.</summary>
        public static void MiniOyunOyna(Oyun oyun, OyuncuId b, OyuncuId c, bool cKazansin)
        {
            switch (Durum(oyun).MiniOyun)
            {
                case MiniOyunTuru.Tkm: TkmOyna(oyun, b, c, cKazansin); return;
                case MiniOyunTuru.Zar: oyun.MiniOyunHamlesi(b, Hamle.ZarAt()); oyun.MiniOyunHamlesi(c, Hamle.ZarAt()); return;
                case MiniOyunTuru.TekCift: // 1 + 2 = 3, tek: B "tek" derse kazanır
                    oyun.MiniOyunHamlesi(b, Hamle.TekCift(1, tekDiyor: !cKazansin));
                    oyun.MiniOyunHamlesi(c, Hamle.TekCift(2));
                    return;
                case MiniOyunTuru.Reaksiyon:
                    oyun.MiniOyunHamlesi(b, Hamle.Reaksiyon(300));
                    oyun.MiniOyunHamlesi(c, Hamle.Reaksiyon(cKazansin ? 200 : 400));
                    return;
            }
        }

        /// <summary>5. turun kaosu istenen kural olana kadar tohum dener. Oyunu 5. turun soru sorma aşamasında döner.</summary>
        public static Oyun KaosaGetir(KaosKurali kural, Action<Ayarlar> ek = null)
        {
            for (int tohum = 1; tohum <= 3000; tohum++)
            {
                var oyun = Yeni(kisi: 5, tohum: tohum, ayarla: x =>
                {
                    x.KaosTurlari = true;
                    x.OneriSorulari = Havuz;
                    x.TurSayisi = 0;
                    ek?.Invoke(x);
                });
                for (int i = 0; i < 4; i++) TurOyna(oyun, cKazansin: i % 2 == 0);
                var g = Durum(oyun);
                Assert.That(g.Tur, Is.EqualTo(5));
                if (g.Kaos == kural) return oyun;
            }
            throw new AssertionException(kural + " kaosu 3000 tohumda çıkmadı");
        }

        /// <summary>A soruyu sorar, B ilk seçeneği C seçer. Mini oyun ya da oylama aşamasında bırakır.</summary>
        public static void SorSec(Oyun oyun, string soru, out OyuncuId a, out OyuncuId b, out OyuncuId c)
        {
            a = GercekA(oyun);
            b = oyun.GorunumAl(a).Secilebilir[0];
            Assert.That(oyun.SoruSor(a, b, soru), Is.EqualTo(Sonuc.Tamam));
            c = oyun.GorunumAl(b).Secilebilir[0];
            Assert.That(oyun.CevapSec(b, c), Is.EqualTo(Sonuc.Tamam));
        }

        public static bool KisitlamaVar(Oyun oyun, OyuncuId id, CezaTuru tur)
        {
            foreach (var k in Oyuncu(oyun, id).Kisitlamalar) if (k.Tur == tur) return true;
            return false;
        }

        public static OyuncuDurumu Oyuncu(Oyun oyun, OyuncuId id)
        {
            foreach (var o in Durum(oyun).Oyuncular) if (o.Id == id) return o;
            return null;
        }
    }
}
