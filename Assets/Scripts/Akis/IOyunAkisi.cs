using System;
using System.Collections.Generic;
using Cakmak.Kurallar;

namespace TheLighter.Akis
{
    /// <summary>
    /// Oyun ekranının konuştuğu akış. İki modu var: elden ele (<see cref="EldenEleAkisi"/>, kural motoru cihazda)
    /// ve online (sunucu tek otorite; istemcide <see cref="Oyun"/> yok). Ekran parçaları ikisinde de aynı:
    /// neyin gösterileceği <see cref="Ekran"/>'dan okunur, kural hesaplanmaz.
    /// </summary>
    public interface IOyunAkisi
    {
        EkranDurumu Ekran { get; }

        /// <summary>Online: her cihaz tek oyuncu, perde yok, komutun sonucu sonradan gelir.</summary>
        bool Online { get; }

        /// <summary>Online'da bu cihazın oyuncusu. Elden elede null (telefon herkesin).</summary>
        OyuncuId? Ben { get; }

        /// <summary>Online: bu cihaz odanın kurucusu (oyunu bitirebilir, tekrar başlatabilir).</summary>
        bool Kurucuyum { get; }

        IReadOnlyList<Oyuncu> Oyuncular { get; }

        /// <summary>Mod, mini oyun ve "fikir ver" için öneri soruları.</summary>
        Ayarlar Ayarlar { get; }

        /// <summary>Menü açıkken true. Elden elede saat ilerlemez; online'da sunucunun saati durmaz.</summary>
        bool Duraklatildi { get; set; }

        bool Bitti { get; }

        void Ilerle(TimeSpan gecen);

        // --- Komutlar. Online'da Tamam "gönderildi" demek; hata gelirse sonradan bildirilir. ---

        void PerdeyiKaldir();
        void GorevGordum();
        Sonuc HavuzaSoruEkle(string metin);
        Sonuc SoruGonder(OyuncuId b, string metin);
        Sonuc Fisildadim(OyuncuId b);
        Sonuc OneriCek();
        Sonuc SoruyuDegistir();
        Sonuc CevapSec(OyuncuId c);
        Sonuc Hamle(OyuncuId kim, Hamle hamle);
        Sonuc Oy(bool ifsa);
        Sonuc IkiyeKatla();
        Sonuc BedelOde();
        Sonuc KarariGec();
        void CezalariOnayla();
        Sonuc OyunuBitir();

        // --- Yardımcılar ---

        /// <summary>Renk numarası (renk listesindeki sıra).</summary>
        int Sira(OyuncuId id);

        string Isim(OyuncuId? id);
    }
}
