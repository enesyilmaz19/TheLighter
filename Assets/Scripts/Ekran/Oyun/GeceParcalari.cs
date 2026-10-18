using System;
using System.Collections.Generic;
using Cakmak.Kurallar;
using TheLighter.Akis;
using UnityEngine.UIElements;
using static TheLighter.Istemci.Arayuz;

namespace TheLighter.Istemci
{
    /// <summary>
    /// Çakmak Gecesi ekranlarının ortak parçaları (docs/Oyunlar.md 5, tasks/g2-cakmak-gecesi.md): rol kartı, gün yazısı, ölü ve
    /// susturulan etiketleri, gece bakışlarının sonuçları, sabah/linç/suikast duyuruları, gündüz yeteneği, oyun sonu. Kural
    /// hesaplanmaz: rol, taraf, adaylar, haklar, bulgular kural motorunun görünümünden; rolün ne yaptığı metin tablosunda.
    /// </summary>
    static class Gece
    {
        public static bool Mi(Gorunum g) => g != null && g.Oyun == OyunTuru.CakmakGecesi;

        public static string RolAdi(Rol r) => Metin.Al("gece.rol." + r);

        public static string TarafAdi(Rol r) => Metin.Al("gece.taraf." + Oyun.TarafI(r));

        static OyuncuDurumu Bul(Gorunum g, OyuncuId? id)
        {
            if (g == null || !id.HasValue) return null;
            foreach (var o in g.Oyuncular) if (o.Id == id.Value) return o;
            return null;
        }

        /// <summary>
        /// Rol kartı (rol perdesi ve "Rolüm"): ad, taraf, ne yaptığı, kalan hak; katile diğer katiller, Cellat'a hedefi, Bombacı'ya
        /// bombalılar; Gözde, Düellocu ve Sır Küpü'ne sayaçları. Deli'ye sandığı rol (görünüm öyle verir; ayrıca gösterilmez).
        /// </summary>
        public static VisualElement RolKarti(IOyunAkisi akis, Gorunum g)
        {
            var r = g.Rolun;
            var kart = Kutu("rol-karti", "gece-rol", "taraf-" + Oyun.TarafI(r));
            kart.Add(Yazi(RolAdi(r), "rol-ad"));
            kart.Add(Yazi(Metin.Al("gece.taraf_satir", TarafAdi(r)), "aciklama", "orta"));
            kart.Add(Yazi(Metin.Al("gece.rol_aciklama." + r), "ipucu", "orta"));
            if (g.KalanHakkin.HasValue) kart.Add(Yazi(Metin.Al("gece.kalan_hak", g.KalanHakkin.Value), "ipucu", "orta"));
            var sayac = Sayac(g);
            if (sayac != null) kart.Add(Yazi(sayac, "ipucu", "orta"));
            if (g.Katiller.Count > 0)
            {
                var diger = new List<string>();
                foreach (var k in g.Katiller)
                    if (k.Kim != g.Sen) diger.Add(akis.Isim(k.Kim) + " (" + RolAdi(k.Rol) + ")");
                kart.Add(Yazi(diger.Count > 0 ? Metin.Al("gece.diger_katiller", string.Join(", ", diger)) : Metin.Al("gece.tek_katil"),
                    "aciklama", "orta"));
            }
            if (g.CellatHedefin.HasValue) kart.Add(Yazi(Metin.Al("gece.cellat_hedefin", akis.Isim(g.CellatHedefin)), "aciklama", "orta"));
            if (g.Bombalilar.Count > 0) kart.Add(Yazi(Metin.Al("gece.bombalilar", Isimler(akis, g.Bombalilar)), "ipucu", "orta"));
            return kart;
        }

        /// <summary>Ek kazananların sayacı (sahibine): Gözde gösterilme, Düellocu kazanılan düello, Sır Küpü soru alma.</summary>
        static string Sayac(Gorunum g)
        {
            var ben = Bul(g, g.Sen);
            if (ben == null) return null;
            switch (g.Rolun)
            {
                case Rol.Gozde: return Metin.Al("gece.sayac.Gozde", ben.Gosterilme);
                case Rol.Duellocu: return Metin.Al("gece.sayac.Duellocu", ben.MiniOyunKazanma, Oyun.DuellocuEsigi);
                case Rol.SirKupu:
                    return Metin.Al("gece.sayac.SirKupu", ben.SoruAlma, Oyun.SirKupuEsigi,
                        Metin.Al(ben.SoruAlma == ben.Saklama ? "gece.sayac.ifsa_yok" : "gece.sayac.ifsa_var"));
                default: return null;
            }
        }

