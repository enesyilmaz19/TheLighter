using System;
using System.Collections.Generic;

namespace Cakmak.Kurallar
{
    // Çakmak Gecesi (docs/Oyunlar.md, 5; eski adı Vampir Köylü): gizli roller, gündüz ve gece. Taraflar Masumlar ve
    // Katiller, bir de sadece kendisi kazanan tarafsızlar. Gündüz canlıların yarısı kadar çakmak turu, sonra linç. Gece
    // her canlı bir seçim yapar (yeteneği olmayan "birini düşünür": kimin uzun düşündüğünden rol anlaşılmasın), sabah
    // gece sırasına göre çözülür.

    public enum Taraf { Masum, Katil, Tarafsiz }

    public enum GeceKazanan { Masumlar, Katiller, SeriKatil, Bombaci, Soytari, Cellat, Berabere }

    public sealed class OyuncuRolu
    {
        public OyuncuId Kim;
        public Rol Rol;
    }

    public sealed class KatilSecimi
    {
        public OyuncuId Kim;
        public OyuncuId Hedef;
    }

    /// <summary>Bir gece bakışının sonucu, sadece sahibine. Sadece rolün doldurduğu alanlar dolu.</summary>
    public sealed class Bulgu
    {
        public int Gun;
        /// <summary>Bakılan (Falcı'da seçilen).</summary>
        public OyuncuId Kim;
        /// <summary>Dedektif: şüpheli mi.</summary>
        public bool? Supheli;
        /// <summary>Casus: tam rolü.</summary>
        public Rol? Rol;
        /// <summary>Bekçi: o gece ona uğrayanlar.</summary>
        public IReadOnlyList<OyuncuId> Ziyaretciler = Array.Empty<OyuncuId>();
        /// <summary>Falcı: vizyondaki grup (seçilen dahil) ve aralarında kötü var mı.</summary>
        public IReadOnlyList<OyuncuId> Grup = Array.Empty<OyuncuId>();
        public bool? KotuVar;
    }

    /// <summary>Oyun sonunda herkese.</summary>
    public sealed class CakmakGecesiSonucu
    {
        /// <summary>Oyun yarıda kesildiyse (kurucu bitirdi, oyuncu kalmadı) null.</summary>
        public GeceKazanan? Kazanan;
        /// <summary>Kazanan oyuncular (ölüler dahil; hayattaki Hayatta Kalan da).</summary>
        public IReadOnlyList<OyuncuId> Kazananlar = Array.Empty<OyuncuId>();
        /// <summary>Herkesin son rolü (Deli, Deli olarak).</summary>
        public IReadOnlyList<OyuncuRolu> Roller = Array.Empty<OyuncuRolu>();

        internal CakmakGecesiSonucu Kopya() => (CakmakGecesiSonucu)MemberwiseClone();
    }

    public sealed partial class Oyun
    {
        public const int GeceEnAzOyuncu = 6;
        public const int GeceEnFazlaOyuncu = 18;
        public const int EnFazlaGecelikKurban = 3;

        /// <summary>Bu oyunun bütün rolleri: Masumlar, Katiller, Tarafsızlar. Kurucunun kapatabilecekleri (Masum ve Katil hariç) bunlardan.</summary>
        public static IReadOnlyList<Rol> GeceRolleri => TumGeceRolleri;

        static readonly Rol[] TumGeceRolleri =
        {
            Rol.Masum, Rol.Koruyucu, Rol.Dedektif, Rol.Serif, Rol.Kanunsuz, Rol.Bekci, Rol.Kulak, Rol.Falci, Rol.Deli,
            Rol.Katil, Rol.Patron, Rol.Casus, Rol.Susturucu, Rol.Tuzakci, Rol.Temizlikci, Rol.Suikastci, Rol.Palyaco, Rol.Vampir,
            Rol.Soytari, Rol.SeriKatil, Rol.Cellat, Rol.HayattaKalan, Rol.Hirsiz, Rol.Bombaci,
        };

        static readonly Rol[] MasumOzelRolleri = { Rol.Serif, Rol.Kanunsuz, Rol.Bekci, Rol.Kulak, Rol.Falci, Rol.Deli };
        static readonly Rol[] KatilOzelRolleri = { Rol.Patron, Rol.Casus, Rol.Susturucu, Rol.Tuzakci, Rol.Temizlikci, Rol.Suikastci, Rol.Palyaco, Rol.Vampir };
        static readonly Rol[] TarafsizRoller = { Rol.Soytari, Rol.SeriKatil, Rol.Cellat, Rol.HayattaKalan, Rol.Hirsiz, Rol.Bombaci };
        /// <summary>Deli'nin kendini sandığı roller: sonucu sahtelenebilen, öldürmeyen masum rolleri.</summary>
        static readonly Rol[] DeliRolleri = { Rol.Koruyucu, Rol.Dedektif, Rol.Bekci, Rol.Falci };
        /// <summary>Gece yeteneği bir başkasına uğrar (Bekçi bunları görür). Katiller ayrıca: kurbanına oy verip kurban seçilen.</summary>
        static readonly Rol[] UgrayanRoller = { Rol.Koruyucu, Rol.Dedektif, Rol.Serif, Rol.Kanunsuz, Rol.Bekci, Rol.Falci, Rol.Deli, Rol.SeriKatil, Rol.Hirsiz, Rol.Bombaci };
        /// <summary>Gece ikinci hedefi olan katil rolleri (kurban oyuna ek olarak).</summary>
        static readonly Rol[] IkinciHedefliRoller = { Rol.Casus, Rol.Susturucu, Rol.Tuzakci, Rol.Temizlikci, Rol.Palyaco, Rol.Vampir };

