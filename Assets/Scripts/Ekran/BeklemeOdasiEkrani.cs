using TheLighter.Ag;
using UnityEngine.UIElements;
using static TheLighter.Istemci.Arayuz;

namespace TheLighter.Istemci
{
    /// <summary>
    /// Oyun sürerken katılan: oyunda değil, sonraki oyunda oynayacak. Sadece herkese açık bilgi gelir
    /// (oyuncular, tur); soru, görev, seçim hiçbir şey yok. Oyun bitince kurucunun "Tekrar oyna"sını bekler.
    /// Sohbete yazabilir ve DM alabilir (docs/Protokol.md, "Sohbet"). PC'de listeler solda, sohbet sağda.
    /// </summary>
    public sealed class BeklemeOdasiEkrani : IEkran, IOnlineEkran, IYanliEkran
    {
        readonly OnlineYonetici online;
        readonly Label baslik;
        readonly Label aciklama;
        readonly VisualElement oyuncular;
        readonly VisualElement bekleyenler;
        readonly Label hata;
        readonly SohbetPaneli sohbet;
        readonly Button sohbetDugmesi;
        readonly VisualElement listeler;
        readonly VisualElement listeYeri;
        int sonSurum = -1;

        public VisualElement Kok { get; }

        public BeklemeOdasiEkrani(Uygulama uygulama, OnlineYonetici online)
        {
            this.online = online;
            Kok = Kutu("ekran", "kurulum", "bekleme-odasi");

            var ust = Kutu("satir");
            ust.Add(Buton(Metin.Al("lobi.ayril"), online.Ayril, "buton-kucuk"));
            ust.Add(Esnek());
            if (OnlineYonetici.SohbetVar)
            {
                sohbet = online.SohbetPaneliKur();
                sohbetDugmesi = Buton(Metin.Al("sohbet.ac"), sohbet.Ac, "buton-kucuk");
                ust.Add(sohbetDugmesi);
            }
            Kok.Add(ust);

            var kaydirma = new ScrollView(ScrollViewMode.Vertical);
            kaydirma.AddToClassList("kaydirma");
            Kok.Add(kaydirma);

            baslik = Yazi("", "baslik");
            kaydirma.Add(baslik);
            aciklama = Yazi("", "aciklama");
            kaydirma.Add(aciklama);

            listeYeri = Kutu("lobi-oyuncu-yeri");
            kaydirma.Add(listeYeri);
            listeler = Kutu("lobi-oyuncu-bolumu");
            listeler.Add(Yazi(Metin.Al("bekleme.oyundakiler"), "alt-baslik"));
            oyuncular = Kutu("isim-listesi");
            listeler.Add(oyuncular);
            listeler.Add(Yazi(Metin.Al("bekleme.bekleyenler"), "alt-baslik"));
            bekleyenler = Kutu("isim-listesi");
            listeler.Add(bekleyenler);
            listeYeri.Add(listeler);

            hata = Yazi("", "hata");
            Kok.Add(hata);
            if (sohbet != null) Kok.Add(sohbet.Kok);
            Yenile();
        }

        void Yenile()
        {
            var d = online.Istemci.SonDurum;
            if (d == null || d.Yer != OdaYeri.BeklemeOdasi) return;
            sonSurum = d.Surum;

            if (d.OyunBitti)
            {
                baslik.text = Metin.Al("bekleme.bitti_baslik");
                aciklama.text = Metin.Al("bekleme.bitti_aciklama");
            }
            else
            {
                baslik.text = Metin.Al("bekleme.baslik");
                aciklama.text = d.ToplamTur > 0
                    ? Metin.Al("bekleme.aciklama_sinirli", d.Tur, d.ToplamTur)
                    : Metin.Al("bekleme.aciklama_sinirsiz", d.Tur);
            }

            Liste(oyuncular, d, d.Oyuncular);
            Liste(bekleyenler, d, d.Bekleyenler);
        }

        void Liste(VisualElement kutu, DurumMesaji d, System.Collections.Generic.List<OdaOyuncusu> liste)
        {
            kutu.Clear();
            foreach (var o in liste)
            {
                var satir = Kutu("satir", "lobi-oyuncu");
                satir.Add(RenkNoktasi(o.Renk));
                string ad = o.Isim;
                if (o.Id == online.Istemci.Sen) ad += " " + Metin.Al("lobi.sen");
                satir.Add(Yazi(ad, "lobi-isim"));
                if (d.Kurucu == o.Id) satir.Add(Yazi(Metin.Al("lobi.kurucu"), "lobi-rozet"));
                if (!o.Bagli) satir.Add(Yazi(Metin.Al("online.baglaniyor_kisa"), "lobi-rozet", "soluk"));
                kutu.Add(satir);
            }
        }

        public void Guncelle(float gecenSn)
        {
            var d = online.Istemci.SonDurum;
            if (d != null && d.Surum != sonSurum) Yenile();
            sohbet?.Guncelle(gecenSn);
        }

        public void ArkaPlanaGecti() { }

        public void HataGeldi(HataMesaji h)
        {
            if (sohbet != null && online.Istemci.SonGidenSohbet) sohbet.HataGoster(HataMetni.Mesaj(h));
            else hata.text = HataMetni.Mesaj(h);
        }

        public void OlayGeldi(OlayMesaji o) { }

        public void Yerlesim(bool genis, VisualElement sol, VisualElement sag)
        {
            if (genis) sol.Add(listeler);
            else listeYeri.Add(listeler);
            listeler.EnableInClassList("menu-kutu", genis);
            if (sohbet == null) return;
            sohbet.YanaAl(genis);
            sohbetDugmesi.EnableInClassList("gizli", genis);
            if (genis) sag.Add(sohbet.Kok);
            else Kok.Add(sohbet.Kok);
        }
    }
}
