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
    /// </summary>
    sealed class SoruSormaParcasi : IOyunParcasi
    {
        const int SoruSiniri = Oyun.SoruEnFazlaKarakter;

        readonly EldenEleAkisi akis;
        readonly Label sure;
        readonly Label sayac;
        readonly Label hata;
        readonly TextField kutu;
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

            Kok = Kutu("parca", "soru-sorma");

            var bas = Kutu("satir");
            bas.Add(Yazi(Metin.Al("soru.baslik", akis.Isim(g.Sen)), "baslik"));
            bas.Add(Esnek());
            sure = Yazi("", "sure");
            bas.Add(sure);
            Kok.Add(bas);
            Kok.Add(Yazi(Metin.Al("soru.aciklama"), "aciklama"));

            kutu = new TextField { maxLength = SoruSiniri, multiline = true };
            kutu.AddToClassList("soru-kutusu");
            // Uzun soru alt satıra geçsin, yana kaymasın: yazan kişi sorusunun tamamını görmeli.
            // Unity'nin kendi stili USS'teki white-space'i eziyor, bu yüzden satır içi stil.
            kutu.verticalScrollerVisibility = ScrollerVisibility.Auto;
            kutu.style.whiteSpace = WhiteSpace.Normal;
            var icYazi = kutu.Q(className: TextField.inputUssClassName)?.Q<TextElement>();
            if (icYazi != null) icYazi.style.whiteSpace = WhiteSpace.Normal;
            kutu.RegisterValueChangedCallback(_ => Durum());
            Kok.Add(kutu);

            var alt = Kutu("satir");
            alt.Add(Buton(Metin.Al("soru.oneri"), OneriCek, "buton-kucuk"));
            alt.Add(Esnek());
            sayac = Yazi("", "sayac");
            alt.Add(sayac);
            Kok.Add(alt);

            Kok.Add(Yazi(Metin.Al("soru.kime"), "alt-baslik"));
            var cipKutusu = Kutu("cipler");
            foreach (var id in g.Secilebilir)
            {
                var o = akis.OyuncuBul(id);
                var secim = id;
                var cip = OyuncuCipi(o.Isim, akis.Sira(id), () => { secili = secim; Durum(); });
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
            fisilda = Buton(Metin.Al("soru.fisildadim"), Fisilda, "buton-ikincil");
            Kok.Add(fisilda);
            Kok.Add(Yazi(Metin.Al("soru.fisildadim_ipucu"), "ipucu"));

            Durum();
        }

        public void Guncelle(EkranDurumu e)
        {
            SureyiYaz(sure, e.Gorunum.KalanMs, 10000);
        }

        void Durum()
        {
            string metin = kutu.value ?? "";
            sayac.text = metin.Length + "/" + SoruSiniri;
            for (int i = 0; i < cipler.Count; i++)
                cipler[i].EnableInClassList("secili", secili.HasValue && ciptekiler[i] == secili.Value);
            gonder.SetEnabled(secili.HasValue && metin.Trim().Length > 0);
            fisilda.SetEnabled(secili.HasValue);
            hata.text = "";
        }

        void OneriCek()
        {
            var s = akis.OneriCek();
            var oneri = akis.Ekran.Gorunum != null ? akis.Ekran.Gorunum.Oneri : null;
            if (s == Sonuc.Tamam && oneri != null) kutu.value = oneri;
            else hata.text = HataMetni.Al(s);
        }

        void Gonder()
        {
            if (!secili.HasValue) return;
            var s = akis.SoruGonder(secili.Value, kutu.value);
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
