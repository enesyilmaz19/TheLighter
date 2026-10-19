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
        readonly Moderasyon moderasyon;
        readonly ILogger<Saat> log;

        public Saat(OdaYoneticisi odalar, Moderasyon moderasyon, ILogger<Saat> log)
        {
            this.odalar = odalar;
            this.moderasyon = moderasyon;
            this.log = log;
        }

        protected override async Task ExecuteAsync(CancellationToken iptal)
        {
            using var zamanlayici = new PeriodicTimer(TimeSpan.FromMilliseconds(100));
            var kronometre = Stopwatch.StartNew();
            var temizlik = Stopwatch.StartNew();
            try
            {
                while (await zamanlayici.WaitForNextTickAsync(iptal))
                {
                    var gecen = kronometre.Elapsed;
                    kronometre.Restart();
                    if (temizlik.Elapsed > TimeSpan.FromHours(1))
                    {
                        temizlik.Restart();
                        moderasyon.EskileriSil(DateTime.UtcNow);
                    }
                    foreach (var oda in odalar.Odalar)
                    {
                        try
                        {
                            if (oda.Ilerle(gecen))
                            {
                                log.LogInformation("Oda {Kod} lobide başlatılmadan çok bekledi, kapatıldı", oda.Kod);
                                odalar.Sil(oda);
                            }
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
