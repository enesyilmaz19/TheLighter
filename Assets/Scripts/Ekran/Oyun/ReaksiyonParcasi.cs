using System;
using System.Diagnostics;
using Cakmak.Kurallar;
using TheLighter.Akis;
using UnityEngine.UIElements;
using static TheLighter.Istemci.Arayuz;

namespace TheLighter.Istemci
{
    /// <summary>
    /// Reaksiyon (sadece elden ele): ikisi aynı anda bakar, ekran yeşile dönünce kendi yarısına basar.
    /// Kırmızıyken basan erken basmış olur. Yeşile dönüş anı burada rastgele seçilir (görsel bekleme);
    /// tepki süresini bu cihaz ölçer, kimin kazandığını kural motoru söyler.
    /// Biri basınca ekran baştan çizilmez (EldenEleAkisi anahtarı), zamanlama kaybolmasın diye.
    /// </summary>
    sealed class ReaksiyonParcasi : IOyunParcasi
    {
        static readonly Random Rastgele = new Random();

        readonly EldenEleAkisi akis;
        readonly Stopwatch saat = Stopwatch.StartNew();
        readonly long yesilOlacak;
        readonly Yari ust;
        readonly Yari alt;
        long yesilAni = -1;

        public VisualElement Kok { get; }

        public ReaksiyonParcasi(EldenEleAkisi akis, EkranDurumu e)
        {
            this.akis = akis;
            yesilOlacak = Rastgele.Next(1500, 4000);

            Kok = Kutu("parca", "bolunmus");
            ust = new Yari(this, e.CGorunumu, false);
            ust.Kok.AddToClassList("ters");
            alt = new Yari(this, e.BGorunumu, true);
            Kok.Add(ust.Kok);
            Kok.Add(Kutu("ayrac"));
            Kok.Add(alt.Kok);
        }

        bool Yesil => yesilAni >= 0;

        public void Guncelle(EkranDurumu e)
        {
            if (!Yesil && saat.ElapsedMilliseconds >= yesilOlacak)
            {
                yesilAni = saat.ElapsedMilliseconds;
                ust.YesileDon();
                alt.YesileDon();
            }
            ust.Guncelle(e.CGorunumu);
            alt.Guncelle(e.BGorunumu);
        }

        /// <summary>Erken basış -1 ms olarak gider, kural motoru erken sayar.</summary>
        Sonuc Bas(OyuncuId kim)
        {
            int ms = Yesil ? (int)(saat.ElapsedMilliseconds - yesilAni) : -1;
            return akis.Hamle(kim, Hamle.Reaksiyon(ms));
        }

        sealed class Yari
        {
            readonly ReaksiyonParcasi ana;
            readonly OyuncuId ben;
            readonly VisualElement alan;
            readonly Label durum;
            readonly Label sure;
            bool bastim;

            public VisualElement Kok { get; }

            public Yari(ReaksiyonParcasi ana, Gorunum g, bool bMi)
            {
                this.ana = ana;
                ben = g.Sen;
                Kok = Kutu("yari");
                Kok.Add(YariEkran.Baslik(ana.akis, g, bMi, out sure));

                alan = Kutu("reaksiyon-alani", "reaksiyon-bekle");
                durum = Yazi(Metin.Al("mini.reaksiyon_bekle"), "reaksiyon-yazi");
                durum.pickingMode = PickingMode.Ignore;
                alan.Add(durum);
                alan.RegisterCallback<PointerDownEvent>(_ => Bas());
                Kok.Add(alan);
            }

            void Bas()
            {
                if (bastim) return;
                bool erken = !ana.Yesil;
                var s = ana.Bas(ben);
                if (s != Sonuc.Tamam)
                {
                    durum.text = HataMetni.Al(s);
                    return;
                }
                bastim = true;
                durum.text = Metin.Al(erken ? "mini.reaksiyon_erken" : "mini.reaksiyon_bastin");
                alan.RemoveFromClassList("reaksiyon-bekle");
                alan.RemoveFromClassList("reaksiyon-yesil");
                alan.AddToClassList("reaksiyon-basildi");
            }

            public void YesileDon()
            {
                if (bastim) return;
                alan.RemoveFromClassList("reaksiyon-bekle");
                alan.AddToClassList("reaksiyon-yesil");
                durum.text = Metin.Al("mini.reaksiyon_bas");
            }

            public void Guncelle(Gorunum g)
            {
                SureyiYaz(sure, g.KalanMs, 3000);
            }
        }
    }
}
