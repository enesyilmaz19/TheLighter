using System;
using System.Collections.Generic;

namespace TheLighter.Ag
{
    /// <summary>
    /// Kurucunun "Bizim paket"i lobide sunucuya parça parça yüklemesi (docs/Protokol.md, "Grup paketi").
    /// Bir mesaj 4096 baytı geçemez: sorular <see cref="Cevirici.GrupPaketiParcalari"/> ile bölünür, ilki <c>ilk: true</c>.
    /// Sonraki parça, öncekinden sonra gelen durum görülünce gider (mesaj sınırına takılmasın). Durum gelmezse
    /// <see cref="BeklemeSn"/> sonra yine gider. Hata gelirse durur; sunucu hatalı parçanın hiçbirini eklemez.
    /// UnityEngine'siz: ekran her karede <see cref="Ilerle"/> çağırır.
    /// </summary>
    public sealed class GrupPaketiYukleyici
    {
        /// <summary>Durum gelmezse sonraki parça bu kadar sonra gider.</summary>
        public const double BeklemeSn = 2.0;

        readonly List<string> parcalar;
        readonly Func<string, bool> gonder;
        int siradaki;
        int bekledigiSurum = -1;
        double kalan;

        public enum Hal { Bekliyor, Yukleniyor, Bitti, Hata }

        public Hal Durum { get; private set; } = Hal.Bekliyor;
        /// <summary>Hata geldiyse kodu (GECERSIZ_GIRDI...).</summary>
        public string HataKodu { get; private set; }
        public int Toplam => parcalar.Count;
        /// <summary>Sunucunun kabul ettiği (sonrasında durum gelen) parça sayısı.</summary>
        public int Biten { get; private set; }

        /// <param name="gonder">Mesajı yollar (gerçekte <see cref="OnlineIstemci.Gonder"/>). Gitmezse false.</param>
        public GrupPaketiYukleyici(IEnumerable<string> sorular, Func<string, bool> gonder)
        {
            parcalar = Cevirici.GrupPaketiParcalari(sorular);
            this.gonder = gonder ?? throw new ArgumentNullException(nameof(gonder));
        }

        /// <param name="sonSurum">Şu an görülen durum'un sürümü: cevap bundan sonraki durum'la gelir.</param>
        public void Basla(int sonSurum)
        {
            if (Durum != Hal.Bekliyor) return;
            if (parcalar.Count == 0)
            {
                Durum = Hal.Bitti;
                return;
            }
            Durum = Hal.Yukleniyor;
            Yolla(sonSurum);
        }

        void Yolla(int sonSurum)
        {
            bekledigiSurum = sonSurum;
            kalan = BeklemeSn;
            if (!gonder(parcalar[siradaki]))
            {
                Durum = Hal.Hata;
                HataKodu = null;
                return;
            }
            siradaki++;
        }

        /// <summary>Lobi durum'u geldi: önceki parça işlendi, sıradakini yolla.</summary>
        public void DurumGeldi(int surum)
        {
            if (Durum != Hal.Yukleniyor || surum <= bekledigiSurum) return;
            Ilerlet(surum);
        }

        public void Ilerle(double sn)
        {
            if (Durum != Hal.Yukleniyor) return;
            kalan -= Math.Max(0, sn);
            if (kalan <= 0) Ilerlet(bekledigiSurum);
        }

        void Ilerlet(int surum)
        {
            Biten = siradaki;
            if (siradaki >= parcalar.Count)
            {
                Durum = Hal.Bitti;
                return;
            }
            Yolla(surum);
        }

        /// <summary>Yüklerken hata geldi: dur. Sohbetin hatası değilse çağrılır.</summary>
        public void HataGeldi(string kod)
        {
            if (Durum != Hal.Yukleniyor) return;
            Durum = Hal.Hata;
            HataKodu = kod;
        }
    }
}
