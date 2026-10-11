using System.Collections.Generic;
using TheLighter.Akis;
using Cakmak.Kurallar;
using UnityEngine.UIElements;
using static TheLighter.Istemci.Arayuz;

namespace TheLighter.Istemci
{
    /// <summary>
    /// A'nın ekranı: kime soracağını seçer, soruyu yazar (en fazla 140 harf) ya da kulağına fısıldar.
    /// Yazı kutusu üstte: mobilde klavye ekranın alt yarısını kapatıyor (Ana Plan 6.2).
    /// Soru A'ya atandıysa (Curcuna, "havuzdan sor" cezası) yazı kutusu yerine atanan soru görünür;
    /// Kör Soru turunda A soruyu hiç görmez. Hangisi olduğu görünümden okunur, kural hesaplanmaz.
    /// </summary>
    sealed class SoruSormaParcasi : IOyunParcasi
    {
        const int SoruSiniri = Oyun.SoruEnFazlaKarakter;

        enum Bicim { Yazili, Atanan, Kor }

        readonly EldenEleAkisi akis;
        readonly Bicim bicim;
        readonly Label sure;
        readonly Label sayac;
        readonly Label hata;
        readonly TextField kutu;
        readonly Label atananYazi;
        readonly Button degistir;
        readonly Button gonder;
        readonly Button fisilda;
        readonly List<OyuncuId> ciptekiler = new List<OyuncuId>();
        readonly List<Button> cipler = new List<Button>();
        OyuncuId? secili;

        public VisualElement Kok { get; }

        public SoruSormaParcasi(EldenEleAkisi akis, EkranDurumu e)
        {
            this.akis = akis;
            var g = e.Gorunum;
            bicim = g.Kaos == KaosKurali.KorSoru ? Bicim.Kor : g.AtananSoru != null ? Bicim.Atanan : Bicim.Yazili;

            Kok = Kutu("parca", "soru-sorma");

            var bas = Kutu("satir");
            bas.Add(Yazi(Metin.Al("soru.baslik", akis.Isim(g.Sen)), "baslik"));
            bas.Add(Esnek());
            sure = Yazi("", "sure");
            bas.Add(sure);
            Kok.Add(bas);
            if (g.Kaos != KaosKurali.Yok) Kok.Add(Tekrar.KaosSeridi(g.Kaos));

            switch (bicim)
            {
                case Bicim.Kor:
                    Kok.Add(Yazi(Metin.Al("soru.kor"), "aciklama"));
                    break;

                case Bicim.Atanan:
                    if (akis.Ayarlar.Mod != OyunModu.Curcuna) Kok.Add(Yazi(Metin.Al("soru.havuzdan_ceza"), "banner"));
                    Kok.Add(Yazi(Metin.Al("soru.atanan_baslik"), "aciklama"));
                    var kart = Kutu("okuma-alani");
                    atananYazi = Yazi(g.AtananSoru, "okuma-soru");
                    kart.Add(atananYazi);
                    Kok.Add(kart);
                    if (akis.Ayarlar.Mod == OyunModu.Curcuna)
                    {
                        var satir = Kutu("satir");
                        degistir = Buton(Metin.Al("soru.degistir"), Degistir, "buton-kucuk");
                        degistir.SetEnabled(g.DegistirmeHakki);
                        satir.Add(degistir);
                        satir.Add(Esnek());
                        Kok.Add(satir);
                    }
                    break;

                default:
                    Kok.Add(Yazi(Metin.Al("soru.aciklama"), "aciklama"));
                    kutu = SoruKutusu.Kur();
                    kutu.RegisterValueChangedCallback(_ => Durum());
                    Kok.Add(kutu);

                    var alt = Kutu("satir");
                    alt.Add(Buton(Metin.Al("soru.oneri"), OneriCek, "buton-kucuk"));
                    alt.Add(Esnek());
                    sayac = Yazi("", "sayac");
                    alt.Add(sayac);
                    Kok.Add(alt);
                    break;
            }

            Kok.Add(Yazi(Metin.Al("soru.kime"), "alt-baslik"));
            var cipKutusu = Kutu("cipler");
            foreach (var id in g.Secilebilir)
            {
                var secim = id;
                var cip = OyuncuCipi(Tekrar.IsimEtiketi(akis, g, id), akis.Sira(id), () => { secili = secim; Durum(); });
                ciptekiler.Add(id);
                cipler.Add(cip);
                cipKutusu.Add(cip);
            }
            Kok.Add(cipKutusu);

            Kok.Add(Esnek());
            hata = Yazi("", "hata");
            Kok.Add(hata);
            gonder = Buton(Metin.Al("soru.gonder"), Gonder, "buton-birincil", "buton-buyuk");
            Kok.Add(gonder);
            if (bicim == Bicim.Yazili)
            {
                fisilda = Buton(Metin.Al("soru.fisildadim"), Fisilda, "buton-ikincil");
                Kok.Add(fisilda);
                Kok.Add(Yazi(Metin.Al("soru.fisildadim_ipucu"), "ipucu"));
            }

            Durum();
        }

        public void Guncelle(EkranDurumu e)
        {
            SureyiYaz(sure, e.Gorunum.KalanMs, 10000);
        }

        void Durum()
        {
            for (int i = 0; i < cipler.Count; i++)
                cipler[i].EnableInClassList("secili", secili.HasValue && ciptekiler[i] == secili.Value);

            if (bicim == Bicim.Yazili)
            {
                string metin = kutu.value ?? "";
                sayac.text = metin.Length + "/" + SoruSiniri;
                gonder.SetEnabled(secili.HasValue && metin.Trim().Length > 0);
                fisilda.SetEnabled(secili.HasValue);
            }
            else
            {
                gonder.SetEnabled(secili.HasValue);
            }
            hata.text = "";
        }

        void OneriCek()
        {
            var s = akis.OneriCek();
            var oneri = akis.Ekran.Gorunum != null ? akis.Ekran.Gorunum.Oneri : null;
            if (s == Sonuc.Tamam && oneri != null) kutu.value = oneri;
            else hata.text = HataMetni.Al(s);
        }

        void Degistir()
        {
            var s = akis.SoruyuDegistir();
            var g = akis.Ekran.Gorunum;
            if (s == Sonuc.Tamam && g != null && g.AtananSoru != null) atananYazi.text = g.AtananSoru;
            else hata.text = HataMetni.Al(s);
            if (g != null) degistir.SetEnabled(g.DegistirmeHakki);
        }

        void Gonder()
        {
            if (!secili.HasValue) return;
            // Atanan ya da kör soruda metin kural motorunda yok sayılır.
            var s = akis.SoruGonder(secili.Value, kutu != null ? kutu.value : "");
            if (s != Sonuc.Tamam) hata.text = HataMetni.Al(s);
        }

        void Fisilda()
        {
            if (!secili.HasValue) return;
            var s = akis.Fisildadim(secili.Value);
            if (s != Sonuc.Tamam) hata.text = HataMetni.Al(s);
        }
    }

    /// <summary>Kural motorunun döndürdüğü hata kodunun oyuncuya görünen metni.</summary>
    static class HataMetni
    {
        public static string Al(Sonuc s) => Metin.Al("hata." + s);
    }
}