        public static string Isimler(IOyunAkisi akis, IEnumerable<OyuncuId> ids)
        {
            var l = new List<string>();
            foreach (var x in ids) l.Add(akis.Isim(x));
            return string.Join(", ", l);
        }

        /// <summary>Üst çubuk: "Gün 2 · linçe 1 tur", "Gün 2 · Linç", "Gece 2".</summary>
        public static string GunYazisi(Gorunum g)
        {
            if (g.Asama == Asama.Gece) return Metin.Al("gece.ust_gece", g.Gun);
            if (g.Asama == Asama.Linc) return Metin.Al("gece.ust_linc", g.Gun);
            return Metin.Al("gece.ust_gun", g.Gun, g.KalanGunduzTuru + 1);
        }

        /// <summary>İsmin yanına: ölü (rolü açıldıysa rolüyle) ya da susturuldu. Çakmak Gecesi değilse null.</summary>
        public static string Etiket(Gorunum g, OyuncuDurumu o)
        {
            if (!Mi(g) || o == null) return null;
            if (o.Elendi)
                return o.AcikRol.HasValue ? Metin.Al("gece.olu_rol", RolAdi(o.AcikRol.Value)) : Metin.Al("gece.olu");
            return o.Susturuldu ? Metin.Al("gece.susturuldu") : null;
        }

        /// <summary>"Ayşe (Dedektif), Ali (rolü bilinmiyor)": ölenler, açılan rolleriyle.</summary>
        public static string OlenListesi(IOyunAkisi akis, Gorunum g, IReadOnlyList<OyuncuId> olenler)
        {
            var l = new List<string>();
            foreach (var id in olenler)
            {
                var o = Bul(g, id);
                string rol = o != null && o.AcikRol.HasValue ? RolAdi(o.AcikRol.Value) : Metin.Al("gece.rol_bilinmiyor");
                l.Add(akis.Isim(id) + " (" + rol + ")");
            }
            return string.Join(", ", l);
        }

        /// <summary>Sabah, linç ya da suikast duyurusu.</summary>
        public static string DuyuruMetni(IOyunAkisi akis, Gorunum g, GeceDuyurusu d)
        {
            switch (d.Tur)
            {
                case GeceDuyurusu.Turu.Sabah:
                    return d.Olenler.Count == 0 ? Metin.Al("gece.sabah_yok") : Metin.Al("gece.sabah_olenler", OlenListesi(akis, g, d.Olenler));
                case GeceDuyurusu.Turu.Linc:
                    return d.Olenler.Count == 0 ? Metin.Al("gece.linc_yok") : Metin.Al("gece.linc_elendi", OlenListesi(akis, g, d.Olenler));
                default:
                    return Metin.Al("gece.suikast", OlenListesi(akis, g, d.Olenler));
            }
        }

        public static void Duyurular(VisualElement kok, IOyunAkisi akis, Gorunum g, IReadOnlyList<GeceDuyurusu> liste)
        {
            if (liste == null) return;
            foreach (var d in liste) kok.Add(Yazi(DuyuruMetni(akis, g, d), "banner", d.Tur == GeceDuyurusu.Turu.Sabah ? "gece-sabah" : "tetik-bam"));
        }

        /// <summary>Bir gece bakışının sonucu: "Ayşe: şüpheli", "Ali: Patron", "Can'a uğrayanlar: …", "Grupta kötü var".</summary>
        public static string BulguMetni(IOyunAkisi akis, Bulgu b)
        {
            string kim = akis.Isim(b.Kim);
            if (b.Supheli.HasValue) return Metin.Al(b.Supheli.Value ? "gece.bulgu.supheli" : "gece.bulgu.temiz", kim);
            if (b.Rol.HasValue) return Metin.Al("gece.bulgu.rol", kim, RolAdi(b.Rol.Value));
            if (b.Grup.Count > 0)
                return Metin.Al(b.KotuVar == true ? "gece.bulgu.kotu_var" : "gece.bulgu.kotu_yok", kim, Isimler(akis, b.Grup));
            return b.Ziyaretciler.Count > 0
                ? Metin.Al("gece.bulgu.ziyaret", kim, TurkceEk.Yonelme(kim), Isimler(akis, b.Ziyaretciler))
                : Metin.Al("gece.bulgu.ziyaret_yok", kim, TurkceEk.Yonelme(kim));
        }

