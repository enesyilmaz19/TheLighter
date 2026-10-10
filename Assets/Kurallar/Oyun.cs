using System;
using System.Collections.Generic;

namespace Cakmak.Kurallar
{
    /// <summary>
    /// Bir oyunun bütün kuralları. Elden ele modda Unity, online modda sunucu aynı sınıfı çalıştırır.
    /// Saat tutmaz (<see cref="Ilerle"/>), rastgeleliği tohumdan alır, hata fırlatmaz (<see cref="Sonuc"/>).
    /// </summary>
    public sealed class Oyun
    {
        public const int EnAzOyuncu = 4;
        public const int EnFazlaOyuncu = 10;
        public const int SoruEnFazlaKarakter = 140;
        public const int KisiBasiHavuzSorusu = 2;
        public const int BeraberlikSiniri = 3;
        /// <summary>Reaksiyonda bundan hızlısı "erken bastı" sayılır.</summary>
        public const int ReaksiyonEnAzMs = 80;

        static readonly MiniOyunTuru[] KarisikHavuzu = { MiniOyunTuru.Tkm, MiniOyunTuru.TekCift, MiniOyunTuru.Zar };

        /// <summary>Kur içinde olay yayılmaz. Kurduktan sonra ilk durumu GorunumAl ile oku.</summary>
        public event Action<Olay> OlayOldu;

        readonly Ayarlar ayar;
        readonly List<OyuncuDurumu> oyuncular = new List<OyuncuDurumu>();
        readonly Random rastgele;
        readonly Deste oneriDestesi;
        readonly List<string> toplanan = new List<string>();
        Deste havuz;

        Asama asama;
        double kalanMs;
        int tur;
        OyuncuId a;
        OyuncuId? b;
        OyuncuId? c;
        string soru;
        bool fisilti;
        string atananSoru;
        bool degistirmeHakki;
        string oneri;

        MiniOyunTuru buTurMiniOyun;
        Hamle? bHamle;
        Hamle? cHamle;
        int beraberlik;
        MiniOyunSonucu sonMiniOyun;

        Oyun(Ayarlar ayar, IReadOnlyList<Oyuncu> liste, int tohum)
        {
            this.ayar = ayar;
            rastgele = new Random(tohum);
            foreach (var o in liste)
                oyuncular.Add(new OyuncuDurumu { Id = o.Id, Isim = o.Isim.Trim() });
            oneriDestesi = new Deste(ayar.OneriSorulari ?? Array.Empty<string>());

            if (ayar.Mod == OyunModu.Curcuna)
            {
                asama = Asama.SoruToplama;
                kalanMs = ayar.ToplamaSuresiSn * 1000.0;
            }
            else
            {
                TurBaslat(RastgeleOyuncu());
            }
        }

        public static Sonuc Kur(Ayarlar ayarlar, IReadOnlyList<Oyuncu> oyuncular, int tohum, out Oyun oyun)
        {
            oyun = null;
            if (ayarlar == null || oyuncular == null) return Sonuc.GecersizGirdi;
            if (oyuncular.Count < EnAzOyuncu || oyuncular.Count > EnFazlaOyuncu) return Sonuc.OyuncuSayisiGecersiz;

            var gorulen = new HashSet<OyuncuId>();
            foreach (var o in oyuncular)
                if (o == null || string.IsNullOrWhiteSpace(o.Isim) || !gorulen.Add(o.Id)) return Sonuc.GecersizGirdi;

            if (ayarlar.TurSayisi < 0 || ayarlar.SoruSuresiSn <= 0 || ayarlar.CevapSuresiSn <= 0 || ayarlar.HamleSuresiSn <= 0
                || ayarlar.ToplamaSuresiSn <= 0 || ayarlar.SonucSuresiSn <= 0)
                return Sonuc.GecersizGirdi;

            oyun = new Oyun(ayarlar, oyuncular, tohum);
            return Sonuc.Tamam;
        }

        // ------------------------------------------------------------ Komutlar