        public static Taraf TarafI(Rol r)
        {
            if (Array.IndexOf(KatilOzelRolleri, r) >= 0 || r == Rol.Katil) return Taraf.Katil;
            return Array.IndexOf(TarafsizRoller, r) >= 0 ? Taraf.Tarafsiz : Taraf.Masum;
        }

        /// <summary>Falcı'nın "kötü"sü: katil tarafı ve öldüren tarafsızlar.</summary>
        public static bool Kotu(Rol r) => TarafI(r) == Taraf.Katil || OldurenTarafsiz(r);

        static bool OldurenTarafsiz(Rol r) => r == Rol.SeriKatil || r == Rol.Bombaci;

        static int KatilSayisi(int kisi) => kisi >= 15 ? 4 : kisi >= 11 ? 3 : kisi >= 8 ? 2 : 1;
        static int TarafsizSayisi(int kisi) => kisi >= 17 ? 3 : kisi >= 13 ? 2 : kisi >= 8 ? 1 : 0;

        /// <summary>Sınırlı yeteneğin başlangıç hakkı; sınırsızsa ya da yeteneği yoksa null. Koruyucu: kendini koruma. Seri Katil: kaçınma.</summary>
        static int? IlkHak(Rol r, int kisi)
        {
            switch (r)
            {
                case Rol.Koruyucu: case Rol.Kanunsuz: case Rol.Suikastci: case Rol.Vampir: case Rol.Hirsiz: case Rol.SeriKatil: return 1;
                case Rol.Temizlikci: return 2;
                case Rol.Falci: return kisi >= 12 ? 3 : 2;
                default: return null;
            }
        }

        readonly Dictionary<OyuncuId, Rol> roller = new Dictionary<OyuncuId, Rol>();
        /// <summary>Deli → kendini sandığı rol.</summary>
        readonly Dictionary<OyuncuId, Rol> deliSandigi = new Dictionary<OyuncuId, Rol>();
        readonly Dictionary<OyuncuId, int> haklar = new Dictionary<OyuncuId, int>();
        readonly Dictionary<OyuncuId, List<Bulgu>> bulgular = new Dictionary<OyuncuId, List<Bulgu>>();
        /// <summary>Cellat → hedefi.</summary>
        readonly Dictionary<OyuncuId, OyuncuId> cellatHedefleri = new Dictionary<OyuncuId, OyuncuId>();
        readonly HashSet<OyuncuId> bombalilar = new HashSet<OyuncuId>();
        /// <summary>Kandırılan → Palyaço (bugünün linci için).</summary>
        readonly Dictionary<OyuncuId, OyuncuId> kuklalar = new Dictionary<OyuncuId, OyuncuId>();
        /// <summary>Linçte kimin kime oy verdiği. Hiçbir görünümde yok.</summary>
        readonly Dictionary<OyuncuId, OyuncuId> lincOylari = new Dictionary<OyuncuId, OyuncuId>();
        /// <summary>Gece seçimleri: hedef (yetenek ya da katillerde kurban oyu) ve katillerde ikinci hedef. Katiller birbirininkini görür.</summary>
        readonly Dictionary<OyuncuId, (OyuncuId? Hedef, OyuncuId? Ikinci)> geceSecimleri = new Dictionary<OyuncuId, (OyuncuId?, OyuncuId?)>();
        /// <summary>Kulak'ın duyduğu güme sorusu: sadece güme ekranı süresince, sonra silinir (docs/Oyunlar.md 5: bilinçli istisna).</summary>
        string kulakSorusu;
        int gun;
        int kalanGunduzTuru;
        CakmakGecesiSonucu geceSonucu;

        bool GeceOyunu => ayar.Oyun == OyunTuru.CakmakGecesi;

        /// <summary>Oyuncunun kendini bildiği rol: Deli'de sandığı, diğerlerinde gerçeği. Seçim adayları ve haklar buna göre.</summary>
        Rol EtkinRol(OyuncuId kim) => deliSandigi.TryGetValue(kim, out var sandigi) ? sandigi : roller[kim];

        int Hak(OyuncuId kim) => haklar.TryGetValue(kim, out var h) ? h : 0;

        void HakKullan(OyuncuId kim) => haklar[kim] = Hak(kim) - 1;

        bool KatilTarafinda(OyuncuId kim) => TarafI(roller[kim]) == Taraf.Katil;

        // ------------------------------------------------------------ Komutlar

        /// <summary>Linç oyu: oy hakkı olan canlılar (susturulan hariç) bir kez, kendisi dışında bir canlıya.</summary>
        public Sonuc LincOyu(OyuncuId kim, OyuncuId hedef)
        {
            if (asama != Asama.Linc) return Sonuc.YanlisAsama;
            if (!oylayanlar.Contains(kim)) return Sonuc.SiraSendeDegil;
            if (lincOylari.ContainsKey(kim)) return Sonuc.HakKalmadi;
            if (!LincAdaylari(kim).Contains(hedef)) return Sonuc.Secilemez;
            lincOylari[kim] = hedef;
            SecimKaydedildi(kim);
            return Sonuc.Tamam;
        }

