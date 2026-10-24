using System;
using System.Collections.Generic;
using System.Linq;
using Cakmak.Kurallar;
using NUnit.Framework;
using TheLighter.Akis;

namespace TheLighter.Tests
{
    /// <summary>
    /// Elden ele, altı oyunun hepsi baştan sona (tasks/g2-test.md 2): rastgele ayarlar ve rastgele ama geçerli hamlelerle
    /// yüzlerce oyun. Her adımda ekranın verdiği seçeneklerden biri seçilir, bazen süre doldurulur, bazen yanlış kişi
    /// basar (rol belli olmasın diye herkese açık düğmeler). Her oyun bitmeli; hiçbir adımda gizli bilgi ortaya düşmemeli
    /// (AGENTS.md "Gizlilik"); saat durması gereken ekranda saat işlememeli. Çakmak Gecesi'nde 39 özel rolün her biri en az
    /// bir oyunda çıkar (kurucunun öbürlerini kapatmasıyla). Gerçek kural motoruyla, sunucusuz.
    /// </summary>
    [TestFixture]
    public class TamOyunTestleri
    {
        const int EnFazlaAdim = 40000;

        /// <summary>
        /// Öneri havuzu: hepsi farklı ve uzun, oyun boyu tekrar etmesin (güme giden bir sorunun metni sonra başka bir
        /// turda ifşa olan soruyla karışmasın). Yazılan sorular da adım numarasıyla tek.
        /// </summary>
        static readonly string[] Sorular = Enumerable.Range(1, 400).Select(i => "Havuz sorusu " + i + ": bu grupta kim en çok gülüyor?").ToArray();

        static readonly HashSet<EkranTuru> HerkeseAcik = new HashSet<EkranTuru>
        {
            EkranTuru.Perde, EkranTuru.MiniOyunPerdesi, EkranTuru.MiniOyun, EkranTuru.Ifsa, EkranTuru.Gume, EkranTuru.Karar,
            EkranTuru.SuclamaTartisma, EkranTuru.Tetik, EkranTuru.CasusTahmini, EkranTuru.Bekleniyor,
        };

        static readonly HashSet<EkranTuru> KisiyeOzelEkranlar = new HashSet<EkranTuru>
        {
            EkranTuru.SoruSorma, EkranTuru.CevapSecme, EkranTuru.GorevGoster, EkranTuru.Oylama, EkranTuru.Suclama,
            EkranTuru.Linc, EkranTuru.Gece, EkranTuru.SoruToplama,
        };

        /// <summary>Bir oyunun gidişi: hangi ekranlar görüldü, hangi roller çıktı, güme giden sorular.</summary>
        sealed class Gidis
        {
            public readonly HashSet<EkranTuru> Ekranlar = new HashSet<EkranTuru>();
            public readonly HashSet<Rol> GorulenRoller = new HashSet<Rol>();
            public readonly HashSet<string> GumeGiden = new HashSet<string>();
            public string SonSoru;
            public int Adim;
        }

        // ------------------------------------------------------------ Testler

        [Test]
        public void Cakmak_HerModVeAyarla_OyunlarBiter() => Seri(OyunTuru.Cakmak, 24);

        [Test]
        public void Kostebek_OyunlarBiter() => Seri(OyunTuru.Kostebek, 14);

        [Test]
        public void RusRuleti_OyunlarBiter() => Seri(OyunTuru.RusRuleti, 14);

        [Test]
        public void TakimDuellosu_OyunlarBiter() => Seri(OyunTuru.Takim, 14);

        [Test]
        public void Casus_OyunlarBiter() => Seri(OyunTuru.Casus, 14);

        [Test]
        public void CakmakGecesi_OyunlarBiter_KazananVar()
        {
            var kazananlar = new Dictionary<GeceKazanan, int>();
            for (int i = 0; i < 18; i++)
            {
                var a = Kur(OyunTuru.CakmakGecesi, 7000 + i, out string etiket);
                Oyna(a, new Random(7000 + i), etiket);
                var k = a.Ekran.Ozet?.Gece?.Kazanan;
                Assert.IsTrue(k.HasValue, "Kazanan yok (oyun kendiliğinden bitmeli): " + etiket);
                kazananlar[k.Value] = kazananlar.TryGetValue(k.Value, out int n) ? n + 1 : 1;
            }
            TestContext.WriteLine("Çakmak Gecesi kazananları: " + string.Join(", ", kazananlar.Select(x => x.Key + "=" + x.Value)));
        }

