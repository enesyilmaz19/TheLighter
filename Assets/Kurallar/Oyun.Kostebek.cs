using System.Collections.Generic;

namespace Cakmak.Kurallar
{
    // Köstebek (docs/Oyunlar.md, 1): grubun içinde gizli görevli bir hain. Turlar normal çakmak, herkesin gizli görevi
    // var. Her 5 turda bir ve erken suçlamayla "Köstebek kim?" oylaması. Köstebek görevini bitirince ya da yakalanmadan
    // oyun bitince kazanır.

    /// <summary>Gizli rol, sadece sahibinin görünümünde (<see cref="Gorunum.Rolun"/>). Yeni roller sona.</summary>
    public enum Rol { Yok, Kostebek }

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

        OyuncuId kostebek;
        KostebekSonucu kostebekSonucu;
        readonly List<OyuncuId> supheliler = new List<OyuncuId>();
        readonly HashSet<OyuncuId> suclamaKullanan = new HashSet<OyuncuId>();
        readonly Dictionary<OyuncuId, OyuncuId> suclamaOylari = new Dictionary<OyuncuId, OyuncuId>();
        bool sonSuclama;

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

        /// <summary>"Köstebek kim?" oyu: oyundan çıkmamış herkes bir kez, kendisi dışında oyundaki birine.</summary>
        public Sonuc SuclamaOyu(OyuncuId kim, OyuncuId hedef)
        {
            if (asama != Asama.Suclama) return Sonuc.YanlisAsama;
            if (!oylayanlar.Contains(kim)) return Sonuc.SiraSendeDegil;
            if (suclamaOylari.ContainsKey(kim)) return Sonuc.HakKalmadi;
            if (hedef == kim || Bul(hedef) == null || cikan.Contains(hedef)) return Sonuc.Secilemez;

            suclamaOylari[kim] = hedef;
            Yay(new Olay { Tur = OlayTuru.OyVerildi, Kim = kim });
            if (suclamaOylari.Count == oylayanlar.Count) SuclamayiBitir();
            return Sonuc.Tamam;
        }

        // ------------------------------------------------------------ Akış

        /// <summary>Kurulumda, görevler dağıtıldıktan sonra: köstebek seçilir, normal görevinin yerine köstebek görevi alır.</summary>
        void KostebekKur()
        {
            if (!Kostebekli) return;
            kostebek = oyuncular[rastgele.Next(oyuncular.Count)].Id;
            var secim = KostebekGorevleri[rastgele.Next(KostebekGorevleri.Length)];
            IzAl(kostebek).Gorev = new Gorev
            {
                Tur = secim.Tur,
                Adet = secim.Adet,
                Zorluk = Zorluk.Zor,
                Hedef = secim.Hedefli ? RastgeleOyuncu(kostebek) : (OyuncuId?)null,
            };
        }

        /// <summary>Tur sınırında (yeni tur başlamadan): köstebek görevini bitirdiyse kazanır. true: oyun bitti.</summary>
        bool KostebekGoreviBitti()
        {
            if (!Kostebekli || !GorevBasarili(Bul(kostebek), IzAl(kostebek))) return false;
            KostebekBitir(KostebekKazanan.Kostebek, KostebekNedeni.GorevBitti);
            return true;
        }

        /// <summary>Yeni tur başladı: her 5 turda bir oylama.</summary>
        void PeriyodikSuclama()
        {
            if (Kostebekli && tur > 1 && (tur - 1) % SuclamaAraligi == 0) SuclamayiBaslat(null);
        }

        /// <summary>
        /// Oyun bitecek (tur sayısı doldu ya da kurucu bitirdi): Köstebek'te önce son oylama. Kurucu da köstebek olabilir:
        /// oyunu kendisi bitirip kazanamasın. Görevini bu turda bitirmiş köstebek oylamaya kalmadan kazanır.
        /// true: oylama başladı ya da oyun bitti.
        /// </summary>
        bool SonSuclamayiBaslat()
        {
            if (!Kostebekli || sonSuclama) return false;
            if (KostebekGoreviBitti()) return true;
            SoruyuKapat();
            sonSuclama = true;
            SuclamayiBaslat(null);
            return true;
        }

