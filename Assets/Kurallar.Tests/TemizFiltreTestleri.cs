using NUnit.Framework;
using static Cakmak.Kurallar.Tests.Kurulum;

namespace Cakmak.Kurallar.Tests
{
    /// <summary>Temiz mod filtresi (Ana Plan 6.4). Paket soruları takılmasın: IcerikTestleri.</summary>
    public class TemizFiltreTestleri
    {
        static string K(int kod) => char.ConvertFromUtf32(kod);

        [TestCase("siktir git")]
        [TestCase("naber amk")]
        [TestCase("AMK")]
        [TestCase("aq")]
        [TestCase("amına")]
        [TestCase("orospu çocuğu")]
        [TestCase("Götün")]
        [TestCase("piç")]
        [TestCase("fuck you")]
        // Kaçamak yazım
        [TestCase("s1kt1r")]
        [TestCase("$ik")]
        [TestCase("a.m.k")]
        [TestCase("s i k")]
        [TestCase("o.ç")]
        [TestCase("sıııık")]
        [TestCase("yarrrrak")]
        [TestCase("SİKTİR")]
        [TestCase("SIKTIR")]
        // Ünsüz yumuşaması: k → ğ, kök değişir
        [TestCase("yarrağı")]
        [TestCase("dalyarrağın")]
        [TestCase("amcığı")]
        [TestCase("taşağa")]
        [TestCase("yavşağın")]
        [TestCase("kaltağı")]
        [TestCase("pezevengi")]
        [TestCase("sürtüğe")]
        // İçerik gözden geçirmesinde eklenenler
        [TestCase("amını")]
        [TestCase("boktan oyun")]
        [TestCase("sürtük")]
        [TestCase("kevaşe")]
        [TestCase("motherfucker")]
        [TestCase("asshole")]
        [TestCase("nigga")]
        [TestCase("niggas")]
        [TestCase("nigger")]
        [TestCase("niggers")]
        public void Yakalar(string metin)
        {
            Assert.That(TemizFiltre.Uygunsuz(metin), Is.True);
        }

        [TestCase("götürmek istiyorum")]
        [TestCase("sikke koleksiyonu")]
        [TestCase("musiki")]
        [TestCase("amaç ne")]
        [TestCase("ama amca amin dedi")]
        [TestCase("çok sıkıldım, sıkıcı oyun")]
        [TestCase("SIKILDIM")]
        [TestCase("sikildim ya")]
        [TestCase("şık giyinmiş, sık sık geliyor")]
        [TestCase("şikayet etti")]
        [TestCase("ocakta boks maçı var")]
        [TestCase("yara bandı, taşa takıldım")]
        [TestCase("sürtünme kuvveti")]
        [TestCase("Nigar abla bokser köpeğini getirdi")]
        [TestCase("100 kişi geldi")]
        [TestCase("Grupta en çok kim uyur?")]
        [TestCase("")]
        [TestCase(null)]
        public void Masumu_gecirir(string metin)
        {
            Assert.That(TemizFiltre.Uygunsuz(metin), Is.False);
        }

        [Test]
        public void Gorunmez_karakter_ve_tam_genislikli_harf_kurtarmaz()
        {
            Assert.That(TemizFiltre.Uygunsuz("si" + K(0x200B) + "k"), Is.True, "sıfır genişlikli boşluk");
            Assert.That(TemizFiltre.Uygunsuz(K(0xFF53) + K(0xFF49) + K(0xFF4B)), Is.True, "tam genişlikli ｓｉｋ");
            Assert.That(TemizFiltre.Uygunsuz("s" + K(0x0307) + "ik"), Is.True, "birleşen işaret");
        }

        [Test]
        public void Temiz_modda_yakalanan_soru_gonderilmez_tur_aynen_kalir()
        {
            var oyun = Yeni(ayarla: a => a.Mod = OyunModu.Temiz);
            var a = SiradakiA(oyun);
            var b = oyun.GorunumAl(a).Secilebilir[0];

            Assert.That(oyun.SoruSor(a, b, "Kim en çok s1kt1r der?"), Is.EqualTo(Sonuc.Uygunsuz));
            Assert.That(Durum(oyun).Asama, Is.EqualTo(Asama.SoruSorma));
            Assert.That(Durum(oyun).B, Is.Null);

            Assert.That(oyun.SoruSor(a, b, "Kim en çok uyur?"), Is.EqualTo(Sonuc.Tamam));
        }

        [Test]
        public void Normal_modda_filtre_yok()
        {
            var oyun = Yeni();
            var a = SiradakiA(oyun);
            Assert.That(oyun.SoruSor(a, oyun.GorunumAl(a).Secilebilir[0], "Kim en çok s1kt1r der?"), Is.EqualTo(Sonuc.Tamam));
        }
    }
}
