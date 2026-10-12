using Cakmak.Kurallar;
using TheLighter.Akis;
using UnityEngine.UIElements;
using static TheLighter.Istemci.Arayuz;

namespace TheLighter.Istemci
{
    /// <summary>
    /// Herkesin baktığı sonuç ekranı: ifşa ya da güme. Süre bitince kural motoru kendiliğinden yeni tura geçer.
    /// Bu turda ceza çekildiyse önce çark döner, kartlar açılır; "Tamam" denene kadar saat durur (akış).
    /// </summary>
    sealed class SonucParcasi : IOyunParcasi
    {
        readonly Label sure;

        public VisualElement Kok { get; }

        public SonucParcasi(IOyunAkisi akis, EkranDurumu e)
        {
            var g = e.Gorunum;
            Kok = Kutu("parca", "sonuc");

            // Bir turda 3-4 ceza çekilebilir: orta kısım kayar, "Tamam" butonu ve alt satır hep ekranda kalır.
            var orta = new ScrollView(ScrollViewMode.Vertical);
            orta.AddToClassList("kaydirma");
            Kok.Add(orta);

            if (g.SonMiniOyun != null) orta.Add(ElKarti(akis, g, g.SonMiniOyun));
            if (g.SonOylama != null)
                orta.Add(Yazi(Metin.Al("sonuc.oylama", g.SonOylama.Ifsa, g.SonOylama.Gume), "banner"));

            orta.Add(e.Tur == EkranTuru.Ifsa ? IfsaKarti(akis, g) : GumeKarti(akis, g));

            // Bu turun cezaları (yanma kartları mini oyun perdesinde gösterildi ama burada da hatırlatılır).
            if (g.TurCezalari.Count > 0)
            {
                if (e.CezaOnayBekliyor)
                {
                    var tamam = Buton(Metin.Al("ceza.tamam"), akis.CezalariOnayla, "buton-birincil");
                    tamam.AddToClassList("gizli");
                    orta.Add(Tekrar.Cark(akis, g.TurCezalari, () =>
                    {
                        tamam.RemoveFromClassList("gizli");
                        GecikmeliAc(tamam);
                    }));
                    Kok.Add(tamam);
                }
                else
                {
                    var alan = Kutu("ceza-alani");
                    foreach (var c in g.TurCezalari) alan.Add(Tekrar.CezaKarti(akis, c));
                    orta.Add(alan);
                }
            }

            // Sıra her durumda C'ye geçer (Yön Değişti'de rastgele birine). Son turda "sıra geçiyor" yazılmaz.
            string sonraki = akis.Isim(g.C);
            bool sonTur = g.ToplamTur > 0 && g.Tur >= g.ToplamTur;
            string sira = sonTur ? Metin.Al("sonuc.son_tur")
                : g.Kaos == KaosKurali.YonDegisti ? Tekrar.KaosAdi(g.Kaos)
                : Metin.Al("sonuc.sira", sonraki, TurkceEk.Yonelme(sonraki));

            var alt = Kutu("satir");
            alt.Add(Yazi(sira, "aciklama"));
            alt.Add(Esnek());
            sure = Yazi("", "sure");
            alt.Add(sure);
            Kok.Add(alt);
        }

        static VisualElement ElKarti(IOyunAkisi akis, Gorunum g, MiniOyunSonucu s)
        {
            var kart = Kutu("el-karti");
            kart.Add(Yazi(ElYazisi.AcilanEl(akis, g, s), "el-hamleler"));
            if (s.ZarlaKarar) kart.Add(Yazi(Metin.Al("el.zarla_karar", akis.Isim(g.B), s.BZar, akis.Isim(g.C), s.CZar), "ipucu"));
            if (s.SureDoldu) kart.Add(Yazi(Metin.Al("el.sure_doldu"), "ipucu"));
            kart.Add(Yazi(Metin.Al("el.kazandi", akis.Isim(s.CKazandi ? g.C : g.B)), "el-kazanan"));
            return kart;
        }

        static VisualElement IfsaKarti(IOyunAkisi akis, Gorunum g)
        {
            string soran = akis.Isim(g.A);
            string cevap = akis.Isim(g.C);
            var kart = Kutu("ifsa-karti");
            kart.Add(Yazi(Metin.Al("sonuc.ifsa_baslik"), "ifsa-baslik"));
            kart.Add(Yazi(g.IfsaFisilti || g.IfsaSoru == null
                ? Metin.Al("sonuc.ifsa_fisilti", soran)
                : Metin.Al("sonuc.ifsa_soru", g.IfsaSoru), "ifsa-soru"));
            kart.Add(Yazi(Metin.Al("sonuc.soran_cevap", soran, cevap), "ifsa-alt"));
            return kart;
        }

        static VisualElement GumeKarti(IOyunAkisi akis, Gorunum g)
        {
            var kart = Kutu("gume-karti");
            kart.Add(Yazi(Metin.Al("sonuc.gume_baslik"), "gume-baslik"));
            // Bedel: herkes "Ayşe bedel ödedi" diye görür (Ana Plan 8.1). Soru yine gizli.
            if (g.BedelOdendi) kart.Add(Yazi(Metin.Al("sonuc.bedel", akis.Isim(g.B)), "aciklama", "orta"));
            kart.Add(Yazi(Metin.Al("sonuc.gume_alt"), "aciklama", "orta"));
            return kart;
        }

        public void Guncelle(EkranDurumu e)
        {
            SureyiYaz(sure, e.Gorunum.KalanMs, 0);
        }
    }
}
