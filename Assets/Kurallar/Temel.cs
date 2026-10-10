using System;
using System.Collections.Generic;

namespace Cakmak.Kurallar
{
    /// <summary>Oyuncunun kimliği. Ağda "p3" diye yazılır.</summary>
    public readonly struct OyuncuId : IEquatable<OyuncuId>
    {
        public readonly int Deger;

        public OyuncuId(int deger) { Deger = deger; }

        public bool Equals(OyuncuId diger) => Deger == diger.Deger;
        public override bool Equals(object obj) => obj is OyuncuId diger && Equals(diger);
        public override int GetHashCode() => Deger;
        public static bool operator ==(OyuncuId x, OyuncuId y) => x.Deger == y.Deger;
        public static bool operator !=(OyuncuId x, OyuncuId y) => x.Deger != y.Deger;
        public override string ToString() => "p" + Deger;
    }

    public sealed class Oyuncu
    {
        public readonly OyuncuId Id;
        public readonly string Isim;

        public Oyuncu(OyuncuId id, string isim)
        {
            Id = id;
            Isim = isim;
        }
    }

    /// <summary>Her komutun cevabı. Kural kodu hata fırlatmaz.</summary>
    public enum Sonuc
    {
        Tamam,
        SiraSendeDegil,
        Secilemez,
        YanlisAsama,
        GecersizGirdi,
        HakKalmadi,
        HavuzBos,
        OyuncuSayisiGecersiz,
    }

    public enum Asama
    {
        SoruToplama,   // Curcuna başı: herkes 2 soru yazar
        SoruSorma,     // A, B'yi seçer ve soruyu gönderir
        CevapSecme,    // B, C'yi seçer
        MiniOyun,      // B ile C oynar
        Ifsa,          // C kazandı, soru herkese açık
        Gume,          // B kazandı, soru gizli kaldı
        OyunSonu,
    }

    public enum OyunModu { Normal, Temiz, Curcuna }

    public enum MiniOyunTuru { Tkm, TekCift, Zar, Reaksiyon, Karisik }

    public enum TkmSecim { Tas, Kagit, Makas }

    /// <summary>Mini oyundaki tek hamle. Fabrika metotlarıyla kurulur.</summary>
    public readonly struct Hamle
    {
        public readonly MiniOyunTuru Tur;
        public readonly TkmSecim Tkm;
        /// <summary>Tek-Çift: B'nin tahmini. C için anlamsız.</summary>
        public readonly bool TekDiyor;
        /// <summary>Tek-Çift: 1–5 parmak. Reaksiyon: tepki süresi (ms), eksi = erken bastı.</summary>
        public readonly int Sayi;

        Hamle(MiniOyunTuru tur, TkmSecim tkm, bool tekDiyor, int sayi)
        {
            Tur = tur;
            Tkm = tkm;
            TekDiyor = tekDiyor;
            Sayi = sayi;
        }

        /// <summary>
        /// Reaksiyonda kırmızıyken (ya da insan dışı hızda) basıldı mı? Kuralın tek tanımı burada.
        /// İstemci "Erken bastın" yazısı için de bunu kullanır, eşiği kendisi karşılaştırmaz.
        /// </summary>
        public bool Erken => Tur == MiniOyunTuru.Reaksiyon && Sayi < Oyun.ReaksiyonEnAzMs;

        public static Hamle TasKagitMakas(TkmSecim secim) => new Hamle(MiniOyunTuru.Tkm, secim, false, 0);
        public static Hamle TekCift(int parmak, bool tekDiyor = false) => new Hamle(MiniOyunTuru.TekCift, default, tekDiyor, parmak);
        public static Hamle ZarAt() => new Hamle(MiniOyunTuru.Zar, default, false, 0);
        public static Hamle Reaksiyon(int milisaniye) => new Hamle(MiniOyunTuru.Reaksiyon, default, false, milisaniye);
    }

    public sealed class Ayarlar
    {
        public OyunModu Mod = OyunModu.Normal;
        public MiniOyunTuru MiniOyun = MiniOyunTuru.Tkm;
        /// <summary>0 = sınırsız, kurucu bitirir.</summary>
        public int TurSayisi = 15;
        public int SoruSuresiSn = 60;
        public int CevapSuresiSn = 20;
        public int HamleSuresiSn = 10;
        public int ToplamaSuresiSn = 90;
        public int SonucSuresiSn = 6;
        /// <summary>Seçilen paketlerin soruları (<see cref="Paket.Ayristir"/> ile). Öneri çek ve Curcuna boşlukları buradan.</summary>
        public IReadOnlyList<string> OneriSorulari = Array.Empty<string>();
    }
}
