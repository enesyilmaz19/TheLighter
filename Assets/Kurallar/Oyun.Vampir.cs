using System;
using System.Collections.Generic;

namespace Cakmak.Kurallar
{
    // Vampir Köylü (docs/Oyunlar.md, 5): gizli roller, gündüz ve gece. Gündüz canlıların yarısı kadar çakmak turu, sonra
    // linç; gece vampirler kurban, doktor koruma, gözcü bakış seçer, köylü de "birini düşünür" (kimin uzun düşündüğünden
    // rol anlaşılmasın). Bütün vampirler elenirse köylüler, vampirler sayıca eşit ya da fazla olursa vampirler kazanır.

    public enum VampirKazanan { Koylu, Vampir }

    public sealed class OyuncuRolu
    {
        public OyuncuId Kim;
        public Rol Rol;
    }

    /// <summary>Oyun sonunda herkese: kazanan ve herkesin rolü.</summary>
    public sealed class VampirSonucu
    {
        /// <summary>Oyun yarıda kesildiyse (kurucu bitirdi, oyuncu kalmadı) null.</summary>
        public VampirKazanan? Kazanan;
        public IReadOnlyList<OyuncuRolu> Roller = Array.Empty<OyuncuRolu>();

        internal VampirSonucu Kopya() => (VampirSonucu)MemberwiseClone();
    }

    public sealed partial class Oyun
    {
        public const int VampirEnAzOyuncu = 6;
        /// <summary>Bu kadar kişiden itibaren 2 vampir (altında 1).</summary>
        public const int IkiVampirSiniri = 8;

        readonly Dictionary<OyuncuId, Rol> roller = new Dictionary<OyuncuId, Rol>();
        /// <summary>Linçte kimin kime oy verdiği, gecede kimin kimi seçtiği. Hiçbir görünümde yok (vampirler birbirininkini görür).</summary>
        readonly Dictionary<OyuncuId, OyuncuId> vampirOylari = new Dictionary<OyuncuId, OyuncuId>();
        readonly List<GozcuBilgisi> gozcuBilgileri = new List<GozcuBilgisi>();
        bool doktorKendiniKorudu;
        int gun;
        int kalanGunduzTuru;
        VampirSonucu vampirSonucu;

        bool Vampirli => ayar.Oyun == OyunTuru.Vampir;

        // ------------------------------------------------------------ Komutlar

        /// <summary>Linç oyu: canlılar bir kez, kendisi dışında bir canlıya.</summary>
        public Sonuc LincOyu(OyuncuId kim, OyuncuId hedef)
        {
            if (asama != Asama.Linc) return Sonuc.YanlisAsama;
            var sonuc = OyDenetle(kim, hedef, LincAdaylari(kim));
            if (sonuc == Sonuc.Tamam) OyuKaydet(kim, hedef);
            return sonuc;
        }

        /// <summary>
        /// Gece seçimi: canlıların hepsi bir kez. Vampir kurban (vampir olmayan), doktor korunacak kişi (kendini oyunda 1 kez),
        /// gözcü bakılacak kişi (cevap hemen, sadece kendisine), köylü "birini düşünür" (hiçbir etkisi yok).
        /// </summary>
        public Sonuc GeceSecimi(OyuncuId kim, OyuncuId hedef)
        {
            if (asama != Asama.Gece) return Sonuc.YanlisAsama;
            var sonuc = OyDenetle(kim, hedef, GeceAdaylari(kim));
            if (sonuc != Sonuc.Tamam) return sonuc;
            // Kaydetmeden önce: son seçimse gece hemen biter.
            if (roller[kim] == Rol.Doktor && hedef == kim) doktorKendiniKorudu = true;
            if (roller[kim] == Rol.Gozcu) gozcuBilgileri.Add(new GozcuBilgisi { Kim = hedef, Vampir = roller[hedef] == Rol.Vampir });
            OyuKaydet(kim, hedef);
            return sonuc;
        }

        Sonuc OyDenetle(OyuncuId kim, OyuncuId hedef, List<OyuncuId> adaylar)
        {
            if (!oylayanlar.Contains(kim)) return Sonuc.SiraSendeDegil;
            if (vampirOylari.ContainsKey(kim)) return Sonuc.HakKalmadi;
            return adaylar.Contains(hedef) ? Sonuc.Tamam : Sonuc.Secilemez;
        }

