using System;
using Cakmak.Kurallar;
using TheLighter.Ag;
using UnityEngine.UIElements;
using static TheLighter.Istemci.Arayuz;

namespace TheLighter.Istemci
{
    /// <summary>
    /// PC'de oyun ekranının sol sütunu (Ana Plan 6.2): oyuncular (renk, kurucu, bağlantı, AFK, çıktı) ve
    /// bekleme odasındakiler. Sadece herkese açık alanlar: durum'un oyuncu listeleri. Kural hesaplamaz.
    /// Telefonda oyun menüsündeki "Oyuncular" katmanında. Her satırda engelle / raporla ("…" ya da uzun bas).
    /// </summary>
    public sealed class OyuncuSutunu
    {
        readonly OnlineIstemci istemci;
        readonly Action<OyuncuId, string> islem;
        readonly VisualElement liste;
        readonly VisualElement bekleyenBaslik;
        readonly VisualElement bekleyenler;
        int sonSurum = -1;
        int sonEngel = -1;

        public VisualElement Kok { get; }

        /// <param name="islem">Engelle / raporla katmanını açar (<see cref="OyuncuIslemleri.Ac"/>). null: yok.</param>
        public OyuncuSutunu(OnlineIstemci istemci, Action<OyuncuId, string> islem = null)
        {
            this.istemci = istemci;
            this.islem = islem;
            Kok = Kutu("menu-kutu", "oyuncu-sutunu");
            Kok.Add(Yazi(Metin.Al("lobi.oyuncular"), "alt-baslik"));
            liste = Kutu("isim-listesi");
            Kok.Add(liste);
            bekleyenBaslik = Yazi(Metin.Al("bekleme.bekleyenler"), "alt-baslik");
            Kok.Add(bekleyenBaslik);
            bekleyenler = Kutu("isim-listesi");
            Kok.Add(bekleyenler);
            Guncelle();
        }

        public void Guncelle()
        {
            var d = istemci.SonDurum;
            if (d == null || (d.Surum == sonSurum && istemci.EngelSurumu == sonEngel)) return;
            sonSurum = d.Surum;
            sonEngel = istemci.EngelSurumu;

            liste.Clear();
            foreach (var o in d.Oyuncular) liste.Add(Satir(d, o, d.Gorunum));
            bekleyenler.Clear();
            foreach (var o in d.Bekleyenler) bekleyenler.Add(Satir(d, o, null));
            bekleyenBaslik.EnableInClassList("gizli", d.Bekleyenler.Count == 0);
        }

        VisualElement Satir(DurumMesaji d, OdaOyuncusu o, Gorunum g)
        {
            var satir = Kutu("satir", "lobi-oyuncu");
            satir.Add(RenkNoktasi(o.Renk));
            string ad = o.Isim;
            if (o.Id == istemci.Sen) ad += " " + Metin.Al("lobi.sen");
            satir.Add(Yazi(ad, "lobi-isim"));
            if (d.Kurucu == o.Id) satir.Add(Yazi(Metin.Al("lobi.kurucu"), "lobi-rozet"));

            OyuncuDurumu od = null;
            if (g != null)
                foreach (var x in g.Oyuncular)
                    if (x.Id == o.Id) { od = x; break; }

            string rozet = od != null && od.Cikti ? Metin.Al("online.etiket.ayrildi")
                : !o.Bagli ? Metin.Al("online.etiket.baglaniyor")
                : od != null && od.Afk ? Metin.Al("online.etiket.afk")
                : null;
            if (rozet != null) satir.Add(Yazi(rozet, "lobi-rozet", "soluk"));
            var can = RusRuleti.CanEtiketi(g, od);
            if (can != null) satir.Add(Yazi(can, "lobi-rozet"));
            if (od != null && od.Takim.HasValue) satir.Add(Yazi(Takim.Ad(od.Takim.Value), "lobi-rozet", Takim.Sinif(od.Takim.Value)));
            var gece = Gece.Etiket(g, od);
            if (gece != null) satir.Add(Yazi(gece, "lobi-rozet", "soluk"));
            if (g != null)
                foreach (var sp in g.Supheliler)
                    if (sp == o.Id) { satir.Add(Yazi(Metin.Al("kostebek.supheli_etiket"), "lobi-rozet")); break; }
            satir.EnableInClassList("soluk", (od != null && od.Cikti) || !o.Bagli);
            satir.Add(Esnek());
            OyuncuIslemleri.SatiraEkle(satir, istemci, o.Id, o.Isim, islem);
            return satir;
        }
    }
}
