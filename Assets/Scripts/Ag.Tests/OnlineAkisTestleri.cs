using System;
using System.Collections.Generic;
using System.Linq;
using Cakmak.Kurallar;
using NUnit.Framework;
using TheLighter.Ag;
using TheLighter.Akis;

namespace TheLighter.Testler
{
    /// <summary>
    /// Online akış: sunucunun durum'undan bu cihazın ekranını doğru seçiyor mu, komutları protokol biçiminde
    /// ve doğru turla yolluyor mu. Kural hesaplamadığını da gösterir: her şey durum'dan.
    /// </summary>
    public class OnlineAkisTestleri
    {
        sealed class Sahte : ITasima
        {
            public readonly Queue<TasimaOlayi> Gelen = new Queue<TasimaOlayi>();
            public readonly List<string> Giden = new List<string>();
            bool acik;
            public void Ac(Uri adres) { }
            public bool Gonder(string json) { if (!acik) return false; Giden.Add(json); return true; }
            public void Kapat() { acik = false; }
            public bool Al(out TasimaOlayi o)
            {
                if (Gelen.Count > 0) { o = Gelen.Dequeue(); return true; }
                o = default;
                return false;
            }
            public void Acil() { acik = true; Gelen.Enqueue(new TasimaOlayi { Tur = TasimaOlayTuru.Acildi }); }
            public void Mesaj(string j) => Gelen.Enqueue(new TasimaOlayi { Tur = TasimaOlayTuru.Mesaj, Metin = j });
        }

        Sahte tasima;
        OnlineIstemci istemci;
        OnlineAkis akis;
        int surum;

        static readonly OyuncuId P1 = new OyuncuId(1), P2 = new OyuncuId(2), P3 = new OyuncuId(3), P4 = new OyuncuId(4);

        /// <summary>Bu cihaz p2, kurucu p1.</summary>
        [SetUp]
        public void Kur()
        {
            tasima = new Sahte();
            istemci = new OnlineIstemci(() => tasima, null);
            istemci.Katil(new Uri("ws://localhost:5000/ws"), "KXTPM", "Ayşe");
            tasima.Acil();
            tasima.Mesaj("{\"t\":\"hosgeldin\",\"sen\":\"p2\",\"anahtar\":\"k\",\"oda\":\"KXTPM\"}");
            Kare();
            surum = 0;
            akis = new OnlineAkis(istemci, new Ayarlar());
        }

        void Kare(double sn = 0)
        {
            istemci.Guncelle(TimeSpan.FromSeconds(sn));
            akis?.Ilerle(TimeSpan.FromSeconds(sn));
        }

        static string Oyuncu(string id, string isim, int renk, string ek = "") =>
            "{\"id\":\"" + id + "\",\"isim\":\"" + isim + "\",\"renk\":" + renk + ",\"bagli\":true,\"bedelHakki\":2" + ek + "}";

        /// <summary>Oyun durumu: alanlar sunucunun yazdığı biçimde, değişenler <paramref name="ek"/> ile.</summary>
        void Durum(string asama, string ek = "", int tur = 3, string oyuncuEki = "", int kalanMs = 10000)
        {
            surum++;
            string json = "{\"t\":\"durum\",\"surum\":" + surum + ",\"oda\":\"KXTPM\",\"asama\":\"" + asama + "\",\"tur\":" + tur +
                ",\"toplamTur\":15,\"kalanMs\":" + kalanMs + ",\"kurucu\":\"p1\"," +
                "\"oyuncular\":[" + Oyuncu("p1", "Enes", 0) + "," + Oyuncu("p2", "Ayşe", 1, oyuncuEki) + "," +
                Oyuncu("p3", "Can", 2) + "," + Oyuncu("p4", "Deniz", 3) + "]," +
                "\"miniOyun\":\"tkm\",\"kaos\":\"yok\",\"sana\":{}" + ek + "}";
            tasima.Mesaj(json);
            Kare();
        }

        JsonDeger SonGiden => JsonDeger.Coz(tasima.Giden.Last());

        [Test]
        public void Lobide_OyunEkraniYok()
        {
            tasima.Mesaj("{\"t\":\"durum\",\"surum\":1,\"oda\":\"KXTPM\",\"asama\":\"lobi\",\"kurucu\":\"p1\",\"oyuncular\":[],\"ayarlar\":{}}");
            Kare();
            Assert.That(akis.Ekran.Tur, Is.EqualTo(EkranTuru.Bekleniyor));
        }