        /// <summary>
        /// Son gecenin (ve bugünün) bakış sonuçları, sadece sahibinin kendi ekranında. Bakışı olmayana da aynı kutu gelir ("yeni bir
        /// şey yok"): kutunun varlığından rol anlaşılmasın.
        /// </summary>
        public static VisualElement BulguKutusu(IOyunAkisi akis, Gorunum g)
        {
            var kutu = Kutu("gece-bulgu");
            if (!Mi(g)) return kutu;
            kutu.Add(Yazi(Metin.Al(g.Gun <= 1 ? "gece.bulgu_baslik_ilk" : "gece.bulgu_baslik"), "ipucu"));
            int n = 0;
            foreach (var b in g.Bulgularin)
            {
                if (b.Gun < g.Gun - 1) continue;
                kutu.Add(Yazi(BulguMetni(akis, b), "aciklama"));
                n++;
            }
            if (n == 0) kutu.Add(Yazi(Metin.Al("gece.bulgu_yok"), "aciklama", "soluk"));
            return kutu;
        }

        /// <summary>Oyunun bütün bulguları (Rolüm penceresinde).</summary>
        public static VisualElement TumBulgular(IOyunAkisi akis, Gorunum g)
        {
            var kutu = Kutu("gece-bulgu");
            if (g.Bulgularin.Count == 0) return kutu;
            kutu.Add(Yazi(Metin.Al("gece.bulgular_hepsi"), "ipucu"));
            foreach (var b in g.Bulgularin) kutu.Add(Yazi(Metin.Al("gece.bulgu_gun", b.Gun, BulguMetni(akis, b)), "aciklama"));
            return kutu;
        }

        /// <summary>Soru yazma ekranında herkese: güme giden soruyu Kulak ya da Dinleyici duyabilir (Ana Plan 8.5'in tek istisnası).</summary>
        public static VisualElement KulakNotu(Gorunum g) => Mi(g) ? Yazi(Metin.Al("gece.kulak_notu"), "ipucu") : null;

        /// <summary>
        /// Gündüz yeteneği: Sorgucu, Sınayıcı, Kuklacı A iken (soru yazma ekranında), Kışkırtıcı B iken (cevap seçme ekranında).
        /// Rolü uymayana boş bir kutu: ekranın düzeni herkeste aynı. Kuklacı C'yi seçer. Hak bittiyse düğme kapalı.
        /// </summary>
        public static VisualElement GunduzYetenegi(IOyunAkisi akis, Gorunum g, bool aIken, VisualElement perdeYeri, Action<string> hataYaz)
        {
            var kap = Kutu("gece-yetenek");
            if (!Mi(g)) return kap;
            var r = g.Rolun;
            bool uyar = aIken ? r == Rol.Sorgucu || r == Rol.Sinayici || r == Rol.Kuklaci : r == Rol.Kiskirtici;
            if (!uyar) return kap;

            bool hakVar = !g.KalanHakkin.HasValue || g.KalanHakkin.Value > 0;
            string yazi = Metin.Al("gece.yetenek." + r) + (g.KalanHakkin.HasValue ? "  " + Metin.Al("gece.hak_kisa", g.KalanHakkin.Value) : "");
            Button dugme = null;
            dugme = Buton(yazi, () =>
            {
                if (r == Rol.Kuklaci)
                {
                    KuklaSec(akis, g, perdeYeri, dugme, hataYaz);
                    return;
                }
                Sonuclandir(akis.GunduzYetenegi(null), dugme, hataYaz);
            }, "buton-ikincil");
            dugme.SetEnabled(hakVar);
            kap.Add(dugme);
            kap.Add(Yazi(Metin.Al("gece.yetenek_ipucu." + r), "ipucu"));
            return kap;
        }

        static void Sonuclandir(Sonuc s, Button dugme, Action<string> hataYaz)
        {
            if (s == Sonuc.Tamam)
            {
                dugme.text = Metin.Al("gece.yetenek_kullanildi");
                dugme.SetEnabled(false);
            }
            else hataYaz?.Invoke(HataMetni.Al(s));
        }

