using System;
using System.Collections.Generic;
using Cakmak.Kurallar;

namespace TheLighter.Ag
{
    /// <summary>Bir sohbet mesajı. <see cref="Kanal"/> null ise genel sohbet, doluysa o kişiyle DM.</summary>
    public sealed class SohbetMesaji
    {
        public OyuncuId? Kanal;
        public OyuncuId Kim;
        public string Metin;
        public bool Benim;
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

        static string Anahtar(OyuncuId? kanal) => kanal.HasValue ? kanal.Value.ToString() : "genel";

        /// <summary>DM açılmış kişiler, ilk mesaj sırasıyla (sekmeler için).</summary>
        public IReadOnlyList<OyuncuId> DmKanallari => dmSirasi;

        public IReadOnlyList<SohbetMesaji> Mesajlar(OyuncuId? kanal) =>
            kanallar.TryGetValue(Anahtar(kanal), out var l) ? (IReadOnlyList<SohbetMesaji>)l : Array.Empty<SohbetMesaji>();

        public int Okunmamis(OyuncuId? kanal) => okunmamis.TryGetValue(Anahtar(kanal), out int n) ? n : 0;

        public int ToplamOkunmamis
        {
            get
            {
                int t = 0;
                foreach (var n in okunmamis.Values) t += n;
                return t;
            }
        }

        public void Okundu(OyuncuId? kanal) => okunmamis[Anahtar(kanal)] = 0;

        /// <summary>DM sekmesini mesaj gelmeden açar (bir oyuncuya "DM at").</summary>
        public void DmAc(OyuncuId kim)
        {
            if (!dmSirasi.Contains(kim)) dmSirasi.Add(kim);
        }

        /// <summary>Gelen ya da giden mesajı kanala ekler. Başkasından gelen okunmamış sayılır.</summary>
        public void Ekle(SohbetMesaji m)
        {
            if (m == null || string.IsNullOrEmpty(m.Metin)) return;
            string a = Anahtar(m.Kanal);
            if (!kanallar.TryGetValue(a, out var l)) kanallar[a] = l = new List<SohbetMesaji>();
            l.Add(m);
            if (l.Count > KanalBasinaMesaj) l.RemoveAt(0);
            if (m.Kanal.HasValue) DmAc(m.Kanal.Value);
            if (!m.Benim) okunmamis[a] = Okunmamis(m.Kanal) + 1;
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
