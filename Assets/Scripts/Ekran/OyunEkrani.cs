using System;
using Cakmak.Kurallar;
using TheLighter.Akis;
using UnityEngine.UIElements;
using static TheLighter.Istemci.Arayuz;

namespace TheLighter.Istemci
{
    /// <summary>Oyun ekranının bir bölümü: perde, soru sorma, mini oyun...</summary>
    public interface IOyunParcasi
    {
        VisualElement Kok { get; }

        /// <summary>Ekran baştan çizilmeden değişen şeyler (kalan süre).</summary>
        void Guncelle(EkranDurumu e);
    }

    /// <summary>
    /// Elden ele oyunun tek ekranı. Her karede akışı ilerletir. Akışın istediği ekran değişince
    /// (EkranDurumu.Anahtar) ilgili parçayı baştan kurar, değişmediyse sadece süreleri günceller.
    /// </summary>
    public sealed class OyunEkrani : IEkran
    {
        readonly Uygulama uygulama;
        readonly EldenEleAkisi akis;
        readonly VisualElement ustCubuk;
        readonly Label turYazisi;
        readonly VisualElement govde;
        readonly VisualElement menu;
        IOyunParcasi parca;
        string anahtar;

        public VisualElement Kok { get; }

        public OyunEkrani(Uygulama uygulama, EldenEleAkisi akis)
        {
            this.uygulama = uygulama;
            this.akis = akis;

            Kok = Kutu("ekran", "oyun-ekrani");

            ustCubuk = Kutu("ust-cubuk");
            turYazisi = Yazi("", "ust-cubuk-yazi");
            ustCubuk.Add(turYazisi);
            ustCubuk.Add(Esnek());
            ustCubuk.Add(Buton(Metin.Al("oyun.menu"), MenuyuAc, "buton-kucuk"));
            Kok.Add(ustCubuk);

            govde = Kutu("govde");
            Kok.Add(govde);

            menu = MenuKur();
            Kok.Add(menu);

            Ciz();
        }

        public void Guncelle(float gecenSn)
        {
            akis.Ilerle(TimeSpan.FromSeconds(gecenSn));
            if (akis.Ekran.Anahtar != anahtar) Ciz();
            else parca.Guncelle(akis.Ekran);
        }

        public void ArkaPlanaGecti()
        {
            if (!akis.Bitti) MenuyuAc();
        }

        void Ciz()
        {
            var e = akis.Ekran;
            anahtar = e.Anahtar;
            govde.Clear();
            parca = ParcaKur(e);
            govde.Add(parca.Kok);
            parca.Guncelle(e);

            // Mini oyunda ekranın üst yarısı ters döndüğü için üst çubuk gizlenir.
            ustCubuk.EnableInClassList("gizli", e.Tur == EkranTuru.MiniOyun || e.Tur == EkranTuru.OyunSonu);
            var g = e.Gorunum;
            turYazisi.text = g.ToplamTur > 0
                ? Metin.Al("oyun.tur_sinirli", g.Tur, g.ToplamTur)
                : Metin.Al("oyun.tur_sinirsiz", g.Tur);
        }

        IOyunParcasi ParcaKur(EkranDurumu e)
        {
            switch (e.Tur)
            {
                case EkranTuru.Perde: return new PerdeParcasi(akis, e);
                case EkranTuru.SoruSorma: return new SoruSormaParcasi(akis, e);
                case EkranTuru.CevapSecme: return new CevapSecmeParcasi(akis, e);
                case EkranTuru.MiniOyunPerdesi: return new MiniOyunPerdeParcasi(akis, e);
                case EkranTuru.MiniOyun:
                    if (e.Gorunum.MiniOyun == MiniOyunTuru.Reaksiyon) return new ReaksiyonParcasi(akis, e);
                    return new MiniOyunParcasi(akis, e);
                case EkranTuru.Ifsa:
                case EkranTuru.Gume: return new SonucParcasi(akis, e);
                case EkranTuru.OyunSonu:
                    return new OyunSonuParcasi(akis, e,
                        () => uygulama.OyunuBaslat(uygulama.SonIsimler, uygulama.SonAyarlar),
                        uygulama.AnaMenu);
                default: return new BekleParcasi();
            }
        }

        // ---------- Menü ----------

        VisualElement MenuKur()
        {
            var m = Kutu("menu-perde", "gizli");
            var kutu = Kutu("menu-kutu");
            kutu.Add(Yazi(Metin.Al("oyun.menu_baslik"), "baslik"));
            kutu.Add(Buton(Metin.Al("oyun.devam"), MenuyuKapat, "buton-birincil"));
            kutu.Add(Buton(Metin.Al("oyun.bitir"), () => { akis.OyunuBitir(); MenuyuKapat(); }, "buton-ikincil"));
            kutu.Add(Buton(Metin.Al("oyun.ana_menu"), uygulama.AnaMenu, "buton-ikincil"));
            kutu.Add(Yazi(Metin.Al("oyun.menu_ipucu"), "ipucu"));
            m.Add(kutu);
            return m;
        }

        void MenuyuAc()
        {
            akis.Duraklatildi = true;
            menu.RemoveFromClassList("gizli");
        }

        void MenuyuKapat()
        {
            akis.Duraklatildi = false;
            menu.AddToClassList("gizli");
        }
    }

    sealed class BekleParcasi : IOyunParcasi
    {
        public VisualElement Kok { get; } = Kutu("parca");

        public BekleParcasi()
        {
            Kok.Add(Yazi(Metin.Al("oyun.bekleniyor"), "aciklama"));
        }

        public void Guncelle(EkranDurumu e) { }
    }
}