        static void KuklaSec(IOyunAkisi akis, Gorunum g, VisualElement perde, Button dugme, Action<string> hataYaz)
        {
            perde.Clear();
            var kutu = Kutu("menu-kutu");
            kutu.Add(Yazi(Metin.Al("gece.kukla_baslik"), "baslik"));
            kutu.Add(Yazi(Metin.Al("gece.kukla_ipucu"), "ipucu"));
            var cipler = Kutu("cipler");
            foreach (var o in g.Oyuncular)
            {
                if (o.Id == g.Sen || o.Elendi || o.Cikti) continue;
                var id = o.Id;
                cipler.Add(OyuncuCipi(akis.Isim(id), akis.Sira(id), () =>
                {
                    perde.AddToClassList("gizli");
                    Sonuclandir(akis.GunduzYetenegi(id), dugme, hataYaz);
                }));
            }
            kutu.Add(cipler);
            kutu.Add(Buton(Metin.Al("genel.geri"), () => perde.AddToClassList("gizli"), "buton-ikincil"));
            perde.Add(kutu);
            perde.RemoveFromClassList("gizli");
            perde.BringToFront();
        }

        /// <summary>Oyun sonunun en üstü: kim kazandı, kazananlar, herkesin gerçek rolü (Deli dahil).</summary>
        public static VisualElement Acilis(IOyunAkisi akis, CakmakGecesiSonucu s)
        {
            var kart = Kutu("kostebek-acilis", "gece-acilis");
            kart.Add(Yazi(s.Kazanan.HasValue ? Metin.Al("gece.kazanan." + s.Kazanan.Value) : Metin.Al("gece.kazanan_yok"), "kostebek-kazanan"));
            if (s.Kazananlar.Count > 0) kart.Add(Yazi(Metin.Al("gece.kazananlar", Isimler(akis, s.Kazananlar)), "aciklama", "orta"));
            kart.Add(Yazi(Metin.Al("gece.roller_baslik"), "alt-baslik"));
            foreach (var r in s.Roller)
            {
                var satir = Kutu("satir");
                satir.Add(RenkNoktasi(akis.Sira(r.Kim)));
                bool kazandi = false;
                foreach (var k in s.Kazananlar) if (k == r.Kim) kazandi = true;
                satir.Add(Yazi(Metin.Al("gece.rol_satir", akis.Isim(r.Kim), RolAdi(r.Rol), TarafAdi(r.Rol)), "aciklama"));
                if (kazandi) satir.Add(Yazi(Metin.Al("gece.kazandi_etiket"), "lobi-rozet"));
                kart.Add(satir);
            }
            return kart;
        }
    }

    /// <summary>
    /// Linç oyu: "Kim gitsin?" Elden elede telefon sırayla dolaşır (saat durur), online'da herkes kendi telefonunda. Kimin kime
    /// oy verdiği hiçbir yerde yok. Suikastçı'da ayrıca "Suikast": kişi ve rol seçer, onaylar.
    /// </summary>
    sealed class LincParcasi : IOyunParcasi
    {
        readonly IOyunAkisi akis;
        readonly Label sure;
        readonly Label oySayisi;
        readonly Label hata;
        readonly Button ver;
        readonly VisualElement perde;
        readonly List<OyuncuId> ciptekiler = new List<OyuncuId>();
        readonly List<Button> cipler = new List<Button>();
        OyuncuId? secili;

        public VisualElement Kok { get; }

