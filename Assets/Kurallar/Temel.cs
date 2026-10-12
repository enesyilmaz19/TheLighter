using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

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

        /// <summary>
        /// İki isim aynı kişi mi: anahtarları eşitse aynı. Kural motoru, sunucu ve istemci hep bunu kullanır.
        /// Önce <see cref="TemizMetin.Temizle"/> (görünmez karakterler, boşluklar), sonra büyük/küçük harf ve
        /// I/İ/ı/i farkı gözetilmez: "İrem" = "irem", "ILKER" = "ılker", "Ali" = sıfır genişlikli boşluklu "Ali".
        /// Harfler küçültmeden önce değiştirilir: sonuç kültüre ve çalışma ortamına (Mono, .NET) göre değişmesin.
        /// Boş anahtar = geçersiz isim.
        /// </summary>
        public static string IsimAnahtari(string isim) =>
            TemizMetin.Temizle(isim).Replace('İ', 'i').Replace('I', 'i').Replace('ı', 'i').ToLowerInvariant();
    }

    /// <summary>
    /// Oyuncunun yazdığı her metin (isim, soru) için tek temizlik. Kontrol ve görünmez biçim karakterleri
    /// (sıfır genişlikli boşluk, yazı yönünü çeviren karakterler, Unicode etiketleri) atılır; her türlü boşluk
    /// tek boşluk olur; baş/son boşluk silinir. Böylece görünmez karakterle başkasının kılığına girilemez,
    /// başkalarının ekranı yön karakterleriyle bozulamaz, boş görünen isim/soru yazılamaz.
    /// </summary>
    public static class TemizMetin
    {
        public static string Temizle(string metin)
        {
            if (string.IsNullOrEmpty(metin)) return "";
            var sb = new StringBuilder(metin.Length);
            bool boslukBekliyor = false;
            for (int i = 0; i < metin.Length; i++)
            {
                // Kategori kod noktasına göre: BMP dışındaki etiket karakterleri de (U+E0000...) yakalansın.
                int uzunluk = char.IsSurrogatePair(metin, i) ? 2 : 1;
                var tur = CharUnicodeInfo.GetUnicodeCategory(metin, i);
                if (uzunluk == 1 && (char.IsWhiteSpace(metin[i]) || BosGorunur(metin[i])))
                {
                    boslukBekliyor = sb.Length > 0;
                    continue;
                }
                // Tek başına kalmış vekil (bozuk UTF-16) de atılır. Tanımsız karakter atılmaz: Unity'nin Unicode
                // tablosu eski olabilir, yeni bir emojiyi sunucu tutup istemci atarsa isim anahtarları ayrışır.
                if (tur == UnicodeCategory.Control || tur == UnicodeCategory.Format || tur == UnicodeCategory.Surrogate)
                {
                    i += uzunluk - 1;
                    continue;
                }
                if (boslukBekliyor) { sb.Append(' '); boslukBekliyor = false; }
                sb.Append(metin, i, uzunluk);
                i += uzunluk - 1;
            }
            return sb.ToString();
        }

        /// <summary>Harf ya da simge sayılan ama ekranda boş görünen karakterler ("görünmez isim" hilesi).</summary>
        static bool BosGorunur(char c) =>
            c == '\u115F' || c == '\u1160' || c == '\u3164' || c == '\uFFA0' || c == '\u2800'; // Hangul dolgu ve boş Braille
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
        /// <summary>Temiz mod: soru filtreye takıldı, gönderilmedi (<see cref="TemizFiltre"/>).</summary>
        Uygunsuz,
    }

    public enum Asama
    {
        SoruToplama,   // Curcuna başı: herkes 2 soru yazar
        SoruSorma,     // A, B'yi seçer ve soruyu gönderir
        CevapSecme,    // B, C'yi seçer
        MiniOyun,      // B ile C oynar
        Oylama,        // kaos Grup Kararı: mini oyun yerine A ve B dışındakiler oylar
        IkiyeKatla,    // kaos İkiye Katla: kaybeden "bir el daha" der mi (5 sn)
        Bedel,         // soru ifşa olmak üzere: B bedel öder mi (5 sn)
        Ifsa,          // soru herkese açık
        Gume,          // soru gizli kaldı
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

        // --- S3: tekrar oynatma (Ana Plan 8). Varsayılan hepsi kapalı; "Hızlı başla" istemcide açar.

        /// <summary>Temiz modda en fazla Hafif.</summary>
        public CezaSeviyesi CezaSeviyesi = CezaSeviyesi.Kapali;
        public bool GizliGorevler;
        public bool KaosTurlari;
        /// <summary>🗣️ sesli ceza metinleri (Assets/Icerik/Cezalar.csv, <see cref="Paket.Ayristir"/> ile). Sadece Cesur'da çekilir.</summary>
        public IReadOnlyList<string> SesliCezalar = Array.Empty<string>();
        public int KararSuresiSn = 5;
        /// <summary>Elden ele (tek cihaz). Açıkken kaos turlarında "Soran Gizli" çıkmaz: herkes telefonu kimin tuttuğunu görüyor.</summary>
        public bool TekCihaz;
    }
}
