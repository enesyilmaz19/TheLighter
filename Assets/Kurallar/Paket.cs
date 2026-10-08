using System;
using System.Collections.Generic;

namespace Cakmak.Kurallar
{
    public static class Paket
    {
        /// <summary>
        /// Düz metin paketi okur: her satır bir soru. Boş satırlar ve # ile başlayanlar atlanır.
        /// Dosyayı istemci TextAsset olarak yükler, metni buraya verir.
        /// </summary>
        public static List<string> Ayristir(string metin)
        {
            var sorular = new List<string>();
            if (string.IsNullOrEmpty(metin)) return sorular;

            foreach (var ham in metin.Split('\n'))
            {
                var satir = ham.Trim();
                if (satir.Length == 0 || satir.StartsWith("#", StringComparison.Ordinal)) continue;
                sorular.Add(satir);
            }
            return sorular;
        }
    }

    /// <summary>Karıştırılmış deste. Bitince aynı sorular yeniden karıştırılır.</summary>
    sealed class Deste
    {
        readonly List<string> kaynak = new List<string>();
        readonly List<string> kalan = new List<string>();

        public Deste(IEnumerable<string> sorular)
        {
            foreach (var s in sorular)
                if (!string.IsNullOrWhiteSpace(s)) kaynak.Add(s.Trim());
        }

        public bool Bos => kaynak.Count == 0;

        public string Cek(Random rastgele)
        {
            if (kaynak.Count == 0) return null;
            if (kalan.Count == 0)
            {
                kalan.AddRange(kaynak);
                for (int i = kalan.Count - 1; i > 0; i--)
                {
                    int j = rastgele.Next(i + 1);
                    var t = kalan[i]; kalan[i] = kalan[j]; kalan[j] = t;
                }
            }
            var soru = kalan[kalan.Count - 1];
            kalan.RemoveAt(kalan.Count - 1);
            return soru;
        }
    }
}