        public LincParcasi(IOyunAkisi akis, EkranDurumu e)
        {
            this.akis = akis;
            var g = e.Gorunum;
            Kok = Kutu("parca", "perde", "gece-linc");

            var bas = Kutu("satir");
            bas.Add(Yazi(Metin.Al("gece.linc_baslik", akis.Isim(g.Sen)), "baslik"));
            bas.Add(Esnek());
            sure = Yazi("", "sure");
            sure.EnableInClassList("gizli", !akis.Online);
            bas.Add(sure);
            Kok.Add(bas);
            Kok.Add(Yazi(Metin.Al("gece.rolun_kisa", Gece.RolAdi(g.Rolun)), "ipucu"));
            Kok.Add(Gece.BulguKutusu(akis, g));

            Kok.Add(Yazi(Metin.Al("gece.linc_kim"), "perde-buyuk"));
            Kok.Add(Yazi(Metin.Al("gece.linc_aciklama"), "aciklama", "orta"));
            var cipKutusu = Kutu("cipler");
            foreach (var id in g.Secilebilir)
            {
                var secim = id;
                var cip = OyuncuCipi(Tekrar.IsimEtiketi(akis, g, id), akis.Sira(id), () => { secili = secim; Durum(); });
                ciptekiler.Add(id);
                cipler.Add(cip);
                cipKutusu.Add(cip);
            }
            Kok.Add(cipKutusu);
            oySayisi = Yazi("", "ipucu", "orta");
            Kok.Add(oySayisi);
            Kok.Add(Esnek());

            perde = Kutu("menu-perde", "gizli");
            // Suikastçı: linç sürerken oyunda 1 kez (hak görünümde).
            if (g.Rolun == Rol.Suikastci && g.KalanHakkin.HasValue && g.KalanHakkin.Value > 0)
                Kok.Add(Buton(Metin.Al("gece.suikast_dugme"), () => SuikastKisi(g), "buton-ikincil"));

            Kok.Add(Yazi(Metin.Al("oy.gizli"), "ipucu"));
            hata = Yazi("", "hata");
            Kok.Add(hata);
            ver = Buton(Metin.Al("gece.linc_ver"), Ver, "buton-birincil", "buton-buyuk");
            Kok.Add(ver);
            Kok.Add(perde);
            Durum();
            Guncelle(e);
        }

        void Durum()
        {
            for (int i = 0; i < cipler.Count; i++)
                cipler[i].EnableInClassList("secili", secili.HasValue && ciptekiler[i] == secili.Value);
            ver.SetEnabled(secili.HasValue);
            hata.text = "";
        }

        void Ver()
        {
            if (!secili.HasValue) return;
            var s = akis.LincOyu(secili.Value);
            if (s != Sonuc.Tamam) hata.text = HataMetni.Al(s);
            else if (akis.Online) Kok.SetEnabled(false);
        }

        void SuikastKisi(Gorunum g)
        {
            perde.Clear();
            var kutu = Kutu("menu-kutu");
            kutu.Add(Yazi(Metin.Al("gece.suikast_kim"), "baslik"));
            kutu.Add(Yazi(Metin.Al("gece.suikast_ipucu"), "ipucu"));
            var c = Kutu("cipler");
            foreach (var id in g.Secilebilir)
            {
                var hedef = id;
                c.Add(OyuncuCipi(akis.Isim(hedef), akis.Sira(hedef), () => SuikastRol(hedef)));
            }
            kutu.Add(c);
            kutu.Add(Buton(Metin.Al("genel.geri"), () => perde.AddToClassList("gizli"), "buton-ikincil"));
            Ac(kutu);
        }

        void SuikastRol(OyuncuId hedef)
        {
            var kutu = Kutu("menu-kutu");
            kutu.Add(Yazi(Metin.Al("gece.suikast_rol", akis.Isim(hedef)), "baslik"));
            var kaydirma = new ScrollView(ScrollViewMode.Vertical);
            kaydirma.AddToClassList("kaydirma");
            var izgara = Kutu("secenekler");
            foreach (var r in Oyun.GeceRolleri)
            {
                var rol = r;
                izgara.Add(Buton(Gece.RolAdi(rol), () => SuikastOnay(hedef, rol), "secenek"));
            }
            kaydirma.Add(izgara);
            kutu.Add(kaydirma);
            kutu.Add(Buton(Metin.Al("genel.geri"), () => perde.AddToClassList("gizli"), "buton-ikincil"));
            Ac(kutu);
        }

        void SuikastOnay(OyuncuId hedef, Rol rol)
        {
            var kutu = Kutu("menu-kutu");
            kutu.Add(Yazi(Metin.Al("gece.suikast_emin", akis.Isim(hedef), Gece.RolAdi(rol)), "baslik"));
            kutu.Add(Yazi(Metin.Al("gece.suikast_uyari"), "ipucu"));
            kutu.Add(Buton(Metin.Al("gece.suikast_evet"), () =>
            {
                perde.AddToClassList("gizli");
                var s = akis.Suikast(hedef, rol);
                if (s != Sonuc.Tamam) hata.text = HataMetni.Al(s);
            }, "buton-birincil"));
            kutu.Add(Buton(Metin.Al("genel.geri"), () => perde.AddToClassList("gizli"), "buton-ikincil"));
            Ac(kutu);
        }

