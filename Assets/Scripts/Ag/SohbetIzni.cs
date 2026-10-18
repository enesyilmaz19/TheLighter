using System.Collections.Generic;
using Cakmak.Kurallar;

namespace TheLighter.Ag
{
    /// <summary>
    /// Sohbet panelinin sekmeleri ve yazma kutusu: hangi adlı kanallar görünür, nereye yazılamaz (ve neden). Kuralın kendisi
    /// kural motorunda (<c>Oyun.SohbeteYazabilir</c>, <c>Oyun.KanalAlicilari</c>), sunucu uygular; burada görünümün herkese açık
    /// alanlarından (aşama, ölü, susturuldu, katil listesi) sadece kutu kapatılır, yazılırsa sunucu yine de reddeder.
    /// docs/Oyunlar.md 5 "Sohbet (online)".
    /// </summary>
    public static class SohbetIzni
    {
        static OyuncuDurumu Ben(Gorunum g, OyuncuId? ben)
        {
            if (g == null || !ben.HasValue) return null;
            foreach (var o in g.Oyuncular) if (o.Id == ben.Value) return o;
            return null;
        }

        static bool OyunSuruyor(DurumMesaji d) =>
            d != null && d.Yer == OdaYeri.Oyun && d.Gorunum != null && d.Gorunum.Asama != Asama.OyunSonu;

        /// <summary>
        /// Görünen adlı kanallar: Takım Düellosu'nda takım; Çakmak Gecesi'nde katillere katil kanalı, ölülere ölüler kanalı.
        /// Oyun sürerken (lobide, bekleme odasında ve oyun sonunda yok).
        /// </summary>
        public static List<SohbetKanali> AdliKanallar(DurumMesaji d, OyuncuId? ben)
        {
            var l = new List<SohbetKanali>();
            if (!OyunSuruyor(d)) return l;
            var g = d.Gorunum;
            var o = Ben(g, ben);
            if (o == null) return l;
            if (g.Oyun == OyunTuru.Takim && o.Takim.HasValue) l.Add(SohbetKanali.Takim);
            if (g.Oyun == OyunTuru.CakmakGecesi)
            {
                if (g.Katiller.Count > 0) l.Add(SohbetKanali.Katil);
                if (o.Elendi) l.Add(SohbetKanali.Oluler);
            }
            return l;
        }

        /// <summary>
        /// Bu yere şimdi yazılamıyorsa nedeninin metin anahtarı ("sohbet.yazamaz.gece" gibi), yazılabiliyorsa null.
        /// Çakmak Gecesi: genel ve DM'e gece kimse, ölü ve o gün susturulan hiç yazamaz; katil kanalı sadece gece ve canlıyken.
        /// </summary>
        public static string YazamazNedeni(DurumMesaji d, OyuncuId? ben, SohbetYeri yer)
        {
            if (!OyunSuruyor(d) || d.Gorunum.Oyun != OyunTuru.CakmakGecesi) return null;
            var g = d.Gorunum;
            var o = Ben(g, ben);
            if (yer.Kanal == SohbetKanali.Katil)
                return g.Asama == Asama.Gece && o != null && !o.Elendi ? null : "sohbet.yazamaz.katil";
            if (yer.Kanal.HasValue) return null;
            if (o != null && o.Elendi) return "sohbet.yazamaz.olu";
            if (g.Asama == Asama.Gece) return "sohbet.yazamaz.gece";
            if (o != null && o.Susturuldu) return "sohbet.yazamaz.susturuldu";
            return null;
        }
    }
}
