namespace TheLighter.Akis
{
    /// <summary>Geri tuşunun anlamı için ekranın türü.</summary>
    public enum GeriEkrani
    {
        /// <summary>Kullanım şartları kabul ekranı (ilk açılış).</summary>
        Kabul,
        AnaMenu,
        /// <summary>Elden ele oyuncu ve ayar ekranı.</summary>
        Kurulum,
        OnlineMenu,
        /// <summary>"Oyunu güncelle" ekranı.</summary>
        Guncelle,
        Lobi,
        BeklemeOdasi,
        /// <summary>Oyun ekranı (elden ele ya da online).</summary>
        Oyun,
    }

    public enum GeriEylemi
    {
        /// <summary>Hiçbir şey: "Bağlanıyor…" katmanının kendi "Vazgeç"i var, geri tuşu kazara odadan çıkarmasın.</summary>
        Yok,
        /// <summary>En üstteki açık katmanı (menü, ayarlar, sohbet, onay, rapor…) kapat.</summary>
        KatmaniKapat,
        /// <summary>Bir önceki ekran: ana menü.</summary>
        AnaMenuyeDon,
        /// <summary>"Çıkılsın mı?" diye sor: ana menüde uygulamadan, odada odadan, oyunda oyundan.</summary>
        CikisSor,
    }

    /// <summary>
    /// Android geri tuşu (Input System'da <c>escapeKey</c>; PC'de Esc): ekran ekran ne yapar (tasks/g2-android.md 3).
    /// Açık katmanı kapatır, menüde bir önceki ekrana döner, odada ve oyunda "çıkılsın mı?" sorar.
    /// Hiçbir yerde uygulamayı habersiz kapatmaz: ana menüde de sorar.
    /// </summary>
    public static class GeriTusu
    {
        /// <param name="ekran">Şu anki ekran.</param>
        /// <param name="acikKatman">Ekranda kapatılabilir bir katman açık mı.</param>
        /// <param name="baglaniyor">"Bağlanıyor…" katmanı açık mı (bağlantı koptu, yeniden deneniyor).</param>
        public static GeriEylemi Ne(GeriEkrani ekran, bool acikKatman, bool baglaniyor)
        {
            if (baglaniyor) return GeriEylemi.Yok;
            if (acikKatman) return GeriEylemi.KatmaniKapat;
            switch (ekran)
            {
                case GeriEkrani.OnlineMenu:
                case GeriEkrani.Guncelle:
                    return GeriEylemi.AnaMenuyeDon;
                default:
                    // Kabul, ana menü, lobi, bekleme odası, oyun: sormadan çıkılmaz.
                    return GeriEylemi.CikisSor;
            }
        }
    }

    /// <summary>
    /// Kullanım şartlarının kabulü (Google Play: kullanıcı içerik yazmadan önce). Metin docs/yasal/KullanimSartlari.md'nin
    /// sonunda. Kabul edilen sürüm cihazda saklanır; şartlar önemli ölçüde değişince <see cref="Surum"/> artar, ekran yeniden çıkar.
    /// </summary>
    public static class Sartlar
    {
        public const int Surum = 1;

        /// <param name="kabulEdilen">Cihazda kayıtlı kabul edilen sürüm (hiç yoksa 0).</param>
        public static bool KabulGerekli(int kabulEdilen) => kabulEdilen <= Surum;
    }
}
