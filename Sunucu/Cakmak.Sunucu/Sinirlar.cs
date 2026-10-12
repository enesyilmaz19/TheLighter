using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.RateLimiting;
using Microsoft.Extensions.Options;

namespace Cakmak.Sunucu
{
    /// <summary>
    /// Sınır değerleri. Ayardan değişir: appsettings'te "Sinirlar" bölümü ya da ortam değişkeni
    /// (örnek: <c>Sinirlar__IpBasinaBaglanti=50</c>). Testler küçültüp sınırların kendisini dener.
    /// </summary>
    sealed class SinirAyarlari
    {
        /// <summary>Aynı anda açık WebSocket. Aynı Wi-Fi'daki 10 kişilik grup ve yeniden bağlananlar sığsın.</summary>
        public int IpBasinaBaglanti { get; set; } = 32;
        /// <summary>odaKur + katil. Oda kodu denemeyi (24^5 ≈ 8 milyon kod) yavaşlatır.</summary>
        public int IpBasinaGirisDakikada { get; set; } = 30;
        public int MesajSaniyede { get; set; } = 10;
        /// <summary>Kısa süreli patlama: arka arkaya bu kadar mesaj beklemeden geçer.</summary>
        public int MesajPatlama { get; set; } = 20;
        /// <summary>Bu kadar saniyede başlatılmayan lobi kapanır.</summary>
        public int LobiSn { get; set; } = 30 * 60;
        /// <summary>Bağlanıp bu kadar saniyede odaya girmeyen bağlantı kesilir (boşta yer tutmasın).</summary>
        public int OdasizSn { get; set; } = 120;
        /// <summary>Sunucudaki toplam oda. Çok IP'den oda açılarak bellek doldurulamasın. Aşılınca SUNUCU_DOLU.</summary>
        public int EnFazlaOda { get; set; } = 2000;
        /// <summary>Sunucudaki toplam WebSocket (Kestrel sınırı, test sunucusunda yok).</summary>
        public int EnFazlaBaglanti { get; set; } = 5000;
        /// <summary>Oyunda kopanın yeri bu kadar saniye tutulur, sonra oyundan çıkar (Ana Plan 6.1).</summary>
        public int KopukSn { get; set; } = 60;
        /// <summary>Bağlı oyuncu 4'ün altına düşünce oyun durur, bu kadar saniye bekler, sonra biter (Ana Plan 6.1).</summary>
        public int AzOyuncuSn { get; set; } = 120;
    }

    /// <summary>IP ve bağlantı başına sınırlar. .NET'in hazır sınırlayıcıları; boşta kalan bölümleri kendisi siler.</summary>
    sealed class Sinirlar : IDisposable
    {
        readonly PartitionedRateLimiter<string> baglanti;
        readonly PartitionedRateLimiter<string> giris;
        readonly PartitionedRateLimiter<Baglanti> mesaj;
        public readonly TimeSpan LobiSuresi;
        public readonly TimeSpan OdasizSure;
        public readonly TimeSpan KopukSuresi;
        public readonly TimeSpan AzOyuncuSuresi;
        public readonly int EnFazlaOda;

        public Sinirlar(IOptions<SinirAyarlari> secenek)
        {
            var a = secenek.Value;
            LobiSuresi = TimeSpan.FromSeconds(a.LobiSn);
            OdasizSure = TimeSpan.FromSeconds(a.OdasizSn);
            KopukSuresi = TimeSpan.FromSeconds(a.KopukSn);
            AzOyuncuSuresi = TimeSpan.FromSeconds(a.AzOyuncuSn);
            EnFazlaOda = a.EnFazlaOda;
            baglanti = PartitionedRateLimiter.Create<string, string>(ip => RateLimitPartition.GetConcurrencyLimiter(ip,
                _ => new ConcurrencyLimiterOptions { PermitLimit = a.IpBasinaBaglanti, QueueLimit = 0 }));
            // Dakikada N hak, tek tek dolar (her 60/N saniyede bir): dakika başında N deneme birden açılmasın.
            giris = PartitionedRateLimiter.Create<string, string>(ip => RateLimitPartition.GetTokenBucketLimiter(ip,
                _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit = a.IpBasinaGirisDakikada,
                    TokensPerPeriod = 1,
                    ReplenishmentPeriod = TimeSpan.FromMinutes(1) / a.IpBasinaGirisDakikada,
                    QueueLimit = 0,
                }));
            mesaj = PartitionedRateLimiter.Create<Baglanti, Baglanti>(b => RateLimitPartition.GetTokenBucketLimiter(b,
                _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit = a.MesajPatlama,
                    TokensPerPeriod = a.MesajSaniyede,
                    ReplenishmentPeriod = TimeSpan.FromSeconds(1),
                    QueueLimit = 0,
                }));
        }

        /// <summary>Yeni WebSocket. Dönen izin bağlantı boyunca tutulur, kapanınca bırakılır.</summary>
        public RateLimitLease BaglantiIzni(string ip) => baglanti.AttemptAcquire(ip);

        public bool GirisIzni(string ip)
        {
            using var izin = giris.AttemptAcquire(ip);
            return izin.IsAcquired;
        }

        public bool MesajIzni(Baglanti b)
        {
            using var izin = mesaj.AttemptAcquire(b);
            return izin.IsAcquired;
        }

        /// <summary>
        /// Sınırların anahtarı. IPv6'da bir ev ya da telefon koca bir /64 bloğu alır: adresi değiştirerek
        /// sınırı aşmasın diye blok sayılır. IPv4'e eşlenmiş IPv6 adresi düz IPv4 olur.
        /// </summary>
        public static string IpAnahtari(IPAddress ip)
        {
            if (ip == null) return "bilinmiyor";
            if (ip.IsIPv4MappedToIPv6) ip = ip.MapToIPv4();
            if (ip.AddressFamily != AddressFamily.InterNetworkV6) return ip.ToString();
            var baytlar = ip.GetAddressBytes();
            Array.Clear(baytlar, 8, 8);
            return new IPAddress(baytlar) + "/64";
        }

        public void Dispose()
        {
            baglanti.Dispose();
            giris.Dispose();
            mesaj.Dispose();
        }
    }
}
