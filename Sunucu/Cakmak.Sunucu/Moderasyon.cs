using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cakmak.Sunucu
{
    /// <summary>
    /// Ayardan değişir: "Moderasyon" bölümü ya da ortam değişkeni (<c>Moderasyon__Klasor=/var/lib/cakmak</c>).
    /// VPS'te klasör yayın klasörünün dışında olmalı: yeni sürüm yüklenince raporlar silinmesin.
    /// </summary>
    sealed class ModerasyonAyarlari
    {
        /// <summary>Göreliyse sunucunun klasörüne göre.</summary>
        public string Klasor { get; set; } = "Moderasyon";
        /// <summary>Rapor bu kadar gün tutulur (Ana Plan: gizlilik).</summary>
        public int RaporGun { get; set; } = 30;
    }

    /// <summary>
    /// Raporlar ve IP yasağı (Google Play kullanıcı içeriği şartı). Raporlar <c>raporlar/yyyy-MM-dd.jsonl</c>'e, satır başına
    /// bir rapor; Enes elle okur. Yasak listesi <c>yasakli-ip.txt</c>: satır başına bir IP, raporlardaki <c>ip</c> alanıyla
    /// aynı biçim (IPv6'da /64). Dosya değişince sunucuyu yeniden başlatmak gerekmez.
    /// ponytail: dosya, tek sunucu için; birden çok sunucu olursa veritabanına taşınır.
    /// </summary>
    sealed class Moderasyon
    {
        readonly string raporKlasoru;
        readonly string yasakDosyasi;
        readonly TimeSpan saklama;
        readonly object kilit = new object();
        readonly ILogger<Moderasyon> log;
        HashSet<string> yasakli = new HashSet<string>();
        DateTime yasakDosyasiZamani = DateTime.MinValue;

        public Moderasyon(IOptions<ModerasyonAyarlari> secenek, ILogger<Moderasyon> log)
        {
            this.log = log;
            var a = secenek.Value;
            var klasor = Path.GetFullPath(a.Klasor, AppContext.BaseDirectory);
            raporKlasoru = Path.Combine(klasor, "raporlar");
            yasakDosyasi = Path.Combine(klasor, "yasakli-ip.txt");
            saklama = TimeSpan.FromDays(a.RaporGun);
            EskileriSil(DateTime.UtcNow);
        }

        /// <summary>Disk yazamazsa (dolu, izin yok) false: oyuncu tekrar dener, sunucu düşmez.</summary>
        public bool RaporYaz(JsonObject rapor)
        {
            lock (kilit)
            {
                try
                {
                    Directory.CreateDirectory(raporKlasoru);
                    var dosya = Path.Combine(raporKlasoru, DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + ".jsonl");
                    File.AppendAllText(dosya, rapor.ToJsonString() + "\n");
                    // Sadece oda kodu: isim, IP ve metin günlüğe yazılmaz (Guvenlik.md 13). Enes journalctl'de yeni raporu görür.
                    log.LogWarning("Yeni rapor (oda {Oda}): {Dosya}", (string)rapor["oda"], dosya);
                    return true;
                }
                catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
                {
                    log.LogError("Rapor yazılamadı (oda {Oda}): {Tip}", (string)rapor["oda"], e.GetType().Name);
                    return false;
                }
            }
        }

        /// <summary>Saklama süresini geçen günlerin dosyaları silinir (Saat saatte bir çağırır).</summary>
        public void EskileriSil(DateTime simdi)
        {
            lock (kilit)
            {
                if (!Directory.Exists(raporKlasoru)) return;
                foreach (var dosya in Directory.GetFiles(raporKlasoru, "*.jsonl"))
                    if (DateTime.TryParseExact(Path.GetFileNameWithoutExtension(dosya), "yyyy-MM-dd", CultureInfo.InvariantCulture,
                            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var gun)
                        && simdi - gun.AddDays(1) > saklama)
                        File.Delete(dosya);
            }
        }

        /// <summary>Her yeni bağlantıda. Dosya sadece değiştiyse yeniden okunur.</summary>
        public bool Yasakli(string ip)
        {
            lock (kilit)
            {
                var zaman = File.Exists(yasakDosyasi) ? File.GetLastWriteTimeUtc(yasakDosyasi) : DateTime.MinValue;
                if (zaman != yasakDosyasiZamani)
                {
                    yasakli = new HashSet<string>();
                    if (zaman != DateTime.MinValue)
                        foreach (var satir in File.ReadAllLines(yasakDosyasi))
                        {
                            var s = satir.Trim();
                            if (s.Length > 0 && !s.StartsWith("#")) yasakli.Add(s);
                        }
                    yasakDosyasiZamani = zaman;
                }
                return yasakli.Contains(ip);
            }
        }
    }
}