        /// <summary>
        /// Gece seçimi: her canlı bir kez. <paramref name="hedef"/> null = pas. Katil tarafında <paramref name="hedef"/> kurban oyu,
        /// <paramref name="ikinci"/> rolün yeteneği (Casus, Susturucu, Tuzakçı, Temizlikçi, Palyaço, Vampir). Bombacı kendini seçerse
        /// bombaları patlatır. Geçerli hedefler görünümde (<see cref="Gorunum.Secilebilir"/>, <see cref="Gorunum.IkinciSecilebilir"/>).
        /// </summary>
        public Sonuc GeceSecimi(OyuncuId kim, OyuncuId? hedef, OyuncuId? ikinci = null)
        {
            if (asama != Asama.Gece) return Sonuc.YanlisAsama;
            if (!oylayanlar.Contains(kim)) return Sonuc.SiraSendeDegil;
            if (geceSecimleri.ContainsKey(kim)) return Sonuc.HakKalmadi;
            if (hedef.HasValue && !GeceAdaylari(kim).Contains(hedef.Value)) return Sonuc.Secilemez;
            if (ikinci.HasValue && !IkinciAdaylar(kim).Contains(ikinci.Value)) return Sonuc.Secilemez;
            geceSecimleri[kim] = (hedef, ikinci);
            SecimKaydedildi(kim);
            return Sonuc.Tamam;
        }

        /// <summary>
        /// Suikastçı, linç sırasında oyunda 1 kez: <paramref name="hedef"/>'in rolünü tahmin eder. Bilirse hedef, bilemezse kendisi
        /// hemen ölür. Ölenin oyu ve ona verilen oylar düşer (verenler yeniden oy verir).
        /// </summary>
        public Sonuc Suikast(OyuncuId kim, OyuncuId hedef, Rol tahmin)
        {
            if (asama != Asama.Linc) return Sonuc.YanlisAsama;
            var o = Bul(kim);
            if (o == null || o.Elendi || cikan.Contains(kim) || roller[kim] != Rol.Suikastci) return Sonuc.SiraSendeDegil;
            if (Hak(kim) <= 0) return Sonuc.HakKalmadi;
            if (!LincAdaylari(kim).Contains(hedef)) return Sonuc.Secilemez;
            if (Array.IndexOf(TumGeceRolleri, tahmin) < 0) return Sonuc.GecersizGirdi;

            HakKullan(kim);
            var olen = roller[hedef] == tahmin ? hedef : kim;
            Olum(olen, gizli: false);
            Yay(new Olay { Tur = OlayTuru.Suikast, Kim = olen });
            oylayanlar.Remove(olen);
            lincOylari.Remove(olen);
            var gecersiz = new List<OyuncuId>();
            foreach (var oy in lincOylari)
                if (oy.Value == olen) gecersiz.Add(oy.Key);
            foreach (var x in gecersiz) lincOylari.Remove(x);
            if (GeceKazananVarsaBitir()) return Sonuc.Tamam;
            if (oylayanlar.Count > 0 && lincOylari.Count == oylayanlar.Count) GeceOylamasiBitti();
            return Sonuc.Tamam;
        }

        void SecimKaydedildi(OyuncuId kim)
        {
            Yay(new Olay { Tur = OlayTuru.OyVerildi, Kim = kim });
            if (SecenSayisi == oylayanlar.Count) GeceOylamasiBitti();
            // Elden elede telefon sıradakine geçer: herkesin süresi tam (mini oyundaki gibi).
            else if (ayar.TekCihaz) kalanMs = ayar.SuclamaSuresiSn * 1000.0;
        }

        int SecenSayisi => asama == Asama.Linc ? lincOylari.Count : geceSecimleri.Count;

        bool Secti(OyuncuId kim) => asama == Asama.Linc ? lincOylari.ContainsKey(kim) : geceSecimleri.ContainsKey(kim);

        List<OyuncuId> LincAdaylari(OyuncuId kim)
        {
            var liste = Oynayanlar();
            liste.Remove(kim);
            return liste;
        }

        /// <summary>Gece ana hedefinin adayları (rolüne göre; Deli kendini sandığı role göre).</summary>
        List<OyuncuId> GeceAdaylari(OyuncuId kim)
        {
            var liste = Oynayanlar();
            var rol = EtkinRol(kim);
            if (TarafI(rol) == Taraf.Katil)
            {
                liste.RemoveAll(KatilTarafinda);
                return liste;
            }
            switch (rol)
            {
                case Rol.Koruyucu:
                    if (Hak(kim) <= 0) liste.Remove(kim);
                    break;
                case Rol.Bombaci:
                    if (bombalilar.Count == 0) liste.Remove(kim); // kendini seçmek = patlatmak
                    break;
                case Rol.Kanunsuz:
                case Rol.Falci:
                case Rol.Hirsiz:
                    if (Hak(kim) <= 0) liste.Clear();
                    else liste.Remove(kim);
                    break;
                default:
                    liste.Remove(kim);
                    break;
            }
            return liste;
        }