        void Ac(VisualElement kutu)
        {
            perde.Clear();
            perde.Add(kutu);
            perde.RemoveFromClassList("gizli");
            perde.BringToFront();
        }

        public void Guncelle(EkranDurumu e)
        {
            var g = e.Gorunum;
            if (akis.Online) SureyiYaz(sure, g.KalanMs, 10000);
            oySayisi.text = Metin.Al("online.bekle.oy_sayisi", g.OyKullanan, g.Oylayanlar.Count);
        }
    }

    /// <summary>
    /// Gece seçimi. Herkes bir seçim yapar ya da pas der; yeteneği olmayan "birini düşünür" (kimin uzun düşündüğünden rol
    /// anlaşılmasın). Bütün rollerde aynı düzen ve aynı en kısa süre (düğmeler aynı anda açılır). Katil tarafında kurban oyu,
    /// diğer katillerin oyları ve (varsa) yetenek; Hakem'de yarınki karar. Adaylar görünümden, süzülmez.
    /// </summary>
    sealed class GeceParcasi : IOyunParcasi
    {
        /// <summary>Gece yeteneği olan masum ve tarafsız roller: talimatı kendine özel. Diğerleri "birini düşün".</summary>
        static readonly HashSet<Rol> Talimatli = new HashSet<Rol>
        {
            Rol.Koruyucu, Rol.Dedektif, Rol.Serif, Rol.Kanunsuz, Rol.Bekci, Rol.Falci, Rol.SeriKatil, Rol.Hirsiz, Rol.Bombaci,
            Rol.Kibritci, Rol.Hakem, Rol.Sondurucu,
        };

        readonly IOyunAkisi akis;
        readonly Label sure;
        readonly Label katilOylari;
        readonly Label hata;
        readonly Button sec;
        readonly Button pas;
        readonly List<OyuncuId> ciptekiler = new List<OyuncuId>();
        readonly List<Button> cipler = new List<Button>();
        readonly List<OyuncuId> ikinciCiptekiler = new List<OyuncuId>();
        readonly List<Button> ikinciCipler = new List<Button>();
        readonly Button ifsaDugmesi;
        readonly Button gumeDugmesi;
        readonly bool hakem;
        OyuncuId? secili;
        OyuncuId? ikinciSecili;
        bool ifsa = true;

        public VisualElement Kok { get; }

