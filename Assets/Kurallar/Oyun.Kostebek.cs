using System.Collections.Generic;

namespace Cakmak.Kurallar
{
    // Köstebek (docs/Oyunlar.md, 1): grubun içinde gizli görevli bir hain. Turlar normal çakmak, herkesin gizli görevi
    // var. Her 5 turda bir ve erken suçlamayla "Köstebek kim?" oylaması (Oyun.Suclama.cs). Köstebek görevini bitirince
    // ya da yakalanmadan oyun bitince kazanır.

    /// <summary>Gizli rol, sadece sahibinin görünümünde (<see cref="Gorunum.Rolun"/>). Yeni roller sona.</summary>
    public enum Rol { Yok, Kostebek, Casus }

    public enum KostebekKazanan { Grup, Kostebek }

    public enum KostebekNedeni
    {
        /// <summary>Grup kazandı: oylamada en çok oyu köstebek aldı.</summary>
        Yakalandi,
        /// <summary>Köstebek kazandı: gizli görevini bitirdi.</summary>
        GorevBitti,
        /// <summary>Köstebek kazandı: son oylamada da yakalanmadı (ya da oyun yarıda kesildi).</summary>
        Yakalanmadi,
        /// <summary>Grup kazandı: köstebek oyundan çıktı.</summary>
        Kacti,
    }

    /// <summary>Oyun sonunda herkese: köstebek kimdi, kim kazandı. Oyun bitmeden hiçbir yerde yok.</summary>
    public sealed class KostebekSonucu
    {
        public OyuncuId Kostebek;
        public KostebekKazanan Kazanan;
        public KostebekNedeni Neden;

        internal KostebekSonucu Kopya() => (KostebekSonucu)MemberwiseClone();
    }

    public sealed partial class Oyun
    {
        /// <summary>Bu kadar turda bir "Köstebek kim?" oylaması (6., 11., 16. turun başında, A sormadan önce).</summary>
        public const int SuclamaAraligi = 5;

        readonly struct KostebekGorevi
        {
            public readonly GorevTuru Tur;
            public readonly int Adet;
            public readonly bool Hedefli;

            public KostebekGorevi(GorevTuru tur, int adet, bool hedefli = false)
            {
                Tur = tur;
                Adet = adet;
                Hedefli = hedefli;
            }
        }

        /// <summary>
        /// Köstebeğin görevi: mevcut görev türlerinden oyun ortasında bitebilenler ("hiç..." görevleri olmaz), köstebeğe göre
        /// adetle. Çoğu başkalarının seçimine bağlı: grup köstebekten şüphelenip onu göstermezse görevi zorlaşır.
        /// ponytail: adetler tahmin; Playtest'te ayarlanır (rastgele oyunla ayarlanan unvan ağırlıkları gibi).
        /// </summary>
        static readonly KostebekGorevi[] KostebekGorevleri =
        {
            new KostebekGorevi(GorevTuru.GumeyeGonder, 2),
            new KostebekGorevi(GorevTuru.KisiyiGoster, 2, hedefli: true),
            new KostebekGorevi(GorevTuru.AyniKisiyeSor, 2),
            new KostebekGorevi(GorevTuru.FarkliKisiGoster, 3),
            new KostebekGorevi(GorevTuru.FarkliKisiyeSor, 3),
            new KostebekGorevi(GorevTuru.MiniOyunKazan, 3),
            new KostebekGorevi(GorevTuru.UstUsteKazan, 2),
            new KostebekGorevi(GorevTuru.IfsaEttir, 2),
            new KostebekGorevi(GorevTuru.HedefiYen, 1, hedefli: true),
            new KostebekGorevi(GorevTuru.KisiyeSor, 2, hedefli: true),
            new KostebekGorevi(GorevTuru.SoruSor, 3),
            new KostebekGorevi(GorevTuru.CokGosterilme, 3),
        };

        KostebekSonucu kostebekSonucu;
        readonly HashSet<OyuncuId> suclamaKullanan = new HashSet<OyuncuId>();