        [Test]
        public void SoruSorma_SiraBendeyse_SoruEkrani_DegilseKimiBekledigim()
        {
            Durum("soruSorma", ",\"a\":\"p2\",\"secilebilir\":[\"p1\",\"p3\",\"p4\"]");
            Assert.That(akis.Ekran.Tur, Is.EqualTo(EkranTuru.SoruSorma));
            Assert.That(akis.Ekran.Gorunum.Sen, Is.EqualTo(P2));
            Assert.That(akis.Ekran.Gorunum.Secilebilir.Count, Is.EqualTo(3));

            Durum("soruSorma", ",\"a\":\"p3\"", tur: 4);
            Assert.That(akis.Ekran.Tur, Is.EqualTo(EkranTuru.Bekleniyor));
            Assert.That(akis.Ekran.Kime, Is.EqualTo(P3));
        }

        [Test]
        public void SoranGizli_AYoksa_Bekleniyor_KimeYok()
        {
            Durum("soruSorma", ",\"kaos\":\"soranGizli\"");
            Assert.That(akis.Ekran.Tur, Is.EqualTo(EkranTuru.Bekleniyor));
            Assert.That(akis.Ekran.Kime, Is.Null);
        }

        [Test]
        public void CevapSecme_BBensem_SoruBende()
        {
            Durum("cevapSecme", ",\"a\":\"p1\",\"b\":\"p2\",\"secilebilir\":[\"p3\",\"p4\"],\"sana\":{\"soru\":\"Kim?\"}");
            Assert.That(akis.Ekran.Tur, Is.EqualTo(EkranTuru.CevapSecme));
            Assert.That(akis.Ekran.Gorunum.Soru, Is.EqualTo("Kim?"));
        }

        [Test]
        public void MiniOyun_OynayanaKendiYarisi_HamledenSonraAnahtarDegisir_SeyirciBekler()
        {
            Durum("miniOyun", ",\"a\":\"p1\",\"b\":\"p2\",\"c\":\"p3\",\"bHamleYapti\":false");
            Assert.That(akis.Ekran.Tur, Is.EqualTo(EkranTuru.MiniOyun));
            Assert.That(akis.Ekran.BGorunumu, Is.Not.Null);
            Assert.That(akis.Ekran.CGorunumu, Is.Null);
            string once = akis.Ekran.Anahtar;

            Durum("miniOyun", ",\"a\":\"p1\",\"b\":\"p2\",\"c\":\"p3\",\"bHamleYapti\":true");
            Assert.That(akis.Ekran.Anahtar, Is.Not.EqualTo(once), "hamleden sonra 'rakip bekleniyor' çizilir");

            Durum("miniOyun", ",\"a\":\"p2\",\"b\":\"p3\",\"c\":\"p4\"", tur: 4);
            Assert.That(akis.Ekran.Tur, Is.EqualTo(EkranTuru.Bekleniyor));
        }

        [Test]
        public void Oylama_OyVerecekOlana_OyVerinceBekler()
        {
            Durum("oylama", ",\"a\":\"p1\",\"b\":\"p3\",\"oylayanlar\":[\"p2\",\"p4\"],\"kaos\":\"grupKarari\"");
            Assert.That(akis.Ekran.Tur, Is.EqualTo(EkranTuru.Oylama));
            Durum("oylama", ",\"a\":\"p1\",\"b\":\"p3\",\"oylayanlar\":[\"p2\",\"p4\"],\"oyKullanan\":1,\"sana\":{\"oyVerdin\":true}");
            Assert.That(akis.Ekran.Tur, Is.EqualTo(EkranTuru.Bekleniyor));
            Durum("oylama", ",\"a\":\"p2\",\"b\":\"p3\",\"oylayanlar\":[\"p1\",\"p4\"]", tur: 5);
            Assert.That(akis.Ekran.Tur, Is.EqualTo(EkranTuru.Bekleniyor), "A oy vermez");
        }

        [Test]
        public void BedelVeIkiyeKatla_KararEkrani_KararVerenKime()
        {
            Durum("bedel", ",\"a\":\"p1\",\"b\":\"p2\",\"c\":\"p3\",\"kararVeren\":\"p2\"");
            Assert.That(akis.Ekran.Tur, Is.EqualTo(EkranTuru.Karar));
            Assert.That(akis.Ekran.Kime, Is.EqualTo(P2));
            Durum("ikiyeKatla", ",\"a\":\"p1\",\"b\":\"p3\",\"c\":\"p4\",\"kararVeren\":\"p4\"", tur: 4);
            Assert.That(akis.Ekran.Tur, Is.EqualTo(EkranTuru.Karar));
            Assert.That(akis.Ekran.Kime, Is.EqualTo(P4));
        }