        public GeceParcasi(IOyunAkisi akis, EkranDurumu e)
        {
            this.akis = akis;
            var g = e.Gorunum;
            var r = g.Rolun;
            bool katil = Oyun.TarafI(r) == Taraf.Katil;
            hakem = r == Rol.Hakem;
            Kok = Kutu("parca", "perde", "gece-secim");

            var bas = Kutu("satir");
            bas.Add(Yazi(Metin.Al("gece.gece_baslik", g.Gun, akis.Isim(g.Sen)), "baslik"));
            bas.Add(Esnek());
            sure = Yazi("", "sure");
            sure.EnableInClassList("gizli", !akis.Online);
            bas.Add(sure);
            Kok.Add(bas);
            Kok.Add(Yazi(Metin.Al("gece.rolun_kisa", Gece.RolAdi(r)), "ipucu"));
            Kok.Add(Gece.BulguKutusu(akis, g));

            string talimat = katil ? "gece.talimat_katil" : Talimatli.Contains(r) ? "gece.talimat." + r : "gece.talimat_yok";
            Kok.Add(Yazi(Metin.Al(talimat), "perde-buyuk"));

            var kaydirma = new ScrollView(ScrollViewMode.Vertical);
            kaydirma.AddToClassList("kaydirma");
            var cipKutusu = Kutu("cipler");
            foreach (var id in g.Secilebilir)
            {
                var secim = id;
                // Bombacı kendini seçerse bombaları patlatır.
                string isim = id == g.Sen && r == Rol.Bombaci ? Metin.Al("gece.patlat") : Tekrar.IsimEtiketi(akis, g, id);
                var cip = OyuncuCipi(isim, akis.Sira(id), () => { secili = secili == secim ? (OyuncuId?)null : secim; Durum(); });
                ciptekiler.Add(id);
                cipler.Add(cip);
                cipKutusu.Add(cip);
            }
            kaydirma.Add(cipKutusu);

            // Katil tarafı: diğer katillerin kurban oyları ve rolün yeteneği (ikinci hedef).
            katilOylari = Yazi("", "ipucu");
            if (katil) kaydirma.Add(katilOylari);
            if (g.IkinciSecilebilir.Count > 0)
            {
                kaydirma.Add(Yazi(Metin.Al("gece.ikinci." + r), "alt-baslik"));
                var ikinci = Kutu("cipler");
                foreach (var id in g.IkinciSecilebilir)
                {
                    var secim = id;
                    var cip = OyuncuCipi(Tekrar.IsimEtiketi(akis, g, id), akis.Sira(id), () =>
                    {
                        ikinciSecili = ikinciSecili == secim ? (OyuncuId?)null : secim;
                        Durum();
                    });
                    ikinciCiptekiler.Add(id);
                    ikinciCipler.Add(cip);
                    ikinci.Add(cip);
                }
                kaydirma.Add(ikinci);
            }
            // Hakem: yarınki düellonun kararı.
            if (hakem)
            {
                var karar = Kutu("secenekler");
                ifsaDugmesi = Buton(Metin.Al("gece.hakem_ifsa"), () => { ifsa = true; Durum(); }, "secenek");
                gumeDugmesi = Buton(Metin.Al("gece.hakem_gume"), () => { ifsa = false; Durum(); }, "secenek");
                karar.Add(ifsaDugmesi);
                karar.Add(gumeDugmesi);
                kaydirma.Add(karar);
            }
            Kok.Add(kaydirma);

            hata = Yazi("", "hata");
            Kok.Add(hata);
            var alt = Kutu("satir");
            pas = Buton(Metin.Al("gece.pas"), Pas, "buton-ikincil");
            sec = Buton(Metin.Al("gece.sec"), Sec, "buton-birincil", "buton-buyuk");
            alt.Add(pas);
            alt.Add(Esnek());
            alt.Add(sec);
            Kok.Add(alt);
            // Herkes en az bu kadar bekler: kimin hızlı geçtiğinden rol anlaşılmasın.
            GecikmeliAc(pas, Kostebek.RolPerdesiMs);
            GecikmeliAc(sec, Kostebek.RolPerdesiMs);
            Durum();
            Guncelle(e);
        }

        void Durum()
        {
            for (int i = 0; i < cipler.Count; i++)
                cipler[i].EnableInClassList("secili", secili.HasValue && ciptekiler[i] == secili.Value);
            for (int i = 0; i < ikinciCipler.Count; i++)
                ikinciCipler[i].EnableInClassList("secili", ikinciSecili.HasValue && ikinciCiptekiler[i] == ikinciSecili.Value);
            ifsaDugmesi?.EnableInClassList("secili", ifsa);
            gumeDugmesi?.EnableInClassList("secili", !ifsa);
            sec.text = Metin.Al(secili.HasValue || ikinciSecili.HasValue ? "gece.sec" : "gece.sec_bos");
            hata.text = "";
        }

        void Sec() => Gonder(secili, ikinciSecili, hakem && secili.HasValue ? ifsa : (bool?)null);

        void Pas() => Gonder(null, null, null);

        void Gonder(OyuncuId? hedef, OyuncuId? ikinci, bool? karar)
        {
            var s = akis.GeceSecimi(hedef, ikinci, karar);
            if (s != Sonuc.Tamam) hata.text = HataMetni.Al(s);
            else if (akis.Online) Kok.SetEnabled(false);
        }

        public void Guncelle(EkranDurumu e)
        {
            var g = e.Gorunum;
            if (akis.Online) SureyiYaz(sure, g.KalanMs, 10000);
            if (g.KatilSecimleri.Count == 0)
            {
                katilOylari.text = Oyun.TarafI(g.Rolun) == Taraf.Katil ? Metin.Al("gece.katil_oy_yok") : "";
                return;
            }
            var l = new List<string>();
            foreach (var k in g.KatilSecimleri)
                if (k.Kim != g.Sen) l.Add(Metin.Al("gece.katil_oy", akis.Isim(k.Kim), akis.Isim(k.Hedef)));
            katilOylari.text = l.Count > 0 ? string.Join("  ·  ", l) : Metin.Al("gece.katil_oy_yok");
        }
    }