        /// <summary>
        /// Her özel rol (Masum ve Katil hariç 39 rol) bir oyunda: kurucu öbürlerini kapatır, açık kalan rol önce dağıtılır,
        /// kesin çıkar (tasks/g2-test.md 2: "41 rolün hepsi en az bir oyunda görülsün"; Masum ve Katil her oyunda var).
        /// </summary>
        [Test]
        public void CakmakGecesi_HerRolBirOyunda()
        {
            var ozel = Oyun.GeceRolleri.Where(r => r != Rol.Masum && r != Rol.Katil).ToList();
            Assert.AreEqual(39, ozel.Count, "rol sayısı değişti: testi güncelle");
            int tohum = 9000;
            foreach (var rol in ozel)
            {
                tohum++;
                var ayar = new Ayarlar
                {
                    Oyun = OyunTuru.CakmakGecesi,
                    TekCihaz = true,
                    MiniOyun = MiniOyunTuru.Zar,
                    OneriSorulari = Sorular,
                    RolAcilir = tohum % 2 == 0,
                    KapaliRoller = ozel.Where(x => x != rol).ToList(),
                };
                int kisi = 13;
                Assert.AreEqual(Sonuc.Tamam, EldenEleAkisi.Kur(ayar, Kisiler(kisi), tohum, out var a), rol.ToString());
                var g = Oyna(a, new Random(tohum), "rol " + rol);
                var sonRoller = a.Ekran.Ozet?.Gece?.Roller?.Select(x => x.Rol) ?? Enumerable.Empty<Rol>();
                Assert.IsTrue(g.GorulenRoller.Contains(rol) || sonRoller.Contains(rol), rol + " hiç çıkmadı");
            }
        }

        // ------------------------------------------------------------ Kurulum

        static List<Oyuncu> Kisiler(int sayi)
        {
            var l = new List<Oyuncu>();
            for (int i = 0; i < sayi; i++) l.Add(new Oyuncu(new OyuncuId(i + 1), "Oyuncu" + (i + 1)));
            return l;
        }

        static void Seri(OyunTuru oyun, int adet)
        {
            for (int i = 0; i < adet; i++)
            {
                int tohum = 1000 * ((int)oyun + 1) + i;
                var a = Kur(oyun, tohum, out string etiket);
                Oyna(a, new Random(tohum), etiket);
            }
        }

        /// <summary>Bu oyun için geçerli rastgele ayarlar (Oyun.AyarlarUyumlu'ya uyan).</summary>
        static EldenEleAkisi Kur(OyunTuru oyun, int tohum, out string etiket)
        {
            var r = new Random(tohum);
            var ayar = new Ayarlar
            {
                Oyun = oyun,
                TekCihaz = true,
                OneriSorulari = Sorular,
                SesliCezalar = new[] { "Bir şarkı söyle", "Tavuk gibi bağır" },
                MiniOyun = (MiniOyunTuru)r.Next(Enum.GetValues(typeof(MiniOyunTuru)).Length),
                TurSayisi = 3 + r.Next(6),
            };
            int enAz = OyuncuSayisi.EnAz(oyun), enFazla = OyuncuSayisi.EnFazla(oyun);
            int kisi = enAz + r.Next(enFazla - enAz + 1);

            switch (oyun)
            {
                case OyunTuru.Cakmak:
                    ayar.Mod = (OyunModu)r.Next(3);
                    ayar.CezaSeviyesi = (CezaSeviyesi)r.Next(3);
                    if (ayar.Mod == OyunModu.Temiz && ayar.CezaSeviyesi == CezaSeviyesi.Cesur) ayar.CezaSeviyesi = CezaSeviyesi.Hafif;
                    ayar.GizliGorevler = r.Next(2) == 0;
                    ayar.KaosTurlari = r.Next(2) == 0;
                    break;
                case OyunTuru.Kostebek:
                    ayar.Mod = (OyunModu)r.Next(3);
                    ayar.CezaSeviyesi = r.Next(2) == 0 ? CezaSeviyesi.Kapali : CezaSeviyesi.Hafif;
                    break;
                case OyunTuru.RusRuleti:
                    ayar.Mod = r.Next(2) == 0 ? OyunModu.Normal : OyunModu.Temiz;
                    ayar.Can = 1 + r.Next(3);
                    break;
                case OyunTuru.Takim:
                    ayar.Mod = (OyunModu)r.Next(3);
                    ayar.KaosTurlari = r.Next(2) == 0;
                    break;
                case OyunTuru.Casus:
                    ayar.Mod = r.Next(2) == 0 ? OyunModu.Normal : OyunModu.Temiz;
                    ayar.Casus = r.Next(2) == 0 ? CasusTuru.Mekan : CasusTuru.Kelime;
                    break;
                case OyunTuru.CakmakGecesi:
                    ayar.RolAcilir = r.Next(2) == 0;
                    ayar.GecelikKurban = 1 + r.Next(kisi >= 11 ? 3 : kisi >= 8 ? 2 : 1);
                    var kapali = new List<Rol>();
                    foreach (var rol in Oyun.GeceRolleri)
                        if (rol != Rol.Masum && rol != Rol.Katil && r.Next(4) == 0) kapali.Add(rol);
                    ayar.KapaliRoller = kapali;
                    break;
            }
            Assert.IsTrue(Oyun.AyarlarUyumlu(ayar), "testin kurduğu ayar geçersiz: " + oyun);
            etiket = oyun + " tohum " + tohum + " " + kisi + " kişi, " + ayar.Mod + ", " + ayar.MiniOyun + ", ceza " + ayar.CezaSeviyesi
                     + (ayar.GizliGorevler ? ", görev" : "") + (ayar.KaosTurlari ? ", kaos" : "");
            Assert.AreEqual(Sonuc.Tamam, EldenEleAkisi.Kur(ayar, Kisiler(kisi), tohum, out var a), etiket);
            return a;
        }

