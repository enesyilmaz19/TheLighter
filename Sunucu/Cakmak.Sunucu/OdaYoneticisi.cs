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
        /// <summary>Paket adı (dosya adı, uzantısız: "Genel") → soruları. Ada göre sıralı.</summary>
        readonly SortedDictionary<string, IReadOnlyList<string>> paketler = new SortedDictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        readonly IReadOnlyList<string> sesliCezalar = Array.Empty<string>();
        readonly IReadOnlyList<string> geceSorulari = Array.Empty<string>();
        readonly Sinirlar sinirlar;

        public OdaYoneticisi(ILogger<OdaYoneticisi> log, Sinirlar sinirlar)
        {
            this.sinirlar = sinirlar;
            var klasor = Path.Combine(AppContext.BaseDirectory, "Icerik");
            if (Directory.Exists(klasor))
            {
                foreach (var dosya in Directory.GetFiles(klasor, "*.txt"))
                    paketler[Path.GetFileNameWithoutExtension(dosya)] = Paket.Ayristir(File.ReadAllText(dosya));
                var cezalar = Path.Combine(klasor, "Cezalar.csv");
                if (File.Exists(cezalar)) sesliCezalar = Paket.Ayristir(File.ReadAllText(cezalar));
                var gece = Path.Combine(klasor, "GeceSorulari.csv");
                if (File.Exists(gece)) geceSorulari = Paket.Ayristir(File.ReadAllText(gece));
            }
            foreach (var p in paketler) log.LogInformation("Paket {Ad}: {Adet} soru", p.Key, p.Value.Count);
            log.LogInformation("Sesli ceza: {Adet}, Çakmak Gecesi sorusu: {Gece} ({Klasor})", sesliCezalar.Count, geceSorulari.Count, klasor);
        }

        public IEnumerable<Oda> Odalar => odalar.Values;

        /// <summary>Gelen tek bir mesajı işler. Bozuk mesaj GECERSIZ_MESAJ döner, bağlantı açık kalır.</summary>
        public void Isle(Baglanti b, string metin)
        {
            // Sınırı aşan mesaj okunmaz bile. Cevap yine gider: istemci yavaşlaması gerektiğini bilsin. Durmayan
            // istemcinin cevapları gönderim kuyruğunu doldurur, kuyruk dolunca bağlantı kesilir (Baglanti.Yolla).
            if (!sinirlar.MesajIzni(b)) { b.Yolla(Protokol.Hata("COK_HIZLI")); return; }
            try
            {
                var m = Protokol.Nesne(metin);
                var oda = b.Oda;
                var t = Protokol.Metin(m, "t");
                if ((t == "odaKur" || t == "katil") && !sinirlar.GirisIzni(b.Ip)) { b.Yolla(Protokol.Hata("COK_HIZLI")); return; }
                switch (t)
                {
                    case "odaKur":
                        if (oda != null) { b.Yolla(Protokol.Hata("ZATEN_ODADASIN")); return; }
                        var isim = Protokol.Metin(m, "isim");
                        if (!Oda.IsimGecerli(isim)) { b.Yolla(Protokol.Hata("ISIM_GECERSIZ")); return; }
                        // ponytail: sayım ile ekleme arasında birkaç oda fazla açılabilir; sınırın amacı belleği korumak.
                        if (odalar.Count >= sinirlar.EnFazlaOda) { b.Yolla(Protokol.Hata("SUNUCU_DOLU")); return; }
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
                    case "buradayim": OdadaysaYap(b, o => o.Buradayim(b)); return;
                    case "at": OdadaysaYap(b, o => o.At(b, m)); return;
                    case "tekrarOyna": OdadaysaYap(b, o => o.TekrarOyna(b)); return;
                    case "sohbet": OdadaysaYap(b, o => o.Sohbet(b, m)); return;
                    case "grupPaketi": OdadaysaYap(b, o => o.GrupPaketi(b, m)); return;

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
            // 24^5 ≈ 8 milyon kod, çakışırsa yeniden çekilir. Toplam oda sınırı odaKur'da (EnFazlaOda).
            while (true)
            {
                var kod = RandomNumberGenerator.GetString(KodHarfleri, KodUzunlugu);
                var oda = new Oda(kod, paketler, sesliCezalar, geceSorulari, sinirlar);
                if (odalar.TryAdd(kod, oda)) return oda;
            }
        }
    }
}
