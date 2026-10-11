using System;
using System.Collections.Generic;
using NUnit.Framework;
using static Cakmak.Kurallar.Tests.Kurulum;
using static Cakmak.Kurallar.Tests.TekrarYardim;

namespace Cakmak.Kurallar.Tests
{
    /// <summary>
    /// Bütün görev ve unvan türleri. Rastgele ayarlarla rastgele oyunlar oynar, olanları kural kodundan
    /// bağımsız olarak dışarıdan (komutlar, görünümler, olaylar) kendisi kaydeder; görev sonuçlarını ve
    /// unvanları bu kayıttan yeniden hesaplayıp özetle karşılaştırır.
    /// </summary>
    public class GorevUnvanTestleri
    {
        const int OyunSayisi = 600;

        /// <summary>Bir oyuncu hakkında testin kendi tuttuğu kayıt.</summary>
        sealed class Kayit
        {
            public readonly Dictionary<OyuncuId, int> Sordugu = new Dictionary<OyuncuId, int>();
            public readonly Dictionary<OyuncuId, int> SoranKisi = new Dictionary<OyuncuId, int>();
            public readonly Dictionary<OyuncuId, int> Gosterdigi = new Dictionary<OyuncuId, int>();
            public readonly Dictionary<OyuncuId, int> GosterenKisi = new Dictionary<OyuncuId, int>();
            public readonly Dictionary<OyuncuId, int> Yendigi = new Dictionary<OyuncuId, int>();
            public readonly Dictionary<OyuncuId, int> Yenildigi = new Dictionary<OyuncuId, int>();
            public readonly Dictionary<OyuncuId, int> IfsaEttirdigi = new Dictionary<OyuncuId, int>();
            public int Intikam, BIkenIfsa, CIkenGume, Seri, EnUzunSeri, Beraberlik, SureDoldu, Katlama, KizginVerdi;
        }

        [Test]
        public void Gorev_sonuclari_ve_unvanlar_disaridan_tutulan_kayitla_ayni()
        {
            var gorevGoruldu = new Dictionary<GorevTuru, (bool basarili, bool basarisiz)>();
            var unvanGoruldu = new HashSet<UnvanTuru>();

            for (int tohum = 1; tohum <= OyunSayisi; tohum++)
            {
                var r = new Random(tohum);
                var oyun = RastgeleOyun(r, tohum, out var cezaAcik);
                var kayit = new Dictionary<OyuncuId, Kayit>();
                foreach (var o in Durum(oyun).Oyuncular) kayit[o.Id] = new Kayit();
                oyun.OlayOldu += olay =>
                {
                    if (olay.Tur != OlayTuru.Berabere) return;
                    kayit[olay.B.Value].Beraberlik++;
                    kayit[olay.C.Value].Beraberlik++;
                };

                Oyna(oyun, r, kayit);
                var ozet = oyun.Ozet();
                var durumlar = new Dictionary<OyuncuId, OyuncuDurumu>();
                foreach (var o in ozet.Oyuncular) durumlar[o.Id] = o;

                foreach (var s in ozet.Gorevler)
                {
                    bool beklenen = Basarili(s.Gorev, durumlar[s.Kim], kayit[s.Kim]);
                    Assert.That(s.Basarili, Is.EqualTo(beklenen), $"tohum {tohum}, {s.Kim}: {s.Gorev.Tur} (hedef {s.Gorev.Hedef}, adet {s.Gorev.Adet})");
                    if (!cezaAcik) Assert.That(s.Gorev.Tur, Is.Not.EqualTo(GorevTuru.BedelOde).And.Not.EqualTo(GorevTuru.HicCezaCekme), "ceza kapalıyken verilmez");
                    gorevGoruldu.TryGetValue(s.Gorev.Tur, out var g);
                    gorevGoruldu[s.Gorev.Tur] = (g.basarili || s.Basarili, g.basarisiz || !s.Basarili);
                }

                Assert.That(ozet.Unvanlar.Count, Is.LessThanOrEqualTo(3));
                var buOyundaki = new HashSet<UnvanTuru>();
                foreach (var u in ozet.Unvanlar)
                {
                    Assert.That(buOyundaki.Add(u.Tur), Is.True, $"tohum {tohum}: aynı unvan iki kez");
                    unvanGoruldu.Add(u.Tur);
                    if (u.Tur == UnvanTuru.Kacak) Assert.That(cezaAcik, Is.True, "ceza kapalıyken Kaçak verilmez");
                    if (u.Tur == UnvanTuru.Gorunmez)
                    {
                        foreach (var o in ozet.Oyuncular)
                            Assert.That(o.Gosterilme == 0, Is.EqualTo(o.Id == u.Kim), $"tohum {tohum}: Görünmez tek gösterilmeyen");
                        continue;
                    }
                    Assert.That(u.Deger, Is.GreaterThan(0), u.Tur.ToString());
                    foreach (var o in ozet.Oyuncular)
                    {
                        int d = Deger(u.Tur, o, kayit[o.Id]);
                        if (o.Id == u.Kim) Assert.That(d, Is.EqualTo(u.Deger), $"tohum {tohum}, {u.Tur}: değer");
                        else Assert.That(d, Is.LessThan(u.Deger), $"tohum {tohum}, {u.Tur}: tek sahibi olmalı");
                    }
                }
            }

            foreach (GorevTuru t in Enum.GetValues(typeof(GorevTuru)))
            {
                Assert.That(gorevGoruldu.ContainsKey(t), Is.True, t + " hiç verilmedi");
                Assert.That(gorevGoruldu[t].basarili, Is.True, t + " hiç başarılmadı");
                Assert.That(gorevGoruldu[t].basarisiz, Is.True, t + " hiç kaçırılmadı");
            }
            foreach (UnvanTuru t in Enum.GetValues(typeof(UnvanTuru)))
                Assert.That(unvanGoruldu, Has.Member(t), t + " hiç verilmedi");
        }