        // ------------------------------------------------------------ Oynatma

        static Gidis Oyna(EldenEleAkisi a, Random r, string etiket)
        {
            var g = new Gidis();
            for (; g.Adim < EnFazlaAdim && !a.Bitti; g.Adim++)
            {
                var e = a.Ekran;
                g.Ekranlar.Add(e.Tur);
                Denetle(e, g, etiket);
                SaatDurmali(a, e, etiket);
                Adim(a, e, r, g, etiket);
            }
            Assert.IsTrue(a.Bitti, "Oyun " + EnFazlaAdim + " adımda bitmedi: " + etiket + ", son ekran " + a.Ekran.Tur
                                   + " / " + a.Ekran.Gorunum?.Asama);
            Assert.AreEqual(EkranTuru.OyunSonu, a.Ekran.Tur, etiket);
            var ozet = a.Ekran.Ozet;
            Assert.IsNotNull(ozet, "oyun sonunda özet yok: " + etiket);
            foreach (var k in ozet.IfsaOlanlar)
                if (k.Soru != null) Assert.IsFalse(g.GumeGiden.Contains(k.Soru), "Güme giden soru oyun sonu listesinde: " + etiket);
            return g;
        }

        static void Adim(EldenEleAkisi a, EkranDurumu e, Random r, Gidis g, string etiket)
        {
            var gr = e.Gorunum;
            string yer = " (" + e.Tur + ", adım " + g.Adim + ", " + etiket + ")";
            switch (e.Tur)
            {
                case EkranTuru.Perde:
                case EkranTuru.MiniOyunPerdesi:
                    if (e.SuresiDolanBydi && g.SonSoru != null) g.GumeGiden.Add(g.SonSoru);
                    a.PerdeyiKaldir();
                    break;

                case EkranTuru.GorevGoster:
                    if (gr.Rolun != Rol.Yok) g.GorulenRoller.Add(gr.Rolun);
                    a.GorevGordum();
                    break;

                case EkranTuru.SoruToplama:
                    if (r.Next(20) == 0) { a.Ilerle(TimeSpan.FromSeconds(a.Ayarlar.ToplamaSuresiSn + 1)); break; }
                    Assert.AreEqual(Sonuc.Tamam, a.HavuzaSoruEkle(Sorular[r.Next(Sorular.Length)] + " #" + g.Adim), yer);
                    break;

                case EkranTuru.SoruSorma:
                    YanEylemler(a, e, r);
                    if (a.Ekran.Anahtar != e.Anahtar || a.Bitti) break; // yan eylem ekranı değiştirdi (suçlama, casus bildi)
                    if (r.Next(14) == 0 || gr.Secilebilir.Count == 0) { a.Ilerle(TimeSpan.FromSeconds(a.Ayarlar.SoruSuresiSn + 1)); break; }
                    // Ekrandaki gibi: öneri ve fısıltı sadece soru yazılırken (atanan soruda ve Kör Soru'da yok).
                    bool yazili = gr.AtananSoru == null && gr.Kaos != KaosKurali.KorSoru;
                    if (r.Next(6) == 0 && yazili && a.Ayarlar.Mod != OyunModu.Curcuna) Assert.AreEqual(Sonuc.Tamam, a.OneriCek(), yer);
                    var b = gr.Secilebilir[r.Next(gr.Secilebilir.Count)];
                    if (r.Next(8) == 0 && yazili)
                    {
                        var f = a.Fisildadim(b);
                        if (f == Sonuc.Tamam) break;
                    }
                    Assert.AreEqual(Sonuc.Tamam, a.SoruGonder(b, "Yazılan soru " + g.Adim + ": en son ne zaman ağladın?"), yer);
                    break;

                case EkranTuru.CevapSecme:
                    g.SonSoru = gr.Soru;
                    if (gr.Rolun == Rol.Kiskirtici && r.Next(3) == 0) a.GunduzYetenegi();
                    if (a.Ekran.Anahtar != e.Anahtar) break;
                    if (r.Next(14) == 0 || gr.Secilebilir.Count == 0) { a.Ilerle(TimeSpan.FromSeconds(a.Ayarlar.CevapSuresiSn + 1)); break; }
                    Assert.AreEqual(Sonuc.Tamam, a.CevapSec(gr.Secilebilir[r.Next(gr.Secilebilir.Count)]), yer);
                    break;

                case EkranTuru.MiniOyun:
                    if (r.Next(15) == 0) { a.Ilerle(TimeSpan.FromSeconds(a.Ayarlar.HamleSuresiSn + 1)); break; }
                    HamleYap(a, e, r, yer);
                    break;

                case EkranTuru.Ifsa:
                case EkranTuru.Gume:
                    if (e.Tur == EkranTuru.Gume && g.SonSoru != null) g.GumeGiden.Add(g.SonSoru);
                    if (e.Tur == EkranTuru.Ifsa && gr.IfsaSoru != null)
                        Assert.IsFalse(g.GumeGiden.Contains(gr.IfsaSoru), "Güme giden soru ifşa oldu" + yer);
                    if (e.CezaOnayBekliyor) a.CezalariOnayla();
                    else a.Ilerle(TimeSpan.FromSeconds(1));
                    break;

                case EkranTuru.Oylama:
                    Assert.AreEqual(Sonuc.Tamam, a.Oy(r.Next(2) == 0), yer);
                    break;

                case EkranTuru.Karar:
                    switch (r.Next(4))
                    {
                        case 0: a.Ilerle(TimeSpan.FromSeconds(a.Ayarlar.KararSuresiSn + 1)); break;
                        case 1: a.KarariGec(); break;
                        default:
                            var s = gr.Asama == Asama.Bedel ? a.BedelOde() : a.IkiyeKatla();
                            if (s != Sonuc.Tamam) a.KarariGec();
                            break;
                    }
                    break;

                case EkranTuru.SuclamaTartisma:
                    a.TartismayiBitir();
                    break;

                case EkranTuru.Suclama:
                    Assert.AreEqual(Sonuc.Tamam, BirineDene(a.SuclamaOyu, Adaylar(gr, e.Kime), r), "suçlama oyu verilemedi" + yer);
                    break;

                case EkranTuru.Tetik:
                    if (r.Next(10) == 0) { a.Ilerle(TimeSpan.FromSeconds(30)); break; }
                    var ben = gr.Oyuncular.FirstOrDefault(o => o.Id == gr.TetikCeken);
                    if (ben != null && ben.DevretmeHakki && r.Next(3) == 0 && BirineDene(a.TetikDevret, Adaylar(gr, gr.TetikCeken), r) == Sonuc.Tamam)
                        break;
                    Assert.AreEqual(Sonuc.Tamam, a.TetikCek(), yer);
                    break;

                case EkranTuru.CasusTahmini:
                    if (r.Next(4) == 0) { a.Ilerle(TimeSpan.FromSeconds(Oyun.CasusTahminiSn + 1)); break; }
                    Assert.AreEqual(Sonuc.Tamam, a.CasusTahmini(e.Kime.Value, gr.CasusListesi[r.Next(gr.CasusListesi.Count)]), yer);
                    break;

                case EkranTuru.Linc:
                    if (gr.Rolun == Rol.Suikastci && gr.KalanHakkin > 0 && r.Next(3) == 0)
                    {
                        var hedef = Adaylar(gr, e.Kime);
                        if (hedef.Count > 0) a.Suikast(hedef[r.Next(hedef.Count)], Oyun.GeceRolleri[r.Next(Oyun.GeceRolleri.Count)]);
                        if (a.Ekran.Anahtar != e.Anahtar || a.Bitti) break;
                    }
                    Assert.Greater(gr.Secilebilir.Count, 0, "linçte seçilecek kimse yok" + yer);
                    Assert.AreEqual(Sonuc.Tamam, a.LincOyu(gr.Secilebilir[r.Next(gr.Secilebilir.Count)]), yer);
                    break;

                case EkranTuru.Gece:
                {
                    OyuncuId? hedef = gr.Secilebilir.Count > 0 && r.Next(5) != 0 ? gr.Secilebilir[r.Next(gr.Secilebilir.Count)] : (OyuncuId?)null;
                    OyuncuId? ikinci = gr.IkinciSecilebilir.Count > 0 && r.Next(2) == 0
                        ? gr.IkinciSecilebilir[r.Next(gr.IkinciSecilebilir.Count)] : (OyuncuId?)null;
                    bool? ifsa = gr.Rolun == Rol.Hakem && hedef.HasValue ? r.Next(2) == 0 : (bool?)null;
                    var s = a.GeceSecimi(hedef, ikinci, ifsa);
                    if (s != Sonuc.Tamam) Assert.AreEqual(Sonuc.Tamam, a.GeceSecimi(null), "pas bile geçmedi" + yer);
                    break;
                }

                default:
                    a.Ilerle(TimeSpan.FromSeconds(1));
                    break;
            }
        }

