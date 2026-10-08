namespace TheLighter.Akis
{
    /// <summary>
    /// Oyuncu isimlerine Türkçe hal ekleri: "Ayşe'ye", "Mehmet'te", "Burak'ın", "Ali'yi".
    /// Metin tablosundaki Türkçe cümleler bu biçimleri ayrı parametre olarak alır,
    /// ileride İngilizce/Almanca metinler sadece çıplak ismi kullanır.
    /// </summary>
    public static class TurkceEk
    {
        const string KalinUnluler = "aıouAIOU";
        const string InceUnluler = "eiöüEİÖÜ";
        const string YuvarlakUnluler = "oöuüOÖUÜ";
        const string SertUnsuzler = "fstkçşhpFSTKÇŞHP";

        /// <summary>-e hali: Ali'ye, Mehmet'e, Burak'a.</summary>
        public static string Yonelme(string isim)
        {
            isim = Temizle(isim);
            string u = Kalin(isim) ? "a" : "e";
            return isim + "'" + (UnluyleBiter(isim) ? "y" : "") + u;
        }

        /// <summary>-de hali: Ali'de, Mehmet'te, Burak'ta.</summary>
        public static string Bulunma(string isim)
        {
            isim = Temizle(isim);
            string u = Kalin(isim) ? "a" : "e";
            return isim + "'" + (SertUnsuzleBiter(isim) ? "t" : "d") + u;
        }

        /// <summary>-in hali: Ali'nin, Mehmet'in, Burak'ın, Umut'un, Gül'ün.</summary>
        public static string Ilgi(string isim)
        {
            isim = Temizle(isim);
            string u = DortluUnlu(isim);
            return isim + "'" + (UnluyleBiter(isim) ? "n" + u + "n" : u + "n");
        }

        /// <summary>-i hali: Ali'yi, Mehmet'i, Burak'ı.</summary>
        public static string Belirtme(string isim)
        {
            isim = Temizle(isim);
            string u = DortluUnlu(isim);
            return isim + "'" + (UnluyleBiter(isim) ? "y" : "") + u;
        }

        static string Temizle(string isim) => (isim ?? "").Trim();

        static char SonUnlu(string s)
        {
            for (int i = s.Length - 1; i >= 0; i--)
            {
                char c = s[i];
                if (KalinUnluler.IndexOf(c) >= 0 || InceUnluler.IndexOf(c) >= 0) return c;
            }
            return 'e'; // ünlüsü olmayan isimler (ör. "XY") ince sayılır
        }

        static char SonHarf(string s)
        {
            for (int i = s.Length - 1; i >= 0; i--)
                if (char.IsLetter(s[i])) return s[i];
            return '\0';
        }

        static bool Kalin(string s) => KalinUnluler.IndexOf(SonUnlu(s)) >= 0;

        static bool UnluyleBiter(string s)
        {
            char c = SonHarf(s);
            return c != '\0' && (KalinUnluler.IndexOf(c) >= 0 || InceUnluler.IndexOf(c) >= 0);
        }

        static bool SertUnsuzleBiter(string s)
        {
            char c = SonHarf(s);
            return c != '\0' && SertUnsuzler.IndexOf(c) >= 0;
        }

        static string DortluUnlu(string s)
        {
            char u = SonUnlu(s);
            bool kalin = KalinUnluler.IndexOf(u) >= 0;
            bool yuvarlak = YuvarlakUnluler.IndexOf(u) >= 0;
            if (kalin) return yuvarlak ? "u" : "ı";
            return yuvarlak ? "ü" : "i";
        }
    }
}