        /// <summary>Katillerin ikinci hedefi: rolü yetenekliyse (ve hakkı varsa) canlı, katil olmayan biri.</summary>
        List<OyuncuId> IkinciAdaylar(OyuncuId kim)
        {
            var rol = roller[kim];
            if (Array.IndexOf(IkinciHedefliRoller, rol) < 0 || (IlkHak(rol, oyuncular.Count).HasValue && Hak(kim) <= 0))
                return new List<OyuncuId>();
            var liste = Oynayanlar();
            liste.RemoveAll(KatilTarafinda);
            return liste;
        }

        // ------------------------------------------------------------ Kurulum ve gündüz

        /// <summary>Kurulumda: roller kişi sayısına ve kurucunun kapattıklarına göre dağıtılır (docs/Oyunlar.md 5, Dağılım).</summary>
        void GeceKur()
        {
            if (!GeceOyunu) return;
            int kisi = oyuncular.Count;
            var kapali = new HashSet<Rol>(ayar.KapaliRoller);
            List<Rol> Acik(Rol[] havuz)
            {
                var liste = new List<Rol>();
                foreach (var r in havuz)
                    if (!kapali.Contains(r)) liste.Add(r);
                return Karistir(liste);
            }

            var dagilim = new List<Rol>();
            int katil = KatilSayisi(kisi);
            var katilHavuzu = Acik(KatilOzelRolleri);
            for (int i = 0; i < katil; i++) dagilim.Add(i < katilHavuzu.Count ? katilHavuzu[i] : Rol.Katil);

            var tarafsizHavuzu = Acik(TarafsizRoller);
            int tarafsiz = Math.Min(TarafsizSayisi(kisi), tarafsizHavuzu.Count);
            for (int i = 0; i < tarafsiz; i++) dagilim.Add(tarafsizHavuzu[i]);

            int masum = kisi - katil - tarafsiz;
            int ozelMasum = masum - masum / 3; // üçte biri düz Masum
            var masumHavuzu = new List<Rol>();
            if (!kapali.Contains(Rol.Koruyucu)) masumHavuzu.Add(Rol.Koruyucu);
            if (!kapali.Contains(Rol.Dedektif)) masumHavuzu.Add(Rol.Dedektif);
            masumHavuzu.AddRange(Acik(MasumOzelRolleri));
            for (int i = 0; i < masum; i++) dagilim.Add(i < ozelMasum && i < masumHavuzu.Count ? masumHavuzu[i] : Rol.Masum);

            var sira = Karistir(new List<OyuncuDurumu>(oyuncular));
            for (int i = 0; i < kisi; i++) roller[sira[i].Id] = dagilim[i];
            foreach (var o in oyuncular)
            {
                if (roller[o.Id] == Rol.Deli) deliSandigi[o.Id] = DeliRolleri[rastgele.Next(DeliRolleri.Length)];
                var hak = IlkHak(EtkinRol(o.Id), kisi);
                if (hak.HasValue) haklar[o.Id] = hak.Value;
                if (roller[o.Id] == Rol.Cellat)
                {
                    var adaylar = oyuncular.FindAll(x => TarafI(roller[x.Id]) == Taraf.Masum);
                    cellatHedefleri[o.Id] = adaylar[rastgele.Next(adaylar.Count)].Id; // masum hep var: en az 6 kişide en az 4
                }
            }
            gun = 1;
            kalanGunduzTuru = GunduzTuru();
        }

        /// <summary>Canlıların yarısı kadar (en az 1). Canlılar 4'ün altındaysa çakmak turu olamaz: 0, sadece linç ve gece.</summary>
        int GunduzTuru()
        {
            int canli = Oynayanlar().Count;
            return canli < EnAzOyuncu ? 0 : Math.Max(1, canli / 2);
        }

        /// <summary>Yeni tur başlamadan: bugünün turları bittiyse linç başlar. true: tur başlamaz.</summary>
        bool GunduzBitti()
        {
            if (!GeceOyunu) return false;
            kulakSorusu = null; // güme ekranı bitti
            if (kalanGunduzTuru > 0 && GunduzTuru() > 0) // gün içinde 4'ün altına düşüldüyse kalan turlar da atlanır
            {
                kalanGunduzTuru--;
                return false;
            }
            kalanGunduzTuru = 0;
            OylamaBaslat(Asama.Linc, OlayTuru.LincBasladi);
            return true;
        }

        /// <summary>Turun sonucu: güme gittiyse Kulak soruyu güme ekranı boyunca duyar.</summary>
        void GeceTurSonu(bool ifsa)
        {
            if (GeceOyunu && !ifsa) kulakSorusu = soru;
        }

        void OylamaBaslat(Asama yeni, OlayTuru olay)
        {
            SoruyuKapat();
            kulakSorusu = null;
            b = null;
            c = null;
            oylayanlar.Clear();
            foreach (var x in Oynayanlar())
                if (yeni == Asama.Gece || !Bul(x).Susturuldu) oylayanlar.Add(x); // susturulan linçte oy veremez
            lincOylari.Clear();
            geceSecimleri.Clear();
            if (yeni == Asama.Gece)
            {
                kuklalar.Clear();
                foreach (var o in oyuncular) o.Susturuldu = false; // susturma o günün
            }
            AsamayaGec(yeni, ayar.SuclamaSuresiSn);
            Yay(new Olay { Tur = olay });
        }