        [Test]
        public void Plandaki_icerik_hedefi_20_gorev_30_unvan()
        {
            Assert.That(Enum.GetValues(typeof(GorevTuru)).Length, Is.GreaterThanOrEqualTo(20));
            Assert.That(Enum.GetValues(typeof(UnvanTuru)).Length, Is.GreaterThanOrEqualTo(30));
        }

        // ---------------------------------------------------- Oyun

        static Oyun RastgeleOyun(Random r, int tohum, out bool cezaAcik)
        {
            var mod = (OyunModu)r.Next(3);
            var ceza = (CezaSeviyesi)r.Next(mod == OyunModu.Temiz ? 2 : 3);
            cezaAcik = ceza != CezaSeviyesi.Kapali;
            var miniOyunlar = new[] { MiniOyunTuru.Tkm, MiniOyunTuru.TekCift, MiniOyunTuru.Zar, MiniOyunTuru.Karisik };
            var miniOyun = miniOyunlar[r.Next(miniOyunlar.Length)];
            bool kaos = r.Next(3) != 0;
            int turSayisi = 8 + r.Next(13);
            return Yeni(kisi: 4 + r.Next(7), tohum: tohum, ayarla: x =>
            {
                x.Mod = mod;
                x.MiniOyun = miniOyun;
                x.TurSayisi = turSayisi;
                x.CezaSeviyesi = ceza;
                x.GizliGorevler = true;
                x.KaosTurlari = kaos;
                x.OneriSorulari = Havuz;
                x.SesliCezalar = Sesli;
            });
        }

