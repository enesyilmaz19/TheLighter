using TheLighter.Ag;
using UnityEngine.UIElements;
using static TheLighter.Istemci.Arayuz;

namespace TheLighter.Istemci
{
    /// <summary>
    /// Oyun sürerken katılan: oyunda değil, sonraki oyunda oynayacak. Sadece herkese açık bilgi gelir
    /// (oyuncular, tur); soru, görev, seçim hiçbir şey yok. Oyun bitince kurucunun "Tekrar oyna"sını bekler.
    /// </summary>
    public sealed class BeklemeOdasiEkrani : IEkran, IOnlineEkran
    {
        readonly OnlineYonetici online;
        readonly Label baslik;
        readonly Label aciklama;
        readonly VisualElement oyuncular;
        readonly VisualElement bekleyenler;
        readonly Label hata;
        int sonSurum = -1;

        public VisualElement Kok { get; }

        public BeklemeOdasiEkrani(Uygulama uygulama, OnlineYonetici online)
        {
            this.online = online;
            Kok = Kutu("ekran", "kurulum", "bekleme-odasi");

            var ust = Kutu("satir");
            ust.Add(Buton(Metin.Al("lobi.ayril"), online.Ayril, "buton-kucuk"));
            ust.Add(Esnek());
            Kok.Add(ust);

            var kaydirma = new ScrollView(ScrollViewMode.Vertical);
            kaydirma.AddToClassList("kaydirma");
            Kok.Add(kaydirma);

            baslik = Yazi("", "baslik");
            kaydirma.Add(baslik);
            aciklama = Yazi("", "aciklama");
            kaydirma.Add(aciklama);

            kaydirma.Add(Yazi(Metin.Al("bekleme.oyundakiler"), "alt-baslik"));
            oyuncular = Kutu("isim-listesi");
            kaydirma.Add(oyuncular);
            kaydirma.Add(Yazi(Metin.Al("bekleme.bekleyenler"), "alt-baslik"));
            bekleyenler = Kutu("isim-listesi");
            kaydirma.Add(bekleyenler);

            hata = Yazi("", "hata");
            Kok.Add(hata);
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
        }

        public void ArkaPlanaGecti() { }

        public void HataGeldi(HataMesaji h) => hata.text = HataMetni.Kod(h.Kod);

        public void OlayGeldi(OlayMesaji o) { }
    }
}
