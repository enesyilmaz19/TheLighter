using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TheLighter.Ag
{
    public enum TasimaOlayTuru { Acildi, Mesaj, Kapandi }

    public struct TasimaOlayi
    {
        public TasimaOlayTuru Tur;
        /// <summary>Mesaj: gelen JSON. Kapandi: neden (günlük için, anahtar içermez).</summary>
        public string Metin;
    }

    /// <summary>
    /// Tek bir bağlantı denemesi. Her yeniden bağlanmada yenisi kurulur (eski bağlantının geç gelen olayları karışmasın).
    /// Olaylar arka planda birikir, ana iş parçacığı <see cref="Al"/> ile çeker. Testte sahtesi kullanılır.
    /// </summary>
    public interface ITasima
    {
        void Ac(Uri adres);
        /// <summary>Sıraya koyar. Bağlantı açık değilse false.</summary>
        bool Gonder(string json);
        void Kapat();
        bool Al(out TasimaOlayi olay);
    }

    /// <summary>
    /// <see cref="ClientWebSocket"/> ile gerçek bağlantı (Windows ve Android, paket gerekmez).
    /// <c>Origin</c> başlığı yollamaz (sunucu tarayıcı sanıp reddederdi). Sunucunun ping'ine çerçeve katmanı kendisi cevap verir.
    /// </summary>
    public sealed class WebSocketTasima : ITasima
    {
        static readonly TimeSpan BaglanmaSuresi = TimeSpan.FromSeconds(10);

        readonly ConcurrentQueue<TasimaOlayi> gelen = new ConcurrentQueue<TasimaOlayi>();
        readonly ConcurrentQueue<string> giden = new ConcurrentQueue<string>();
        readonly SemaphoreSlim gidenVar = new SemaphoreSlim(0);
        readonly CancellationTokenSource iptal = new CancellationTokenSource();
        ClientWebSocket soket;
        volatile bool acik;
        int kapandiBildirildi;

        public void Ac(Uri adres)
        {
            soket = new ClientWebSocket();
            soket.Options.KeepAliveInterval = TimeSpan.FromSeconds(10);
            _ = Task.Run(() => Calis(adres));
        }

        public bool Gonder(string json)
        {
            if (!acik) return false;
            giden.Enqueue(json);
            gidenVar.Release();
            return true;
        }

        public void Kapat()
        {
            acik = false;
            try { iptal.Cancel(); } catch (ObjectDisposedException) { }
        }

        public bool Al(out TasimaOlayi olay) => gelen.TryDequeue(out olay);

        void Bildir(TasimaOlayTuru tur, string metin = null) => gelen.Enqueue(new TasimaOlayi { Tur = tur, Metin = metin });

        void Kapandi(string neden)
        {
            acik = false;
            if (Interlocked.Exchange(ref kapandiBildirildi, 1) == 0) Bildir(TasimaOlayTuru.Kapandi, neden);
        }

        async Task Calis(Uri adres)
        {
            try
            {
                using (var sure = CancellationTokenSource.CreateLinkedTokenSource(iptal.Token))
                {
                    sure.CancelAfter(BaglanmaSuresi);
                    await soket.ConnectAsync(adres, sure.Token).ConfigureAwait(false);
                }
                acik = true;
                Bildir(TasimaOlayTuru.Acildi);
                _ = Task.Run(Yolla);
                await Dinle().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Kapandi(e.GetType().Name);
            }
            finally
            {
                Kapandi("bitti");
                try { soket.Abort(); soket.Dispose(); } catch (Exception) { }
            }
        }

        async Task Dinle()
        {
            var tampon = new byte[16 * 1024];
            while (!iptal.IsCancellationRequested)
            {
                using (var parca = new MemoryStream())
                {
                    WebSocketReceiveResult r;
                    do
                    {
                        r = await soket.ReceiveAsync(new ArraySegment<byte>(tampon), iptal.Token).ConfigureAwait(false);
                        if (r.MessageType == WebSocketMessageType.Close)
                        {
                            Kapandi("sunucu kapattı " + (int?)r.CloseStatus);
                            return;
                        }
                        parca.Write(tampon, 0, r.Count);
                    } while (!r.EndOfMessage);

                    if (r.MessageType == WebSocketMessageType.Text)
                        Bildir(TasimaOlayTuru.Mesaj, Encoding.UTF8.GetString(parca.ToArray()));
                }
            }
        }

        /// <summary>ClientWebSocket aynı anda tek gönderim kabul eder: sırayla tek iş parçacığından.</summary>
        async Task Yolla()
        {
            try
            {
                while (!iptal.IsCancellationRequested)
                {
                    await gidenVar.WaitAsync(iptal.Token).ConfigureAwait(false);
                    if (!giden.TryDequeue(out var json)) continue;
                    var bayt = Encoding.UTF8.GetBytes(json);
                    await soket.SendAsync(new ArraySegment<byte>(bayt), WebSocketMessageType.Text, true, iptal.Token).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                Kapandi(e.GetType().Name);
                try { soket.Abort(); } catch (Exception) { }
            }
        }
    }
}
