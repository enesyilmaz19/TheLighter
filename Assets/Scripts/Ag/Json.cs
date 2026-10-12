using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace TheLighter.Ag
{
    public enum JsonTuru { Yok, Mantik, Sayi, Metin, Dizi, Nesne }

    /// <summary>JSON bozuk. Sadece <see cref="JsonDeger.Coz"/> atar; çevirici yakalayıp mesajı yok sayar.</summary>
    public sealed class JsonHatasi : Exception
    {
        public JsonHatasi(string neden) : base(neden) { }
    }

    /// <summary>
    /// Küçük, bağımlılıksız JSON okuyucu (sunucu mesajları için yeterli).
    /// Olmayan alan ya da <c>null</c> <see cref="Yok"/> döner, hiçbir okuma null ya da hata vermez:
    /// eksik/yanlış tipli alanlı bir mesajda çökmemek için.
    /// </summary>
    public sealed class JsonDeger
    {
        public static readonly JsonDeger Yok = new JsonDeger(JsonTuru.Yok);
        static readonly JsonDeger Dogru = new JsonDeger(JsonTuru.Mantik) { mantik = true };
        static readonly JsonDeger Yanlis = new JsonDeger(JsonTuru.Mantik) { mantik = false };

        /// <summary>İç içe en fazla bu kadar dizi/nesne. Kötü niyetli derin bir mesaj yığını taşırmasın.</summary>
        const int EnFazlaDerinlik = 64;

        public readonly JsonTuru Tur;
        bool mantik;
        double sayi;
        string metin;
        List<JsonDeger> dizi;
        Dictionary<string, JsonDeger> nesne;

        JsonDeger(JsonTuru tur) { Tur = tur; }

        public bool Var => Tur != JsonTuru.Yok;

        /// <summary>Nesnenin alanı. Nesne değilse ya da alan yoksa <see cref="Yok"/>.</summary>
        public JsonDeger this[string alan] =>
            Tur == JsonTuru.Nesne && alan != null && nesne.TryGetValue(alan, out var d) ? d : Yok;

        /// <summary>Dizinin elemanı. Dizi değilse ya da sınır dışıysa <see cref="Yok"/>.</summary>
        public JsonDeger this[int i] => Tur == JsonTuru.Dizi && i >= 0 && i < dizi.Count ? dizi[i] : Yok;

        /// <summary>Dizi değilse boş.</summary>
        public IReadOnlyList<JsonDeger> Elemanlar => Tur == JsonTuru.Dizi ? (IReadOnlyList<JsonDeger>)dizi : Array.Empty<JsonDeger>();

        /// <summary>Metin değilse null.</summary>
        public string Metin => Tur == JsonTuru.Metin ? metin : null;

        /// <summary>Tamsayı değilse (ondalıklı, aralık dışı ya da sayı değil) null.</summary>
        public int? Tamsayi =>
            Tur == JsonTuru.Sayi && sayi == Math.Floor(sayi) && sayi >= int.MinValue && sayi <= int.MaxValue ? (int)sayi : (int?)null;

        /// <summary>true/false değilse null.</summary>
        public bool? Mantik => Tur == JsonTuru.Mantik ? mantik : (bool?)null;

        public int TamsayiYa(int yedek) => Tamsayi ?? yedek;
        public bool MantikYa(bool yedek) => Mantik ?? yedek;

        public static JsonDeger Coz(string json)
        {
            if (json == null) throw new JsonHatasi("boş");
            var o = new Okuyucu(json);
            o.Bosluk();
            var d = o.Deger(0);
            o.Bosluk();
            if (!o.Bitti) throw new JsonHatasi("fazladan karakter");
            return d;
        }

        public static bool Dene(string json, out JsonDeger deger)
        {
            try
            {
                deger = Coz(json);
                return true;
            }
            catch (JsonHatasi)
            {
                deger = Yok;
                return false;
            }
        }

        sealed class Okuyucu
        {
            readonly string s;
            int i;

            public Okuyucu(string s) { this.s = s; }

            public bool Bitti => i >= s.Length;

            public void Bosluk()
            {
                while (i < s.Length && (s[i] == ' ' || s[i] == '\t' || s[i] == '\n' || s[i] == '\r')) i++;
            }

            char Bak() => i < s.Length ? s[i] : throw new JsonHatasi("beklenmedik son");

            void Bekle(char c)
            {
                if (Bak() != c) throw new JsonHatasi("'" + c + "' bekleniyordu");
                i++;
            }

            void Kelime(string k)
            {
                if (string.CompareOrdinal(s, i, k, 0, k.Length) != 0) throw new JsonHatasi("bilinmeyen değer");
                i += k.Length;
            }

            public JsonDeger Deger(int derinlik)
            {
                if (derinlik > EnFazlaDerinlik) throw new JsonHatasi("çok derin");
                switch (Bak())
                {
                    case '{': return NesneOku(derinlik);
                    case '[': return DiziOku(derinlik);
                    case '"': return new JsonDeger(JsonTuru.Metin) { metin = MetinOku() };
                    case 't': Kelime("true"); return Dogru;
                    case 'f': Kelime("false"); return Yanlis;
                    case 'n': Kelime("null"); return Yok;
                    default: return SayiOku();
                }
            }

            JsonDeger NesneOku(int derinlik)
            {
                Bekle('{');
                var d = new JsonDeger(JsonTuru.Nesne) { nesne = new Dictionary<string, JsonDeger>() };
                Bosluk();
                if (Bak() == '}') { i++; return d; }
                while (true)
                {
                    Bosluk();
                    string ad = MetinOku();
                    Bosluk();
                    Bekle(':');
                    Bosluk();
                    d.nesne[ad] = Deger(derinlik + 1); // aynı alan iki kez: sonuncusu geçerli
                    Bosluk();
                    if (Bak() == ',') { i++; continue; }
                    Bekle('}');
                    return d;
                }
            }

            JsonDeger DiziOku(int derinlik)
            {
                Bekle('[');
                var d = new JsonDeger(JsonTuru.Dizi) { dizi = new List<JsonDeger>() };
                Bosluk();
                if (Bak() == ']') { i++; return d; }
                while (true)
                {
                    Bosluk();
                    d.dizi.Add(Deger(derinlik + 1));
                    Bosluk();
                    if (Bak() == ',') { i++; continue; }
                    Bekle(']');
                    return d;
                }
            }

            string MetinOku()
            {
                Bekle('"');
                var sb = new StringBuilder();
                while (true)
                {
                    char c = Bak();
                    i++;
                    if (c == '"') return sb.ToString();
                    if (c < 0x20) throw new JsonHatasi("metinde kontrol karakteri");
                    if (c != '\\') { sb.Append(c); continue; }
                    char e = Bak();
                    i++;
                    switch (e)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'u':
                            if (i + 4 > s.Length) throw new JsonHatasi("eksik \\u");
                            if (!int.TryParse(s.Substring(i, 4), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out int kod))
                                throw new JsonHatasi("bozuk \\u");
                            sb.Append((char)kod); // vekil çiftler iki \u olarak gelir, sırayla eklenince doğru birleşir
                            i += 4;
                            break;
                        default: throw new JsonHatasi("bilinmeyen kaçış");
                    }
                }
            }

            JsonDeger SayiOku()
            {
                int bas = i;
                if (i < s.Length && s[i] == '-') i++;
                while (i < s.Length && (char.IsDigit(s[i]) || s[i] == '.' || s[i] == 'e' || s[i] == 'E' || s[i] == '+' || s[i] == '-')) i++;
                if (i == bas || !double.TryParse(s.Substring(bas, i - bas), NumberStyles.Float, CultureInfo.InvariantCulture, out double d)
                    || double.IsNaN(d) || double.IsInfinity(d))
                    throw new JsonHatasi("bozuk sayı");
                return new JsonDeger(JsonTuru.Sayi) { sayi = d };
            }
        }
    }

    /// <summary>
    /// Giden mesajlar için küçük JSON yazıcı. Alan sırası korunur.
    /// Değer: null, string, bool, int, <see cref="JsonNesne"/>, string listesi.
    /// </summary>
    public sealed class JsonNesne
    {
        readonly List<KeyValuePair<string, object>> alanlar = new List<KeyValuePair<string, object>>();

        public JsonNesne Ekle(string ad, object deger)
        {
            alanlar.Add(new KeyValuePair<string, object>(ad, deger));
            return this;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            Yaz(sb, this);
            return sb.ToString();
        }

        static void Yaz(StringBuilder sb, object d)
        {
            switch (d)
            {
                case null: sb.Append("null"); break;
                case string s: MetinYaz(sb, s); break;
                case bool b: sb.Append(b ? "true" : "false"); break;
                case int n: sb.Append(n.ToString(CultureInfo.InvariantCulture)); break;
                case JsonNesne o:
                    sb.Append('{');
                    for (int k = 0; k < o.alanlar.Count; k++)
                    {
                        if (k > 0) sb.Append(',');
                        MetinYaz(sb, o.alanlar[k].Key);
                        sb.Append(':');
                        Yaz(sb, o.alanlar[k].Value);
                    }
                    sb.Append('}');
                    break;
                case IEnumerable<string> liste:
                    sb.Append('[');
                    bool ilk = true;
                    foreach (var x in liste)
                    {
                        if (!ilk) sb.Append(',');
                        ilk = false;
                        MetinYaz(sb, x);
                    }
                    sb.Append(']');
                    break;
                default: throw new ArgumentException("JSON'a yazılamayan tip: " + d.GetType().Name);
            }
        }

        static void MetinYaz(StringBuilder sb, string s)
        {
            sb.Append('"');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
        }
    }
}
