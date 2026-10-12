using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Cakmak.Kurallar;

namespace Cakmak.Sunucu
{
    /// <summary>
    /// Bir WebSocket bağlantısı. Giden mesajlar kuyruğa girer, tek bir döngü sırayla yollar.
    /// Böylece oda kilidi altında sıraya konan mesajlar istemciye aynı sırayla varır.
    /// </summary>
    sealed class Baglanti
    {
        /// <summary>Okumayan istemci için en fazla bu kadar mesaj birikir, sonra bağlantı kesilir.</summary>
        const int KuyrukSiniri = 256;

        public readonly WebSocket Soket;
        /// <summary>Sınırların anahtarı (<see cref="Sinirlar.IpAnahtari"/>).</summary>
        public readonly string Ip;
        readonly Channel<string> kuyruk = Channel.CreateBounded<string>(
            new BoundedChannelOptions(KuyrukSiniri) { SingleReader = true, FullMode = BoundedChannelFullMode.Wait });

        // Oda kilidi altında okunur/yazılır.
        public Oda Oda;
        public OyuncuId Kim;

        public Baglanti(WebSocket soket, string ip)
        {
            Soket = soket;
            Ip = ip;
        }

        /// <summary>
        /// Kilit altında çağrılabilir: beklemez, sadece kuyruğa koyar. Kuyruk doluysa mesaj atılmaz
        /// (sıra bozulur, surum anlamsızlaşır), bağlantı kesilir. İstemci anahtarla geri döner, tam durumu alır.
        /// </summary>
        public void Yolla(string json)
        {
            if (!kuyruk.Writer.TryWrite(json)) Kes();
        }

        public void Kes()
        {
            try { Soket.Abort(); } catch (ObjectDisposedException) { }
        }

        public void Bitir() => kuyruk.Writer.TryComplete();

        /// <summary>Bağlanıp süresi içinde odaya girmeyen bağlantıyı keser: boşta yer tutmasın.</summary>
        public async Task OdasizseKes(TimeSpan sure, CancellationToken iptal)
        {
            try { await Task.Delay(sure, iptal); }
            catch (OperationCanceledException) { return; } // bağlantı zaten bitti
            // Oda oda kilidi altında yazılır, burada kilitsiz okunur. En kötüsü tam o anda giren biri kesilir,
            // anahtarla geri döner.
            if (Volatile.Read(ref Oda) == null) Kes();
        }

        public async Task YaziciDongusu(CancellationToken iptal)
        {
            try
            {
                await foreach (var json in kuyruk.Reader.ReadAllAsync(iptal))
                {
                    if (Soket.State != WebSocketState.Open) break;
                    await Soket.SendAsync(Encoding.UTF8.GetBytes(json), WebSocketMessageType.Text, true, iptal);
                }
            }
            catch (OperationCanceledException) { }
            catch (WebSocketException) { }
        }
    }
}