        [Test]
        public void IfsaGume_CezaOnayiBeklenmez()
        {
            Durum("ifsa", ",\"a\":\"p1\",\"b\":\"p2\",\"c\":\"p3\",\"ifsa\":{\"soru\":\"Kim?\"},\"turCezalari\":[{\"kim\":\"p2\",\"ceza\":{\"tur\":\"sesli\",\"metin\":\"x\"},\"neden\":\"kaybetti\"}]");
            Assert.That(akis.Ekran.Tur, Is.EqualTo(EkranTuru.Ifsa));
            Assert.That(akis.Ekran.CezaOnayBekliyor, Is.False);
            Assert.That(akis.Ekran.SaatDurur, Is.False);
            Assert.That(akis.Ekran.Gorunum.IfsaSoru, Is.EqualTo("Kim?"));
            Durum("gume", ",\"a\":\"p1\",\"b\":\"p2\",\"c\":\"p3\",\"bedelOdendi\":true", tur: 4);
            Assert.That(akis.Ekran.Tur, Is.EqualTo(EkranTuru.Gume));
            Assert.That(akis.Ekran.Gorunum.BedelOdendi, Is.True);
        }

        [Test]
        public void OyunSonu_OzetVeIfsaListesiOzettenGelir()
        {
            Durum("oyunSonu", ",\"ozet\":{\"oynananTur\":15,\"unvanlar\":[{\"tur\":\"miknatis\",\"kim\":\"p3\",\"deger\":6}]," +
                "\"gorevler\":[],\"ifsaOlanlar\":[{\"soru\":\"Kim?\",\"a\":\"p1\",\"b\":\"p2\",\"c\":\"p4\"}],\"curcunaSorulari\":[]}");
            Assert.That(akis.Ekran.Tur, Is.EqualTo(EkranTuru.OyunSonu));
            Assert.That(akis.Bitti, Is.True);
            Assert.That(akis.Ekran.Ozet.Unvanlar.Single().Kim, Is.EqualTo(P3));
            var satir = akis.Ekran.IfsaOlanlar.Single();
            Assert.That(satir.Soru, Is.EqualTo("Kim?"));
            Assert.That(satir.Soran, Is.EqualTo(P1));
            Assert.That(satir.Cevap, Is.EqualTo(P4));
        }

        [Test]
        public void Gorev_OnceBirKezGosterilir_GordumdenSonraOyun()
        {
            Durum("soruSorma", ",\"a\":\"p2\",\"sana\":{\"gorevin\":{\"tur\":\"soruSor\",\"hedef\":null,\"adet\":2,\"zorluk\":\"kolay\"}}");
            Assert.That(akis.Ekran.Tur, Is.EqualTo(EkranTuru.GorevGoster));
            Assert.That(akis.Ekran.Gorunum.Gorevin.Tur, Is.EqualTo(GorevTuru.SoruSor));
            akis.GorevGordum();
            Assert.That(akis.Ekran.Tur, Is.EqualTo(EkranTuru.SoruSorma));
            Durum("soruSorma", ",\"a\":\"p2\",\"sana\":{\"gorevin\":{\"tur\":\"soruSor\",\"adet\":2,\"zorluk\":\"kolay\"}}", tur: 4);
            Assert.That(akis.Ekran.Tur, Is.EqualTo(EkranTuru.SoruSorma), "bir kez");
        }

        [Test]
        public void SoruToplama_IkiSoruyaKadarYazar_SonraBekler()
        {
            Durum("soruToplama", "", tur: 0, oyuncuEki: ",\"havuzaEkledigi\":0");
            Assert.That(akis.Ekran.Tur, Is.EqualTo(EkranTuru.SoruToplama));
            string once = akis.Ekran.Anahtar;
            Durum("soruToplama", "", tur: 0, oyuncuEki: ",\"havuzaEkledigi\":1");
            Assert.That(akis.Ekran.Tur, Is.EqualTo(EkranTuru.SoruToplama));
            Assert.That(akis.Ekran.Anahtar, Is.Not.EqualTo(once), "ikinci soru için kutu boşalır");
            Durum("soruToplama", "", tur: 0, oyuncuEki: ",\"havuzaEkledigi\":2");
            Assert.That(akis.Ekran.Tur, Is.EqualTo(EkranTuru.Bekleniyor));
        }

