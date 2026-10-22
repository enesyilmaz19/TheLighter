using System;
using System.Collections.Generic;
using System.IO;
using TheLighter.Istemci;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace TheLighter.EditorAraclari
{
    /// <summary>
    /// Android build ayarları ve build menüsü (tasks/g2-android.md 1): Menü → TheLighter → Android.
    /// <list type="bullet">
    /// <item>Ayarlar: paket adı (<see cref="Baglantilar.PaketAdi"/>, yer tutucu), sadece dikey, IL2CPP + ARM64, en az API 26,
    /// düz HTTP/ws:// sadece geliştirme build'inde. Her build'den önce yeniden uygulanır.</item>
    /// <item>APK (geliştirme): telefonda denemek için, hata ayıklama anahtarıyla imzalı; "Sunucu" alanı görünür.</item>
    /// <item>AAB (Play): yükleme anahtarıyla imzalı. Anahtar ve şifreleri repoda değil: proje kökündeki
    /// <c>Android.local.json</c>'da (git yok sayar, <c>*.local.json</c>). Anahtar dosyası da repo dışında, yedekli.</item>
    /// </list>
    /// Play'e her yüklemede sürüm kodu +1 ("Sürüm kodunu artır").
    /// </summary>
    public static class AndroidBuild
    {
        const string Menu = "TheLighter/Android/";
        const string CiktiKlasoru = "Build/Android";
        const string YerelDosya = "Android.local.json";
        /// <summary>Yer tutucu uyarlanabilir ikon (arka + ön katman). Görsel kararlar (docs/Gorsel.md) gelince değişir.</summary>
        const string IkonArka = "Assets/UI/Ikon/ikon-arka.png";
        const string IkonOn = "Assets/UI/Ikon/ikon-on.png";

        /// <summary>Android.local.json: yükleme anahtarının yeri ve şifreleri. Repoya girmez.</summary>
        [Serializable]
        sealed class YerelAnahtar
        {
            public string keystore;
            public string keystoreSifre;
            public string alias;
            public string aliasSifre;
        }

        [MenuItem(Menu + "Ayarları uygula")]
        public static void AyarlariUygula()
        {
            var android = NamedBuildTarget.Android;
            PlayerSettings.SetApplicationIdentifier(android, Baglantilar.PaketAdi);
            // Sadece dikey: açılışta yan dönüp düzelmesin.
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
            // Play 64 bit istiyor.
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            if (PlayerSettings.Android.minSdkVersion < AndroidSdkVersions.AndroidApiLevel26)
                PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            // Yerel sunucu (ws://) sadece geliştirme build'inde; yayın hep wss://.
            PlayerSettings.insecureHttpOption = InsecureHttpOption.DevelopmentOnly;
            IkonuKur();
            AssetDatabase.SaveAssets();
            Debug.Log("[TheLighter] Android ayarları uygulandı: " + Baglantilar.PaketAdi + ", dikey, IL2CPP ARM64, API "
                      + (int)PlayerSettings.Android.minSdkVersion + "+, sürüm " + PlayerSettings.bundleVersion
                      + " (" + PlayerSettings.Android.bundleVersionCode + ")");
        }

        /// <summary>
        /// Android uyarlanabilir ikonu iki katman ister (arka + ön; API 26+, en az sürüm de 26). Platform Android değilken
        /// Android'e özel ikon API'si derlenmez: o zaman bir şey yapmaz, build zaten önce Android'e geçirir.
        /// </summary>
        static void IkonuKur()
        {
#if UNITY_ANDROID
            var arka = AssetDatabase.LoadAssetAtPath<Texture2D>(IkonArka);
            var on = AssetDatabase.LoadAssetAtPath<Texture2D>(IkonOn);
            if (arka == null || on == null)
            {
                Debug.LogWarning("[TheLighter] İkon katmanları bulunamadı: " + IkonArka + ", " + IkonOn);
                return;
            }
            var tur = UnityEditor.Android.AndroidPlatformIconKind.Adaptive;
            var ikonlar = PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, tur);
            foreach (var ikon in ikonlar) ikon.SetTextures(arka, on);
            PlayerSettings.SetPlatformIcons(NamedBuildTarget.Android, tur, ikonlar);
#endif
        }

        [MenuItem(Menu + "Sürüm kodunu artır (Play'e yüklemeden önce)")]
        public static void SurumKodunuArtir()
        {
            PlayerSettings.Android.bundleVersionCode++;
            AssetDatabase.SaveAssets();
            Debug.Log("[TheLighter] Android sürüm kodu: " + PlayerSettings.Android.bundleVersionCode);
        }

        [MenuItem(Menu + "APK yap (geliştirme, deneme)")]
        public static void ApkGelistirme() => Yap(aab: false, gelistirme: true, calistir: false);

        [MenuItem(Menu + "APK yap ve USB'deki telefona kur (geliştirme)")]
        public static void ApkGelistirmeKur() => Yap(aab: false, gelistirme: true, calistir: true);

        [MenuItem(Menu + "AAB yap (Play, imzalı)")]
        public static void AabYayin() => Yap(aab: true, gelistirme: false, calistir: false);

        static void Yap(bool aab, bool gelistirme, bool calistir)
        {
            // Önce platform Android olmalı: manifest eklentisi (AndroidPaylasimManifesti) sadece Android'deyken derlenir.
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                if (!EditorUtility.DisplayDialog("Platform Android değil",
                        "Build'den önce platform Android'e geçmeli (biraz sürer). Geçince bu menüyü bir daha seç.", "Android'e geç", "Vazgeç"))
                    return;
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
                return;
            }
            // Platform yeni değiştiyse Unity bir süre derler; o sırada build "scripts are compiling" diye düşer.
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorUtility.DisplayDialog("Unity derliyor", "Derleme bitince (sağ alttaki çubuk) bu menüyü bir daha seç.", "Tamam");
                return;
            }
            AyarlariUygula();

            YerelAnahtar anahtar = null;
            if (aab)
            {
                anahtar = AnahtarOku(out string neden);
                if (anahtar == null)
                {
                    EditorUtility.DisplayDialog("AAB yapılamadı", neden, "Tamam");
                    return;
                }
            }

            var sahneler = new List<string>();
            foreach (var s in EditorBuildSettings.scenes)
                if (s.enabled) sahneler.Add(s.path);
            if (sahneler.Count == 0) sahneler.Add("Assets/Scenes/SampleScene.unity");

            Directory.CreateDirectory(CiktiKlasoru);
            string ad = aab ? "cakmak-" + PlayerSettings.bundleVersion + "-" + PlayerSettings.Android.bundleVersionCode + ".aab"
                            : "cakmak-gelistirme.apk";
            var secenekler = new BuildPlayerOptions
            {
                scenes = sahneler.ToArray(),
                locationPathName = Path.Combine(CiktiKlasoru, ad),
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = (gelistirme ? BuildOptions.Development : BuildOptions.None) | (calistir ? BuildOptions.AutoRunPlayer : BuildOptions.None),
            };

            bool eskiAab = EditorUserBuildSettings.buildAppBundle;
            bool eskiOzel = PlayerSettings.Android.useCustomKeystore;
            string eskiAnahtar = PlayerSettings.Android.keystoreName;
            string eskiAlias = PlayerSettings.Android.keyaliasName;
            try
            {
                EditorUserBuildSettings.buildAppBundle = aab;
                if (anahtar != null)
                {
                    PlayerSettings.Android.useCustomKeystore = true;
                    PlayerSettings.Android.keystoreName = anahtar.keystore;
                    PlayerSettings.Android.keystorePass = anahtar.keystoreSifre;
                    PlayerSettings.Android.keyaliasName = anahtar.alias;
                    PlayerSettings.Android.keyaliasPass = anahtar.aliasSifre;
                }
                else PlayerSettings.Android.useCustomKeystore = false;

                var rapor = BuildPipeline.BuildPlayer(secenekler);
                if (rapor.summary.result == BuildResult.Succeeded)
                {
                    Debug.Log("[TheLighter] Build hazır: " + secenekler.locationPathName + " (" + rapor.summary.totalSize / (1024 * 1024) + " MB)");
                    if (!calistir) EditorUtility.RevealInFinder(secenekler.locationPathName);
                }
                else Debug.LogError("[TheLighter] Build olmadı: " + rapor.summary.result + " (" + rapor.summary.totalErrors + " hata, Console'a bak)");
            }
            finally
            {
                // Anahtarın yeri ve şifreleri proje ayarlarında kalmasın (ProjectSettings repoda).
                EditorUserBuildSettings.buildAppBundle = eskiAab;
                PlayerSettings.Android.useCustomKeystore = eskiOzel;
                PlayerSettings.Android.keystoreName = eskiAnahtar;
                PlayerSettings.Android.keyaliasName = eskiAlias;
                PlayerSettings.Android.keystorePass = "";
                PlayerSettings.Android.keyaliasPass = "";
            }
        }

        /// <summary>Android.local.json'u okur. Yoksa ya da eksikse null ve ne yapılacağı.</summary>
        static YerelAnahtar AnahtarOku(out string neden)
        {
            string yol = Path.GetFullPath(Path.Combine(Application.dataPath, "..", YerelDosya));
            if (!File.Exists(yol))
            {
                neden = "Yükleme anahtarı ayarlı değil. Proje kökünde " + YerelDosya + " oluştur (git yok sayar):\n\n"
                        + "{ \"keystore\": \"C:/.../cakmak-yukleme.keystore\", \"keystoreSifre\": \"...\", \"alias\": \"cakmak\", \"aliasSifre\": \"...\" }\n\n"
                        + "Anahtarı Player Settings → Publishing Settings → Keystore Manager ile oluştur; dosyayı repo dışında, ikinizin de "
                        + "ulaşabileceği yedekli bir yerde tut.";
                return null;
            }
            YerelAnahtar a;
            try { a = JsonUtility.FromJson<YerelAnahtar>(File.ReadAllText(yol)); }
            catch (Exception) { a = null; }
            if (a == null || string.IsNullOrEmpty(a.keystore) || string.IsNullOrEmpty(a.alias)
                || string.IsNullOrEmpty(a.keystoreSifre) || string.IsNullOrEmpty(a.aliasSifre))
            {
                neden = YerelDosya + " eksik ya da bozuk: keystore, keystoreSifre, alias, aliasSifre alanları dolu olmalı.";
                return null;
            }
            if (!File.Exists(a.keystore))
            {
                neden = "Anahtar dosyası bulunamadı: " + a.keystore;
                return null;
            }
            neden = null;
            return a;
        }
    }
}
