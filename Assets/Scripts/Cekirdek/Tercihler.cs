using UnityEngine;

namespace TheLighter.Istemci
{
    /// <summary>
    /// Oyuncunun cihazdaki ayarları: ses, müzik, dil. PlayerPrefs'te saklanır (oyun ayarlarından ayrı: onlar
    /// kural motorunun <c>Ayarlar</c>'ı). Ses ve müzik 0–1. Dil varsayılan Türkçe (Ana Plan: önce TR).
    /// Oyunda henüz ses ve müzik dosyası yok: çalan her şey <see cref="SesSeviyesi"/> ve <see cref="MuzikSeviyesi"/>'ne uyar.
    /// </summary>
    public static class Tercihler
    {
        public const string Turkce = "tr";
        public const string Ingilizce = "en";

        const string KayitSes = "ayar.ses";
        const string KayitMuzik = "ayar.muzik";
        const string KayitDil = "ayar.dil";

        public static float SesSeviyesi
        {
            get => Mathf.Clamp01(PlayerPrefs.GetFloat(KayitSes, 1f));
            set
            {
                PlayerPrefs.SetFloat(KayitSes, Mathf.Clamp01(value));
                PlayerPrefs.Save();
                Uygula();
            }
        }

        public static float MuzikSeviyesi
        {
            get => Mathf.Clamp01(PlayerPrefs.GetFloat(KayitMuzik, 1f));
            set
            {
                PlayerPrefs.SetFloat(KayitMuzik, Mathf.Clamp01(value));
                PlayerPrefs.Save();
            }
        }

        public static string Dil
        {
            get
            {
                string d = PlayerPrefs.GetString(KayitDil, "");
                return d == Ingilizce ? Ingilizce : Turkce;
            }
            set
            {
                PlayerPrefs.SetString(KayitDil, value == Ingilizce ? Ingilizce : Turkce);
                PlayerPrefs.Save();
            }
        }

        /// <summary>Açılışta ve ses değişince: genel ses seviyesi.</summary>
        public static void Uygula() => AudioListener.volume = SesSeviyesi;
    }
}