        /// <summary>
        /// Soru sırasında herkese açık düğmeler: Köstebek'te erken suçlama, Casus'ta "Mekânı biliyorum" (casus olmayan basınca
        /// hiçbir şey olmaz), Çakmak Gecesi'nde gündüz yeteneği (rolü uymayanda kural motoru reddeder).
        /// </summary>
        static void YanEylemler(EldenEleAkisi a, EkranDurumu e, Random r)
        {
            var gr = e.Gorunum;
            if (a.SuclamaHakkiOlanlar.Count > 0 && r.Next(25) == 0)
                Assert.AreEqual(Sonuc.Tamam, a.Sucla(a.SuclamaHakkiOlanlar[r.Next(a.SuclamaHakkiOlanlar.Count)]), "erken suçlama");
            else if (gr.Oyun == OyunTuru.Casus && gr.CasusListesi.Count > 0 && r.Next(20) == 0)
            {
                var s = a.CasusTahmini(e.Kime.Value, gr.CasusListesi[r.Next(gr.CasusListesi.Count)]);
                if (gr.Rolun == Rol.Casus) Assert.AreEqual(Sonuc.Tamam, s, "casusun tahmini");
                else Assert.AreEqual(Sonuc.SiraSendeDegil, s, "casus olmayan: hiçbir şey olmamalı");
            }
            else if (gr.Oyun == OyunTuru.CakmakGecesi && r.Next(3) == 0 && gr.Secilebilir.Count > 0)
                a.GunduzYetenegi(gr.Secilebilir[r.Next(gr.Secilebilir.Count)]);
        }