        /// <summary>Herkes seçti ya da süre doldu (seçmeyen seçmemiş sayılır).</summary>
        void GeceOylamasiBitti()
        {
            if (asama == Asama.Linc) LinciCoz();
            else GeceyiCoz();
        }

        // ------------------------------------------------------------ Linç

        void LinciCoz()
        {
            // Palyaço'nun kuklası: oyu Palyaço'nun oy verdiği kişiye sayılır (Palyaço oy vermediyse kendi oyu).
            var oylar = new List<OyuncuId>();
            foreach (var oy in lincOylari)
                oylar.Add(kuklalar.TryGetValue(oy.Key, out var palyaco) && lincOylari.TryGetValue(palyaco, out var p) ? p : oy.Value);
            var enCok = EnCokOyAlanlar(oylar);
            OyuncuId? elenen = enCok.Count == 1 ? enCok[0] : (OyuncuId?)null;
            lincOylari.Clear();
            oylayanlar.Clear();
            Yay(new Olay { Tur = OlayTuru.LincBitti, Kim = elenen });
            if (elenen.HasValue)
            {
                var e = elenen.Value;
                bool soytari = roller[e] == Rol.Soytari;
                OyuncuId? cellat = null;
                foreach (var x in cellatHedefleri)
                    if (x.Value == e) cellat = x.Key;
                if (cellat.HasValue) cellatHedefleri.Remove(cellat.Value); // hedefi linç edildi: kazandı, Soytarı'ya dönmez
                Olum(e, gizli: false);
                if (soytari) { GeceBitir(GeceKazanan.Soytari, e); return; }
                if (cellat.HasValue) { GeceBitir(GeceKazanan.Cellat, cellat.Value); return; }
            }
            if (GeceKazananVarsaBitir()) return;
            OylamaBaslat(Asama.Gece, OlayTuru.GeceBasladi);
        }

        // ------------------------------------------------------------ Gece

