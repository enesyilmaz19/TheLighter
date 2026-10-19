using NUnit.Framework;
using TheLighter.Ag;

namespace TheLighter.Testler
{
    /// <summary>
    /// Sunucu adresi (karar 29; tasks/g2-android.md 1): yayın adresi tek sabitte, "Sunucu" alanı ve cihazdaki kayıt
    /// sadece editörde ve geliştirme build'inde işe yarar.
    /// </summary>
    public class SunucuAdresiTestleri
    {
        const string TelefonDenemesi = "ws://192.168.1.34:5000/ws";

        [Test]
        public void Gelistirmede_KayitliAdresKullanilir_YoksaYerel()
        {
            Assert.That(SunucuAdresi.Sec(true, TelefonDenemesi), Is.EqualTo(TelefonDenemesi));
            Assert.That(SunucuAdresi.Sec(true, "  " + TelefonDenemesi + " "), Is.EqualTo(TelefonDenemesi));
            Assert.That(SunucuAdresi.Sec(true, null), Is.EqualTo(SunucuAdresi.Yerel));
            Assert.That(SunucuAdresi.Sec(true, "http://bozuk"), Is.EqualTo(SunucuAdresi.Yerel), "ws/wss değilse yok sayılır");
            Assert.That(SunucuAdresi.AlanGorunur(true), Is.True);
        }

        [Test]
        public void Yayinda_HepSabit_AlanYok()
        {
            Assert.That(SunucuAdresi.Sec(false, TelefonDenemesi), Is.EqualTo(SunucuAdresi.Yayin), "cihazdaki kayıt yayında işlemez");
            Assert.That(SunucuAdresi.Sec(false, null), Is.EqualTo(SunucuAdresi.Yayin));
            Assert.That(SunucuAdresi.AlanGorunur(false), Is.False);
        }

        [Test]
        public void YayinAdresi_BosYaDaWss()
        {
            // VPS gelene kadar boş; gelince düz ws:// değil, wss:// (HTTPS'i Caddy veriyor).
            Assert.That(SunucuAdresi.Yayin == "" || SunucuAdresi.Yayin.StartsWith("wss://"), Is.True, SunucuAdresi.Yayin);
            if (SunucuAdresi.Yayin != "") Assert.That(SunucuAdresi.Gecerli(SunucuAdresi.Yayin, out _), Is.True);
        }

        [Test]
        public void Gecerli_SadeceWsVeWss()
        {
            Assert.That(SunucuAdresi.Gecerli("ws://localhost:5000/ws", out _), Is.True);
            Assert.That(SunucuAdresi.Gecerli("wss://ornek.com/ws", out var u), Is.True);
            Assert.That(u.Host, Is.EqualTo("ornek.com"));
            Assert.That(SunucuAdresi.Gecerli("https://ornek.com/ws", out _), Is.False);
            Assert.That(SunucuAdresi.Gecerli("", out _), Is.False);
            Assert.That(SunucuAdresi.Gecerli("localhost:5000", out _), Is.False);
        }
    }
}
