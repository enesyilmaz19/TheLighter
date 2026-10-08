using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;

namespace SunucuTestleri
{
    /// <summary>
    /// Tek bir oyuncunun WebSocket bağlantısı. Arka planda her mesajı okur ve saklar.
    /// Hiçbir okuma süresiz beklemez: süre dolarsa test kalır, asılı kalmaz.
    /// </summary>
    public sealed class TestIstemci : IAsyncDisposable
    {
        public static readonly TimeSpan VarsayilanSure = TimeSpan.FromSeconds(5);

        readonly WebSocket soket;
        readonly object kilit = new object();
        readonly List<string> hepsi = new List<string>();       // gelen her ham mesaj, sırasıyla (gizlilik kontrolü için)
        readonly List<JsonNode> bekleyen = new List<JsonNode>(); // henüz Al/Bekle ile alınmamış mesajlar
        bool kapandi;
        JsonNode sonDurum;

        /// <summary>Son gelen hosgeldin'den: "p1"…</summary>
        public string Sen { get; private set; }
        public string Anahtar { get; private set; }
        public string Oda { get; private set; }
        public string Isim { get; set; }

        TestIstemci(WebSocket soket)
        {
            this.soket = soket;
            _ = Task.Run(Dinle);
        }

        public static async Task<TestIstemci> Baglan(WebApplicationFactory<Program> fabrika)
        {
            using var cts = new CancellationTokenSource(VarsayilanSure);
            var ws = await fabrika.Server.CreateWebSocketClient().ConnectAsync(new Uri("ws://localhost/ws"), cts.Token);
            return new TestIstemci(ws);
        }

        // ------------------------------------------------------------ Gönderme

        public Task Gonder(object mesaj) => Gonder(JsonSerializer.Serialize(mesaj));

        public async Task Gonder(string json)
        {
            using var cts = new CancellationTokenSource(VarsayilanSure);
            await soket.SendAsync(Encoding.UTF8.GetBytes(json), WebSocketMessageType.Text, true, cts.Token);
        }

        /// <summary>Metin yerine ikili (binary) çerçeve yollar. Protokol sadece metin kabul ediyor.</summary>
        public async Task IkiliGonder(byte[] veri)
        {
            using var cts = new CancellationTokenSource(VarsayilanSure);
            await soket.SendAsync(veri, WebSocketMessageType.Binary, true, cts.Token);
        }

        /// <summary>Bağlantıyı düzgünce kapatır (oyuncu uygulamayı kapattı).</summary>
        public async Task Kapat()
        {
            using var cts = new CancellationTokenSource(VarsayilanSure);
            await soket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", cts.Token);
        }

        // ------------------------------------------------------------ Okuma

        /// <summary>Sıradaki (henüz alınmamış) mesaj.</summary>
        public Task<JsonNode> Al(TimeSpan? sure = null) => Bekle(null, null, sure);

        /// <summary>
        /// <paramref name="t"/> tipinde ve <paramref name="kosul"/>u sağlayan ilk alınmamış mesajı döner ve onu alındı sayar.
        /// Uymayan mesajlar atılmaz, sonraki Bekle çağrıları için sırada kalır (durum/olay sırası protokolde yazmıyor).
        /// <paramref name="t"/> null ise tipi ne olursa olsun sıradaki mesaj.
        /// </summary>
        public async Task<JsonNode> Bekle(string t, Func<JsonNode, bool> kosul = null, TimeSpan? sure = null)
        {
            var son = DateTime.UtcNow + (sure ?? VarsayilanSure);
            while (true)
            {
                lock (kilit)
                {
                    int i = bekleyen.FindIndex(m => Uyar(m, t, kosul));
                    if (i >= 0)
                    {
                        var m = bekleyen[i];
                        bekleyen.RemoveAt(i);
                        return m;
                    }
                    if (kapandi || DateTime.UtcNow > son)
                        throw new AssertionException(
                            $"{Sen ?? "?"}: '{t ?? "herhangi"}' mesajı {(kapandi ? "gelmeden bağlantı kapandı" : "süre içinde gelmedi")}.\n" +
                            "Alınmamış mesajlar:\n  " + string.Join("\n  ", bekleyen.TakeLast(8).Select(x => x.ToJsonString())));
                }
                await Task.Delay(10);
            }
        }

        /// <summary>Gelen hata mesajının kodunu bekler.</summary>
        public async Task Hata(string kod)
        {
            var h = await Bekle("hata");
            Assert.That((string)h["kod"], Is.EqualTo(kod), $"{Sen}: beklenen hata kodu");
        }

        /// <summary>Şimdiye kadar gelen ama alınmamış mesajları atar.</summary>
        public void Temizle()
        {
            lock (kilit) bekleyen.Clear();
        }

        /// <summary>En son gelen durum mesajı (alınmış ya da alınmamış).</summary>
        public JsonNode SonDurum
        {
            get { lock (kilit) return sonDurum; }
        }

        /// <summary>Bağlantı boyunca gelen bütün ham mesajlar, geliş sırasıyla.</summary>
        public List<string> Hepsi
        {
            get { lock (kilit) return hepsi.ToList(); }
        }

        public bool Kapandi
        {
            get { lock (kilit) return kapandi; }
        }

        /// <summary>Sunucu bağlantıyı kapatana kadar bekler. Süre içinde kapanırsa true.</summary>
        public async Task<bool> KapanmaBekle(TimeSpan? sure = null)
        {
            var son = DateTime.UtcNow + (sure ?? VarsayilanSure);
            while (DateTime.UtcNow < son)
            {
                if (Kapandi) return true;
                await Task.Delay(10);
            }
            return Kapandi;
        }

        static string Metin(JsonNode n) => n is JsonValue v && v.TryGetValue(out string s) ? s : null;

        static bool Uyar(JsonNode m, string t, Func<JsonNode, bool> kosul)
        {
            try
            {
                return (t == null || (string)m["t"] == t) && (kosul == null || kosul(m));
            }
            catch (Exception)
            {
                return false; // koşul eksik/yanlış tipte alana takıldı: bu mesaj aranan değil
            }
        }

        async Task Dinle()
        {
            var tampon = new byte[16 * 1024];
            try
            {
                while (true)
                {
                    using var parca = new MemoryStream();
                    WebSocketReceiveResult r;
                    do
                    {
                        r = await soket.ReceiveAsync(tampon, CancellationToken.None);
                        if (r.MessageType == WebSocketMessageType.Close) return;
                        parca.Write(tampon, 0, r.Count);
                    } while (!r.EndOfMessage);

                    var ham = Encoding.UTF8.GetString(parca.ToArray());
                    JsonNode m = null;
                    try { m = JsonNode.Parse(ham); }
                    catch (JsonException) { }
                    // Protokol: her mesaj tek bir JSON nesnesi. Değilse testler görsün diye işaretle.
                    if (m is not JsonObject) m = new JsonObject { ["t"] = "<nesne-degil>", ["ham"] = ham };

                    lock (kilit)
                    {
                        hepsi.Add(ham);
                        bekleyen.Add(m);
                        var t = Metin(m["t"]);
                        if (t == "durum") sonDurum = m;
                        if (t == "hosgeldin")
                        {
                            Sen = Metin(m["sen"]);
                            Anahtar = Metin(m["anahtar"]);
                            Oda = Metin(m["oda"]);
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Sunucu bağlantıyı kopardı ya da biz kapattık.
            }
            finally
            {
                lock (kilit) kapandi = true;
            }
        }

        public ValueTask DisposeAsync()
        {
            soket.Abort();
            soket.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