        /// <summary>Gece sırası (docs/Oyunlar.md 5): işaret/susturma/kandırma, koruma, saldırılar, bakışlar, ölümler, dönüşümler.</summary>
        void GeceyiCoz()
        {
            var canlilar = Oynayanlar();
            var secimler = new List<KeyValuePair<OyuncuId, (OyuncuId? Hedef, OyuncuId? Ikinci)>>(geceSecimleri);
            geceSecimleri.Clear();
            oylayanlar.Clear();

            // 1–2. İşaret, susturma, kandırma, koruma
            var isaretli = new HashSet<OyuncuId>();
            var korunan = new HashSet<OyuncuId>();
            foreach (var s in secimler)
            {
                var rol = roller[s.Key];
                var ik = s.Value.Ikinci;
                if (ik.HasValue && rol == Rol.Tuzakci) isaretli.Add(ik.Value);
                if (ik.HasValue && rol == Rol.Susturucu) Bul(ik.Value).Susturuldu = true;
                if (ik.HasValue && rol == Rol.Palyaco) kuklalar[ik.Value] = s.Key;
                if (s.Value.Hedef == s.Key && EtkinRol(s.Key) == Rol.Koruyucu) HakKullan(s.Key); // Deli'nin de hakkı düşer
                if (s.Value.Hedef.HasValue && rol == Rol.Koruyucu) korunan.Add(s.Value.Hedef.Value);
            }

            // 3. Saldırılar
            var katilOylari = new List<OyuncuId>();
            int katil = 0;
            foreach (var x in canlilar)
                if (KatilTarafinda(x)) katil++;
            foreach (var s in secimler)
                if (KatilTarafinda(s.Key) && s.Value.Hedef.HasValue) katilOylari.Add(s.Value.Hedef.Value);
            var kurbanlar = EnCokOylananlar(katilOylari, Math.Min(ayar.GecelikKurban, katil));

            var saldirilar = new List<(OyuncuId Hedef, Rol Kaynak)>();
            foreach (var k in kurbanlar) saldirilar.Add((k, Rol.Katil));
            var olenler = new Dictionary<OyuncuId, bool>(); // ölen → rolü gizli mi
            foreach (var s in secimler)
            {
                var h = s.Value.Hedef;
                switch (roller[s.Key])
                {
                    case Rol.SeriKatil when h.HasValue:
                        saldirilar.Add((h.Value, Rol.SeriKatil));
                        break;
                    case Rol.Serif when h.HasValue:
                        saldirilar.Add((h.Value, Rol.Serif));
                        if (TarafI(roller[h.Value]) == Taraf.Masum) olenler[s.Key] = false; // masum vurdu: kendisi de (korunsa bile)
                        break;
                    case Rol.Kanunsuz when h.HasValue:
                        HakKullan(s.Key);
                        saldirilar.Add((h.Value, Rol.Kanunsuz));
                        break;
                    case Rol.Bombaci when h == s.Key:
                        foreach (var x in bombalilar) saldirilar.Add((x, Rol.Bombaci));
                        bombalilar.Clear();
                        break;
                    case Rol.Bombaci when h.HasValue:
                        bombalilar.Add(h.Value);
                        break;
                }
            }
            foreach (var (hedef, kaynak) in saldirilar)
            {
                if (!canlilar.Contains(hedef) || korunan.Contains(hedef)) continue;
                if (kaynak == Rol.Katil && roller[hedef] == Rol.SeriKatil && Hak(hedef) > 0) { HakKullan(hedef); continue; } // kaçınma
                bool gizli = kaynak == Rol.SeriKatil; // Seri Katil'in kurbanının rolü açılmaz
                olenler[hedef] = gizli || (olenler.TryGetValue(hedef, out var once) && once);
            }
            foreach (var s in secimler)
            {
                var ik = s.Value.Ikinci;
                if (roller[s.Key] == Rol.Temizlikci && ik.HasValue && olenler.ContainsKey(ik.Value) && Hak(s.Key) > 0)
                {
                    HakKullan(s.Key);
                    olenler[ik.Value] = true;
                }
            }

            // 4. Bakışlar (gecenin başındaki rollerle)
            foreach (var s in secimler)
            {
                var h = s.Value.Hedef;
                switch (roller[s.Key])
                {
                    case Rol.Deli:
                        DeliBulgusu(s.Key, h, canlilar);
                        break;
                    case Rol.Dedektif when h.HasValue:
                        var rol = roller[h.Value];
                        BulguEkle(s.Key, new Bulgu { Kim = h.Value, Supheli = (Kotu(rol) && rol != Rol.Patron) || isaretli.Contains(h.Value) });
                        break;
                    case Rol.Bekci when h.HasValue:
                        BulguEkle(s.Key, new Bulgu { Kim = h.Value, Ziyaretciler = Ziyaretciler(h.Value, s.Key, secimler, kurbanlar) });
                        break;
                    case Rol.Falci when h.HasValue:
                        HakKullan(s.Key);
                        var grup = FalciGrubu(s.Key, h.Value, canlilar);
                        BulguEkle(s.Key, new Bulgu { Kim = h.Value, Grup = grup, KotuVar = grup.Exists(x => Kotu(roller[x])) });
                        break;
                    case Rol.Casus when s.Value.Ikinci.HasValue:
                        BulguEkle(s.Key, new Bulgu { Kim = s.Value.Ikinci.Value, Rol = roller[s.Value.Ikinci.Value] });
                        break;
                }
            }

            // 5. Ölümler
            foreach (var o in olenler)
            {
                Olum(o.Key, o.Value);
                Yay(new Olay { Tur = OlayTuru.Elendi, Kim = o.Key });
            }

            // 6. Dönüşümler: Vampir'in ısırdığı Katil olur, Hırsız rolü çalar. Bu gece ölen ısıramaz, çalamaz.
            foreach (var s in secimler)
            {
                var ik = s.Value.Ikinci;
                if (roller[s.Key] != Rol.Vampir || !ik.HasValue) continue;
                HakKullan(s.Key);
                if (Bul(s.Key).Elendi) continue;
                var isirilan = ik.Value;
                if (Bul(isirilan).Elendi || korunan.Contains(isirilan) || TarafI(roller[isirilan]) != Taraf.Masum) continue;
                RolDegistir(isirilan, Rol.Katil);
            }
            foreach (var s in secimler)
                if (roller[s.Key] == Rol.Hirsiz && s.Value.Hedef.HasValue && !Bul(s.Key).Elendi) RolCal(s.Key, s.Value.Hedef.Value);

            Yay(new Olay { Tur = OlayTuru.GeceBitti });
            if (GeceKazananVarsaBitir()) return;
            gun++;
            kalanGunduzTuru = GunduzTuru();
            OncekiTuruUnut();
            TurBaslat(RastgeleOyuncu());
        }

        /// <summary>Bekçi'nin gördükleri: o gece hedefe yeteneğini kullananlar (Bekçi'nin kendisi hariç).</summary>
        List<OyuncuId> Ziyaretciler(OyuncuId hedef, OyuncuId bekci,
            List<KeyValuePair<OyuncuId, (OyuncuId? Hedef, OyuncuId? Ikinci)>> secimler, List<OyuncuId> kurbanlar)
        {
            var liste = new List<OyuncuId>();
            foreach (var s in secimler)
            {
                if (s.Key == bekci || s.Key == hedef) continue;
                bool ugradi = KatilTarafinda(s.Key)
                    ? s.Value.Ikinci == hedef || (s.Value.Hedef == hedef && kurbanlar.Contains(hedef))
                    : s.Value.Hedef == hedef && Array.IndexOf(UgrayanRoller, roller[s.Key]) >= 0;
                if (ugradi) liste.Add(s.Key);
            }
            return liste;
        }

        /// <summary>Falcı'nın vizyonu: seçilen ve rastgele başkaları, canlıların üçte biri kadar (en az 3; Falcı hariç).</summary>
        List<OyuncuId> FalciGrubu(OyuncuId falci, OyuncuId secilen, List<OyuncuId> canlilar)
        {
            var digerleri = new List<OyuncuId>(canlilar);
            digerleri.Remove(falci);
            digerleri.Remove(secilen);
            Karistir(digerleri);
            int boy = Math.Max(3, canlilar.Count / 3);
            var grup = new List<OyuncuId> { secilen };
            for (int i = 0; grup.Count < boy && i < digerleri.Count; i++) grup.Add(digerleri[i]);
            return Karistir(grup);
        }

