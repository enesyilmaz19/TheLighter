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
        readonly Channel<string> kuyruk = Channel.CreateBounded<string>(
            new BoundedChannelOptions(KuyrukSiniri) { SingleReader = true, FullMode = BoundedChannelFullMode.Wait });

        // Oda kilidi altında okunur/yazılır.
        public Oda Oda;
        public OyuncuId Kim;

        public Baglanti(WebSocket soket) { Soket = soket; }

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