        public Sonuc HavuzaSoruEkle(OyuncuId kim, string metin)
        {
            if (asama != Asama.SoruToplama) return Sonuc.YanlisAsama;
            var o = Bul(kim);
            if (o == null) return Sonuc.SiraSendeDegil;
            if (o.HavuzaEkledigi >= KisiBasiHavuzSorusu) return Sonuc.HakKalmadi;
            var temiz = SoruTemizle(metin);
            if (temiz == null) return Sonuc.GecersizGirdi;

            toplanan.Add(temiz);
            o.HavuzaEkledigi++;
            if (oyuncular.TrueForAll(x => x.HavuzaEkledigi >= KisiBasiHavuzSorusu)) ToplamayiBitir();
            return Sonuc.Tamam;
        }

        /// <summary>Normal/Temiz: havuzdan bir öneri çeker, A'nın görünümüne <c>Oneri</c> olarak düşer.</summary>
        public Sonuc OneriCek(OyuncuId kim)
        {
            if (asama != Asama.SoruSorma || ayar.Mod == OyunModu.Curcuna) return Sonuc.YanlisAsama;
            if (kim != a) return Sonuc.SiraSendeDegil;
            if (oneriDestesi.Bos) return Sonuc.HavuzBos;
            oneri = oneriDestesi.Cek(rastgele);
            return Sonuc.Tamam;
        }

        /// <summary>Curcuna: atanan soruyu bir kez değiştirir.</summary>
        public Sonuc SoruyuDegistir(OyuncuId kim)
        {
            if (asama != Asama.SoruSorma || ayar.Mod != OyunModu.Curcuna) return Sonuc.YanlisAsama;
            if (kim != a) return Sonuc.SiraSendeDegil;
            if (!degistirmeHakki) return Sonuc.HakKalmadi;
            if (havuz == null || havuz.Bos) return Sonuc.HavuzBos;
            atananSoru = havuz.Cek(rastgele);
            degistirmeHakki = false;
            return Sonuc.Tamam;
        }

        /// <summary>
        /// A, B'yi seçer ve soruyu gönderir. <paramref name="metin"/> null ise soru fısıldandı (elden ele).
        /// Curcuna'da metin yok sayılır, atanan soru gider.
        /// </summary>
        public Sonuc SoruSor(OyuncuId kim, OyuncuId hedef, string metin)
        {
            if (asama != Asama.SoruSorma) return Sonuc.YanlisAsama;
            if (kim != a) return Sonuc.SiraSendeDegil;
            if (hedef == a || Bul(hedef) == null) return Sonuc.Secilemez;

            string gidecek;
            bool fisildandi = false;
            if (ayar.Mod == OyunModu.Curcuna && atananSoru != null)
            {
                gidecek = atananSoru;
            }
            else if (metin == null)
            {
                gidecek = null;
                fisildandi = true;
            }
            else
            {
                gidecek = SoruTemizle(metin);
                if (gidecek == null) return Sonuc.GecersizGirdi;
            }

            b = hedef;
            soru = gidecek;
            fisilti = fisildandi;
            atananSoru = null;
            oneri = null;
            Bul(a).SoruSorma++;
            Bul(hedef).SoruAlma++;
            AsamayaGec(Asama.CevapSecme, ayar.CevapSuresiSn);
            Yay(new Olay { Tur = OlayTuru.SoruSoruldu, A = a, B = b });
            return Sonuc.Tamam;
        }

        /// <summary>B, sorunun cevabı olan C'yi seçer. C ne B ne A olabilir.</summary>
        public Sonuc CevapSec(OyuncuId kim, OyuncuId hedef)
        {
            if (asama != Asama.CevapSecme) return Sonuc.YanlisAsama;
            if (kim != b) return Sonuc.SiraSendeDegil;
            if (hedef == kim || hedef == a || Bul(hedef) == null) return Sonuc.Secilemez;

            c = hedef;
            Bul(hedef).Gosterilme++;
            AsamayaGec(Asama.MiniOyun, ayar.HamleSuresiSn);
            Yay(new Olay { Tur = OlayTuru.CevapSecildi, A = a, B = b, C = c });
            return Sonuc.Tamam;
        }

