using System;
using System.Collections.Generic;
using Cakmak.Kurallar;

namespace TheLighter.Akis
{
    /// <summary>
    /// Takım Düellosu dağıtma ekranının (elden ele kurulum ve online lobi) durumu: kim hangi takımda (0 ya da 1).
    /// Boşsa takımlar rastgele ve dengeli dağıtılır. Kural yazılmaz: dağılım geçerli mi, kural motorunun kurulumuna
    /// sorulur (<see cref="Gecerli"/>, kuru <see cref="Oyun.Kur"/>).
    /// </summary>
    public sealed class TakimDagitimi
    {
        readonly Dictionary<OyuncuId, int> takim = new Dictionary<OyuncuId, int>();

        public TakimDagitimi() { }

        public TakimDagitimi(IEnumerable<KeyValuePair<OyuncuId, int>> dagilim) => Ayarla(dagilim);

        /// <summary>Şu anki dağılım (boş: rastgele).</summary>
        public IReadOnlyDictionary<OyuncuId, int> Dagilim => takim;

        /// <summary>Kimse takıma konmadı: oyun başlarken rastgele ve dengeli dağıtılır.</summary>
        public bool Rastgele => takim.Count == 0;

        public int? Takimi(OyuncuId id) => takim.TryGetValue(id, out var t) ? t : (int?)null;

        /// <summary>Dağılımı baştan yazar (lobide sunucudan gelen). Takım numarası 0 ya da 1 olmayan alınmaz.</summary>
        public void Ayarla(IEnumerable<KeyValuePair<OyuncuId, int>> dagilim)
        {
            takim.Clear();
            if (dagilim == null) return;
            foreach (var kv in dagilim)
                if (kv.Value >= 0 && kv.Value < Oyun.TakimSayisi) takim[kv.Key] = kv.Value;
        }

        /// <summary>Kişiyi takıma koyar (0 ya da 1; başka sayı yok sayılır).</summary>
        public void Koy(OyuncuId id, int t)
        {
            if (t >= 0 && t < Oyun.TakimSayisi) takim[id] = t;
        }

        /// <summary>"Rastgele" düğmesi: dağılımı siler.</summary>
        public void Temizle() => takim.Clear();

        /// <summary>
        /// Dokunuş: takımda olan öbür takıma geçer; takımı olmayan az kişili takıma (eşitse ilk takıma) girer.
        /// Kim nereye geçerse geçsin denge kuralı burada değil: <see cref="Gecerli"/> söyler.
        /// </summary>
        public void Dokun(OyuncuId id)
        {
            if (takim.TryGetValue(id, out var t))
            {
                takim[id] = (t + 1) % Oyun.TakimSayisi;
                return;
            }
            takim[id] = Sayi(1) < Sayi(0) ? 1 : 0;
        }

        /// <summary>Takımdaki kişi sayısı.</summary>
        public int Sayi(int t)
        {
            int n = 0;
            foreach (var kv in takim) if (kv.Value == t) n++;
            return n;
        }

        /// <summary>Artık listede olmayanlar (lobiden çıkan, silinen satır) dağılımdan düşer. Değiştiyse true.</summary>
        public bool Esitle(IEnumerable<OyuncuId> herkes)
        {
            var kalan = new HashSet<OyuncuId>(herkes ?? Array.Empty<OyuncuId>());
            var dusen = new List<OyuncuId>();
            foreach (var id in takim.Keys) if (!kalan.Contains(id)) dusen.Add(id);
            foreach (var id in dusen) takim.Remove(id);
            return dusen.Count > 0;
        }

        /// <summary>Listede olup takımı olmayanlar (rastgelede herkes).</summary>
        public List<OyuncuId> Takimsizlar(IEnumerable<OyuncuId> herkes)
        {
            var l = new List<OyuncuId>();
            if (herkes == null) return l;
            foreach (var id in herkes) if (!takim.ContainsKey(id)) l.Add(id);
            return l;
        }

        /// <summary>Takımın üyeleri, <paramref name="herkes"/> sırasıyla.</summary>
        public List<OyuncuId> Uyeler(IEnumerable<OyuncuId> herkes, int t)
        {
            var l = new List<OyuncuId>();
            if (herkes == null) return l;
            foreach (var id in herkes) if (takim.TryGetValue(id, out var x) && x == t) l.Add(id);
            return l;
        }

        /// <summary>
        /// Bu kişilerle bu dağılım oyunu kurdurur mu: kural motoruna kuru kurulum (<see cref="Oyun.Kur"/>, isimler
        /// önemsiz, oyun hemen atılır). Kişi sayısı oyun için yetmiyorsa ya da fazlaysa null: dağılım için bir şey denemez.
        /// </summary>
        public bool? Gecerli(IReadOnlyList<OyuncuId> herkes)
        {
            if (herkes == null) return null;
            var oyuncular = new List<Oyuncu>();
            for (int i = 0; i < herkes.Count; i++) oyuncular.Add(new Oyuncu(herkes[i], "o" + (i + 1)));
            var ayar = new Ayarlar
            {
                Oyun = OyunTuru.Takim,
                TakimDagilimi = Rastgele ? null : new Dictionary<OyuncuId, int>(takim),
            };
            var s = Oyun.Kur(ayar, oyuncular, new Random(0), out _);
            if (s == Sonuc.OyuncuSayisiGecersiz) return null;
            return s == Sonuc.Tamam;
        }

        /// <summary>Kurulacak oyuna giden dağılım: rastgeleyse null.</summary>
        public Dictionary<OyuncuId, int> Kopya() => Rastgele ? null : new Dictionary<OyuncuId, int>(takim);
    }
}
