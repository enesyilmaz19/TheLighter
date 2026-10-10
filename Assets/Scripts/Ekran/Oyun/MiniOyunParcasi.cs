using System;
using System.Collections.Generic;
using Cakmak.Kurallar;
using TheLighter.Akis;
using UnityEngine.UIElements;
using static TheLighter.Istemci.Arayuz;

namespace TheLighter.Istemci
{
    /// <summary>
    /// Bölünmüş ekran: telefon B ile C'nin arasında. Alt yarı B'nin, üst yarı C'nin ve ters döner
    /// (karşılıklı oturuyorlar). Gizli seçimlerde önce B, sonra C seçer. Butonların yeri her seferinde
    /// karışır, rakip parmağın nereye bastığından seçimi anlamasın.
    /// Kimin kazandığını kural motoru söyler, burada hesap yok.
    /// </summary>
    sealed class MiniOyunParcasi : IOyunParcasi
    {
        readonly YariEkran ust;
        readonly YariEkran alt;

        public VisualElement Kok { get; }

        public MiniOyunParcasi(EldenEleAkisi akis, EkranDurumu e)
        {
            Kok = Kutu("parca", "bolunmus");
            ust = new YariEkran(akis, e.CGorunumu, false, e.SonBerabere);
            ust.Kok.AddToClassList("ters");
            alt = new YariEkran(akis, e.BGorunumu, true, e.SonBerabere);
            Kok.Add(ust.Kok);
            Kok.Add(Kutu("ayrac"));
            Kok.Add(alt.Kok);
        }

        public void Guncelle(EkranDurumu e)
        {
            ust.Guncelle(e.CGorunumu);
            alt.Guncelle(e.BGorunumu);
        }
    }

    sealed class YariEkran
    {
        static readonly Random Karistirici = new Random();

        readonly EldenEleAkisi akis;
        readonly OyuncuId ben;
        readonly Label sure;
        readonly Label hata;
        readonly List<Button> tekCiftButonlari = new List<Button>();
        bool? tekDiyor;

        public VisualElement Kok { get; }

        public YariEkran(EldenEleAkisi akis, Gorunum g, bool bMi, BerabereEl sonBerabere)
        {
            this.akis = akis;
            ben = g.Sen;
            string rakip = akis.Isim(bMi ? g.C : g.B);

            Kok = Kutu("yari");
            Kok.Add(Baslik(akis, g, bMi, out sure));

            if (sonBerabere != null)
                Kok.Add(Yazi(Metin.Al("mini.beraberlik", g.Beraberlik, ElYazisi.Berabere(akis, g, sonBerabere)), "banner"));

            Kok.Add(Esnek());
            Kok.Add(Icerik(g, bMi, rakip));
            hata = Yazi("", "hata");
            Kok.Add(hata);
            Kok.Add(Esnek());
        }

        /// <summary>İsim, rol ve kalan süre. Reaksiyon ekranı da kullanır.</summary>
        public static VisualElement Baslik(EldenEleAkisi akis, Gorunum g, bool bMi, out Label sure)
        {
            var kap = Kutu("yari-baslik");
            var bas = Kutu("satir");
            bas.Add(RenkNoktasi(akis.Sira(g.Sen)));
            bas.Add(Yazi(akis.Isim(g.Sen), "yari-isim"));
            bas.Add(Esnek());
            sure = Yazi("", "sure");
            bas.Add(sure);
            kap.Add(bas);
            kap.Add(Yazi(Metin.Al(bMi ? "mini.rol_b" : "mini.rol_c"), "yari-rol"));
            return kap;
        }

        VisualElement Icerik(Gorunum g, bool bMi, string rakip)
        {
            switch (EldenEleAkisi.SiradakiAdim(g))
            {
                case MiniOyunAdimi.BSeciyor:
                    return bMi ? GizliSecim(g.MiniOyun, true) : Yazi(Metin.Al("mini.bakma", rakip), "yari-durum");

                case MiniOyunAdimi.CSeciyor:
                    return bMi ? Yazi(Metin.Al("mini.sectin_bakma", rakip), "yari-durum") : GizliSecim(g.MiniOyun, false);

                case MiniOyunAdimi.IkisiDeAtiyor:
                    bool attim = bMi ? g.BHamleYapti : g.CHamleYapti;
                    if (attim) return Yazi(Metin.Al("mini.zar_attin", rakip), "yari-durum");
                    var zar = Kutu("secimler");
                    zar.Add(Buton(Metin.Al("mini.zar_at"), () => Oyna(Hamle.ZarAt()), "secim"));
                    return zar;

                default:
                    return Yazi(Metin.Al("mini.bekle"), "yari-durum");
            }
        }