    /// <summary>
    /// Çakmak Gecesi ayarları (elden ele kurulum ve online lobi ortak): ölenin rolü açılsın mı, gecelik kurban, roller (taraf taraf,
    /// her rolün yanında aç/kapat). Masum ve Katil kapatılamaz (listede yok). Uyum kural motoruna sorulur.
    /// </summary>
    sealed class GeceAyarlari
    {
        static readonly int[] KurbanSecenekleri = { 1, 2, 3 };

        readonly Action<bool> rolAcilirDegisti;
        readonly Action<int> kurbanDegisti;
        readonly Action<Rol> rolDegisti;
        readonly Button rolAcilir;
        readonly List<Button> kurbanButonlari = new List<Button>();
        readonly Dictionary<Rol, Button> rolButonlari = new Dictionary<Rol, Button>();

        public VisualElement Kok { get; }

        public GeceAyarlari(Action<bool> rolAcilirDegisti, Action<int> kurbanDegisti, Action<Rol> rolDegisti)
        {
            this.rolAcilirDegisti = rolAcilirDegisti;
            this.kurbanDegisti = kurbanDegisti;
            this.rolDegisti = rolDegisti;
            Kok = Kutu("ayar-bolumu", "gece-ayarlari");
            Kok.Add(Yazi(Metin.Al("gece.en_az", Oyun.GeceEnAzOyuncu, Oyun.GeceEnFazlaOyuncu), "ipucu"));

            Kok.Add(Yazi(Metin.Al("gece.ayar_rol_acilir"), "alt-baslik"));
            var satir = Kutu("secenekler");
            rolAcilir = Buton("", () => this.rolAcilirDegisti?.Invoke(!rolAcilir.ClassListContains("secili")), "secenek");
            satir.Add(rolAcilir);
            Kok.Add(satir);

            Kok.Add(Yazi(Metin.Al("gece.ayar_kurban"), "alt-baslik"));
            var kurban = Kutu("secenekler");
            foreach (var k in KurbanSecenekleri)
            {
                var sayi = k;
                var b = Buton(Metin.Al("gece.ayar_kurban_sayi", k), () => this.kurbanDegisti?.Invoke(sayi), "secenek");
                kurbanButonlari.Add(b);
                kurban.Add(b);
            }
            Kok.Add(kurban);
            Kok.Add(Yazi(Metin.Al("gece.ayar_kurban_ipucu"), "ipucu"));

            Kok.Add(Yazi(Metin.Al("gece.ayar_roller"), "alt-baslik"));
            Kok.Add(Yazi(Metin.Al("gece.ayar_roller_ipucu"), "ipucu"));
            foreach (Taraf t in Enum.GetValues(typeof(Taraf)))
            {
                Kok.Add(Yazi(Metin.Al("gece.taraf." + t), "ipucu", "gece-taraf-baslik"));
                var kutu = Kutu("secenekler");
                foreach (var r in Oyun.GeceRolleri)
                {
                    if (Oyun.TarafI(r) != t || r == Rol.Masum || r == Rol.Katil) continue;
                    var rol = r;
                    var b = Buton(Gece.RolAdi(rol), () => this.rolDegisti?.Invoke(rol), "secenek", "gece-rol-dugme");
                    rolButonlari[rol] = b;
                    kutu.Add(b);
                }
                Kok.Add(kutu);
            }
        }

        /// <summary>Durumdan çizer: seçili olanlar açık roller.</summary>
        public void Ciz(bool rolAcilirMi, int gecelikKurban, IEnumerable<Rol> kapali)
        {
            rolAcilir.text = Metin.Al(rolAcilirMi ? "gece.ayar_rol_acik" : "gece.ayar_rol_gizli");
            rolAcilir.EnableInClassList("secili", rolAcilirMi);
            for (int i = 0; i < KurbanSecenekleri.Length; i++)
                kurbanButonlari[i].EnableInClassList("secili", KurbanSecenekleri[i] == gecelikKurban);
            var kapaliKume = new HashSet<Rol>(kapali ?? Array.Empty<Rol>());
            foreach (var kv in rolButonlari) kv.Value.EnableInClassList("secili", !kapaliKume.Contains(kv.Key));
        }

        /// <summary>Rolün açık/kapalı değiştiği yeni liste.</summary>
        public static List<Rol> Degistir(IEnumerable<Rol> kapali, Rol rol)
        {
            var l = new List<Rol>(kapali ?? Array.Empty<Rol>());
            if (!l.Remove(rol)) l.Add(rol);
            return l;
        }
    }
}
