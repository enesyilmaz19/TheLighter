using System.Collections.Generic;
using UnityEngine;

namespace TheLighter.Istemci
{
    /// <summary>
    /// Soru paketlerinin listesi ("Öneri çek", Curcuna boşlukları, Kör Soru) ve sesli ceza kartları.
    /// Paketler Enes'in: Assets/Icerik/*.txt, sesli cezalar Assets/Icerik/Cezalar.csv.
    /// Liste editörde kendiliğinden güncellenir (Editor/PaketListesiGuncelleyici), elle doldurulmaz.
    /// Resources'ta durduğu için içindekiler build'e de girer.
    /// </summary>
    public sealed class PaketListesi : ScriptableObject
    {
        public const string ResourcesYolu = "TheLighter/PaketListesi";
        public const string AssetYolu = "Assets/UI/Resources/TheLighter/PaketListesi.asset";
        public const string IcerikKlasoru = "Assets/Icerik";
        public const string SesliCezaYolu = "Assets/Icerik/Cezalar.csv";
        public const string GeceSorulariYolu = "Assets/Icerik/GeceSorulari.csv";

        public List<TextAsset> Paketler = new List<TextAsset>();

        /// <summary>🗣️ sesli ceza metinleri (Cesur seviyede çekilir).</summary>
        public TextAsset SesliCezalar;

        /// <summary>Çakmak Gecesi'nin suçlama soruları: paket değil, sadece bu oyunda öneri olarak.</summary>
        public TextAsset GeceSorulari;
    }

    /// <summary>Bir soru paketi: dosya adı (kimlik), görünen ad ("# Paket: Aile Dostu") ve soruları.</summary>
    public sealed class PaketBilgisi
    {
        public const string Genel = "Genel";
        public const string AileDostu = "AileDostu";
        /// <summary>Grubun kendi paketi (Curcuna soruları + ifşa olanlar). Dosya değil, grup kaydından gelir.</summary>
        public const string Bizim = "@bizim";

        public string Ad;
        public string GorunenAd;
        public List<string> Sorular = new List<string>();

        /// <summary>Paketin ilk satırındaki "# Paket: X" başlığı; yoksa dosya adı.</summary>
        public static string BaslikBul(string metin, string yedek)
        {
            if (string.IsNullOrEmpty(metin)) return yedek;
            foreach (var ham in metin.Split('\n'))
            {
                var satir = ham.Trim().TrimStart('﻿');
                if (satir.Length == 0) continue;
                const string On = "# Paket:";
                if (satir.StartsWith(On)) return satir.Substring(On.Length).Trim();
                break;
            }
            return yedek;
        }
    }
}
