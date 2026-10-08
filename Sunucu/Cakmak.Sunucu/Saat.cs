using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cakmak.Sunucu
{
    /// <summary>Kural motoru saat tutmaz. Her ~100 ms'de bütün odalarda zamanı gerçekte geçen kadar ilerletir.</summary>
    sealed class Saat : BackgroundService
    {
        readonly OdaYoneticisi odalar;
        readonly ILogger<Saat> log;

        public Saat(OdaYoneticisi odalar, ILogger<Saat> log)
        {
            this.odalar = odalar;
            this.log = log;
        }

        protected override async Task ExecuteAsync(CancellationToken iptal)
        {
            using var zamanlayici = new PeriodicTimer(TimeSpan.FromMilliseconds(100));
            var kronometre = Stopwatch.StartNew();
            try
            {
                while (await zamanlayici.WaitForNextTickAsync(iptal))
                {
                    var gecen = kronometre.Elapsed;
                    kronometre.Restart();
                    foreach (var oda in odalar.Odalar)
                    {
                        try
                        {
                            oda.Ilerle(gecen);
                        }
                        catch (Exception e)
                        {
                            // Bir odanın hatası bütün sunucuyu durdurmasın. O oda kapanır, oyuncular kopar.
                            // Sadece hata tipi yazılır: mesaj ya da yığın oyuncu metni taşıyabilir (AGENTS.md: gizlilik).
                            log.LogError("Oda {Kod} bozuldu ve kapatıldı: {Tip}", oda.Kod, e.GetType().Name);
                            odalar.Sil(oda);
                        }
                    }
                }
            }
            catch (OperationCanceledException) { }
        }
    }
}