        /// <summary>Deli'nin sandığı rolün sonucu, rastgele (koruması zaten işlemez).</summary>
        void DeliBulgusu(OyuncuId deli, OyuncuId? hedef, List<OyuncuId> canlilar)
        {
            if (!hedef.HasValue) return;
            var h = hedef.Value;
            switch (deliSandigi[deli])
            {
                case Rol.Dedektif:
                    BulguEkle(deli, new Bulgu { Kim = h, Supheli = rastgele.Next(2) == 0 });
                    break;
                case Rol.Bekci:
                    var digerleri = new List<OyuncuId>(canlilar);
                    digerleri.Remove(deli);
                    digerleri.Remove(h);
                    Karistir(digerleri);
                    BulguEkle(deli, new Bulgu { Kim = h, Ziyaretciler = digerleri.GetRange(0, Math.Min(rastgele.Next(3), digerleri.Count)) });
                    break;
                case Rol.Falci:
                    HakKullan(deli);
                    BulguEkle(deli, new Bulgu { Kim = h, Grup = FalciGrubu(deli, h, canlilar), KotuVar = rastgele.Next(2) == 0 });
                    break;
            }
        }

        void BulguEkle(OyuncuId kim, Bulgu bulgu)
        {
            bulgu.Gun = gun;
            if (!bulgular.TryGetValue(kim, out var liste)) bulgular[kim] = liste = new List<Bulgu>();
            liste.Add(bulgu);
        }

        /// <summary>
        /// En çok oylananlar, en fazla <paramref name="adet"/> kişi. Eşitlikte sıra rastgele (karıştırılıp oya göre kararlı sıralanır).
        /// </summary>
        List<OyuncuId> EnCokOylananlar(List<OyuncuId> oylar, int adet)
        {
            var sayilar = new Dictionary<OyuncuId, int>();
            foreach (var x in oylar) Artir(sayilar, x);
            var sira = Karistir(new List<OyuncuId>(sayilar.Keys));
            for (int i = 1; i < sira.Count; i++) // kararlı ekleme sıralaması: eşitlerin rastgele sırası korunur
                for (int j = i; j > 0 && sayilar[sira[j]] > sayilar[sira[j - 1]]; j--)
                    (sira[j], sira[j - 1]) = (sira[j - 1], sira[j]);
            return sira.GetRange(0, Math.Min(adet, sira.Count));
        }

        /// <summary>Öldü. Rolü açılır (ayar açıksa ve gizli değilse). Cellat'ın hedefiyse (yaşayan) Cellat Soytarı olur.</summary>
        void Olum(OyuncuId kim, bool gizli)
        {
            var o = Bul(kim);
            o.Elendi = true;
            o.Susturuldu = false;
            if (ayar.RolAcilir && !gizli) o.AcikRol = roller[kim];
            var cellatlar = new List<OyuncuId>();
            foreach (var x in cellatHedefleri)
                if (x.Value == kim && !Bul(x.Key).Elendi) cellatlar.Add(x.Key); // ölünün rolü değişmez
            foreach (var x in cellatlar)
            {
                cellatHedefleri.Remove(x);
                RolDegistir(x, Rol.Soytari);
            }
        }

        void RolDegistir(OyuncuId kim, Rol yeni)
        {
            roller[kim] = yeni;
            deliSandigi.Remove(kim);
            haklar.Remove(kim);
            var hak = IlkHak(yeni, oyuncular.Count);
            if (hak.HasValue) haklar[kim] = hak.Value;
        }

        /// <summary>
        /// Hırsız rolü çalar: rol, kalan hakları, Deli'nin sandığı ve Cellat'ın hedefi onun olur. Çalınan (yaşıyorsa) kendi tarafının
        /// düz rolüne düşer (tarafsızsa Hayatta Kalan).
        /// </summary>
        void RolCal(OyuncuId hirsiz, OyuncuId hedef)
        {
            var rol = roller[hedef];
            roller[hirsiz] = rol;
            haklar.Remove(hirsiz);
            if (haklar.TryGetValue(hedef, out var hak)) haklar[hirsiz] = hak;
            if (deliSandigi.TryGetValue(hedef, out var sandigi)) deliSandigi[hirsiz] = sandigi;
            if (cellatHedefleri.TryGetValue(hedef, out var cellatHedefi))
            {
                cellatHedefleri.Remove(hedef);
                cellatHedefleri[hirsiz] = cellatHedefi;
            }
            if (Bul(hedef).Elendi) return;
            var taraf = TarafI(rol);
            RolDegistir(hedef, taraf == Taraf.Masum ? Rol.Masum : taraf == Taraf.Katil ? Rol.Katil : Rol.HayattaKalan);
        }

        // ------------------------------------------------------------ Kazanma

