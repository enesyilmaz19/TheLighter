using System.Collections.Generic;

namespace Cakmak.Kurallar
{
    // Çakmak Gecesi'nin çakmağa özel rolleri (docs/Oyunlar.md 5, "Çakmağa özel roller"): yetenekleri gündüz turuna bağlı (kim kime
    // sordu, kim kimi gösterdi, düelloyu kim kazandı, soru ifşa mı güme mi). Günün turları burada tutulur, gece sırası
    // (Oyun.CakmakGecesi.cs) onları kullanır; her sabah sıfırlanır. Elden elede gizli yetenekleri telefonu tutan kullanır: A (Sorgucu,
    // Sınayıcı, Kuklacı), B (Kışkırtıcı); başkasının düellosuna karışanlar (Hakem, Söndürücü) gece hazırlar.
    public sealed partial class Oyun
    {
        public const int DuellocuEsigi = 3;
        public const int SirKupuEsigi = 3;

        /// <summary>Gözde'nin kazanmak için C olarak gösterilmesi gereken sayı: 4, 12+ kişide 5.</summary>
        int GozdeEsigi => oyuncular.Count >= 12 ? 5 : 4;

        /// <summary>Bugünün turları: A, B ve (seçildiyse) C. Her sabah boşalır.</summary>
        readonly List<(OyuncuId A, OyuncuId B, OyuncuId? C)> gununTurlari = new List<(OyuncuId, OyuncuId, OyuncuId?)>();
        /// <summary>Kışkırtıcı'nın gösterdikleri: bugünün linçine 2 oyla başlar.</summary>
        readonly List<OyuncuId> kiskirtilanlar = new List<OyuncuId>();
        /// <summary>Gece sonuçlanacak şüphe bakışları: Sezgici (gösterdiği C), Sınayıcı (sorduğu B), Söndürücü (söndürdüğü turun A'sı).</summary>
        readonly List<(OyuncuId Sahip, OyuncuId Bakilan)> bekleyenSorgular = new List<(OyuncuId, OyuncuId)>();
        /// <summary>Hakem'in bu geceki seçimindeki karar (ifşa mı); gece çözülünce hazırlığa döner.</summary>
        readonly Dictionary<OyuncuId, bool> hakemKararlari = new Dictionary<OyuncuId, bool>();
        /// <summary>Gece hazırlanır, ertesi gün hedefin karıştığı (B ya da C) ilk düelloda işler; işlemezse o gün biter.</summary>
        readonly Dictionary<OyuncuId, (OyuncuId Hedef, bool Ifsa)> hakemHazirliklari = new Dictionary<OyuncuId, (OyuncuId, bool)>();
        readonly Dictionary<OyuncuId, OyuncuId> sondurucuHazirliklari = new Dictionary<OyuncuId, OyuncuId>();

        // Bu turun bayrakları (her yeni turda sıfırlanır).
        OyuncuId? kuklaSecimi;
        bool sorguBuTur;
        bool sinamaBuTur;
        bool kiskirtmaBekliyor;

        bool GunduzTuruSuruyor => asama == Asama.SoruSorma || asama == Asama.CevapSecme || asama == Asama.MiniOyun;

        bool BugunSordu(OyuncuId a, OyuncuId b) => gununTurlari.Exists(t => t.A == a && t.B == b);

        bool BugunGosterdi(OyuncuId b, OyuncuId c) => gununTurlari.Exists(t => t.B == b && t.C == c);

        bool Hazir(OyuncuId kim) => !Bul(kim).Elendi && !cikan.Contains(kim) && Hak(kim) > 0;

        /// <summary>Dedektif'in kuralı: katil tarafı ve öldüren tarafsızlar şüpheli, Patron temiz; işaretlenen (Tuzakçı, Fitneci) şüpheli.</summary>
        bool SupheliMi(OyuncuId kim, HashSet<OyuncuId> isaretli)
        {
            var rol = roller[kim];
            return (Kotu(rol) && rol != Rol.Patron) || (isaretli != null && isaretli.Contains(kim));
        }

        // ------------------------------------------------------------ Gündüz yeteneği