        public Sonuc MiniOyunHamlesi(OyuncuId kim, Hamle hamle)
        {
            if (asama != Asama.MiniOyun) return Sonuc.YanlisAsama;
            bool bMi = kim == b;
            if (!bMi && kim != c) return Sonuc.SiraSendeDegil;
            if (hamle.Tur != buTurMiniOyun) return Sonuc.GecersizGirdi;
            if (hamle.Tur == MiniOyunTuru.TekCift && (hamle.Sayi < 1 || hamle.Sayi > 5)) return Sonuc.GecersizGirdi;
            if (bMi ? bHamle.HasValue : cHamle.HasValue) return Sonuc.HakKalmadi;

            if (bMi) bHamle = hamle; else cHamle = hamle;
            if (bHamle.HasValue && cHamle.HasValue) MiniOyunuCoz();
            return Sonuc.Tamam;
        }

        /// <summary>Oyunu hemen bitirir. Online'da kimin çağırabileceğine sunucu karar verir.</summary>
        public Sonuc OyunuBitir()
        {
            if (asama == Asama.OyunSonu) return Sonuc.YanlisAsama;
            Bitir();
            return Sonuc.Tamam;
        }

        /// <summary>Zamanı ilerletir. Elden ele: her karede deltaTime. Sunucu: her odada ~100 ms'de bir.</summary>
        public void Ilerle(TimeSpan gecen)
        {
            if (asama == Asama.OyunSonu) return;
            kalanMs -= gecen.TotalMilliseconds;
            if (kalanMs > 0) return;

            // ponytail: süre dolunca yeni aşama tam süreyle başlar, artan süre taşınmaz.
            // Tek çağrıda en fazla bir geçiş olur. Dev bir adım (dakikalar) birden çok aşama atlatmaz.
            switch (asama)
            {
                case Asama.SoruToplama:
                    ToplamayiBitir();
                    break;
                case Asama.SoruSorma:
                    Yay(new Olay { Tur = OlayTuru.SureDoldu, Kim = a });
                    TurBaslat(RastgeleOyuncu(a));
                    break;
                case Asama.CevapSecme:
                    var sureliB = b.Value;
                    soru = null; // güme gitti, hiçbir yerde kalmaz
                    Yay(new Olay { Tur = OlayTuru.SureDoldu, Kim = sureliB });
                    TurBaslat(RastgeleOyuncu(a, sureliB));
                    break;
                case Asama.MiniOyun:
                    // Hamle yapmayan kaybeder. İkisi de yapmadıysa B kazanır, soru gizli kalır.
                    // Reaksiyonda erken basmak hamle sayılmaz: kırmızıyken basan, süre dolsa da kaybeder.
                    bool cKazandi = Erken(bHamle) || (!Erken(cHamle) && cHamle.HasValue && !bHamle.HasValue);
                    Sonuclandir(cKazandi, sureDoldu: true, zarlaKarar: false, 0, 0);
                    break;
                case Asama.Ifsa:
                case Asama.Gume:
                    TurBaslat(c.Value);
                    break;
            }
        }

        // ------------------------------------------------------------ Okuma

        /// <summary>Bu oyuncunun görmesi gerekenler. Oyunda olmayan biri için null.</summary>
        public Gorunum GorunumAl(OyuncuId kim)
        {
            if (Bul(kim) == null) return null;

            bool turda = asama != Asama.SoruToplama && asama != Asama.OyunSonu;
            var g = new Gorunum
            {
                Sen = kim,
                Asama = asama,
                Tur = tur,
                ToplamTur = ayar.TurSayisi,
                KalanMs = asama == Asama.OyunSonu ? 0 : (int)Math.Ceiling(Math.Max(0, kalanMs)),
                Oyuncular = OyuncuKopyasi(),
                A = turda ? a : (OyuncuId?)null,
                B = turda ? b : null,
                C = turda ? c : null,
                MiniOyun = buTurMiniOyun,
                BHamleYapti = bHamle.HasValue,
                CHamleYapti = cHamle.HasValue,
                Beraberlik = beraberlik,
                SonMiniOyun = asama == Asama.Ifsa || asama == Asama.Gume ? sonMiniOyun.Kopya() : null,
            };

            if (asama == Asama.SoruSorma && kim == a)
            {
                g.Secilebilir = Secilebilir(a);
                g.AtananSoru = atananSoru;
                g.DegistirmeHakki = degistirmeHakki;
                g.Oneri = oneri;
            }
            else if (asama == Asama.CevapSecme && kim == b)
            {
                g.Secilebilir = Secilebilir(a, b.Value);
            }

            if ((asama == Asama.CevapSecme || asama == Asama.MiniOyun) && kim == b)
            {
                g.Soru = soru;
                g.Fisilti = fisilti;
            }

            if (asama == Asama.MiniOyun)
            {
                if (kim == b) g.Hamlen = bHamle;
                else if (kim == c) g.Hamlen = cHamle;
            }

            if (asama == Asama.Ifsa)
            {
                g.IfsaSoru = soru;
                g.IfsaFisilti = fisilti;
            }

            return g;
        }

