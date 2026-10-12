using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cakmak.Sunucu;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

// Online mod sunucusu. Protokol: docs/Protokol.md
var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<SinirAyarlari>(builder.Configuration.GetSection("Sinirlar"));
builder.Services.AddSingleton<Sinirlar>();
builder.Services.AddSingleton<OdaYoneticisi>();
builder.Services.AddHostedService<Saat>();
// Caddy aynı makinede, istemcinin gerçek IP'sini X-Forwarded-For ile yollar. Varsayılan olarak sadece
// localhost'tan gelen başlığa güvenilir: dışarıdan biri başlık yazarak IP'sini değiştiremez.
builder.Services.Configure<ForwardedHeadersOptions>(o => o.ForwardedHeaders = ForwardedHeaders.XForwardedFor);
builder.WebHost.ConfigureKestrel(o =>
{
    o.AddServerHeader = false; // yazılımın adını ve sürümünü söylemesin
    o.Limits.MaxConcurrentUpgradedConnections =
        (builder.Configuration.GetSection("Sinirlar").Get<SinirAyarlari>() ?? new SinirAyarlari()).EnFazlaBaglanti;
});

var app = builder.Build();
app.UseForwardedHeaders();
// Telefon Wi-Fi'dan habersiz düşerse (cebe girdi, tünel) 20 sn içinde cevap gelmez, bağlantı kopmuş sayılır.
app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(10),
    KeepAliveTimeout = TimeSpan.FromSeconds(20),
});
app.MapGet("/", () => "TheLighter sunucusu");
app.Map("/ws", async (HttpContext ctx, OdaYoneticisi odalar, Sinirlar sinirlar) =>
{
    if (!ctx.WebSockets.IsWebSocketRequest)
    {
        ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
        return;
    }
    // Uygulama (Unity) Origin başlığı yollamaz, tarayıcı her zaman yollar. Bir web sayfası ziyaretçilerin
    // tarayıcısını sunucuya karşı kullanamasın. ponytail: WebGL sürümü çıkarsa izinli adres listesi olur.
    if (ctx.Request.Headers.Origin.Count > 0)
    {
        ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
        return;
    }

    var ip = Sinirlar.IpAnahtari(ctx.Connection.RemoteIpAddress);
    using var izin = sinirlar.BaglantiIzni(ip);
    if (!izin.IsAcquired)
    {
        ctx.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        return;
    }

    using var soket = await ctx.WebSockets.AcceptWebSocketAsync();
    var baglanti = new Baglanti(soket, ip);
    var yazici = baglanti.YaziciDongusu(ctx.RequestAborted);
    _ = baglanti.OdasizseKes(sinirlar.OdasizSure, ctx.RequestAborted);
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
        // Bağlantıyı biz kestiysek (Baglanti.Kes: lobi zaman aşımı, başka cihazdan dönüş) ya da karşı taraf düştüyse
        // bazı ortamlar bunları atar. Hepsinin anlamı aynı: bağlantı bitti. Temizlik çağıranın finally'sinde.
        catch (IOException) { }
        catch (ObjectDisposedException) { }
    }
}