        [Test]
        public void GeriSayim_IstemciYurutur_YeniDurumdaSifirlanir_BeklemedeDurur()
        {
            Durum("soruSorma", ",\"a\":\"p3\"", kalanMs: 10000);
            Kare(2.5);
            Assert.That(akis.Ekran.Gorunum.KalanMs, Is.EqualTo(7500).Within(20));
            Kare(20);
            Assert.That(akis.Ekran.Gorunum.KalanMs, Is.EqualTo(0));

            Durum("cevapSecme", ",\"a\":\"p3\",\"b\":\"p4\"", kalanMs: 20000);
            Assert.That(akis.Ekran.Gorunum.KalanMs, Is.EqualTo(20000));

            Durum("cevapSecme", ",\"a\":\"p3\",\"b\":\"p4\",\"bekleme\":{\"kalanMs\":95000}", kalanMs: 15000);
            Kare(5);
            Assert.That(akis.Ekran.Gorunum.KalanMs, Is.EqualTo(15000), "oyun durdu: aşama sayacı işlemez");
            Assert.That(akis.BeklemeKalanMs, Is.EqualTo(90000).Within(20));
        }

        [Test]
        public void Komutlar_ProtokolBiciminde_SonDurumunTuruyla()
        {
            Durum("soruSorma", ",\"a\":\"p2\"", tur: 7);
            Assert.That(akis.SoruGonder(P3, "Kim en çok uyur?"), Is.EqualTo(Sonuc.Tamam));
            Assert.That(SonGiden["t"].Metin, Is.EqualTo("komut"));
            Assert.That(SonGiden["komut"].Metin, Is.EqualTo("soruSor"));
            Assert.That(SonGiden["tur"].Tamsayi, Is.EqualTo(7));
            Assert.That(SonGiden["hedef"].Metin, Is.EqualTo("p3"));

            akis.OneriCek();
            Assert.That(SonGiden["komut"].Metin, Is.EqualTo("oneriCek"));
            akis.Hamle(P2, Hamle.TasKagitMakas(TkmSecim.Kagit));
            Assert.That(SonGiden["hamle"]["secim"].Metin, Is.EqualTo("kagit"));
            akis.Oy(true);
            Assert.That(SonGiden["ifsa"].Mantik, Is.True);
            akis.BedelOde();
            Assert.That(SonGiden["komut"].Metin, Is.EqualTo("bedelOde"));
            akis.Buradayim();
            Assert.That(SonGiden["t"].Metin, Is.EqualTo("buradayim"));
        }

        [Test]
        public void KurucuDegilken_BitirVeTekrarOynaGitmez_FisiltiYok()
        {
            Durum("soruSorma", ",\"a\":\"p2\"");
            int n = tasima.Giden.Count;
            Assert.That(akis.Kurucuyum, Is.False);
            Assert.That(akis.OyunuBitir(), Is.Not.EqualTo(Sonuc.Tamam));
            Assert.That(akis.TekrarOyna(), Is.Not.EqualTo(Sonuc.Tamam));
            Assert.That(akis.Fisildadim(P3), Is.Not.EqualTo(Sonuc.Tamam));
            Assert.That(tasima.Giden.Count, Is.EqualTo(n));
        }

        [Test]
        public void OyundanCiktiysam_SiraBendeOlsaBileIzlerim()
        {
            Durum("soruSorma", ",\"a\":\"p2\"", oyuncuEki: ",\"cikti\":true");
            Assert.That(akis.BenCiktim, Is.True);
            Assert.That(akis.Ekran.Tur, Is.EqualTo(EkranTuru.Bekleniyor));
        }

        [Test]
        public void AfkBilgisi_VeIsimRenkDurumdan()
        {
            Durum("soruSorma", ",\"a\":\"p3\"", oyuncuEki: ",\"afk\":true");
            Assert.That(akis.BenAfkyim, Is.True);
            Assert.That(akis.Isim(P4), Is.EqualTo("Deniz"));
            Assert.That(akis.Sira(P3), Is.EqualTo(2));
            Assert.That(akis.Oyuncular.Count, Is.EqualTo(4));
        }

        [Test]
        public void BeklemeEkrani_HerYeniDurumdaYenidenCizilir()
        {
            Durum("soruSorma", ",\"a\":\"p3\"");
            string once = akis.Ekran.Anahtar;
            Durum("soruSorma", ",\"a\":\"p3\"");
            Assert.That(akis.Ekran.Anahtar, Is.Not.EqualTo(once));
        }
    }
}
