using System.Collections.Generic;

namespace Cakmak.Kurallar
{
    /// <summary>Genel sohbet ve DM dışındaki kanallar. Kimin yazıp kimin okuduğu kural motorunda (sunucu sadece dağıtır).</summary>
    public enum SohbetKanali { Takim, Katil, Oluler }

    public sealed partial class Oyun
    {
        /// <summary>
        /// Genel sohbete ve DM'e yazabilir mi. Çakmak Gecesi'nde ölüler ve o gün susturulan yazamaz (okur), gece kimse yazamaz;
        /// oyun bitince herkes. Diğer oyunlarda hep.
        /// </summary>
        public bool SohbeteYazabilir(OyuncuId kim)
        {
            if (!GeceOyunu || asama == Asama.OyunSonu) return true;
            var o = Bul(kim);
            return o != null && !o.Elendi && !o.Susturuldu && asama != Asama.Gece;
        }

        /// <summary>
        /// Kanala kimler okur (yazan dahil). Yazan yazamıyorsa null. Takım: Takım Düellosu'nda takım arkadaşları. Katil:
        /// Çakmak Gecesi'nde, gece, canlı katiller. Ölüler: Çakmak Gecesi'nde ölenler.
        /// </summary>
        public List<OyuncuId> KanalAlicilari(OyuncuId kim, SohbetKanali kanal)
        {
            var yazan = Bul(kim);
            if (yazan == null) return null;
            switch (kanal)
            {
                case SohbetKanali.Takim when Takimli:
                    return oyuncular.FindAll(o => o.Takim == yazan.Takim).ConvertAll(o => o.Id);
                case SohbetKanali.Katil when GeceOyunu && asama == Asama.Gece && !yazan.Elendi && KatilTarafinda(kim):
                    return oyuncular.FindAll(o => !o.Elendi && KatilTarafinda(o.Id)).ConvertAll(o => o.Id);
                case SohbetKanali.Oluler when GeceOyunu && yazan.Elendi:
                    return oyuncular.FindAll(o => o.Elendi).ConvertAll(o => o.Id);
                default:
                    return null;
            }
        }
    }
}
