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
