using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using static Cakmak.Kurallar.Tests.Kurulum;

namespace Cakmak.Kurallar.Tests
{
    /// <summary>Rus Ruleti (docs/Oyunlar.md, 2).</summary>
    public class RusRuletiTestleri
    {
        static Oyun YeniRR(int kisi = 4, int tohum = 1, int can = 1, Action<Ayarlar> ayarla = null) =>
            Yeni(kisi, tohum, a =>
            {
                a.Oyun = OyunTuru.RusRuleti;
                a.Can = can;
                ayarla?.Invoke(a);
            });

        static List<OyuncuId> Oynayanlar(Oyun oyun)
        {
            var liste = new List<OyuncuId>();
            foreach (var o in Durum(oyun).Oyuncular)
                if (!o.Elendi && !o.Cikti) liste.Add(o.Id);
            return liste;
        }

        static OyuncuDurumu Oyuncu(Oyun oyun, OyuncuId id)
        {
            foreach (var o in Durum(oyun).Oyuncular)
                if (o.Id == id) return o;
            return null;
        }

        /// <summary>Bulunduğu aşamada bir adım: A ilk seçeneğe sorar, B ilkini seçer, C kazanır, tetiği çeken çeker.</summary>
        static void Adim(Oyun oyun)
        {
            var g = Durum(oyun);
            switch (g.Asama)
            {
                case Asama.SoruSorma:
                    var a = g.A.Value;
                    Assert.That(oyun.SoruSor(a, oyun.GorunumAl(a).Secilebilir[0], "Kim?"), Is.EqualTo(Sonuc.Tamam));
                    break;
                case Asama.CevapSecme:
                    Assert.That(oyun.CevapSec(g.B.Value, oyun.GorunumAl(g.B.Value).Secilebilir[0]), Is.EqualTo(Sonuc.Tamam));
                    break;
                case Asama.MiniOyun:
                    TkmOyna(oyun, g.B.Value, g.C.Value, cKazansin: true);
                    break;
                case Asama.Ifsa:
                case Asama.Gume:
                    Saniye(oyun, 7);
                    break;
                case Asama.Tetik:
                    Assert.That(oyun.TetikCek(g.TetikCeken.Value), Is.EqualTo(Sonuc.Tamam));
                    break;
                default:
                    Assert.Fail("beklenmeyen aşama " + g.Asama);
                    break;
            }
        }

        /// <summary>Tetik aşamasına kadar oynar.</summary>
        static void TetigeKadar(Oyun oyun)
        {
            for (int i = 0; i < 20 && Durum(oyun).Asama != Asama.Tetik; i++) Adim(oyun);
            Assert.That(Durum(oyun).Asama, Is.EqualTo(Asama.Tetik));
        }

        // ---------------------------------------------------- Kurulum

        [Test]
        public void Herkesin_cani_ve_devretme_hakki_var_tur_sayisi_yok()
        {
            var oyun = YeniRR(5, can: 3);
            var g = Durum(oyun);
            Assert.That(g.Oyun, Is.EqualTo(OyunTuru.RusRuleti));
            Assert.That(g.ToplamTur, Is.EqualTo(0), "son kalana kadar sürer");
            Assert.That(g.Isi, Is.Not.Null, "tabancanın ısısı");
            Assert.That(g.Final, Is.False);
            foreach (var o in g.Oyuncular)
            {
                Assert.That(o.Can, Is.EqualTo(3));
                Assert.That(o.DevretmeHakki, Is.True);
                Assert.That(o.Elendi, Is.False);
            }
        }

        [Test]
        public void Uyumsuz_ayarlar_kurulamaz()
        {
            Action<Ayarlar>[] uyumsuz =
            {
                a => a.CezaSeviyesi = CezaSeviyesi.Hafif,
                a => a.GizliGorevler = true,
                a => a.KaosTurlari = true,
                a => a.Can = 0,
                a => a.Can = Oyun.EnFazlaCan + 1,
            };
            foreach (var ayarla in uyumsuz)
            {
                var ayar = new Ayarlar { Oyun = OyunTuru.RusRuleti };
                ayarla(ayar);
                Assert.That(Oyun.AyarlarUyumlu(ayar), Is.False);
                Assert.That(Oyun.Kur(ayar, Oyuncular(4), 1, out _), Is.EqualTo(Sonuc.GecersizGirdi));
            }
            Assert.That(Oyun.AyarlarUyumlu(new Ayarlar { Oyun = OyunTuru.RusRuleti, Mod = OyunModu.Temiz, Can = 3 }), Is.True);
            Assert.That(Oyun.AyarlarUyumlu(new Ayarlar { Can = 0, KaosTurlari = true }), Is.True, "Çakmak'ta can ayarı yok sayılır");
        }

