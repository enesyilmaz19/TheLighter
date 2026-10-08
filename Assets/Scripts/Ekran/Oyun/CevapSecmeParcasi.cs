using System.Collections.Generic;
using TheLighter.Akis;
using Cakmak.Kurallar;
using UnityEngine.UIElements;
using static TheLighter.Istemci.Arayuz;

namespace TheLighter.Istemci
{
    /// <summary>
    /// B'nin ekranı. Soru sadece parmak basılıyken görünür, parmak kalkınca kapanır (Ana Plan, Elden ele).
    /// Sonra çakmağı kime vereceğini seçer.
    /// </summary>
    sealed class CevapSecmeParcasi : IOyunParcasi
    {
        readonly EldenEleAkisi akis;
        readonly Label sure;
        readonly Label hata;
        readonly Button ver;
        readonly List<OyuncuId> ciptekiler = new List<OyuncuId>();
        readonly List<Button> cipler = new List<Button>();
        OyuncuId? secili;

        public VisualElement Kok { get; }

        public CevapSecmeParcasi(EldenEleAkisi akis, EkranDurumu e)
        {
            this.akis = akis;
            var g = e.Gorunum;
            string a = akis.Isim(g.A);

            Kok = Kutu("parca", "cevap-secme");

            var bas = Kutu("satir");
            bas.Add(Yazi(akis.Isim(g.Sen), "baslik"));
            bas.Add(Esnek());
            sure = Yazi("", "sure");
            bas.Add(sure);
            Kok.Add(bas);

            if (g.Fisilti || g.Soru == null)
            {
                Kok.Add(Yazi(Metin.Al("cevap.fisildadi", a), "aciklama"));
                var bilgi = Kutu("okuma-alani");
                bilgi.Add(Yazi(Metin.Al("cevap.fisildadi_bilgi"), "okuma-ipucu"));
                Kok.Add(bilgi);
            }
            else
            {
                Kok.Add(Yazi(Metin.Al("cevap.sordu", a), "aciklama"));
                Kok.Add(OkumaAlani(g.Soru));
            }

            Kok.Add(Yazi(Metin.Al("cevap.kime"), "alt-baslik"));
            Kok.Add(Yazi(Metin.Al("cevap.kime_ipucu", a), "ipucu"));
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
            ver = Buton(Metin.Al("cevap.ver"), Ver, "buton-birincil", "buton-buyuk");
            Kok.Add(ver);
            Durum();
        }

        /// <summary>Basılı tutunca soru görünür, bırakınca kapanır.</summary>
        static VisualElement OkumaAlani(string soru)
        {
            var alan = Kutu("okuma-alani");
            var ipucu = Yazi(Metin.Al("cevap.basili_tut"), "okuma-ipucu");
            var metin = Yazi(soru, "okuma-soru", "gizli");
            ipucu.pickingMode = PickingMode.Ignore;
            metin.pickingMode = PickingMode.Ignore;
            alan.Add(ipucu);
            alan.Add(metin);

            void Goster(bool acik)
            {
                metin.EnableInClassList("gizli", !acik);
                ipucu.EnableInClassList("gizli", acik);
                alan.EnableInClassList("basili", acik);
            }

            alan.RegisterCallback<PointerDownEvent>(ev =>
            {
                alan.CapturePointer(ev.pointerId);
                Goster(true);
            });
            alan.RegisterCallback<PointerUpEvent>(ev =>
            {
                if (alan.HasPointerCapture(ev.pointerId)) alan.ReleasePointer(ev.pointerId);
                Goster(false);
            });
            alan.RegisterCallback<PointerCancelEvent>(_ => Goster(false));
            alan.RegisterCallback<PointerCaptureOutEvent>(_ => Goster(false));
            return alan;
        }

        public void Guncelle(EkranDurumu e)
        {
            SureyiYaz(sure, e.Gorunum.KalanMs);
        }

        void Durum()
        {
            for (int i = 0; i < cipler.Count; i++)
                cipler[i].EnableInClassList("secili", secili.HasValue && ciptekiler[i] == secili.Value);
            ver.SetEnabled(secili.HasValue);
            hata.text = "";
        }

        void Ver()
        {
            if (!secili.HasValue) return;
            var s = akis.CevapSec(secili.Value);
            if (s != Sonuc.Tamam) hata.text = HataMetni.Al(s);
        }
    }
}
