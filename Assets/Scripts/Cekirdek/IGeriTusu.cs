using TheLighter.Akis;

namespace TheLighter.Istemci
{
    /// <summary>
    /// Android geri tuşunu (PC'de Esc) karşılayan ekran (tasks/g2-android.md 3). Ne yapılacağına <see cref="GeriTusu"/>
    /// karar verir. Açık katmanı <see cref="Uygulama"/> kapatır (en üstteki görünür "menu-perde"; kapatırken bir şey
    /// yapılacaksa <see cref="Arayuz.Kapaninca"/>), "çıkılsın mı?" sorusunu ekran kendisi sorar.
    /// </summary>
    public interface IGeriTusu
    {
        GeriEkrani GeriTuru { get; }

        /// <summary>"Çıkılsın mı?": ana menüde uygulamadan, odada odadan, oyunda oyundan. Sormadan hiçbir şey kapanmaz.</summary>
        void CikisSor();
    }
}