        bool Kostebekli => ayar.Oyun == OyunTuru.Kostebek;
        /// <summary>Köstebek'te herkesin gizli görevi var: köstebeğin garip hamleleri göze batmasın.</summary>
        bool GorevlerAcik => ayar.GizliGorevler || Kostebekli;

        // ------------------------------------------------------------ Komutlar

        /// <summary>
        /// Erken suçlama: herkesin oyun boyu 1 hakkı. Sadece A soru sormadan önce: oylama hemen başlar,
        /// bitince A'nın turu baştan (tam süre). Düellonun ortası kesilmez.
        /// </summary>
        public Sonuc Sucla(OyuncuId kim)
        {
            if (!Kostebekli || asama != Asama.SoruSorma) return Sonuc.YanlisAsama;
            if (Bul(kim) == null || cikan.Contains(kim)) return Sonuc.SiraSendeDegil;
            if (!suclamaKullanan.Add(kim)) return Sonuc.HakKalmadi;
            SuclamayiBaslat(kim);
            return Sonuc.Tamam;
        }

        // ------------------------------------------------------------ Akış

        /// <summary>Kurulumda, görevler dağıtıldıktan sonra: köstebek seçilir, normal görevinin yerine köstebek görevi alır.</summary>
        void KostebekKur()
        {
            if (!Kostebekli) return;
            hain = oyuncular[rastgele.Next(oyuncular.Count)].Id;
            var secim = KostebekGorevleri[rastgele.Next(KostebekGorevleri.Length)];
            IzAl(hain).Gorev = new Gorev
            {
                Tur = secim.Tur,
                Adet = secim.Adet,
                Zorluk = Zorluk.Zor,
                Hedef = secim.Hedefli ? RastgeleOyuncu(hain) : (OyuncuId?)null,
            };
        }

        /// <summary>Tur sınırında (yeni tur başlamadan, son oylamadan önce): köstebek görevini bitirdiyse kazanır. true: oyun bitti.</summary>
        bool KostebekGoreviBitti()
        {
            if (!Kostebekli || !GorevBasarili(Bul(hain), IzAl(hain))) return false;
            KostebekBitir(KostebekKazanan.Kostebek, KostebekNedeni.GorevBitti);
            return true;
        }

        void KostebekYakalandi()
        {
            if (Kostebekli) KostebekBitir(KostebekKazanan.Grup, KostebekNedeni.Yakalandi);
        }

        void KostebekYakalanmadi()
        {
            if (Kostebekli) KostebekBitir(KostebekKazanan.Kostebek, KostebekNedeni.Yakalanmadi);
        }

        void KostebekKacti()
        {
            if (Kostebekli) KostebekBitir(KostebekKazanan.Grup, KostebekNedeni.Kacti);
        }

        /// <summary>Oyun bitiyor ve sonuç önceden yazılmadı (oyun zorla bitti): köstebek yakalanmadı, görevi bittiyse o sayılır.</summary>
        void KostebekSonucunuYaz()
        {
            if (!Kostebekli || kostebekSonucu != null) return;
            var neden = GorevBasarili(Bul(hain), IzAl(hain)) ? KostebekNedeni.GorevBitti : KostebekNedeni.Yakalanmadi;
            kostebekSonucu = new KostebekSonucu { Kostebek = hain, Kazanan = KostebekKazanan.Kostebek, Neden = neden };
        }

        void KostebekBitir(KostebekKazanan kazanan, KostebekNedeni neden)
        {
            kostebekSonucu = new KostebekSonucu { Kostebek = hain, Kazanan = kazanan, Neden = neden };
            Bitir();
        }

        // ------------------------------------------------------------ Görünüm

        void KostebekGorunumu(Gorunum g, OyuncuId kim)
        {
            if (!Kostebekli) return;
            if (kim == hain) g.Rolun = Rol.Kostebek;
            g.SuclamaHakkin = !suclamaKullanan.Contains(kim) && !cikan.Contains(kim) && asama != Asama.OyunSonu;
        }
    }
}