        static void HamleYap(EldenEleAkisi a, EkranDurumu e, Random r, string yer)
        {
            var g = e.BGorunumu;
            var b = e.BGorunumu.Sen;
            var c = e.CGorunumu.Sen;
            Sonuc s;
            switch (EldenEleAkisi.SiradakiAdim(g))
            {
                case MiniOyunAdimi.BSeciyor:
                    s = a.Hamle(b, GizliHamle(g.MiniOyun, r, true));
                    break;
                case MiniOyunAdimi.CSeciyor:
                    s = a.Hamle(c, GizliHamle(g.MiniOyun, r, false));
                    break;
                case MiniOyunAdimi.IkisiDeAtiyor:
                    s = a.Hamle(g.BHamleYapti ? c : b, Hamle.ZarAt());
                    break;
                case MiniOyunAdimi.Reaksiyon:
                    s = a.Hamle(g.BHamleYapti ? c : b, Hamle.Reaksiyon(r.Next(-50, 600)));
                    break;
                default:
                    a.Ilerle(TimeSpan.FromSeconds(1));
                    return;
            }
            Assert.AreEqual(Sonuc.Tamam, s, "hamle" + yer);
        }

        static Hamle GizliHamle(MiniOyunTuru tur, Random r, bool b) =>
            tur == MiniOyunTuru.TekCift ? Hamle.TekCift(r.Next(1, 6), b && r.Next(2) == 0) : Hamle.TasKagitMakas((TkmSecim)r.Next(3));

