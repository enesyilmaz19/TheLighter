using UnityEngine;

namespace TheLighter.Istemci
{
    /// <summary>
    /// Oyunun dışarıya açılan adresleri. Yasal metinler yayınlanınca (Enes) adresleri buraya gelir; o zamana kadar boş:
    /// düğmeler "yakında" der ve basılmaz.
    /// </summary>
    public static class Baglantilar
    {
        /// <summary>
        /// Android paket adı. <b>Yer tutucu:</b> Enes ile karar verilecek; Play'e bir kez yüklenince değişmez
        /// (tasks/g2-android.md 1). Build ayarları (Editor/AndroidBuild.cs) ve Play bağlantısı buradan okur.
        /// </summary>
        public const string PaketAdi = "com.thelighter.cakmak";

        /// <summary>Kullanım Şartları'nın web adresi (docs/yasal/KullanimSartlari.md yayınlanınca).</summary>
        public const string KullanimSartlari = "";

        /// <summary>Gizlilik Politikası'nın web adresi (docs/yasal/Gizlilik.md yayınlanınca).</summary>
        public const string Gizlilik = "";

        /// <summary>Google Play sayfası. Mağaza sayfası açılınca çalışır; paylaşım kartında ve paylaşma metninde.</summary>
        public static string PlaySayfasi => "https://play.google.com/store/apps/details?id=" + PaketAdi;

        public static bool Var(string adres) => !string.IsNullOrEmpty(adres);

        public static void Ac(string adres)
        {
            if (Var(adres)) Application.OpenURL(adres);
        }
    }
}