        /// <summary>Her ölümde ve oyundan çıkışta: bir taraf kazandıysa ya da 2 kişi kalıp kimse kazanamıyorsa (berabere) oyun biter.</summary>
        bool GeceKazananVarsaBitir()
        {
            if (!GeceOyunu || asama == Asama.OyunSonu) return false;
            var canli = Oynayanlar();
            int katil = 0, diger = 0;
            var olduren = new List<OyuncuId>();
            foreach (var x in canli)
            {
                if (KatilTarafinda(x)) katil++;
                else diger++;
                if (OldurenTarafsiz(roller[x])) olduren.Add(x);
            }
            GeceKazanan? kazanan = null;
            if (canli.Count == 0) kazanan = GeceKazanan.Berabere;
            else if (katil == 0 && olduren.Count == 0) kazanan = GeceKazanan.Masumlar;
            else if (olduren.Count == 0 && katil >= diger) kazanan = GeceKazanan.Katiller;
            else if (katil == 0 && olduren.Count == 1 && canli.Count <= 2)
                kazanan = roller[olduren[0]] == Rol.SeriKatil ? GeceKazanan.SeriKatil : GeceKazanan.Bombaci;
            else if (canli.Count <= 2) kazanan = GeceKazanan.Berabere;
            if (!kazanan.HasValue) return false;
            GeceBitir(kazanan.Value, kazanan == GeceKazanan.SeriKatil || kazanan == GeceKazanan.Bombaci ? olduren[0] : (OyuncuId?)null);
            return true;
        }

        /// <summary><paramref name="tek"/>: tek kişilik kazananlarda (Seri Katil, Bombacı, Soytarı, Cellat) kazanan.</summary>
        void GeceBitir(GeceKazanan kazanan, OyuncuId? tek)
        {
            var liste = new List<OyuncuId>();
            foreach (var o in oyuncular)
            {
                var taraf = TarafI(roller[o.Id]);
                bool kazandi = kazanan == GeceKazanan.Masumlar ? taraf == Taraf.Masum
                    : kazanan == GeceKazanan.Katiller ? taraf == Taraf.Katil
                    : o.Id == tek;
                // Hayatta Kalan: oyun bitince hayattaysa kim kazanırsa kazansın.
                if (kazandi || (roller[o.Id] == Rol.HayattaKalan && !o.Elendi && !cikan.Contains(o.Id))) liste.Add(o.Id);
            }
            geceSonucu = YeniGeceSonucu(kazanan, liste);
            Bitir();
        }

        /// <summary>Oyun yarıda bitti (kurucu, oyuncu kalmadı): kazanan yok.</summary>
        void GeceSonucunuYaz()
        {
            kulakSorusu = null;
            if (GeceOyunu && geceSonucu == null) geceSonucu = YeniGeceSonucu(null, new List<OyuncuId>());
        }

        CakmakGecesiSonucu YeniGeceSonucu(GeceKazanan? kazanan, List<OyuncuId> kazananlar)
        {
            var liste = new List<OyuncuRolu>();
            foreach (var o in oyuncular) liste.Add(new OyuncuRolu { Kim = o.Id, Rol = roller[o.Id] });
            return new CakmakGecesiSonucu { Kazanan = kazanan, Kazananlar = kazananlar, Roller = liste };
        }

        // ------------------------------------------------------------ Görünüm

        void GeceGorunumu(Gorunum g, OyuncuId kim)
        {
            if (!GeceOyunu) return;
            g.ToplamTur = 0; // bir taraf kazanana kadar sürer
            g.Gun = gun;
            g.KalanGunduzTuru = kalanGunduzTuru;
            var gercek = roller[kim];
            g.Rolun = EtkinRol(kim);
            g.KalanHakkin = haklar.TryGetValue(kim, out var hak) ? hak : (int?)null;
            if (bulgular.TryGetValue(kim, out var b))
            {
                var kopya = new List<Bulgu>();
                foreach (var x in b) kopya.Add(new Bulgu { Gun = x.Gun, Kim = x.Kim, Supheli = x.Supheli, Rol = x.Rol, Ziyaretciler = x.Ziyaretciler, Grup = x.Grup, KotuVar = x.KotuVar });
                g.Bulgularin = kopya;
            }
            if (TarafI(gercek) == Taraf.Katil)
            {
                var katiller = new List<OyuncuRolu>();
                var secimler = new List<KatilSecimi>();
                foreach (var o in oyuncular)
                {
                    if (!KatilTarafinda(o.Id)) continue;
                    katiller.Add(new OyuncuRolu { Kim = o.Id, Rol = roller[o.Id] });
                    if (asama == Asama.Gece && geceSecimleri.TryGetValue(o.Id, out var s) && s.Hedef.HasValue)
                        secimler.Add(new KatilSecimi { Kim = o.Id, Hedef = s.Hedef.Value });
                }
                g.Katiller = katiller;
                g.KatilSecimleri = secimler;
            }
            if (cellatHedefleri.TryGetValue(kim, out var cellatHedefi)) g.CellatHedefin = cellatHedefi;
            if (gercek == Rol.Bombaci) g.Bombalilar = new List<OyuncuId>(bombalilar);
            // Alanın ömrü güme ekranı kadar (turun sonunda yazılır, ekran bitince silinir): görünüm de ona bağlı.
            if (gercek == Rol.Kulak && !Bul(kim).Elendi) g.KulakSorusu = kulakSorusu;

            if (asama != Asama.Linc && asama != Asama.Gece) return;
            g.Oylayanlar = new List<OyuncuId>(oylayanlar);
            g.OyKullanan = SecenSayisi;
            g.OyVerdin = Secti(kim);
            if (!oylayanlar.Contains(kim) || g.OyVerdin) return;
            g.Secilebilir = asama == Asama.Linc ? LincAdaylari(kim) : GeceAdaylari(kim);
            if (asama == Asama.Gece) g.IkinciSecilebilir = IkinciAdaylar(kim);
        }
    }
}
