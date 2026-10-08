using System;
using Cakmak.Kurallar;
using TheLighter.Akis;
using UnityEngine.UIElements;
using static TheLighter.Istemci.Arayuz;

namespace TheLighter.Istemci
{
    /// <summary>
    /// Herkesin baktığı sonuç ekranı: ifşa ya da güme. Süre bitince kural motoru kendiliğinden yeni tura geçer.
    /// </summary>
    sealed class SonucParcasi : IOyunParcasi
    {
        readonly Label sure;

        public VisualElement Kok { get; }

        public SonucParcasi(EldenEleAkisi akis, EkranDurumu e)
        {
            var g = e.Gorunum;
            Kok = Kutu("parca", "sonuc");

            if (g.SonMiniOyun != null) Kok.Add(ElKarti(akis, g, g.SonMiniOyun));

            Kok.Add(Esnek());
            Kok.Add(e.Tur == EkranTuru.Ifsa ? IfsaKarti(akis, g) : GumeKarti());
            Kok.Add(Esnek());

            // Sıra her durumda C'ye geçer. Son turda "sıra geçiyor" yazmamak için tur sayısına bakılıyor (sadece yazı).
            string sonraki = akis.Isim(g.C);
            bool sonTur = g.ToplamTur > 0 && g.Tur >= g.ToplamTur;
            string sira = sonTur ? Metin.Al("sonuc.son_tur") : Metin.Al("sonuc.sira", sonraki, TurkceEk.Yonelme(sonraki));

            var alt = Kutu("satir");
            alt.Add(Yazi(sira, "aciklama"));
            alt.Add(Esnek());
            sure = Yazi("", "sure");
            alt.Add(sure);
            Kok.Add(alt);
        }

        static VisualElement ElKarti(EldenEleAkisi akis, Gorunum g, MiniOyunSonucu s)
        {
            var kart = Kutu("el-karti");
            kart.Add(Yazi(ElYazisi.AcilanEl(akis, g, s), "el-hamleler"));
            if (s.ZarlaKarar) kart.Add(Yazi(Metin.Al("el.zarla_karar", akis.Isim(g.B), s.BZar, akis.Isim(g.C), s.CZar), "ipucu"));
            if (s.SureDoldu) kart.Add(Yazi(Metin.Al("el.sure_doldu"), "ipucu"));
            kart.Add(Yazi(Metin.Al("el.kazandi", akis.Isim(s.CKazandi ? g.C : g.B)), "el-kazanan"));
            return kart;
        }

        static VisualElement IfsaKarti(EldenEleAkisi akis, Gorunum g)
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

        static VisualElement GumeKarti()
        {
            var kart = Kutu("gume-karti");
            kart.Add(Yazi(Metin.Al("sonuc.gume_baslik"), "gume-baslik"));
            kart.Add(Yazi(Metin.Al("sonuc.gume_alt"), "aciklama", "orta"));
            return kart;
        }

        public void Guncelle(EkranDurumu e)
        {
            SureyiYaz(sure, e.Gorunum.KalanMs, 0);
        }
    }

    sealed class OyunSonuParcasi : IOyunParcasi
    {
        public VisualElement Kok { get; }

        public OyunSonuParcasi(EldenEleAkisi akis, EkranDurumu e, Action tekrar, Action anaMenu)
        {
            var g = e.Gorunum;
            Kok = Kutu("parca", "oyun-sonu");

            Kok.Add(Yazi(Metin.Al("son.baslik"), "logo"));
            Kok.Add(Yazi(Metin.Al("son.tur", g.Tur), "aciklama", "orta"));

            var kaydirma = new ScrollView(ScrollViewMode.Vertical);
            kaydirma.AddToClassList("kaydirma");

            kaydirma.Add(Yazi(Metin.Al("son.ifsalar"), "alt-baslik"));
            if (e.IfsaOlanlar == null || e.IfsaOlanlar.Count == 0) kaydirma.Add(Yazi(Metin.Al("son.ifsa_yok"), "aciklama"));
            else
            {
                foreach (var k in e.IfsaOlanlar)
                {
                    var kart = Kutu("son-kart");
                    kart.Add(Yazi(k.Soru != null ? Metin.Al("sonuc.ifsa_soru", k.Soru) : Metin.Al("son.fisilti"), "son-soru"));
                    kart.Add(Yazi(Metin.Al("sonuc.soran_cevap", akis.Isim(k.Soran), akis.Isim(k.Cevap)), "ipucu"));
                    kaydirma.Add(kart);
                }
            }

            // Kural motorunun tuttuğu sayaçlar. Unvanlar S3'te kural kodundan gelecek.
            kaydirma.Add(Yazi(Metin.Al("son.sayaclar"), "alt-baslik"));
            foreach (var o in g.Oyuncular)
            {
                var satir = Kutu("satir", "son-satir");
                satir.Add(RenkNoktasi(akis.Sira(o.Id)));
                satir.Add(Yazi(Metin.Al("son.sayac", o.Isim, o.Gosterilme, o.IfsaEttirme, o.Saklama), "aciklama"));
                kaydirma.Add(satir);
            }
            Kok.Add(kaydirma);

            Kok.Add(Buton(Metin.Al("son.tekrar"), tekrar, "buton-birincil", "buton-buyuk"));
            Kok.Add(Buton(Metin.Al("oyun.ana_menu"), anaMenu, "buton-ikincil"));
        }

        public void Guncelle(EkranDurumu e) { }
    }
}
