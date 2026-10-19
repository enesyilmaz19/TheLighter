#if UNITY_ANDROID
using System.IO;
using System.Xml;
using UnityEditor.Android;
using UnityEngine;

namespace TheLighter.EditorAraclari
{
    /// <summary>
    /// Paylaşım kartının ContentProvider'ını (Assets/Plugins/Android/PaylasimSaglayici.java) Unity'nin ürettiği
    /// unityLibrary manifestine ekler. Özel bir AndroidManifest.xml tutulmuyor: Unity sürümü değişince şablonu da değişir.
    /// Dışa kapalı, sadece paylaşırken tek seferlik okuma izniyle; authorities = paket adı + ".paylasim".
    /// </summary>
    sealed class AndroidPaylasimManifesti : IPostGenerateGradleAndroidProject
    {
        const string AndroidAd = "http://schemas.android.com/apk/res/android";
        const string Sinif = "com.thelighter.paylasim.PaylasimSaglayici";

        public int callbackOrder => 0;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            string yol = Path.Combine(path, "src", "main", "AndroidManifest.xml");
            if (!File.Exists(yol))
            {
                Debug.LogError("[TheLighter] Manifest bulunamadı, paylaşma menüsü çalışmaz: " + yol);
                return;
            }
            var xml = new XmlDocument();
            xml.Load(yol);
            var manifest = xml.DocumentElement;
            if (manifest == null) return;
            var uygulama = manifest.SelectSingleNode("application") as XmlElement;
            if (uygulama == null)
            {
                uygulama = xml.CreateElement("application");
                manifest.AppendChild(uygulama);
            }
            foreach (XmlNode n in uygulama.ChildNodes)
                if (n is XmlElement e && e.Name == "provider" && e.GetAttribute("name", AndroidAd) == Sinif) return;

            var saglayici = xml.CreateElement("provider");
            Ozellik(xml, saglayici, "name", Sinif);
            Ozellik(xml, saglayici, "authorities", "${applicationId}.paylasim");
            Ozellik(xml, saglayici, "exported", "false");
            Ozellik(xml, saglayici, "grantUriPermissions", "true");
            uygulama.AppendChild(saglayici);
            xml.Save(yol);
        }

        /// <summary>android:ad="değer" (önek açıkça "android").</summary>
        static void Ozellik(XmlDocument xml, XmlElement e, string ad, string deger)
        {
            var a = xml.CreateAttribute("android", ad, AndroidAd);
            a.Value = deger;
            e.Attributes.Append(a);
        }
    }
}
#endif