        /// <summary>
        /// Gündüz turunda gizli yetenek (tur sonuçlanmadan):
        /// Sorgucu (A): soruyu mühürler, düello ne olursa olsun ifşa olur.
        /// Sınayıcı (A): sorduğu B'nin şüpheli olup olmadığını sabah öğrenir.
        /// Kuklacı (A): C'yi B'nin yerine o seçer (<paramref name="hedef"/>); soru sormadan önce seçerse soru gidince işler.
        /// Kışkırtıcı (B): gösterdiği C bugünün linçine 2 oyla başlar; C'yi seçmeden önce kullanırsa seçince işler.
        /// Rol uymuyorsa <see cref="Sonuc.SiraSendeDegil"/> (online'da sadece yazana döner).
        /// </summary>
        public Sonuc GunduzYetenegi(OyuncuId kim, OyuncuId? hedef = null)
        {
            if (!GeceOyunu || !GunduzTuruSuruyor) return Sonuc.YanlisAsama;
            var o = Bul(kim);
            if (o == null || o.Elendi || cikan.Contains(kim)) return Sonuc.SiraSendeDegil;
            switch (roller[kim])
            {
                case Rol.Sorgucu when kim == a:
                    if (sorguBuTur || Hak(kim) <= 0) return Sonuc.HakKalmadi;
                    HakKullan(kim);
                    sorguBuTur = true;
                    zorlaIfsa = true; // "sorun ifşa olur" cezasının aynısı: düello oynanır, bedel yok
                    return Sonuc.Tamam;

                case Rol.Sinayici when kim == a:
                    if (sinamaBuTur || Hak(kim) <= 0) return Sonuc.HakKalmadi;
                    HakKullan(kim);
                    sinamaBuTur = true;
                    if (b.HasValue) bekleyenSorgular.Add((kim, b.Value)); // soru sormadan önceyse soru gidince eklenir
                    return Sonuc.Tamam;

                case Rol.Kuklaci when kim == a:
                    if (!hedef.HasValue) return Sonuc.GecersizGirdi;
                    if (Hak(kim) <= 0) return Sonuc.HakKalmadi;
                    if (asama == Asama.SoruSorma)
                    {
                        if (hedef == a || !Oynayanlar().Contains(hedef.Value)) return Sonuc.Secilemez;
                        kuklaSecimi = hedef;
                        return Sonuc.Tamam;
                    }
                    if (asama != Asama.CevapSecme) return Sonuc.YanlisAsama; // C belli
                    if (!CevapAdaylari().Contains(hedef.Value)) return Sonuc.Secilemez;
                    HakKullan(kim);
                    CevapSec(b.Value, hedef.Value);
                    return Sonuc.Tamam;

                case Rol.Kiskirtici when kim == b:
                    if (Hak(kim) <= 0) return Sonuc.HakKalmadi;
                    if (asama == Asama.CevapSecme)
                    {
                        kiskirtmaBekliyor = true;
                        return Sonuc.Tamam;
                    }
                    HakKullan(kim);
                    kiskirtilanlar.Add(c.Value);
                    return Sonuc.Tamam;

                default:
                    return Sonuc.SiraSendeDegil;
            }
        }

        // ------------------------------------------------------------ Gündüz turunun kancaları

        /// <summary>A soruyu sordu: tur kaydı, Sınayıcı'nın bakışı, Kuklacı'nın önceden seçtiği C.</summary>
        void GeceSoruSoruldu()
        {
            if (!GeceOyunu) return;
            gununTurlari.Add((a, b.Value, null));
            if (sinamaBuTur) bekleyenSorgular.Add((a, b.Value));
            if (!kuklaSecimi.HasValue) return;
            var kukla = kuklaSecimi.Value;
            kuklaSecimi = null;
            // B'yi seçince geçersiz kaldıysa (C, B olamaz) işlemez, hak düşmez: B kendisi seçer.
            if (roller[a] == Rol.Kuklaci && Hak(a) > 0 && CevapAdaylari().Contains(kukla))
            {
                HakKullan(a);
                CevapSec(b.Value, kukla);
            }
        }

