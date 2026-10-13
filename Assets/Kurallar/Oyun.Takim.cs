using System;
using System.Collections.Generic;

namespace Cakmak.Kurallar
{
    // Takım Düellosu (docs/Oyunlar.md, 3): iki takım, turlar normal çakmak. Farklı takımdan B ile C'nin düellosunu
    // kazananın takımı 1 puan; A rakip takımdan birine sorduysa ve soru ifşa olursa A'nın takımı 1 puan daha.
    // Aynı takımdan B ile C: puan yok, çakmak takımda kalır. Önce 7 puana ulaşan takım kazanır.

    /// <summary>Oyun sonunda: kazanan takım ve puanlar.</summary>
    public sealed class TakimSonucu
    {
        /// <summary>Kazanan takım (0 ya da 1), berabereyse null.</summary>
        public int? Kazanan;
        public IReadOnlyList<int> Puanlar = Array.Empty<int>();

        internal TakimSonucu Kopya() => new TakimSonucu { Kazanan = Kazanan, Puanlar = new List<int>(Puanlar) };
    }

    public sealed partial class Oyun
    {
        public const int TakimSayisi = 2;
        public const int HedefPuan = 7;

        readonly int[] takimPuani = new int[TakimSayisi];
        TakimSonucu takimSonucu;

        bool Takimli => ayar.Oyun == OyunTuru.Takim;

        /// <summary>
        /// Kurucunun dağılımı (null = rastgele): herkes tam bir kez (odada olmayan da yok), takımlar 0 ve 1, takımlar
        /// arasında en fazla 1 kişi fark. En az 4 oyuncuyla bu, her takımda en az 2 kişi demek.
        /// </summary>
        static bool TakimDagilimiGecerli(IReadOnlyDictionary<OyuncuId, int> dagilim, IReadOnlyList<Oyuncu> liste)
        {
            if (dagilim == null) return true;
            if (dagilim.Count != liste.Count) return false;
            var sayi = new int[TakimSayisi];
            foreach (var o in liste)
            {
                if (!dagilim.TryGetValue(o.Id, out var t) || t < 0 || t >= TakimSayisi) return false;
                sayi[t]++;
            }
            return Math.Abs(sayi[0] - sayi[1]) <= 1;
        }

        void TakimKur()
        {
            if (!Takimli) return;
            if (ayar.TakimDagilimi != null)
            {
                foreach (var o in oyuncular) o.Takim = ayar.TakimDagilimi[o.Id];
            }
            else
            {
                // Rastgele ve dengeli: karıştır, sırayla dağıt.
                var sira = Karistir(new List<OyuncuDurumu>(oyuncular));
                for (int i = 0; i < sira.Count; i++) sira[i].Takim = i % TakimSayisi;
            }
            // Tek sayıda kişide küçük takım 1 puanla başlar.
            int ilk = oyuncular.FindAll(o => o.Takim == 0).Count;
            int ikinci = oyuncular.Count - ilk;
            if (ilk != ikinci) takimPuani[ilk < ikinci ? 0 : 1] = 1;
        }

        /// <summary>Tur sonucu belli (TuruBitir): düello puanı ve ifşa bonusu.</summary>
        void TakimTurSonu(bool ifsa, OyuncuId? kaybeden)
        {
            if (!Takimli) return;
            int bTakimi = Bul(b.Value).Takim.Value;
            // Grup Kararı'nda düello yok: oylamanın kazananı sayılır. Oylamada kaybeden belli değilse puan yok.
            if (kaybeden.HasValue && bTakimi != Bul(c.Value).Takim)
            {
                var kazanan = kaybeden == b ? c.Value : b.Value;
                PuanVer(Bul(kazanan).Takim.Value, OlayTuru.DuelloPuani, kazanan);
            }
            // Soran Gizli turunda bonus yok: puan A'nın takımını ele verirdi.
            int aTakimi = Bul(a).Takim.Value;
            if (ifsa && !SoranGizli && aTakimi != bTakimi) PuanVer(aTakimi, OlayTuru.IfsaBonusu, a);
        }

        void PuanVer(int takim, OlayTuru neden, OyuncuId kim)
        {
            takimPuani[takim]++;
            Yay(new Olay { Tur = neden, Kim = kim, Takim = takim });
        }

        /// <summary>Tur sınırında (önceki turun sonucu gösterildi): 7 puana ulaşan varsa oyun biter. true: bitti.</summary>
        bool TakimKazandi()
        {
            if (!Takimli || (takimPuani[0] < HedefPuan && takimPuani[1] < HedefPuan)) return false;
            Bitir();
            return true;
        }

        /// <summary>Oyun bitiyor: önde olan kazanır (aynı turda ikisi de 7'yi geçtiyse de), eşitlikte berabere.</summary>
        void TakimSonucunuYaz()
        {
            if (!Takimli || takimSonucu != null) return;
            int? kazanan = takimPuani[0] > takimPuani[1] ? 0 : takimPuani[1] > takimPuani[0] ? 1 : (int?)null;
            takimSonucu = new TakimSonucu { Kazanan = kazanan, Puanlar = new List<int>(takimPuani) };
        }

        void TakimGorunumu(Gorunum g)
        {
            if (Takimli) g.TakimPuanlari = new List<int>(takimPuani);
        }
    }
}
