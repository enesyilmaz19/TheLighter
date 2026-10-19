using System;
using System.IO;
using UnityEngine;

namespace TheLighter.Istemci
{
    /// <summary>
    /// Telefonun paylaşma menüsü (Android <c>ACTION_SEND</c>): paylaşım kartı Instagram, WhatsApp... ile gider.
    /// Kendi küçük Java köprümüz: Assets/Plugins/Android/Paylasim.java (menüyü açar) ve PaylasimSaglayici.java
    /// (resmi salt okunur veren ContentProvider; manifeste Editor/AndroidPaylasimManifesti.cs ekler).
    /// PC'de yok: orada kart Resimler klasörüne kaydedilir.
    /// </summary>
    public static class Paylasim
    {
        /// <summary>Bu cihazda paylaşma menüsü var mı (Android build).</summary>
        public static bool Var => Application.platform == RuntimePlatform.Android;

        /// <summary>
        /// Resmi (PNG) ve metni paylaşma menüsüyle paylaşır. Menü açıldıysa true; kaynak dosya kopyalandıktan sonra silinir
        /// (kartlar telefonda birikmesin). Android değilse ya da bir şey ters giderse false.
        /// </summary>
        public static bool ResimPaylas(string yol, string metin, string baslik)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                bool tamam;
                using (var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var aktivite = unity.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var kopru = new AndroidJavaClass("com.thelighter.paylasim.Paylasim"))
                    tamam = kopru.CallStatic<bool>("resimPaylas", aktivite, yol, metin ?? "", baslik ?? "");
                if (tamam)
                {
                    try { File.Delete(yol); }
                    catch (Exception) { /* silinemezse de paylaşım gitti */ }
                }
                return tamam;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[TheLighter] Paylaşılamadı (" + e.GetType().Name + ")");
                return false;
            }
#else
            return false;
#endif
        }
    }
}