        /// <summary>
        /// B, C'yi seçti: tur kaydı, Sezgici'nin bakışı, Kışkırtıcı. Söndürücü ya da Hakem bu düelloya hazırlandıysa düello oynanmaz.
        /// true: tur sonuçlandı.
        /// </summary>
        bool GeceCevapSecildi()
        {
            if (!GeceOyunu) return false;
            gununTurlari[gununTurlari.Count - 1] = (a, b.Value, c.Value);
            if (roller[b.Value] == Rol.Sezgici) bekleyenSorgular.Add((b.Value, c.Value));
            if (kiskirtmaBekliyor && roller[b.Value] == Rol.Kiskirtici && Hak(b.Value) > 0)
            {
                HakKullan(b.Value);
                kiskirtilanlar.Add(c.Value);
            }
            kiskirtmaBekliyor = false;

            // Söndürücü önce: düello söner, soru güme gider, kimse ceza çekmez; turun A'sına sabah bakar.
            foreach (var x in sondurucuHazirliklari)
            {
                if ((x.Value != b && x.Value != c) || !Hazir(x.Key)) continue;
                var sondurucu = x.Key;
                HakKullan(sondurucu);
                sondurucuHazirliklari.Remove(sondurucu);
                bekleyenSorgular.Add((sondurucu, a));
                Yay(new Olay { Tur = OlayTuru.DuelloSonduruldu, A = GorunenA(), B = b, C = c });
                TuruBitir(ifsa: false, kaybeden: null, bedel: false);
                return true;
            }
            // Hakem: düello onun kararıyla biter (ifşa = C kazandı).
            foreach (var x in hakemHazirliklari)
            {
                if ((x.Value.Hedef != b && x.Value.Hedef != c) || !Hazir(x.Key)) continue;
                var hakem = x.Key;
                bool ifsa = x.Value.Ifsa;
                HakKullan(hakem);
                hakemHazirliklari.Remove(hakem);
                Yay(new Olay { Tur = OlayTuru.HakemKarari, A = GorunenA(), B = b, C = c });
                MiniOyunuSay(ifsa);
                SonucaGec(ifsa);
                return true;
            }
            return false;
        }

        /// <summary>Turun sonucu belli: güme gittiyse Kulak ve Dinleyici soruyu güme ekranı boyunca duyar; Yüzleşmeci C olarak kazandıysa B'ye bakar.</summary>
        void GeceTurSonu(bool ifsa, OyuncuId? kaybeden)
        {
            if (!GeceOyunu) return;
            if (!ifsa) kulakSorusu = soru;
            if (c.HasValue && kaybeden == b && roller[c.Value] == Rol.Yuzlesmeci)
                BulguEkle(c.Value, new Bulgu { Kim = b.Value, Supheli = SupheliMi(b.Value, null) });
        }

        void TurBayraklariniSifirla()
        {
            kuklaSecimi = null;
            sorguBuTur = false;
            sinamaBuTur = false;
            kiskirtmaBekliyor = false;
        }

        /// <summary>Yeni gün: dünün turları ve kışkırtmaları silinir (hazırlıklar gece kurulur, bugün işler).</summary>
        void GunBasladi()
        {
            gununTurlari.Clear();
            kiskirtilanlar.Clear();
            TurBayraklariniSifirla();
        }

        /// <summary>Gece çözülürken: dünkü hazırlıklar biter, bu gecenin Hakem ve Söndürücü seçimleri yarına hazırlanır.</summary>
        void HazirliklariKur(List<KeyValuePair<OyuncuId, (OyuncuId? Hedef, OyuncuId? Ikinci)>> secimler)
        {
            hakemHazirliklari.Clear();
            sondurucuHazirliklari.Clear();
            foreach (var s in secimler)
            {
                if (!s.Value.Hedef.HasValue) continue;
                if (roller[s.Key] == Rol.Hakem)
                    hakemHazirliklari[s.Key] = (s.Value.Hedef.Value, !hakemKararlari.TryGetValue(s.Key, out var ifsa) || ifsa);
                else if (roller[s.Key] == Rol.Sondurucu)
                    sondurucuHazirliklari[s.Key] = s.Value.Hedef.Value;
            }
            hakemKararlari.Clear();
        }

        /// <summary>Kazanana bakılmadan kazananlara eklenenler: hayattaki Hayatta Kalan, hedefini tutturan Gözde, Düellocu, Sır Küpü (ölü de olsa).</summary>
        bool EkKazanan(OyuncuDurumu o)
        {
            switch (roller[o.Id])
            {
                case Rol.HayattaKalan: return !o.Elendi && !cikan.Contains(o.Id);
                case Rol.Gozde: return o.Gosterilme >= GozdeEsigi;
                case Rol.Duellocu: return o.MiniOyunKazanma >= DuellocuEsigi;
                case Rol.SirKupu: return o.SoruAlma >= SirKupuEsigi && IzAl(o.Id).BIkenIfsa == 0; // "hiç ifşa olma" görevi gibi
                default: return false;
            }
        }
    }
}
