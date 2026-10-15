using TheLighter.Akis;
using UnityEngine;

namespace TheLighter.Istemci
{
    /// <summary>
    /// Oyuncuya görünen bütün metinler Assets/UI/Resources/TheLighter/Metinler.csv içinde, dil başına bir sütun
    /// (Türkçe, İngilizce). Koda metin gömülmez. CSV, Unity Localization'ın CSV biçiminde: Localization paketi
    /// kurulunca içe aktarılıp String Table'a çevrilir ve sadece bu sınıfın içi değişir.
    /// Seçili dil <see cref="Tercihler.Dil"/>'de; bir metin o dilde boşsa Türkçesi gösterilir.
    /// </summary>
    public static class Metin
    {
        const string Yol = "TheLighter/Metinler";
        static MetinTablosu tablo;

        public static void Yukle()
        {
            var dosya = Resources.Load<TextAsset>(Yol);
            if (dosya == null)
            {
                Debug.LogError("[TheLighter] Metin tablosu bulunamadı: Resources/" + Yol + ".csv");
                tablo = MetinTablosu.CsvOku("");
                return;
            }
            tablo = MetinTablosu.CsvOku(dosya.text, Sutun(Tercihler.Dil));
        }

        /// <summary>"tr" → "Turkish(tr)", "en" → "English(en)".</summary>
        public static string Sutun(string dil) => dil == Tercihler.Ingilizce ? MetinTablosu.Ingilizce : MetinTablosu.Turkce;

        public static string Al(string anahtar, params object[] parametreler)
        {
            if (tablo == null) Yukle();
            if (!tablo.Var(anahtar)) Debug.LogWarning("[TheLighter] Eksik metin: " + anahtar);
            return tablo.Al(anahtar, parametreler);
        }
    }
}
