using System;
using System.Collections.Generic;
using Cakmak.Kurallar;

namespace TheLighter.Ag
{
    /// <summary>
    /// Sohbetin bir yeri: genel, bir kişiyle DM ya da adlı kanal (takım, katil, ölüler; kim yazıp okur kural motorunda).
    /// </summary>
    public readonly struct SohbetYeri : IEquatable<SohbetYeri>
    {
        /// <summary>DM: karşıdaki kişi. Genel ve adlı kanalda null.</summary>
        public readonly OyuncuId? Dm;
        /// <summary>Adlı kanal (takım, katil, ölüler). Genel ve DM'de null.</summary>
        public readonly SohbetKanali? Kanal;

        SohbetYeri(OyuncuId? dm, SohbetKanali? kanal) { Dm = dm; Kanal = kanal; }

        public static SohbetYeri Genel => default;
        public static SohbetYeri DmIle(OyuncuId kim) => new SohbetYeri(kim, null);
        public static SohbetYeri Adli(SohbetKanali kanal) => new SohbetYeri(null, kanal);
        /// <summary>Eski biçim: null genel, dolu DM.</summary>
        public static SohbetYeri Kimden(OyuncuId? dm) => dm.HasValue ? DmIle(dm.Value) : Genel;

        public bool GenelMi => !Dm.HasValue && !Kanal.HasValue;

        /// <summary>Defterdeki anahtar ve protokoldeki <c>kime</c> (genelde "genel"): "p3", "takim", "katil", "oluler".</summary>
        public string Anahtar => Dm.HasValue ? Dm.Value.ToString() : Kanal.HasValue ? Cevirici.AdYaz(Kanal.Value) : "genel";

        public bool Equals(SohbetYeri d) => Dm == d.Dm && Kanal == d.Kanal;
        public override bool Equals(object o) => o is SohbetYeri d && Equals(d);
        public override int GetHashCode() => Anahtar.GetHashCode();
        public static bool operator ==(SohbetYeri a, SohbetYeri b) => a.Equals(b);
        public static bool operator !=(SohbetYeri a, SohbetYeri b) => !a.Equals(b);
        public override string ToString() => Anahtar;
    }

    /// <summary>Bir sohbet mesajı. <see cref="Kanal"/> doluysa o kişiyle DM, <see cref="OzelKanal"/> doluysa adlı kanal, ikisi de boşsa genel.</summary>
    public sealed class SohbetMesaji
    {
        public OyuncuId? Kanal;
        public SohbetKanali? OzelKanal;
        public OyuncuId Kim;
        public string Metin;
        public bool Benim;

        public SohbetYeri Yer => OzelKanal.HasValue ? SohbetYeri.Adli(OzelKanal.Value) : SohbetYeri.Kimden(Kanal);
    }

    /// <summary>
    /// Sohbet ve DM'in istemci tarafı (Ana Plan 6.4): kanallar, okunmamış sayıları, uzunluk ve hız sınırı.
    /// Ağ yok: protokol (docs/Protokol.md) gelince <see cref="OnlineIstemci"/> mesajları buraya ekleyecek.
    /// Sınırlar sunucuda da denetlenecek; burada sadece oyuncu boşuna yazmasın diye.
    /// </summary>
    public sealed class SohbetDefteri
    {
        /// <summary>Bir mesaj en fazla bu kadar karakter (Ana Plan 6.4).</summary>
        public const int EnFazlaKarakter = 200;
        /// <summary>Saniyede en fazla bir mesaj.</summary>
        public const double MesajArasiSn = 1.0;
        /// <summary>Her kanalda saklanan son mesaj sayısı. Eskiler düşer.</summary>
        public const int KanalBasinaMesaj = 100;

        readonly Dictionary<string, List<SohbetMesaji>> kanallar = new Dictionary<string, List<SohbetMesaji>>();
        readonly Dictionary<string, int> okunmamis = new Dictionary<string, int>();
        readonly List<OyuncuId> dmSirasi = new List<OyuncuId>();
        double sonGonderimdenBeri = double.MaxValue;

        /// <summary>DM açılmış kişiler, ilk mesaj sırasıyla (sekmeler için).</summary>
        public IReadOnlyList<OyuncuId> DmKanallari => dmSirasi;

        public IReadOnlyList<SohbetMesaji> Mesajlar(OyuncuId? kanal) => Mesajlar(SohbetYeri.Kimden(kanal));

        public IReadOnlyList<SohbetMesaji> Mesajlar(SohbetYeri yer) =>
            kanallar.TryGetValue(yer.Anahtar, out var l) ? (IReadOnlyList<SohbetMesaji>)l : Array.Empty<SohbetMesaji>();

        public int Okunmamis(OyuncuId? kanal) => Okunmamis(SohbetYeri.Kimden(kanal));

        public int Okunmamis(SohbetYeri yer) => okunmamis.TryGetValue(yer.Anahtar, out int n) ? n : 0;

        public int ToplamOkunmamis
        {
            get
            {
                int t = 0;
                foreach (var n in okunmamis.Values) t += n;
                return t;
            }
        }

        public void Okundu(OyuncuId? kanal) => Okundu(SohbetYeri.Kimden(kanal));

        public void Okundu(SohbetYeri yer) => okunmamis[yer.Anahtar] = 0;

        /// <summary>DM sekmesini mesaj gelmeden açar (bir oyuncuya "DM at").</summary>
        public void DmAc(OyuncuId kim)
        {
            if (!dmSirasi.Contains(kim)) dmSirasi.Add(kim);
        }

        /// <summary>Gelen ya da giden mesajı kanala ekler. Başkasından gelen okunmamış sayılır.</summary>
        public void Ekle(SohbetMesaji m)
        {
            if (m == null || string.IsNullOrEmpty(m.Metin)) return;
            var yer = m.Yer;
            string a = yer.Anahtar;
            if (!kanallar.TryGetValue(a, out var l)) kanallar[a] = l = new List<SohbetMesaji>();
            l.Add(m);
            if (l.Count > KanalBasinaMesaj) l.RemoveAt(0);
            if (yer.Dm.HasValue) DmAc(yer.Dm.Value);
            if (!m.Benim) okunmamis[a] = Okunmamis(yer) + 1;
        }

        /// <summary>Yazılanı gönderilecek hale getirir: baş/son boşluk atılır, boşsa ya da çok uzunsa null.</summary>
        public static string Hazirla(string metin)
        {
            string s = (metin ?? "").Trim();
            if (s.Length == 0 || s.Length > EnFazlaKarakter) return null;
            return s;
        }

        /// <summary>Hız sınırı: son gönderimden beri 1 sn geçti mi.</summary>
        public bool GonderebilirMi => sonGonderimdenBeri >= MesajArasiSn;

        /// <summary>Gönderildi: hız sayacı baştan.</summary>
        public void Gonderildi() => sonGonderimdenBeri = 0;

        public void Ilerle(TimeSpan gecen)
        {
            if (sonGonderimdenBeri < double.MaxValue) sonGonderimdenBeri += Math.Max(0, gecen.TotalSeconds);
        }

        /// <summary>Odadan çıkınca her şey silinir: sohbet cihazda saklanmaz.</summary>
        public void Temizle()
        {
            kanallar.Clear();
            okunmamis.Clear();
            dmSirasi.Clear();
            sonGonderimdenBeri = double.MaxValue;
        }
    }
}