        /// <summary>Oyunu sonuna kadar rastgele oynar, olanları kayda geçirir. Süreler varsayılan ayarlardan.</summary>
        static void Oyna(Oyun oyun, Random r, Dictionary<OyuncuId, Kayit> k)
        {
            OyuncuId? oncekiB = null, oncekiC = null;
            while (Durum(oyun).Asama != Asama.OyunSonu)
            {
                if (Durum(oyun).Asama == Asama.SoruToplama) { Saniye(oyun, 90.1); continue; }
                Assert.That(Durum(oyun).Asama, Is.EqualTo(Asama.SoruSorma));

                var a = GercekA(oyun);
                if (r.Next(20) == 0)
                {
                    k[a].SureDoldu++;
                    oncekiB = oncekiC = null;
                    Saniye(oyun, 60.1);
                    continue;
                }
                var b = Sec(r, oyun.GorunumAl(a).Secilebilir);
                Assert.That(oyun.SoruSor(a, b, "Soru?"), Is.EqualTo(Sonuc.Tamam));
                Artir(k[a].Sordugu, b);
                Artir(k[b].SoranKisi, a);
                if (a == oncekiC && b == oncekiB) k[a].Intikam++;

                if (r.Next(20) == 0)
                {
                    k[b].SureDoldu++;
                    oncekiB = oncekiC = null;
                    Saniye(oyun, 20.1);
                    continue;
                }
                var bGorunum = oyun.GorunumAl(b);
                var c = Sec(r, bGorunum.Secilebilir);
                if (bGorunum.Isi == IsiSeviyesi.Kizgin) k[b].KizginVerdi++;
                Assert.That(oyun.CevapSec(b, c), Is.EqualTo(Sonuc.Tamam));
                Artir(k[b].Gosterdigi, c);
                Artir(k[c].GosterenKisi, b);

                while (true)
                {
                    var g = Durum(oyun);
                    if (g.Asama == Asama.MiniOyun) MiniOyunEli(oyun, r, b, c, k);
                    else if (g.Asama == Asama.Oylama)
                    {
                        foreach (var v in g.Oylayanlar)
                            if (r.Next(4) != 0) oyun.Oy(v, r.Next(2) == 0);
                        if (Durum(oyun).Asama == Asama.Oylama) Saniye(oyun, 10.1);
                    }
                    else if (g.Asama == Asama.IkiyeKatla || g.Asama == Asama.Bedel)
                    {
                        var kim = g.KararVeren.Value;
                        int secim = r.Next(3);
                        if (secim == 0)
                        {
                            if (g.Asama == Asama.IkiyeKatla) { oyun.IkiyeKatla(kim); k[kim].Katlama++; }
                            else oyun.BedelOde(kim);
                        }
                        else if (secim == 1) oyun.KarariGec(kim);
                        else Saniye(oyun, 5.1);
                    }
                    else break;
                }

                var son = Durum(oyun);
                Assert.That(son.Asama, Is.EqualTo(Asama.Ifsa).Or.EqualTo(Asama.Gume));
                if (son.SonMiniOyun != null)
                {
                    // Sayılan el: İkiye Katla kabul edildiyse ikinci el, edilmediyse ilk el.
                    var kazanan = son.SonMiniOyun.CKazandi ? c : b;
                    var kaybeden = son.SonMiniOyun.CKazandi ? b : c;
                    k[kazanan].Seri++;
                    k[kazanan].EnUzunSeri = Math.Max(k[kazanan].EnUzunSeri, k[kazanan].Seri);
                    k[kaybeden].Seri = 0;
                    Artir(k[kazanan].Yendigi, kaybeden);
                    Artir(k[kaybeden].Yenildigi, kazanan);
                }
                if (son.Asama == Asama.Ifsa)
                {
                    k[b].BIkenIfsa++;
                    Artir(k[c].IfsaEttirdigi, b);
                }
                else k[c].CIkenGume++;
                oncekiB = b;
                oncekiC = c;
                Saniye(oyun, 6.1);
            }
        }

        /// <summary>Bir el: ikisi de oynar (beraberlik olabilir) ya da biri/ikisi oynamaz, süre dolar.</summary>
        static void MiniOyunEli(Oyun oyun, Random r, OyuncuId b, OyuncuId c, Dictionary<OyuncuId, Kayit> k)
        {
            var tur = Durum(oyun).MiniOyun;
            bool bOynar = r.Next(15) != 0;
            bool cOynar = r.Next(15) != 0;
            if (bOynar) Assert.That(oyun.MiniOyunHamlesi(b, RastgeleHamle(r, tur)), Is.EqualTo(Sonuc.Tamam));
            if (cOynar) Assert.That(oyun.MiniOyunHamlesi(c, RastgeleHamle(r, tur)), Is.EqualTo(Sonuc.Tamam));
            if (bOynar && cOynar) return;
            if (!bOynar) k[b].SureDoldu++;
            if (!cOynar) k[c].SureDoldu++;
            Saniye(oyun, 10.1);
        }

        static Hamle RastgeleHamle(Random r, MiniOyunTuru tur)
        {
            switch (tur)
            {
                case MiniOyunTuru.Tkm: return Hamle.TasKagitMakas((TkmSecim)r.Next(3));
                case MiniOyunTuru.TekCift: return Hamle.TekCift(1 + r.Next(5), r.Next(2) == 0);
                default: return Hamle.ZarAt();
            }
        }

        static OyuncuId Sec(Random r, IReadOnlyList<OyuncuId> liste) => liste[r.Next(liste.Count)];

        // ---------------------------------------------------- Beklenen değerler (testin kaydından)