        [Test]
        public void Cakmak_oyununda_tetik_yok()
        {
            var oyun = Yeni();
            Assert.That(oyun.TetikCek(P(1)), Is.EqualTo(Sonuc.YanlisAsama));
            Assert.That(oyun.TetikDevret(P(1), P(2)), Is.EqualTo(Sonuc.YanlisAsama));
            Assert.That(Durum(oyun).Isi, Is.Null);
            foreach (var o in Durum(oyun).Oyuncular) Assert.That(o.Can, Is.EqualTo(0));
        }

        // ---------------------------------------------------- Tetik

        [Test]
        public void Kaybeden_sonuc_ekranindan_sonra_tetige_gecer_sonra_C_sorar()
        {
            var oyun = YeniRR(5, can: 3);
            var a = SiradakiA(oyun);
            var b = oyun.GorunumAl(a).Secilebilir[0];
            Assert.That(oyun.SoruSor(a, b, "Kim?"), Is.EqualTo(Sonuc.Tamam));
            var c = oyun.GorunumAl(b).Secilebilir[0];
            Assert.That(oyun.CevapSec(b, c), Is.EqualTo(Sonuc.Tamam));
            TkmOyna(oyun, b, c, cKazansin: true);
            Assert.That(Durum(oyun).Asama, Is.EqualTo(Asama.Ifsa), "önce sorunun sonucu");

            Saniye(oyun, 7);
            var g = Durum(oyun);
            Assert.That(g.Asama, Is.EqualTo(Asama.Tetik));
            Assert.That(g.TetikCeken, Is.EqualTo(b), "düelloyu kaybeden");
            Assert.That(g.SonMiniOyun, Is.Not.Null, "kim kaybetti görünsün");
            Assert.That(oyun.TetikCek(c), Is.EqualTo(Sonuc.SiraSendeDegil));

            var olaylar = new List<Olay>();
            oyun.OlayOldu += olaylar.Add;
            Assert.That(oyun.TetikCek(b), Is.EqualTo(Sonuc.Tamam));
            Assert.That(olaylar.Exists(o => (o.Tur == OlayTuru.TetikBos || o.Tur == OlayTuru.TetikPatladi) && o.Kim == b), Is.True);
            g = Durum(oyun);
            Assert.That(g.Asama, Is.EqualTo(Asama.SoruSorma));
            Assert.That(g.A, Is.EqualTo(c));
            Assert.That(g.Tur, Is.EqualTo(2));
        }

        [Test]
        public void En_fazla_alti_tetikte_bir_patlar_patlamadan_once_isi_hep_kizgin()
        {
            int patlama = 0;
            var araliklar = new List<int>(); // ilk patlamadan sonraki boş tetik sayıları
            for (int tohum = 1; tohum <= 20; tohum++)
            {
                bool patladiMi = false;
                var oyun = YeniRR(6, tohum, can: 3);
                var olaylar = new List<Olay>();
                oyun.OlayOldu += olaylar.Add;
                int arka = 0;
                for (int adim = 0; adim < 2000 && Durum(oyun).Asama != Asama.OyunSonu; adim++)
                {
                    if (Durum(oyun).Asama != Asama.Tetik) { Adim(oyun); continue; }
                    var isi = Durum(oyun).Isi;
                    olaylar.Clear();
                    Adim(oyun);
                    if (olaylar.Exists(o => o.Tur == OlayTuru.TetikPatladi))
                    {
                        Assert.That(isi, Is.EqualTo(IsiSeviyesi.Kizgin), "patlamadan önce ısı kızgın olmalı");
                        Assert.That(arka, Is.LessThan(Oyun.HazneSayisi), "6 haznede bir mermi");
                        if (patladiMi) araliklar.Add(arka);
                        patladiMi = true;
                        arka = 0;
                        patlama++;
                    }
                    else arka++;
                }
                Assert.That(Durum(oyun).Asama, Is.EqualTo(Asama.OyunSonu), "tohum " + tohum + ": oyun bitmedi");
            }
            Assert.That(patlama, Is.GreaterThan(0));
            Assert.That(araliklar.Exists(x => x > 0), Is.True, "patlayınca tabanca yeniden dolar (yoksa hep patlardı)");
        }

