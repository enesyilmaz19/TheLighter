using System.Collections.Generic;

namespace Cakmak.Kurallar
{
    /// <summary>Genel sohbet ve DM dışındaki kanallar. Kimin yazıp kimin okuduğu kural motorunda (sunucu sadece dağıtır).</summary>
    public enum SohbetKanali { Takim, Vampir, Oluler }

    public sealed partial class Oyun
    {
        /// <summary>
        /// Genel sohbete ve DM'e yazabilir mi. Vampir Köylü'de ölüler yazamaz (canlılara ipucu veremez, okur) ve gece kimse
        /// yazamaz; oyun bitince herkes. Diğer oyunlarda hep.
        /// </summary>
        public bool SohbeteYazabilir(OyuncuId kim)
        {
            if (!Vampirli || asama == Asama.OyunSonu) return true;
            var o = Bul(kim);
            return o != null && !o.Elendi && asama != Asama.Gece;
        }

        /// <summary>
        /// Kanala kimler okur (yazan dahil). Yazan yazamıyorsa null. Takım: Takım Düellosu'nda takım arkadaşları. Vampir:
        /// gece, canlı vampirler. Ölüler: Vampir Köylü'de elenenler.
        /// </summary>
        public List<OyuncuId> KanalAlicilari(OyuncuId kim, SohbetKanali kanal)
        {
            var yazan = Bul(kim);
            if (yazan == null) return null;
            switch (kanal)
            {
                case SohbetKanali.Takim when Takimli:
                    return oyuncular.FindAll(o => o.Takim == yazan.Takim).ConvertAll(o => o.Id);
                case SohbetKanali.Vampir when Vampirli && asama == Asama.Gece && !yazan.Elendi && roller[kim] == Rol.Vampir:
                    return oyuncular.FindAll(o => !o.Elendi && roller[o.Id] == Rol.Vampir).ConvertAll(o => o.Id);
                case SohbetKanali.Oluler when Vampirli && yazan.Elendi:
                    return oyuncular.FindAll(o => o.Elendi).ConvertAll(o => o.Id);
                default:
                    return null;
            }
        }
    }
}