        /// <summary>TKM: üç seçenek. Tek-Çift: B önce tek mi çift mi der, sonra ikisi de 1–5 seçer.</summary>
        VisualElement GizliSecim(MiniOyunTuru tur, bool bMi)
        {
            var kap = Kutu("gizli-secim");

            if (tur == MiniOyunTuru.TekCift)
            {
                if (bMi)
                {
                    kap.Add(Yazi(Metin.Al("mini.tekcift_tahmin"), "yari-durum"));
                    var tc = Kutu("secimler");
                    var tek = Buton(Metin.Al("mini.tek"), () => TekCiftSec(true), "secim", "secim-kucuk");
                    var cift = Buton(Metin.Al("mini.cift"), () => TekCiftSec(false), "secim", "secim-kucuk");
                    tekCiftButonlari.Add(tek);
                    tekCiftButonlari.Add(cift);
                    tc.Add(tek);
                    tc.Add(cift);
                    kap.Add(tc);
                }

                kap.Add(Yazi(Metin.Al("mini.sayi_sec"), "yari-durum"));
                var sayilar = new List<KeyValuePair<string, Action>>();
                for (int i = 1; i <= 5; i++)
                {
                    int n = i;
                    sayilar.Add(Secenek(n.ToString(), () => SayiSec(n, bMi)));
                }
                kap.Add(Secimler(sayilar, true));
                return kap;
            }

            kap.Add(Yazi(Metin.Al("mini.sec"), "yari-durum"));
            kap.Add(Secimler(new List<KeyValuePair<string, Action>>
            {
                Secenek(Metin.Al("mini.tas"), () => Oyna(Hamle.TasKagitMakas(TkmSecim.Tas))),
                Secenek(Metin.Al("mini.kagit"), () => Oyna(Hamle.TasKagitMakas(TkmSecim.Kagit))),
                Secenek(Metin.Al("mini.makas"), () => Oyna(Hamle.TasKagitMakas(TkmSecim.Makas))),
            }, true));
            return kap;
        }

        void TekCiftSec(bool tek)
        {
            tekDiyor = tek;
            tekCiftButonlari[0].EnableInClassList("secili", tek);
            tekCiftButonlari[1].EnableInClassList("secili", !tek);
            hata.text = "";
        }

        void SayiSec(int sayi, bool bMi)
        {
            if (bMi && !tekDiyor.HasValue)
            {
                hata.text = Metin.Al("mini.once_tekcift");
                return;
            }
            Oyna(Hamle.TekCift(sayi, bMi && tekDiyor.Value));
        }

        static KeyValuePair<string, Action> Secenek(string yazi, Action tik) => new KeyValuePair<string, Action>(yazi, tik);

        static VisualElement Secimler(List<KeyValuePair<string, Action>> secenekler, bool karistir)
        {
            if (karistir)
            {
                for (int i = secenekler.Count - 1; i > 0; i--)
                {
                    int j = Karistirici.Next(i + 1);
                    var t = secenekler[i];
                    secenekler[i] = secenekler[j];
                    secenekler[j] = t;
                }
            }

            var satir = Kutu("secimler");
            foreach (var s in secenekler) satir.Add(Buton(s.Key, s.Value, "secim"));
            return satir;
        }

        void Oyna(Hamle h)
        {
            var s = akis.Hamle(ben, h);
            if (s != Sonuc.Tamam) hata.text = HataMetni.Al(s);
        }

        public void Guncelle(Gorunum g)
        {
            SureyiYaz(sure, g.KalanMs, 3000);
        }
    }

    /// <summary>Açılan bir elin hamlelerini yazıya döker. Kazananı hesaplamaz, MiniOyunSonucu'ndan okur.</summary>
    static class ElYazisi
    {
        public static string AcilanEl(EldenEleAkisi akis, Gorunum g, MiniOyunSonucu s)
        {
            string b = akis.Isim(g.B);
            string c = akis.Isim(g.C);
            if (s.Tur == MiniOyunTuru.TekCift && s.BHamle.HasValue && s.CHamle.HasValue)
            {
                int bs = s.BHamle.Value.Sayi;
                int cs = s.CHamle.Value.Sayi;
                int toplam = bs + cs;
                return Metin.Al("el.tekcift", b, TekCift(s.BHamle.Value.TekDiyor), bs, cs, toplam, TekCift(toplam % 2 == 1));
            }
            if (s.Tur == MiniOyunTuru.Zar)
                return Metin.Al("el.ikili", b, s.BZar, c, s.CZar);
            return Metin.Al("el.ikili", b, HamleYazisi(s.BHamle), c, HamleYazisi(s.CHamle));
        }

        public static string Berabere(EldenEleAkisi akis, Gorunum g, BerabereEl el)
        {
            return Metin.Al("el.ikili", akis.Isim(g.B), HamleYazisi(el.BHamle), akis.Isim(g.C), HamleYazisi(el.CHamle));
        }

        static string TekCift(bool tek) => tek ? Metin.Al("mini.tek") : Metin.Al("mini.cift");

        static string HamleYazisi(Hamle? h)
        {
            if (!h.HasValue) return Metin.Al("el.oynamadi");
            var x = h.Value;
            switch (x.Tur)
            {
                case MiniOyunTuru.Tkm:
                    switch (x.Tkm)
                    {
                        case TkmSecim.Tas: return Metin.Al("mini.tas");
                        case TkmSecim.Kagit: return Metin.Al("mini.kagit");
                        default: return Metin.Al("mini.makas");
                    }
                case MiniOyunTuru.TekCift:
                    return x.Sayi.ToString();
                case MiniOyunTuru.Reaksiyon:
                    return x.Erken ? Metin.Al("el.erken") : Metin.Al("el.ms", x.Sayi);
                default:
                    return Metin.Al("mini.zar_atti");
            }
        }
    }
}