        [Test]
        public void Devret_hakki_bir_kez_devralan_da_devredebilir()
        {
            var oyun = YeniRR(5, can: 3);
            TetigeKadar(oyun);
            var x = Durum(oyun).TetikCeken.Value;
            var y = Oynayanlar(oyun).Find(id => id != x);
            Assert.That(oyun.TetikDevret(x, x), Is.EqualTo(Sonuc.Secilemez));
            Saniye(oyun, 5);
            Assert.That(oyun.TetikDevret(x, y), Is.EqualTo(Sonuc.Tamam));
            var g = Durum(oyun);
            Assert.That(g.TetikCeken, Is.EqualTo(y));
            Assert.That(g.KalanMs, Is.EqualTo(10000), "devralanın karar süresi baştan");
            Assert.That(Oyuncu(oyun, x).DevretmeHakki, Is.False);
            Assert.That(oyun.TetikDevret(x, y), Is.EqualTo(Sonuc.SiraSendeDegil), "artık tetik onda değil");

            Assert.That(oyun.TetikDevret(y, x), Is.EqualTo(Sonuc.Tamam), "devralanın kendi hakkı var");
            Assert.That(oyun.TetikDevret(x, y), Is.EqualTo(Sonuc.HakKalmadi));
            var olaylar = new List<Olay>();
            oyun.OlayOldu += olaylar.Add;
            Assert.That(oyun.TetikCek(x), Is.EqualTo(Sonuc.Tamam));
            Assert.That(olaylar.Exists(o => o.Kim == x && (o.Tur == OlayTuru.TetikBos || o.Tur == OlayTuru.TetikPatladi)), Is.True);
        }

        [Test]
        public void Karar_suresi_dolunca_tetigi_kendisi_ceker()
        {
            var oyun = YeniRR(5, can: 3);
            TetigeKadar(oyun);
            var x = Durum(oyun).TetikCeken.Value;
            var olaylar = new List<Olay>();
            oyun.OlayOldu += olaylar.Add;
            Saniye(oyun, 11);
            Assert.That(olaylar.Exists(o => o.Kim == x && (o.Tur == OlayTuru.TetikBos || o.Tur == OlayTuru.TetikPatladi)), Is.True);
            Assert.That(Durum(oyun).Asama, Is.Not.EqualTo(Asama.Tetik));
        }

        [Test]
        public void Tetik_cekecek_oyundan_cikarsa_cekilmez_oyun_surer()
        {
            var oyun = YeniRR(5, can: 3);
            TetigeKadar(oyun);
            var x = Durum(oyun).TetikCeken.Value;
            var olaylar = new List<Olay>();
            oyun.OlayOldu += olaylar.Add;
            Assert.That(oyun.OyuncuCikti(x), Is.EqualTo(Sonuc.Tamam));
            Assert.That(olaylar.Exists(o => o.Tur == OlayTuru.TetikBos || o.Tur == OlayTuru.TetikPatladi), Is.False);
            Assert.That(Durum(oyun).Asama, Is.EqualTo(Asama.SoruSorma));
        }

        [Test]
        public void Kaybeden_sonuc_ekraninda_cikarsa_tetik_cekilmez()
        {
            var oyun = YeniRR(5, can: 3);
            for (int i = 0; i < 10 && Durum(oyun).Asama != Asama.Ifsa; i++) Adim(oyun);
            var kaybeden = Durum(oyun).B.Value; // Adim'da C kazanır
            var olaylar = new List<Olay>();
            oyun.OlayOldu += olaylar.Add;
            Assert.That(oyun.OyuncuCikti(kaybeden), Is.EqualTo(Sonuc.Tamam));
            Saniye(oyun, 7);
            Assert.That(Durum(oyun).Asama, Is.EqualTo(Asama.SoruSorma));
            Assert.That(olaylar.Exists(o => o.Tur == OlayTuru.TetikBos || o.Tur == OlayTuru.TetikPatladi), Is.False);
        }