        void OyuKaydet(OyuncuId kim, OyuncuId hedef)
        {
            vampirOylari[kim] = hedef;
            Yay(new Olay { Tur = OlayTuru.OyVerildi, Kim = kim });
            if (vampirOylari.Count == oylayanlar.Count) VampirOylamasiBitti();
            // Elden elede telefon sıradakine geçer: herkesin süresi tam (mini oyundaki gibi).
            else if (ayar.TekCihaz) kalanMs = ayar.SuclamaSuresiSn * 1000.0;
        }

        List<OyuncuId> LincAdaylari(OyuncuId kim)
        {
            var liste = Oynayanlar();
            liste.Remove(kim);
            return liste;
        }

        List<OyuncuId> GeceAdaylari(OyuncuId kim)
        {
            var liste = Oynayanlar();
            switch (roller[kim])
            {
                case Rol.Vampir:
                    liste.RemoveAll(x => roller[x] == Rol.Vampir);
                    break;
                case Rol.Doktor:
                    if (doktorKendiniKorudu) liste.Remove(kim);
                    break;
                default:
                    liste.Remove(kim);
                    break;
            }
            return liste;
        }

        // ------------------------------------------------------------ Akış

        /// <summary>Kurulumda: roller dağıtılır (6–7 kişide 1, 8–10 kişide 2 vampir, 1 doktor, 1 gözcü), ilk gün başlar.</summary>
        void VampirKur()
        {
            if (!Vampirli) return;
            var sira = Karistir(new List<OyuncuDurumu>(oyuncular));
            int vampir = oyuncular.Count >= IkiVampirSiniri ? 2 : 1;
            for (int i = 0; i < sira.Count; i++)
                roller[sira[i].Id] = i < vampir ? Rol.Vampir : i == vampir ? Rol.Doktor : i == vampir + 1 ? Rol.Gozcu : Rol.Koylu;
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
        bool VampirGunduzuBitti()
        {
            if (!Vampirli) return false;
            if (kalanGunduzTuru > 0 && GunduzTuru() > 0) // gün içinde 4'ün altına düşüldüyse kalan turlar da atlanır
            {
                kalanGunduzTuru--;
                return false;
            }
            kalanGunduzTuru = 0;
            VampirOylamasiniBaslat(Asama.Linc, OlayTuru.LincBasladi);
            return true;
        }

        void VampirOylamasiniBaslat(Asama yeni, OlayTuru olay)
        {
            SoruyuKapat();
            b = null;
            c = null;
            oylayanlar.Clear();
            oylayanlar.AddRange(Oynayanlar());
            vampirOylari.Clear();
            AsamayaGec(yeni, ayar.SuclamaSuresiSn);
            Yay(new Olay { Tur = olay });
        }

        /// <summary>Herkes seçti ya da süre doldu (seçmeyen seçmemiş sayılır).</summary>
        void VampirOylamasiBitti()
        {
            bool linc = asama == Asama.Linc;
            OyuncuId? elenen = linc ? LincSonucu() : GeceSonucu();
            vampirOylari.Clear();
            oylayanlar.Clear();
            Yay(new Olay { Tur = linc ? OlayTuru.LincBitti : OlayTuru.GeceBitti, Kim = elenen });
            if (elenen.HasValue)
            {
                var o = Bul(elenen.Value);
                o.Elendi = true;
                if (ayar.RolAcilir) o.AcikRol = roller[o.Id];
            }
            if (VampirKazananVarsaBitir()) return;

            if (linc)
            {
                VampirOylamasiniBaslat(Asama.Gece, OlayTuru.GeceBasladi);
                return;
            }
            gun++;
            kalanGunduzTuru = GunduzTuru();
            OncekiTuruUnut();
            TurBaslat(RastgeleOyuncu());
        }

        /// <summary>En çok oyu alan elenir; eşitlikte ya da hiç oy yoksa kimse.</summary>
        OyuncuId? LincSonucu()
        {
            var enCok = EnCokOyAlanlar(vampirOylari.Values);
            return enCok.Count == 1 ? enCok[0] : (OyuncuId?)null;
        }

        /// <summary>
        /// Vampirlerin seçimlerinden en çok seçilen kurban (2 vampir farklı seçtiyse biri rastgele). Doktor onu koruduysa
        /// kimse ölmez.
        /// </summary>
        OyuncuId? GeceSonucu()
        {
            var vampirSecimleri = new List<OyuncuId>();
            OyuncuId? korunan = null;
            foreach (var s in vampirOylari)
            {
                if (roller[s.Key] == Rol.Vampir) vampirSecimleri.Add(s.Value);
                else if (roller[s.Key] == Rol.Doktor) korunan = s.Value;
            }
            var adaylar = EnCokOyAlanlar(vampirSecimleri);
            if (adaylar.Count == 0) return null;
            var kurban = adaylar[rastgele.Next(adaylar.Count)];
            return kurban == korunan ? (OyuncuId?)null : kurban;
        }

        /// <summary>Bütün vampirler elendiyse köylüler, vampirler sayıca eşit ya da fazlaysa vampirler kazanır. true: oyun bitti.</summary>
        bool VampirKazananVarsaBitir()
        {
            if (!Vampirli || asama == Asama.OyunSonu) return false;
            int vampir = 0, koylu = 0;
            foreach (var x in Oynayanlar())
                if (roller[x] == Rol.Vampir) vampir++;
                else koylu++;
            if (vampir > 0 && vampir < koylu) return false;
            vampirSonucu = YeniVampirSonucu(vampir == 0 ? VampirKazanan.Koylu : VampirKazanan.Vampir);
            Bitir();
            return true;
        }

        /// <summary>Oyun yarıda bitti (kurucu, oyuncu kalmadı): kazanan yok.</summary>
        void VampirSonucunuYaz()
        {
            if (Vampirli && vampirSonucu == null) vampirSonucu = YeniVampirSonucu(null);
        }

        VampirSonucu YeniVampirSonucu(VampirKazanan? kazanan)
        {
            var liste = new List<OyuncuRolu>();
            foreach (var o in oyuncular) liste.Add(new OyuncuRolu { Kim = o.Id, Rol = roller[o.Id] });
            return new VampirSonucu { Kazanan = kazanan, Roller = liste };
        }

        // ------------------------------------------------------------ Görünüm

        void VampirGorunumu(Gorunum g, OyuncuId kim)
        {
            if (!Vampirli) return;
            g.ToplamTur = 0; // bir taraf kazanana kadar sürer
            g.Gun = gun;
            g.KalanGunduzTuru = kalanGunduzTuru;
            var rol = roller[kim];
            g.Rolun = rol;
            bool oylama = asama == Asama.Linc || asama == Asama.Gece;
            if (rol == Rol.Vampir)
            {
                var vampirler = new List<OyuncuId>();
                var secimler = new List<VampirSecimi>();
                foreach (var o in oyuncular)
                {
                    if (roller[o.Id] != Rol.Vampir) continue;
                    vampirler.Add(o.Id);
                    if (asama == Asama.Gece && vampirOylari.TryGetValue(o.Id, out var hedef))
                        secimler.Add(new VampirSecimi { Kim = o.Id, Hedef = hedef });
                }
                g.Vampirler = vampirler;
                g.VampirSecimleri = secimler;
            }
            if (rol == Rol.Gozcu)
            {
                var bilgiler = new List<GozcuBilgisi>();
                foreach (var x in gozcuBilgileri) bilgiler.Add(new GozcuBilgisi { Kim = x.Kim, Vampir = x.Vampir });
                g.GozcuBilgilerin = bilgiler;
            }
            if (rol == Rol.Doktor) g.KendiniKoruyabilir = !doktorKendiniKorudu && asama != Asama.OyunSonu;
            if (!oylama) return;
            g.Oylayanlar = new List<OyuncuId>(oylayanlar);
            g.OyKullanan = vampirOylari.Count;
            g.OyVerdin = vampirOylari.ContainsKey(kim);
            if (oylayanlar.Contains(kim) && !g.OyVerdin) g.Secilebilir = asama == Asama.Linc ? LincAdaylari(kim) : GeceAdaylari(kim);
        }
    }
}
