using System.Collections.Generic;
using UnityEngine;

namespace TheLighter.Istemci
{
    /// <summary>
    /// Soru paketlerinin listesi ("Öneri çek" için). Paketler Enes'in: Assets/Icerik/*.txt.
    /// Liste editörde kendiliğinden güncellenir (Editor/PaketListesiGuncelleyici), elle doldurulmaz.
    /// Resources'ta durduğu için içindeki paketler build'e de girer.
    /// </summary>
    public sealed class PaketListesi : ScriptableObject
    {
        public const string ResourcesYolu = "TheLighter/PaketListesi";
        public const string AssetYolu = "Assets/UI/Resources/TheLighter/PaketListesi.asset";
        public const string IcerikKlasoru = "Assets/Icerik";

        public List<TextAsset> Paketler = new List<TextAsset>();
    }
}