        // ---------------------------------------------------- Eleme, final, kazanma

        [Test]
        public void Herkes_kopuk_olsa_da_elenen_secilmez()
        {
            var oyun = YeniRR(5);
            for (int i = 0; i < 2000 && Durum(oyun).Oyuncular.Count(o => o.Elendi) == 0; i++) Adim(oyun);
            for (int i = 0; i < 20 && Durum(oyun).Asama != Asama.SoruSorma; i++) Adim(oyun);
            var g = Durum(oyun);
            Assert.That(g.Asama, Is.EqualTo(Asama.SoruSorma));
            var a = g.A.Value;
            foreach (var o in g.Oyuncular)
                if (o.Id != a && !o.Elendi) Assert.That(oyun.OyuncuKoptu(o.Id), Is.EqualTo(Sonuc.Tamam));
            foreach (var id in oyun.GorunumAl(a).Secilebilir)
                Assert.That(Oyuncu(oyun, id).Elendi, Is.False, "aktif kimse yokken de elenen seçilmez");
        }

        [Test]
        public void Tam_oyun_elenen_bir_daha_oynamaz_uc_kisi_kalinca_final_son_kalan_kazanir()
        {
            for (int tohum = 1; tohum <= 30; tohum++)
            {
                int kisi = 4 + tohum % 4;
                var oyun = YeniRR(kisi, tohum, can: 1 + tohum % 3, ayarla: a => a.TurSayisi = 2);
                var olaylar = new List<Olay>();
                oyun.OlayOldu += olaylar.Add;
                bool finaldeydi = false;
                for (int adim = 0; adim < 3000 && Durum(oyun).Asama != Asama.OyunSonu; adim++)
                {
                    var g = Durum(oyun);
                    var kalan = Oynayanlar(oyun);
                    string t = $"tohum {tohum}, adım {adim}, {g.Asama}";
                    foreach (var o in g.Oyuncular)
                        if (o.Elendi)
                        {
                            Assert.That(o.Can, Is.EqualTo(0), t);
                            Assert.That(g.B == o.Id || g.C == o.Id || g.TetikCeken == o.Id, Is.False, t + ": elenen oynuyor");
                            Assert.That(g.Asama == Asama.SoruSorma && g.A == o.Id, Is.False, t + ": elenen soruyor");
                        }
                    if (kalan.Count <= Oyun.FinalSiniri)
                        Assert.That(g.Asama == Asama.SoruSorma || g.Asama == Asama.CevapSecme || g.Asama == Asama.Ifsa, Is.False,
                            t + ": 3 kişi kaldı, çakmak turu olmaz");
                    if (g.Final)
                    {
                        finaldeydi = true;
                        Assert.That(kalan.Count, Is.LessThanOrEqualTo(Oyun.FinalSiniri), t);
                    }
                    Adim(oyun);
                }

                var ozet = oyun.Ozet();
                Assert.That(Durum(oyun).Asama, Is.EqualTo(Asama.OyunSonu), $"tohum {tohum}: bitmedi (tur sayısı yok sayılmalı)");
                var son = Oynayanlar(oyun);
                Assert.That(son.Count, Is.EqualTo(1), "tohum " + tohum);
                Assert.That(ozet.RusRuleti.Kazanan, Is.EqualTo(son[0]));
                Assert.That(ozet.RusRuleti.ElenmeSirasi.Count, Is.EqualTo(kisi - 1));
                Assert.That(finaldeydi, Is.True, "tohum " + tohum + ": final hiç olmadı");
                Assert.That(olaylar.FindAll(o => o.Tur == OlayTuru.FinalBasladi).Count, Is.EqualTo(1));
                Assert.That(olaylar.FindAll(o => o.Tur == OlayTuru.Elendi).Count, Is.EqualTo(kisi - 1));
            }
        }

        [Test]
        public void Final_duellosunda_soru_yok_kaybeden_dogrudan_tetige()
        {
            var oyun = YeniRR(4);
            for (int i = 0; i < 2000 && !Durum(oyun).Final; i++) Adim(oyun);
            var g = Durum(oyun);
            Assert.That(g.Final, Is.True);
            Assert.That(g.Asama, Is.EqualTo(Asama.MiniOyun));
            Assert.That(g.B, Is.Not.EqualTo(g.C));
            Assert.That(g.A, Is.EqualTo(g.B));
            Assert.That(oyun.GorunumAl(g.B.Value).Soru, Is.Null);
            TkmOyna(oyun, g.B.Value, g.C.Value, cKazansin: true);
            g = Durum(oyun);
            Assert.That(g.Asama, Is.EqualTo(Asama.Tetik), "sonuç ekranı yok");
        }

