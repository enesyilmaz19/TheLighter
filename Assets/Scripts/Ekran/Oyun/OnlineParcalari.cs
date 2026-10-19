using System.Collections.Generic;
using Cakmak.Kurallar;
using TheLighter.Ag;
using TheLighter.Akis;
using UnityEngine.UIElements;
using static TheLighter.Istemci.Arayuz;

namespace TheLighter.Istemci
{
    /// <summary>Online mini oyun: telefon tek oyuncunun, ekranın tamamı kendi yarısı (ters dönmez).</summary>
    sealed class OnlineMiniOyunParcasi : IOyunParcasi
    {
        readonly YariEkran yari;
        readonly bool bMi;

        public VisualElement Kok { get; }

        public OnlineMiniOyunParcasi(IOyunAkisi akis, EkranDurumu e)
        {
            bMi = e.BGorunumu != null;
            var g = bMi ? e.BGorunumu : e.CGorunumu;
            Kok = Kutu("parca", "online-mini");
            if (g.Kaos != KaosKurali.Yok) Kok.Add(Tekrar.KaosSeridi(g.Kaos));
            yari = new YariEkran(akis, g, bMi, null, online: true);
            Kok.Add(yari.Kok);
        }

        public void Guncelle(EkranDurumu e) => yari.Guncelle(bMi ? e.BGorunumu : e.CGorunumu);
    }

    /// <summary>
    /// Online'da sıra başkasındayken: kimin ne yaptığı ("Ali soru yazıyor…"), kalan süre ve oyuncuların
    /// bağlantı durumu (Bağlanıyor…, AFK, Ayrıldı). Gizli hiçbir şey yok: hepsi herkese açık alanlardan.
    /// </summary>
    sealed class OnlineBekleParcasi : IOyunParcasi
    {
        readonly Label sure;
        readonly Label oy;

        public VisualElement Kok { get; }

        public OnlineBekleParcasi(IOyunAkisi akis, EkranDurumu e)
        {
            var g = e.Gorunum;
            var online = akis as OnlineAkis;
            Kok = Kutu("parca", "perde", "online-bekle");

            var bas = Kutu("satir");
            bas.Add(Esnek());
            sure = Yazi("", "sure");
            bas.Add(sure);
            Kok.Add(bas);
            if (g.Kaos != KaosKurali.Yok && g.Tur > 0) Kok.Add(Tekrar.KaosKarti(g.Kaos));

            Kok.Add(Esnek());
            OyuncuId? kim = e.Kime;
            if (kim.HasValue) Kok.Add(RenkNoktasi(akis.Sira(kim.Value), "perde-renk"));
            Kok.Add(Yazi(Baslik(akis, g, online), "perde-buyuk"));
            oy = Yazi("", "aciklama", "orta");
            Kok.Add(oy);
            Kok.Add(Esnek());

            Kok.Add(Durumlar(akis, g, online));
            // Köstebek: soru sorulmadan önce herkes kendi telefonundan erken suçlayabilir.
            if (g.Asama == Asama.SoruSorma)
            {
                var hata = Yazi("", "hata");
                Kok.Add(Kostebek.SuclamaDugmesi(akis, Kok, m => hata.text = m));
                Kok.Add(hata);
            }
            Guncelle(e);
        }

        static string Baslik(IOyunAkisi akis, Gorunum g, OnlineAkis online)
        {
            if (online != null && online.BenCiktim) return Metin.Al("online.bekle.izliyorsun");
            string a = akis.Isim(g.A), b = akis.Isim(g.B), c = akis.Isim(g.C);
            switch (g.Asama)
            {
                case Asama.SoruToplama:
                    return Metin.Al("online.bekle.toplama");
                case Asama.SoruSorma:
                    // Soran Gizli kaos turunda A kimseye gelmez.
                    return g.A.HasValue ? Metin.Al("online.bekle.soru", a) : Metin.Al("online.bekle.soru_gizli");
                case Asama.CevapSecme:
                    return Metin.Al("online.bekle.cevap", b);
                case Asama.MiniOyun:
                    return Metin.Al("online.bekle.mini", b, c, Metin.Al("mini.ad." + g.MiniOyun));
                case Asama.Oylama:
                    return Metin.Al("online.bekle.oylama");
                case Asama.Suclama:
                    return g.SonSuclama ? Metin.Al("kostebek.bekle_son") : Kostebek.OylamaMetni(g.Oyun, "bekle");
                case Asama.CasusTahmini:
                    return Metin.Al("casus.son_sans_bekle", akis.Isim(g.Yakalanan));
                case Asama.Linc:
                    return Metin.Al(BenOlum(g) ? "gece.bekle_olu" : "gece.bekle_linc");
                case Asama.Gece:
                    return Metin.Al(BenOlum(g) ? "gece.bekle_olu" : "gece.bekle_gece");
                default:
                    return Metin.Al("oyun.bekleniyor");
            }
        }

        static bool BenOlum(Gorunum g)
        {
            foreach (var o in g.Oyuncular) if (o.Id == g.Sen) return o.Elendi;
            return false;
        }

        /// <summary>Oyuncuların bağlantı durumu: herkese açık (durum'un oyuncular listesinden).</summary>
        static VisualElement Durumlar(IOyunAkisi akis, Gorunum g, OnlineAkis online)
        {
            var kutu = Kutu("cipler", "online-durumlar");
            foreach (var o in g.Oyuncular)
            {
                string isim = Tekrar.IsimEtiketi(akis, g, o.Id);
                var cip = Kutu("cip", "cip-salt");
                cip.Add(RenkNoktasi(akis.Sira(o.Id)));
                cip.Add(Yazi(isim, "cip-yazi"));
                cip.EnableInClassList("soluk", o.Cikti || (online != null && !online.Bagli(o.Id)));
                kutu.Add(cip);
            }
            return kutu;
        }

        public void Guncelle(EkranDurumu e)
        {
            var g = e.Gorunum;
            SureyiYaz(sure, g.KalanMs, 5000);
            if (g.Asama == Asama.Oylama || g.Asama == Asama.Suclama || g.Asama == Asama.Linc || g.Asama == Asama.Gece)
                oy.text = Metin.Al("online.bekle.oy_sayisi", g.OyKullanan, g.Oylayanlar.Count)
                    + (g.OyVerdin ? "  " + Metin.Al("online.bekle.oy_verdin") : "");
            else if (g.Asama == Asama.SoruToplama)
            {
                int biten = 0;
                foreach (var o in g.Oyuncular) if (o.HavuzaEkledigi >= Oyun.KisiBasiHavuzSorusu) biten++;
                oy.text = Metin.Al("online.bekle.toplama_sayisi", biten, g.Oyuncular.Count);
            }
            else oy.text = "";
        }
    }
}