        void SuclamayiBaslat(OyuncuId? suclayan)
        {
            oylayanlar.Clear();
            suclamaOylari.Clear();
            foreach (var o in oyuncular)
                if (!cikan.Contains(o.Id)) oylayanlar.Add(o.Id);
            AsamayaGec(Asama.Suclama, ayar.SuclamaSuresiSn);
            Yay(new Olay { Tur = OlayTuru.SuclamaBasladi, Kim = suclayan });
        }

        /// <summary>En çok oyu alan suçlanır; eşitlikte ya da hiç oy yoksa kimse. Köstebekse grup kazanır.</summary>
        void SuclamayiBitir()
        {
            var sayilar = new Dictionary<OyuncuId, int>();
            foreach (var hedef in suclamaOylari.Values) Artir(sayilar, hedef);
            OyuncuId? suclanan = null;
            int enCok = 0;
            bool esit = false;
            foreach (var s in sayilar)
            {
                if (s.Value > enCok) { enCok = s.Value; suclanan = s.Key; esit = false; }
                else if (s.Value == enCok) esit = true;
            }
            if (esit) suclanan = null;
            suclamaOylari.Clear();
            oylayanlar.Clear();
            Yay(new Olay { Tur = OlayTuru.SuclamaBitti, Kim = suclanan });

            if (suclanan == kostebek) { KostebekBitir(KostebekKazanan.Grup, KostebekNedeni.Yakalandi); return; }
            if (suclanan.HasValue && !supheliler.Contains(suclanan.Value)) supheliler.Add(suclanan.Value);
            if (sonSuclama) { KostebekBitir(KostebekKazanan.Kostebek, KostebekNedeni.Yakalanmadi); return; }

            // A'nın turu baştan. A bu arada oyundan çıktıysa, soru sormadan çıkan A gibi turu yanar.
            if (cikan.Contains(a))
            {
                OncekiTuruUnut();
                TurBaslat(RastgeleOyuncu(a));
            }
            else AsamayaGec(Asama.SoruSorma, ayar.SoruSuresiSn);
        }

        /// <summary>Oyun bitiyor ve sonuç önceden yazılmadı (oyun zorla bitti): köstebek yakalanmadı, görevi bittiyse o sayılır.</summary>
        void KostebekSonucunuYaz()
        {
            if (!Kostebekli || kostebekSonucu != null) return;
            var neden = GorevBasarili(Bul(kostebek), IzAl(kostebek)) ? KostebekNedeni.GorevBitti : KostebekNedeni.Yakalanmadi;
            kostebekSonucu = new KostebekSonucu { Kostebek = kostebek, Kazanan = KostebekKazanan.Kostebek, Neden = neden };
        }

        void KostebekBitir(KostebekKazanan kazanan, KostebekNedeni neden)
        {
            kostebekSonucu = new KostebekSonucu { Kostebek = kostebek, Kazanan = kazanan, Neden = neden };
            Bitir();
        }

        /// <summary>Oyundan çıkan köstebekse grup kazanır. true: oyun bitti.</summary>
        bool KostebekKacti(OyuncuId kim)
        {
            if (!Kostebekli || kim != kostebek) return false;
            KostebekBitir(KostebekKazanan.Grup, KostebekNedeni.Kacti);
            return true;
        }

        // ------------------------------------------------------------ Görünüm

        void KostebekGorunumu(Gorunum g, OyuncuId kim)
        {
            if (!Kostebekli) return;
            if (kim == kostebek) g.Rolun = Rol.Kostebek;
            g.SuclamaHakkin = !suclamaKullanan.Contains(kim) && !cikan.Contains(kim) && asama != Asama.OyunSonu;
            g.Supheliler = new List<OyuncuId>(supheliler);
            if (asama == Asama.Suclama)
            {
                g.Oylayanlar = new List<OyuncuId>(oylayanlar);
                g.OyVerdin = suclamaOylari.ContainsKey(kim);
                g.OyKullanan = suclamaOylari.Count;
                g.SonSuclama = sonSuclama;
            }
        }
    }
}
