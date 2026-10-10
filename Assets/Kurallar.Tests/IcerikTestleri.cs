using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using NUnit.Framework;

namespace Cakmak.Kurallar.Tests
{
    /// <summary>
    /// Assets/Icerik altındaki her paketi kontrol eder. Yeni paket eklenince ayrıca test yazmak gerekmez.
    /// </summary>
    public class IcerikTestleri
    {
        // Bu dosyanın yerinden yola çıkar: hem Unity'de hem dotnet test'te çalışır.
        static string IcerikKlasoru([CallerFilePath] string buDosya = "") =>
            Path.GetFullPath(Path.Combine(Path.GetDirectoryName(buDosya), "..", "Icerik"));

        static IEnumerable<string> Paketler() => Directory.GetFiles(IcerikKlasoru(), "*.txt");

        [Test]
        public void Icerik_klasorunde_paket_var()
        {
            Assert.That(Paketler(), Is.Not.Empty, IcerikKlasoru());
        }

        [Test]
        public void Genel_paketinde_en_az_100_soru_var()
        {
            var sorular = Paket.Ayristir(File.ReadAllText(Path.Combine(IcerikKlasoru(), "Genel.txt")));
            Assert.That(sorular.Count, Is.GreaterThanOrEqualTo(100));
        }

        [Test]
        public void Sesli_cezalar_kurallara_uyuyor()
        {
            // .csv: Arda'nın paket tarayıcısı ve sunucu sadece .txt'leri soru paketi sayıyor.
            var cezalar = Paket.Ayristir(File.ReadAllText(Path.Combine(IcerikKlasoru(), "Cezalar.csv")));
            Assert.That(cezalar.Count, Is.GreaterThanOrEqualTo(20));
            var gorulen = new HashSet<string>();
            foreach (var ceza in cezalar)
            {
                Assert.That(ceza.Length, Is.LessThanOrEqualTo(120), "çok uzun: " + ceza);
                Assert.That(ceza, Does.Not.EndWith("?"), "ceza soru değil, görev: " + ceza);
                Assert.That(gorulen.Add(ceza.ToLowerInvariant()), Is.True, "iki kez var: " + ceza);
            }
        }

        [Test]
        public void Her_soru_kurallara_uyuyor()
        {
            foreach (var dosya in Paketler())
            {
                var ad = Path.GetFileName(dosya);
                var gorulen = new HashSet<string>();
                foreach (var soru in Paket.Ayristir(File.ReadAllText(dosya)))
                {
                    Assert.That(soru.Length, Is.LessThanOrEqualTo(Oyun.SoruEnFazlaKarakter), $"{ad}: çok uzun: {soru}");
                    Assert.That(soru, Does.EndWith("?"), $"{ad}: soru işareti yok: {soru}");
                    Assert.That(soru.ToLowerInvariant(), Does.Contain("kim"), $"{ad}: cevabı bir kişi değil: {soru}");
                    Assert.That(gorulen.Add(soru.ToLowerInvariant()), Is.True, $"{ad}: iki kez var: {soru}");
                }
            }
        }
    }
}
