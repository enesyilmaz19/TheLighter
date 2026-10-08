using System.Collections.Generic;
using System.Text;

namespace TheLighter.Akis
{
    /// <summary>
    /// Oyuncuya görünen metinlerin tablosu. Unity Localization'ın CSV biçimini okur:
    /// ilk satır başlık, "Key" sütunu anahtar, "Turkish(tr)" sütunu metin.
    /// Aynı dosya ileride Localization'a (Window → Asset Management → Localization Tables → Import CSV)
    /// aktarılıp String Table'a çevrilebilir.
    /// </summary>
    public sealed class MetinTablosu
    {
        readonly Dictionary<string, string> metinler = new Dictionary<string, string>();

        public int Sayi => metinler.Count;

        public static MetinTablosu CsvOku(string csv, string dilSutunu = "Turkish(tr)")
        {
            var t = new MetinTablosu();
            var satirlar = Satirlar(csv ?? "");
            if (satirlar.Count == 0) return t;

            var baslik = satirlar[0];
            int anahtarSutunu = baslik.IndexOf("Key");
            int metinSutunu = baslik.IndexOf(dilSutunu);
            if (anahtarSutunu < 0 || metinSutunu < 0) return t;

            for (int i = 1; i < satirlar.Count; i++)
            {
                var s = satirlar[i];
                if (s.Count <= anahtarSutunu) continue;
                string anahtar = s[anahtarSutunu].Trim();
                if (anahtar.Length == 0 || anahtar.StartsWith("#")) continue;
                t.metinler[anahtar] = s.Count > metinSutunu ? s[metinSutunu] : "";
            }
            return t;
        }

        public bool Var(string anahtar) => metinler.ContainsKey(anahtar);

        /// <summary>Anahtar yoksa "[anahtar]" döner, ekranda eksik metin hemen görünür.</summary>
        public string Al(string anahtar, params object[] parametreler)
        {
            string m;
            if (!metinler.TryGetValue(anahtar, out m)) return "[" + anahtar + "]";
            if (parametreler == null || parametreler.Length == 0) return m;
            try
            {
                return string.Format(System.Globalization.CultureInfo.InvariantCulture, m, parametreler);
            }
            catch (System.FormatException)
            {
                return m;
            }
        }

        /// <summary>RFC 4180 CSV: tırnaklı alan, tırnak içinde virgül, satır sonu ve "" kaçışı.</summary>
        static List<List<string>> Satirlar(string csv)
        {
            var sonuc = new List<List<string>>();
            var satir = new List<string>();
            var alan = new StringBuilder();
            bool tirnakta = false;
            bool alanVar = false;

            for (int i = 0; i < csv.Length; i++)
            {
                char c = csv[i];
                if (tirnakta)
                {
                    if (c == '"')
                    {
                        if (i + 1 < csv.Length && csv[i + 1] == '"') { alan.Append('"'); i++; }
                        else tirnakta = false;
                    }
                    else alan.Append(c);
                    continue;
                }

                switch (c)
                {
                    case '"':
                        tirnakta = true;
                        alanVar = true;
                        break;
                    case ',':
                        satir.Add(alan.ToString());
                        alan.Clear();
                        alanVar = true;
                        break;
                    case '\r':
                        break;
                    case '\n':
                        if (alanVar || alan.Length > 0) satir.Add(alan.ToString());
                        if (satir.Count > 0) sonuc.Add(satir);
                        satir = new List<string>();
                        alan.Clear();
                        alanVar = false;
                        break;
                    case '﻿':
                        break; // BOM
                    default:
                        alan.Append(c);
                        alanVar = true;
                        break;
                }
            }

            if (alanVar || alan.Length > 0) satir.Add(alan.ToString());
            if (satir.Count > 0) sonuc.Add(satir);
            return sonuc;
        }
    }
}