        [Test]
        public void Erken_biterse_en_cok_cani_olan_kazanir_esitlikte_kimse()
        {
            var esit = YeniRR(4, can: 3);
            Assert.That(esit.OyunuBitir(), Is.EqualTo(Sonuc.Tamam));
            Assert.That(esit.Ozet().RusRuleti.Kazanan, Is.Null, "herkesin 3 canı var");

            // Tek bir kişinin canı en çok olduğu ana kadar oyna.
            for (int tohum = 1; tohum <= 50; tohum++)
            {
                var oyun = YeniRR(4, tohum, can: 3);
                for (int adim = 0; adim < 3000 && Durum(oyun).Asama != Asama.OyunSonu; adim++)
                {
                    Adim(oyun);
                    var canlar = new List<int>();
                    OyuncuId enCok = default;
                    int max = -1;
                    foreach (var id in Oynayanlar(oyun))
                    {
                        int can = Oyuncu(oyun, id).Can;
                        canlar.Add(can);
                        if (can > max) { max = can; enCok = id; }
                    }
                    if (canlar.FindAll(x => x == max).Count != 1 || Durum(oyun).Asama == Asama.OyunSonu) continue;

                    Assert.That(oyun.OyunuBitir(), Is.EqualTo(Sonuc.Tamam));
                    Assert.That(oyun.Ozet().RusRuleti.Kazanan, Is.EqualTo(enCok));
                    return;
                }
            }
            Assert.Fail("tek bir en çok canlı hiç oluşmadı");
        }

        [Test]
        public void Tetik_beklerken_bitirilirse_ifsa_olan_soru_grup_paketine_kalir()
        {
            var oyun = YeniRR(4, ayarla: a => a.Mod = OyunModu.Curcuna);
            for (int i = 1; i <= 4; i++)
                for (int j = 1; j <= Oyun.KisiBasiHavuzSorusu; j++)
                    Assert.That(oyun.HavuzaSoruEkle(P(i), $"Soru {i}-{j}?"), Is.EqualTo(Sonuc.Tamam));
            var a = SiradakiA(oyun);
            var b = oyun.GorunumAl(a).Secilebilir[0];
            Assert.That(oyun.SoruSor(a, b, null), Is.EqualTo(Sonuc.Tamam));
            var soru = oyun.GorunumAl(b).Soru;
            TetigeKadar(oyun);
            Assert.That(oyun.OyunuBitir(), Is.EqualTo(Sonuc.Tamam));
            Assert.That(oyun.Ozet().CurcunaSorulari, Has.Member(soru), "ifşa olan soru güme sayılmasın");
        }

        [Test]
        public void Curcunada_ifsa_olan_sorular_tetikten_sonra_da_grup_paketine_kalir()
        {
            var oyun = YeniRR(4, ayarla: a => a.Mod = OyunModu.Curcuna);
            for (int i = 1; i <= 4; i++)
                for (int j = 1; j <= Oyun.KisiBasiHavuzSorusu; j++)
                    Assert.That(oyun.HavuzaSoruEkle(P(i), $"Soru {i}-{j}?"), Is.EqualTo(Sonuc.Tamam));
            var ifsalar = new List<string>();
            oyun.OlayOldu += o => { if (o.Tur == OlayTuru.Ifsa && o.Soru != null) ifsalar.Add(o.Soru); };
            for (int adim = 0; adim < 3000 && Durum(oyun).Asama != Asama.OyunSonu; adim++)
            {
                var g = Durum(oyun);
                if (g.Asama == Asama.SoruSorma)
                    Assert.That(oyun.SoruSor(g.A.Value, oyun.GorunumAl(g.A.Value).Secilebilir[0], null), Is.EqualTo(Sonuc.Tamam));
                else Adim(oyun);
            }
            Assert.That(ifsalar, Is.Not.Empty);
            foreach (var s in ifsalar) Assert.That(oyun.Ozet().CurcunaSorulari, Has.Member(s));
        }
    }
}
