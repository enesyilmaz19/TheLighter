using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Cakmak.Kurallar
{
    /// <summary>
    /// Temiz mod küfür filtresi (Ana Plan 6.4): soru ve sohbet. Yakalanan metin gönderilmez.
    /// Kelimeye başından bakar: kara listedeki bir kökle başlıyorsa ya da bir kısaltmanın kendisiyse yakalar,
    /// izin listesi kurtarır ("götürmek", "sikke", "şikayet", "sıkıldım"). Kaçamak yazım sadeleşir:
    /// s1k, $ik, a.m.k, s i k, sıııık, ŞİK, tam genişlikli harfler, görünmez karakterler.
    /// ponytail: kelime başı eşleşmesi; bitişik yazılmış cümle, kelime ortası ve imalı soru yakalanmaz
    /// (Ana Plan 6.4: "Hiçbir filtre bunu yakalamaz"). Rapor + engelleme S6'da.
    /// </summary>
    public static class TemizFiltre
    {
        /// <summary>
        /// Bunlarla başlayan kelime yakalanır. k ile biten kökün ğ'lisi de var: ek alınca kök değişir ("yarrağı").
        /// </summary>
        static readonly string[] kokler = Hazirla(true,
            "sik", "orospu", "orosbu", "yarrak", "yarrağ", "dalyarak", "dalyarağ", "amcık", "amcığ", "amına", "amını",
            "göt", "piç", "pezevenk", "pezeveng", "kahpe", "kaltak", "kaltağ", "gavat", "ibne", "puşt", "şerefsiz",
            "yavşak", "yavşağ", "taşak", "taşağ", "sürtük", "sürtüğ", "kevaşe", "boktan",
            "fuck", "motherfuck", "bitch", "cunt", "whore", "shit", "asshole");

        /// <summary>Sadece kelimenin kendisiyse yakalanır: başka kelimelerin başında çok geçer ("am" → "amaç", "nigga" → "Nigar").</summary>
        static readonly string[] kisaltmalar = Hazirla(true,
            "am", "amk", "amq", "aq", "mk", "mq", "oç", "sg", "bok", "dick", "pussy", "nigga", "niggas", "nigger", "niggers");

        /// <summary>
        /// Bir kökle başlayan masum kelimeler, kelime başı eşleşir. ı korunarak karşılaştırılır: "sık" (ve "şık") masum,
        /// "sik" değil. Türkçe klavyesiz yazılanlar da var ("sikildim" = sıkıldım).
        /// </summary>
        static readonly string[] izinli = Hazirla(false,
            "sık", "sikil", "sikinti", "sikici", "sikist", "sikca", "sikke", "şikayet", "götür", "gotik");

        public static bool Uygunsuz(string metin)
        {
            foreach (var kelime in Kelimeler(Hafif(metin)))
            {
                var agir = Agir(kelime);
                if (Array.IndexOf(kisaltmalar, agir) < 0 && !Basliyor(agir, kokler)) continue;
                if (!Basliyor(kelime, izinli)) return true;
            }
            return false;
        }

        /// <summary>
        /// Görünmez karakterler atılır, NFKD ile ayrışır ve işaretler düşer (ş→s, ö→o, â→a, İ→I, tam genişlikli harf → düz),
        /// küçük harf (kültürsüz: I → i, Türkçedeki gibi ı değil), rakam/işaret → harf. Küçük ı korunur: ayrışmaz,
        /// izin listesi ona bakar.
        /// </summary>
        static string Hafif(string metin)
        {
            var s = TemizMetin.Temizle(metin).Normalize(NormalizationForm.FormKD).ToLowerInvariant();
            var sb = new StringBuilder(s.Length);
            foreach (var c in s)
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(Harf(c));
            return sb.ToString();
        }

        static char Harf(char c)
        {
            switch (c)
            {
                case '1': return 'i';
                case '3': return 'e';
                case '4': case '@': return 'a';
                case '0': return 'o';
                case '5': case '$': return 's';
                default: return c;
            }
        }

        /// <summary>Kara liste için: ı da i olur, tekrarlar teke iner ("sıııık" → "sik", "yarrak" → "yarak").</summary>
        static string Agir(string hafif)
        {
            var sb = new StringBuilder(hafif.Length);
            foreach (var x in hafif)
            {
                var c = x == 'ı' ? 'i' : x;
                if (sb.Length == 0 || sb[sb.Length - 1] != c) sb.Append(c);
            }
            return sb.ToString();
        }

        /// <summary>Harf dizileri. Arka arkaya tek harflik parçalar birleşir: "s i k", "a.m.k" → "sik", "amk".</summary>
        static List<string> Kelimeler(string hafif)
        {
            var kelimeler = new List<string>();
            var tekler = new StringBuilder();
            int i = 0;
            while (i < hafif.Length)
            {
                if (!char.IsLetter(hafif[i])) { i++; continue; }
                int bas = i;
                while (i < hafif.Length && char.IsLetter(hafif[i])) i++;
                if (i - bas == 1) { tekler.Append(hafif[bas]); continue; }
                if (tekler.Length > 0) { kelimeler.Add(tekler.ToString()); tekler.Clear(); }
                kelimeler.Add(hafif.Substring(bas, i - bas));
            }
            if (tekler.Length > 0) kelimeler.Add(tekler.ToString());
            return kelimeler;
        }

        static bool Basliyor(string kelime, string[] onekler)
        {
            foreach (var o in onekler)
                if (kelime.StartsWith(o, StringComparison.Ordinal)) return true;
            return false;
        }

        /// <summary>Listeler metinle aynı yoldan geçer: "piç" ile "pic", "yarrak" ile "yarak" aynı yazılır.</summary>
        static string[] Hazirla(bool agir, params string[] kelimeler)
        {
            var sonuc = new string[kelimeler.Length];
            for (int i = 0; i < kelimeler.Length; i++)
                sonuc[i] = agir ? Agir(Hafif(kelimeler[i])) : Hafif(kelimeler[i]);
            return sonuc;
        }
    }
}