        // ------------------------------------------------------------ İç akış

        void TurBaslat(OyuncuId yeniA)
        {
            tur++;
            if (ayar.TurSayisi > 0 && tur > ayar.TurSayisi)
            {
                tur = ayar.TurSayisi;
                Bitir();
                return;
            }

            a = yeniA;
            b = null;
            c = null;
            soru = null;
            fisilti = false;
            oneri = null;
            bHamle = null;
            cHamle = null;
            beraberlik = 0;
            sonMiniOyun = null;
            buTurMiniOyun = ayar.MiniOyun == MiniOyunTuru.Karisik
                ? KarisikHavuzu[rastgele.Next(KarisikHavuzu.Length)]
                : ayar.MiniOyun;

            if (ayar.Mod == OyunModu.Curcuna && havuz != null && !havuz.Bos)
            {
                atananSoru = havuz.Cek(rastgele);
                degistirmeHakki = true;
            }
            else
            {
                atananSoru = null;
                degistirmeHakki = false;
            }

            AsamayaGec(Asama.SoruSorma, ayar.SoruSuresiSn);
            Yay(new Olay { Tur = OlayTuru.TurBasladi, A = a });
        }

        void ToplamayiBitir()
        {
            // Yazmayanın eksiği öneri havuzundan tamamlanır.
            foreach (var o in oyuncular)
                for (int i = o.HavuzaEkledigi; i < KisiBasiHavuzSorusu && !oneriDestesi.Bos; i++)
                    toplanan.Add(oneriDestesi.Cek(rastgele));

            havuz = new Deste(toplanan);
            toplanan.Clear();
            TurBaslat(RastgeleOyuncu());
        }

        void MiniOyunuCoz()
        {
            var bh = bHamle.Value;
            var ch = cHamle.Value;
            switch (buTurMiniOyun)
            {
                case MiniOyunTuru.Tkm:
                    if (bh.Tkm == ch.Tkm) { Berabere(); return; }
                    Sonuclandir(Yener(ch.Tkm, bh.Tkm), false, false, 0, 0);
                    return;

                case MiniOyunTuru.TekCift:
                    bool toplamTek = (bh.Sayi + ch.Sayi) % 2 == 1;
                    Sonuclandir(toplamTek != bh.TekDiyor, false, false, 0, 0);
                    return;

                case MiniOyunTuru.Zar:
                    ZarAt(out int bz, out int cz);
                    Sonuclandir(cz > bz, false, false, bz, cz);
                    return;

                case MiniOyunTuru.Reaksiyon:
                    bool bErken = Erken(bHamle);
                    bool cErken = Erken(cHamle);
                    if (bErken == cErken && (bErken || bh.Sayi == ch.Sayi)) { Berabere(); return; }
                    bool cKazandi = bErken || (!cErken && ch.Sayi < bh.Sayi);
                    Sonuclandir(cKazandi, false, false, 0, 0);
                    return;
            }
        }

        void Berabere()
        {
            beraberlik++;
            Yay(new Olay { Tur = OlayTuru.Berabere, B = b, C = c });
            if (beraberlik >= BeraberlikSiniri)
            {
                ZarAt(out int bz, out int cz);
                Sonuclandir(cz > bz, false, true, bz, cz);
                return;
            }
            bHamle = null;
            cHamle = null;
            kalanMs = ayar.HamleSuresiSn * 1000.0;
        }

