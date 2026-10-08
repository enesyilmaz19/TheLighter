using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TheLighter.Istemci.Duzenleyici
{
    /// <summary>
    /// Assets/Icerik altındaki .txt paketlerini <see cref="PaketListesi"/>'ne yazar.
    /// Editör açılınca ve Icerik'e paket eklenince, silinince ya da taşınınca kendiliğinden çalışır.
    /// Enes'in klasörüne dokunmaz, sadece okur. Elle çalıştırmak için: menü → TheLighter → Soru paketlerini güncelle.
    /// </summary>
    sealed class PaketListesiGuncelleyici : AssetPostprocessor
    {
        static bool planlandi;

        static void OnPostprocessAllAssets(string[] eklenen, string[] silinen, string[] tasinan, string[] tasinanEski)
        {
            if (Ilgili(eklenen) || Ilgili(silinen) || Ilgili(tasinan) || Ilgili(tasinanEski)) Planla();
        }

        [InitializeOnLoadMethod]
        static void EditorAcildi() => Planla();

        /// <summary>İçe aktarma sırasında asset yazılmaz: bir sonraki editör karesine bırakılır.</summary>
        static void Planla()
        {
            if (planlandi) return;
            planlandi = true;
            EditorApplication.delayCall += () =>
            {
                planlandi = false;
                Guncelle();
            };
        }

        static bool Ilgili(string[] yollar)
        {
            foreach (var y in yollar)
                if (y.StartsWith(PaketListesi.IcerikKlasoru + "/") && y.EndsWith(".txt")) return true;
            return false;
        }

        [MenuItem("TheLighter/Soru paketlerini güncelle")]
        static void Guncelle()
        {
            var paketler = new List<TextAsset>();
            if (AssetDatabase.IsValidFolder(PaketListesi.IcerikKlasoru))
            {
                foreach (var guid in AssetDatabase.FindAssets("t:TextAsset", new[] { PaketListesi.IcerikKlasoru }))
                {
                    var yol = AssetDatabase.GUIDToAssetPath(guid);
                    if (!yol.EndsWith(".txt")) continue;
                    var paket = AssetDatabase.LoadAssetAtPath<TextAsset>(yol);
                    if (paket != null) paketler.Add(paket);
                }
            }
            paketler.Sort((x, y) => string.CompareOrdinal(x.name, y.name));

            var liste = AssetDatabase.LoadAssetAtPath<PaketListesi>(PaketListesi.AssetYolu);
            if (liste == null)
            {
                liste = ScriptableObject.CreateInstance<PaketListesi>();
                liste.Paketler = paketler;
                AssetDatabase.CreateAsset(liste, PaketListesi.AssetYolu);
            }
            else
            {
                if (Ayni(liste.Paketler, paketler)) return; // değişmediyse dosyaya yazma, git'te boşuna değişiklik çıkmasın
                liste.Paketler = paketler;
                EditorUtility.SetDirty(liste);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[TheLighter] Soru paketleri güncellendi: " + paketler.Count + " paket");
        }

        static bool Ayni(List<TextAsset> x, List<TextAsset> y)
        {
            if (x == null || x.Count != y.Count) return false;
            for (int i = 0; i < x.Count; i++)
                if (x[i] != y[i]) return false;
            return true;
        }
    }
}
