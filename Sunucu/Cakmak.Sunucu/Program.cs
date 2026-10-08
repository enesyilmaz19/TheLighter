using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cakmak.Sunucu;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

// Online mod sunucusu. Protokol: docs/Protokol.md
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<OdaYoneticisi>();
builder.Services.AddHostedService<Saat>();

var app = builder.Build();
// Telefon Wi-Fi'dan habersiz düşerse (cebe girdi, tünel) 20 sn içinde cevap gelmez, bağlantı kopmuş sayılır.
app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(10),
    KeepAliveTimeout = TimeSpan.FromSeconds(20),
});
app.MapGet("/", () => "TheLighter sunucusu");
app.Map("/ws", async (HttpContext ctx, OdaYoneticisi odalar) =>
{
    if (!ctx.WebSockets.IsWebSocketRequest)
    {
        ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
        return;
    }

    using var soket = await ctx.WebSockets.AcceptWebSocketAsync();
    var baglanti = new Baglanti(soket);
    var yazici = baglanti.YaziciDongusu(ctx.RequestAborted);
    try
    {
        await Okuyucu.Dongu(soket, metin => odalar.Isle(baglanti, metin), ctx.RequestAborted);
    }
    finally
    {
        odalar.Ayrildi(baglanti);
        baglanti.Bitir();
        await yazici;
    }
});
app.Run();

public partial class Program { }

static class Okuyucu
{
    /// <summary>Protokol sınırı. Daha büyük mesaj bağlantıyı kapatır.</summary>
    const int EnFazlaBayt = 4096;

    public static async Task Dongu(WebSocket soket, Action<string> isle, CancellationToken iptal)
    {
        var tampon = new byte[EnFazlaBayt];
        try
        {
            while (soket.State == WebSocketState.Open)
            {
                int dolu = 0;
                WebSocketReceiveResult sonuc;
                do
                {
                    if (dolu == tampon.Length)
                    {
                        // CloseOutputAsync karşı tarafın cevabını beklemez: susan bir istemci bağlantıyı asılı tutamaz.
                        await soket.CloseOutputAsync(WebSocketCloseStatus.MessageTooBig, "4 KB sınırı", iptal);
                        return;
                    }
                    sonuc = await soket.ReceiveAsync(new ArraySegment<byte>(tampon, dolu, tampon.Length - dolu), iptal);
                    if (sonuc.MessageType == WebSocketMessageType.Close)
                    {
                        await soket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, iptal);
                        return;
                    }
                    dolu += sonuc.Count;
                } while (!sonuc.EndOfMessage);

                // Protokol sadece metin: ikili çerçeve null olarak geçer, GECERSIZ_MESAJ döner.
                isle(sonuc.MessageType == WebSocketMessageType.Text ? Encoding.UTF8.GetString(tampon, 0, dolu) : null);
            }
        }
        catch (WebSocketException) { }
        catch (OperationCanceledException) { }
    }
}
