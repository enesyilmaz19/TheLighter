using System;

namespace TheLighter.Ag
{
    /// <summary>
    /// Online sunucunun adresi (karar 29, tasks/g1-todo.md): yayın adresi tek sabitte, oyuncu adres girmez.
    /// "Sunucu" alanı sadece editörde ve geliştirme build'inde: orada yerel sunucu (ya da bilgisayarın yerel IP'si)
    /// denenebilsin. Adres gizli değil (build'de durur), ama IP değil alan adı yazılır.
    /// </summary>
    public static class SunucuAdresi
    {
        /// <summary>
        /// Yayın adresi: VPS ve alan adı gelince <c>wss://&lt;alan-adı&gt;/ws</c>. Boşken yayın build'inde online yok
        /// (menü "sunucu hazır değil" der). Yayın hep wss:// (HTTPS'i Caddy veriyor), düz ws:// değil.
        /// </summary>
        public const string Yayin = "";

        /// <summary>Yerel deneme (Sunucu/Cakmak.Sunucu, dotnet run). Telefondan: ws://&lt;bilgisayarın IP'si&gt;:5000/ws.</summary>
        public const string Yerel = "ws://localhost:5000/ws";

        /// <summary>"Sunucu" alanı görünür mü: sadece editörde ve geliştirme build'inde.</summary>
        public static bool AlanGorunur(bool gelistirme) => gelistirme;

        /// <summary>
        /// Bağlanılacak adres. Yayın build'inde hep <see cref="Yayin"/> (cihazda kayıtlı adres yok sayılır).
        /// Geliştirmede kayıtlıysa o, yoksa yerel sunucu. Hiç yoksa boş.
        /// </summary>
        /// <param name="gelistirme">Editör ya da geliştirme build'i.</param>
        /// <param name="kayitli">"Sunucu" alanından cihazda saklanan adres (yoksa null).</param>
        public static string Sec(bool gelistirme, string kayitli)
        {
            if (Gecerli(kayitli, out _)) return kayitli.Trim();
            return Yerel;
        }

        /// <summary>ws:// ya da wss:// tam adres mi.</summary>
        public static bool Gecerli(string adres, out Uri uri)
        {
            uri = null;
            if (string.IsNullOrWhiteSpace(adres)) return false;
            if (!Uri.TryCreate(adres.Trim(), UriKind.Absolute, out uri)) return false;
            return uri.Scheme == "ws" || uri.Scheme == "wss";
        }
    }
}