        /// <summary>Kendisi hariç oyunda kalan (elenmemiş, çıkmamış) herkes; ekranın seçenekleri varsa onlar.</summary>
        static List<OyuncuId> Adaylar(Gorunum g, OyuncuId? ben)
        {
            if (g.Secilebilir.Count > 0) return g.Secilebilir.ToList();
            return g.Oyuncular.Where(o => o.Id != ben && !o.Elendi && !o.Cikti).Select(o => o.Id).ToList();
        }

        /// <summary>Adayları karışık sırayla dener, ilk Tamam'da durur.</summary>
        static Sonuc BirineDene(Func<OyuncuId, Sonuc> eylem, List<OyuncuId> adaylar, Random r)
        {
            var s = Sonuc.Secilemez;
            foreach (var id in adaylar.OrderBy(_ => r.Next()))
            {
                s = eylem(id);
                if (s == Sonuc.Tamam) return s;
            }
            return s;
        }

        // ------------------------------------------------------------ Denetimler

        /// <summary>Gizlilik (AGENTS.md): ortadaki ekranda kimsenin gizli bilgisi yok, kişiye özel ekran sahibinin görünümüyle.</summary>
        static void Denetle(EkranDurumu e, Gidis g, string etiket)
        {
            string yer = " (" + e.Tur + ", adım " + g.Adim + ", " + etiket + ")";
            if (KisiyeOzelEkranlar.Contains(e.Tur))
            {
                Assert.IsTrue(e.Kime.HasValue, "kişiye özel ekranda telefon kimde belli değil" + yer);
                Assert.AreEqual(e.Kime.Value, e.Gorunum.Sen, "kişiye özel ekran başkasının görünümüyle" + yer);
            }
            foreach (var gr in new[] { e.Gorunum, e.BGorunumu, e.CGorunumu })
            {
                if (gr == null) continue;
                if (gr.Soru != null)
                {
                    Assert.AreEqual(EkranTuru.CevapSecme, e.Tur, "soru metni yanlış ekranda" + yer);
                    Assert.AreEqual(e.Kime.Value, gr.Sen, "soru metni B'den başkasında" + yer);
                }
                if (gr.Hamlen.HasValue) Assert.Fail("gizli hamle ekran modelinde" + yer);
                if (gr.IfsaSoru != null && g.GumeGiden.Contains(gr.IfsaSoru)) Assert.Fail("güme giden soru yine göründü" + yer);
                if (!HerkeseAcik.Contains(e.Tur)) continue;
                Assert.IsNull(gr.Gorevin, "ortada gizli görev" + yer);
                Assert.AreEqual(Rol.Yok, gr.Rolun, "ortada rol" + yer);
                Assert.IsNull(gr.Sirrin, "ortada Casus'un sırrı" + yer);
                Assert.IsNull(gr.MekanRolun, "ortada mekândaki rol" + yer);
                Assert.IsEmpty(gr.Katiller, "ortada katil listesi" + yer);
                Assert.IsEmpty(gr.KatilSecimleri, "ortada katil oyları" + yer);
                Assert.IsEmpty(gr.Bulgularin, "ortada bulgular" + yer);
                Assert.IsNull(gr.KulakSorusu, "ortada Kulak'ın duyduğu soru" + yer);
                Assert.IsNull(gr.CellatHedefin, "ortada Cellat'ın hedefi" + yer);
                Assert.IsEmpty(gr.Bombalilar, "ortada bombalılar" + yer);
                Assert.IsNull(gr.Oneri, "ortada öneri" + yer);
            }
        }

        /// <summary>Saatin durması gereken ekranda (perde, tartışma, gizli oylar) zaman geçse de kalan süre değişmez.</summary>
        static void SaatDurmali(EldenEleAkisi a, EkranDurumu e, string etiket)
        {
            if (!e.SaatDurur || e.Gorunum == null) return;
            int kalan = e.Gorunum.KalanMs;
            string anahtar = e.Anahtar;
            a.Ilerle(TimeSpan.FromSeconds(7));
            Assert.AreEqual(anahtar, a.Ekran.Anahtar, "saat durması gereken ekran kendiliğinden değişti (" + e.Tur + ", " + etiket + ")");
            Assert.AreEqual(kalan, a.Ekran.Gorunum.KalanMs, "saat durması gereken ekranda saat işledi (" + e.Tur + ", " + etiket + ")");
        }
    }
}
