using Cakmak.Kurallar;
using TheLighter.Akis;
using UnityEngine.UIElements;
using static TheLighter.Istemci.Arayuz;

namespace TheLighter.Istemci
{
    /// <summary>
    /// "Telefonu Ayşe'ye ver." Telefon el değiştirirken gösterilir. Gizli bilgi yok, saat durur.
    /// Buton kısa bir süre kapalı açılır: önceki kişinin ikinci dokunuşu perdeyi kaldırmasın.
    /// </summary>
    sealed class PerdeParcasi : IOyunParcasi
    {
        public VisualElement Kok { get; }

        public PerdeParcasi(EldenEleAkisi akis, EkranDurumu e)
        {
            var kime = e.Kime.Value;
            string isim = akis.Isim(kime);
            var g = e.Gorunum;

            Kok = Kutu("parca", "perde");

            // Süre dolduysa kural motoru yeni tura geçti. Neden olduğunu herkes görsün.
            if (e.SuresiDolan.HasValue)
            {
                string dolan = akis.Isim(e.SuresiDolan);
                Kok.Add(Yazi(e.SuresiDolanBydi
                    ? Metin.Al("perde.sure_doldu_b", dolan, TurkceEk.Ilgi(dolan))
                    : Metin.Al("perde.sure_doldu_a", dolan, TurkceEk.Ilgi(dolan)), "banner"));
            }

            Kok.Add(Esnek());
            Kok.Add(RenkNoktasi(akis.Sira(kime), "perde-renk"));

            if (e.PerdeNedeni == PerdeNedeni.SoruSirasi)
            {
                string anahtar = e.SuresiDolan.HasValue ? "perde.sira_rastgele" : "perde.sira";
                Kok.Add(Yazi(Metin.Al(anahtar, isim, TurkceEk.Bulunma(isim)), "perde-ust"));
            }
            else
            {
                string a = akis.Isim(g.A);
                Kok.Add(Yazi(Metin.Al("perde.sordu", a, isim, TurkceEk.Yonelme(isim)), "perde-ust"));
            }

            Kok.Add(Yazi(Metin.Al("perde.telefonu_ver", isim, TurkceEk.Yonelme(isim)), "perde-buyuk"));
            Kok.Add(Yazi(Metin.Al("perde.bakmasin"), "perde-alt"));
            Kok.Add(Esnek());

            var hazir = Buton(Metin.Al("perde.hazirim", isim), akis.PerdeyiKaldir, "buton-birincil", "buton-buyuk");
            GecikmeliAc(hazir);
            Kok.Add(hazir);
        }

        public void Guncelle(EkranDurumu e) { }
    }

    /// <summary>"Ayşe çakmağı Mehmet'e verdi! Telefonu ikinizin arasına koyun."</summary>
    sealed class MiniOyunPerdeParcasi : IOyunParcasi
    {
        public VisualElement Kok { get; }

        public MiniOyunPerdeParcasi(EldenEleAkisi akis, EkranDurumu e)
        {
            var g = e.Gorunum;
            string b = akis.Isim(g.B);
            string c = akis.Isim(g.C);
            var tur = g.MiniOyun;

            Kok = Kutu("parca", "perde");
            Kok.Add(Esnek());
            Kok.Add(Yazi(Metin.Al("mini.perde_verdi", b, c, TurkceEk.Yonelme(c)), "perde-ust"));
            Kok.Add(Yazi(Metin.Al("mini.ad." + tur), "perde-buyuk"));
            Kok.Add(Yazi(Metin.Al("mini.kural." + tur), "aciklama", "orta"));
            Kok.Add(Yazi(Metin.Al("mini.perde_anlam", b, c), "aciklama", "orta"));
            Kok.Add(Esnek());
            Kok.Add(Yazi(Metin.Al("mini.perde_yerlesim", b, c), "perde-alt"));

            var hazir = Buton(Metin.Al("mini.perde_hazir"), akis.PerdeyiKaldir, "buton-birincil", "buton-buyuk");
            GecikmeliAc(hazir);
            Kok.Add(hazir);
        }

        public void Guncelle(EkranDurumu e) { }
    }
}