        static bool Basarili(Gorev g, OyuncuDurumu o, Kayit k)
        {
            switch (g.Tur)
            {
                case GorevTuru.KisiyiGoster: return Sayi(k.Gosterdigi, g.Hedef.Value) >= g.Adet;
                case GorevTuru.FarkliKisiGoster: return k.Gosterdigi.Count >= g.Adet;
                case GorevTuru.MiniOyunKazan: return o.MiniOyunKazanma >= g.Adet;
                case GorevTuru.HicIfsaOlma: return o.SoruAlma >= 1 && k.BIkenIfsa == 0;
                case GorevTuru.AyniKisiyeSor: return EnBuyuk(k.Sordugu) >= g.Adet;
                case GorevTuru.SeniGostersin: return Sayi(k.GosterenKisi, g.Hedef.Value) >= g.Adet;
                case GorevTuru.HicGosterilme: return o.Gosterilme == 0;
                case GorevTuru.KaosTurundaKazan: return o.KaosKazanma >= 1;
                case GorevTuru.KisiyeSor: return Sayi(k.Sordugu, g.Hedef.Value) >= g.Adet;
                case GorevTuru.SoruSor: return o.SoruSorma >= g.Adet;
                case GorevTuru.IfsaEttir: return o.IfsaEttirme >= g.Adet;
                case GorevTuru.GumeyeGonder: return o.Saklama >= g.Adet;
                case GorevTuru.FarkliKisiyeSor: return k.Sordugu.Count >= g.Adet;
                case GorevTuru.IntikamAl: return k.Intikam >= g.Adet;
                case GorevTuru.SirriniAc: return Sayi(k.IfsaEttirdigi, g.Hedef.Value) >= g.Adet;
                case GorevTuru.BedelOde: return o.BedelOdeme >= g.Adet;
                case GorevTuru.UstUsteKazan: return k.EnUzunSeri >= g.Adet;
                case GorevTuru.HedefiYen: return Sayi(k.Yendigi, g.Hedef.Value) >= g.Adet;
                case GorevTuru.HicCezaCekme: return o.OynadigiMiniOyun >= g.Adet && o.CezaCekme == 0;
                case GorevTuru.CokGosterilme: return o.Gosterilme >= g.Adet;
                default: throw new AssertionException("testte karşılığı yok: " + g.Tur);
            }
        }

        static int Deger(UnvanTuru t, OyuncuDurumu o, Kayit k)
        {
            switch (t)
            {
                case UnvanTuru.Miknatis: return o.Gosterilme;
                case UnvanTuru.Gammaz: return o.IfsaEttirme;
                case UnvanTuru.SirKupu: return o.Saklama;
                case UnvanTuru.HedefTahtasi: return o.SoruAlma;
                case UnvanTuru.SoruMakinesi: return o.SoruSorma;
                case UnvanTuru.Sansli: return o.MiniOyunKazanma;
                case UnvanTuru.Takintili: return EnBuyuk(k.Sordugu);
                case UnvanTuru.Intikamci: return k.Intikam;
                case UnvanTuru.AtesleOynayan: return o.Yanma;
                case UnvanTuru.Bedelci: return o.BedelOdeme;
                case UnvanTuru.Cezakes: return o.CezaCekme;
                case UnvanTuru.KaosCanavari: return o.KaosKazanma;
                case UnvanTuru.Kale: return k.BIkenIfsa == 0 ? o.SoruAlma : 0;
                case UnvanTuru.Talihsiz: return o.MiniOyunKazanma == 0 ? o.OynadigiMiniOyun : 0;
                case UnvanTuru.SosyalKelebek: return k.Gosterdigi.Count;
                case UnvanTuru.Sadik: return EnBuyuk(k.Gosterdigi);
                case UnvanTuru.Dedikoducu: return k.Sordugu.Count;
                case UnvanTuru.Gundem: return k.SoranKisi.Count;
                case UnvanTuru.Yenilmez: return k.EnUzunSeri;
                case UnvanTuru.Zorba: return EnBuyuk(k.Yendigi);
                case UnvanTuru.Av: return EnBuyuk(k.Yenildigi);
                case UnvanTuru.Ayna: return k.Beraberlik;
                case UnvanTuru.Uykucu: return k.SureDoldu;
                case UnvanTuru.Kacak: return o.CezaCekme == 0 ? o.OynadigiMiniOyun : 0;
                case UnvanTuru.AcikKitap: return k.BIkenIfsa;
                case UnvanTuru.EliBos: return k.CIkenGume;
                case UnvanTuru.Kumarbaz: return k.Katlama;
                case UnvanTuru.Sogukkanli: return k.KizginVerdi;
                case UnvanTuru.Bumerang:
                    int toplam = 0;
                    foreach (var x in k.Gosterdigi) toplam += Math.Min(x.Value, Sayi(k.GosterenKisi, x.Key));
                    return toplam;
                default: throw new AssertionException("testte karşılığı yok: " + t);
            }
        }

        static void Artir(Dictionary<OyuncuId, int> d, OyuncuId kim) => d[kim] = Sayi(d, kim) + 1;

        static int Sayi(Dictionary<OyuncuId, int> d, OyuncuId kim) => d.TryGetValue(kim, out var n) ? n : 0;

        static int EnBuyuk(Dictionary<OyuncuId, int> d)
        {
            int enBuyuk = 0;
            foreach (var n in d.Values) if (n > enBuyuk) enBuyuk = n;
            return enBuyuk;
        }
    }
}