        void Sonuclandir(bool cKazandi, bool sureDoldu, bool zarlaKarar, int bZar, int cZar)
        {
            sonMiniOyun = new MiniOyunSonucu
            {
                Tur = buTurMiniOyun,
                BHamle = bHamle,
                CHamle = cHamle,
                BZar = bZar,
                CZar = cZar,
                ZarlaKarar = zarlaKarar,
                SureDoldu = sureDoldu,
                CKazandi = cKazandi,
            };

            var bo = Bul(b.Value);
            var co = Bul(c.Value);
            if (cKazandi)
            {
                co.IfsaEttirme++;
                co.MiniOyunKazanma++;
                AsamayaGec(Asama.Ifsa, ayar.SonucSuresiSn);
                Yay(new Olay { Tur = OlayTuru.Ifsa, A = a, B = b, C = c, Soru = soru });
            }
            else
            {
                bo.Saklama++;
                bo.MiniOyunKazanma++;
                soru = null; // güme giden soru hiçbir yerde kalmaz
                AsamayaGec(Asama.Gume, ayar.SonucSuresiSn);
                Yay(new Olay { Tur = OlayTuru.Gume, A = a, B = b, C = c });
            }
        }

        void Bitir()
        {
            soru = null;
            atananSoru = null;
            oneri = null;
            asama = Asama.OyunSonu;
            kalanMs = 0;
            Yay(new Olay { Tur = OlayTuru.OyunBitti });
        }

        // ------------------------------------------------------------ Yardımcılar

        void AsamayaGec(Asama yeni, int saniye)
        {
            asama = yeni;
            kalanMs = saniye * 1000.0;
        }

        void Yay(Olay olay) => OlayOldu?.Invoke(olay);

        OyuncuDurumu Bul(OyuncuId id) => oyuncular.Find(o => o.Id == id);

        List<OyuncuId> Secilebilir(OyuncuId? haric1 = null, OyuncuId? haric2 = null)
        {
            var liste = new List<OyuncuId>();
            foreach (var o in oyuncular)
                if (o.Id != haric1 && o.Id != haric2) liste.Add(o.Id);
            return liste;
        }

        OyuncuId RastgeleOyuncu(OyuncuId? haric1 = null, OyuncuId? haric2 = null)
        {
            var adaylar = Secilebilir(haric1, haric2);
            return adaylar[rastgele.Next(adaylar.Count)];
        }

        void ZarAt(out int bZar, out int cZar)
        {
            do
            {
                bZar = rastgele.Next(1, 7);
                cZar = rastgele.Next(1, 7);
            } while (bZar == cZar);
        }

        /// <summary>Reaksiyonda kırmızıyken (ya da insan dışı hızda) basıldı mı?</summary>
        static bool Erken(Hamle? h) =>
            h.HasValue && h.Value.Tur == MiniOyunTuru.Reaksiyon && h.Value.Sayi < ReaksiyonEnAzMs;

        static bool Yener(TkmSecim x, TkmSecim y) =>
            (x == TkmSecim.Tas && y == TkmSecim.Makas) ||
            (x == TkmSecim.Kagit && y == TkmSecim.Tas) ||
            (x == TkmSecim.Makas && y == TkmSecim.Kagit);

        static string SoruTemizle(string metin)
        {
            if (metin == null) return null;
            var t = metin.Trim();
            if (t.Length == 0 || t.Length > SoruEnFazlaKarakter) return null;
            return t;
        }

        List<OyuncuDurumu> OyuncuKopyasi()
        {
            var liste = new List<OyuncuDurumu>(oyuncular.Count);
            foreach (var o in oyuncular)
            {
                liste.Add(new OyuncuDurumu
                {
                    Id = o.Id,
                    Isim = o.Isim,
                    HavuzaEkledigi = o.HavuzaEkledigi,
                    Gosterilme = o.Gosterilme,
                    SoruAlma = o.SoruAlma,
                    SoruSorma = o.SoruSorma,
                    IfsaEttirme = o.IfsaEttirme,
                    Saklama = o.Saklama,
                    MiniOyunKazanma = o.MiniOyunKazanma,
                });
            }
            return liste;
        }
    }
}
