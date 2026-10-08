using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using Cakmak.Kurallar;
using Microsoft.Extensions.Logging;

namespace Cakmak.Sunucu
{
    /// <summary>Bütün odalar ve gelen mesajın doğru odaya yönlendirilmesi.</summary>
    sealed class OdaYoneticisi
    {
        // I ve O yok: 1/I, 0/O karışmasın.
        const string KodHarfleri = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const int KodUzunlugu = 5;

        readonly ConcurrentDictionary<string, Oda> odalar = new ConcurrentDictionary<string, Oda>();
        readonly IReadOnlyList<string> oneriSorulari;

        public OdaYoneticisi(ILogger<OdaYoneticisi> log)
        {
            var sorular = new List<string>();
            var klasor = Path.Combine(AppContext.BaseDirectory, "Icerik");
            if (Directory.Exists(klasor))
                foreach (var dosya in Directory.GetFiles(klasor, "*.txt"))
                    sorular.AddRange(Paket.Ayristir(File.ReadAllText(dosya)));
            oneriSorulari = sorular;
            log.LogInformation("Öneri havuzu: {Adet} soru ({Klasor})", sorular.Count, klasor);
        }

        public IEnumerable<Oda> Odalar => odalar.Values;

        /// <summary>Gelen tek bir mesajı işler. Bozuk mesaj GECERSIZ_MESAJ döner, bağlantı açık kalır.</summary>
        public void Isle(Baglanti b, string metin)
        {
            try
            {
                var m = Protokol.Nesne(metin);
                var oda = b.Oda;
                switch (Protokol.Metin(m, "t"))
                {
                    case "odaKur":
                        if (oda != null) { b.Yolla(Protokol.Hata("ZATEN_ODADASIN")); return; }
                        var isim = Protokol.Metin(m, "isim");
                        if (!Oda.IsimGecerli(isim)) { b.Yolla(Protokol.Hata("ISIM_GECERSIZ")); return; }
                        YeniOda().Katil(b, isim, null);
                        return;

                    case "katil":
                        if (oda != null) { b.Yolla(Protokol.Hata("ZATEN_ODADASIN")); return; }
                        var kod = Protokol.Metin(m, "oda").Trim().ToUpperInvariant();
                        if (!odalar.TryGetValue(kod, out var hedef)) { b.Yolla(Protokol.Hata("ODA_YOK")); return; }
                        hedef.Katil(b, Protokol.Metin(m, "isim", false), Protokol.Metin(m, "anahtar", false));
                        return;

                    case "ayarlar": OdadaysaYap(b, o => o.AyarlariDegistir(b, m)); return;
                    case "baslat": OdadaysaYap(b, o => o.Baslat(b)); return;
                    case "bitir": OdadaysaYap(b, o => o.Bitir(b)); return;
                    case "komut": OdadaysaYap(b, o => o.Komut(b, m)); return;

                    default: throw new GecersizMesaj();
                }
            }
            catch (GecersizMesaj)
            {
                b.Yolla(Protokol.Hata("GECERSIZ_MESAJ"));
            }
        }

        public void Ayrildi(Baglanti b)
        {
            var oda = b.Oda;
            if (oda != null && oda.Ayrildi(b)) odalar.TryRemove(oda.Kod, out _);
        }

        /// <summary>Bozulan bir odayı kapatıp siler.</summary>
        public void Sil(Oda oda)
        {
            oda.Kapat();
            odalar.TryRemove(oda.Kod, out _);
        }

        static void OdadaysaYap(Baglanti b, Action<Oda> is_)
        {
            var oda = b.Oda;
            if (oda == null) { b.Yolla(Protokol.Hata("ODADA_DEGILSIN")); return; }
            is_(oda);
        }

        Oda YeniOda()
        {
            // ponytail: oda sayısına sınır yok. 24^5 ≈ 8 milyon kod, çakışırsa yeniden çekilir.
            while (true)
            {
                var kod = RandomNumberGenerator.GetString(KodHarfleri, KodUzunlugu);
                var oda = new Oda(kod, oneriSorulari);
                if (odalar.TryAdd(kod, oda)) return oda;
            }
        }
    }
}
