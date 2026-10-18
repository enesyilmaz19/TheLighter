using System.Collections.Generic;
using Cakmak.Kurallar;
using TheLighter.Akis;
using UnityEngine.UIElements;
using static TheLighter.Istemci.Arayuz;

namespace TheLighter.Istemci
{
    /// <summary>
    /// Köstebek ekranlarının ortak parçaları (docs/Oyunlar.md 1, tasks/g2-kostebek.md): rol başlığı, erken suçlama düğmesi,
    /// suçlama sonucu, oyun sonundaki köstebek açılışı. Kural hesaplanmaz: kim köstebek, kimin hakkı var, kim seçilebilir
    /// kural motorunun görünümünden.
    /// </summary>
    static class Kostebek
    {
        /// <summary>Rol perdesi herkese aynı uzunlukta: "Gördüm" bu kadar sonra açılır, kimin uzun baktığından köstebek anlaşılmasın.</summary>
        public const long RolPerdesiMs = 3000;

        /// <summary>
        /// "Kim?" oylamasının metni. Köstebek ve Casus aynı oylamayı kullanır (kural motorunda da aynı kod), sadece adı farklı:
        /// Casus'ta "casus." anahtarı ("Casus kim?"), diğerlerinde "kostebek." ("Köstebek kim?").
        /// </summary>
        public static string OylamaMetni(OyunTuru oyun, string ad, params object[] p) =>
            Metin.Al((oyun == OyunTuru.Casus ? "casus." : "kostebek.") + ad, p);

        /// <summary>Oylamalar arası tur sayısı (kural motorunun sabiti; ekranda "her N turda bir").</summary>
        public static int Aralik(OyunTuru oyun) => oyun == OyunTuru.Casus ? Oyun.CasusSuclamaAraligi : Oyun.SuclamaAraligi;

        /// <summary>Görev perdesinin üstüne rol: köstebeğe "Köstebeksin!", diğerlerine "Köstebek değilsin". İkisi aynı biçimde.</summary>
        public static VisualElement RolBasligi(Gorunum g)
        {
            bool kostebek = g.Rolun == Rol.Kostebek;
            var kutu = Kutu("rol-karti", kostebek ? "rol-kostebek" : "rol-masum");
            kutu.Add(Yazi(Metin.Al(kostebek ? "kostebek.rol_sen" : "kostebek.rol_degil"), "rol-ad"));
            kutu.Add(Yazi(Metin.Al(kostebek ? "kostebek.rol_sen_aciklama" : "kostebek.rol_degil_aciklama"), "ipucu", "orta"));
            return kutu;
        }

        /// <summary>
        /// Erken suçlama: hakkı olan biri varsa düğme, yoksa boş. Elden elede telefon A'da: "Kim suçluyor?" sorulur.
        /// Online'da sadece kendi hakkım: onaylayınca gider. <paramref name="perdeYeri"/>: soru kutusunun açılacağı yer.
        /// </summary>
        public static VisualElement SuclamaDugmesi(IOyunAkisi akis, VisualElement perdeYeri, System.Action<string> hataYaz)
        {
            var hak = akis.SuclamaHakkiOlanlar;
            var kap = Kutu("suclama-dugmesi");
            if (hak.Count == 0) return kap;

            var perde = Kutu("menu-perde", "gizli");
            perdeYeri.Add(perde);
            kap.Add(Buton(Metin.Al("kostebek.erken_sucla"), () => Ac(akis, perde, hataYaz), "buton-ikincil"));
            kap.Add(Yazi(Metin.Al("kostebek.erken_sucla_ipucu"), "ipucu"));
            return kap;
        }

        static void Ac(IOyunAkisi akis, VisualElement perde, System.Action<string> hataYaz)
        {
            perde.Clear();
            var kutu = Kutu("menu-kutu");
            var hak = akis.SuclamaHakkiOlanlar;
            if (akis.Online)
            {
                kutu.Add(Yazi(Metin.Al("kostebek.erken_soru_online"), "baslik"));
                kutu.Add(Yazi(Metin.Al("kostebek.erken_ipucu_online"), "ipucu"));
                kutu.Add(Buton(Metin.Al("kostebek.erken_evet"), () => Yap(akis, perde, hak[0], hataYaz), "buton-birincil"));
            }
            else
            {
                // Telefon A'da: hakkı olan herkes listelenir, suçlayan kendini seçer.
                kutu.Add(Yazi(Metin.Al("kostebek.erken_kim"), "baslik"));
                kutu.Add(Yazi(Metin.Al("kostebek.erken_ipucu"), "ipucu"));
                var cipler = Kutu("cipler");
                foreach (var id in hak)
                {
                    var kim = id;
                    cipler.Add(OyuncuCipi(akis.Isim(kim), akis.Sira(kim), () => Yap(akis, perde, kim, hataYaz)));
                }
                kutu.Add(cipler);
            }
            kutu.Add(Buton(Metin.Al("genel.geri"), () => perde.AddToClassList("gizli"), "buton-ikincil"));
            perde.Add(kutu);
            perde.RemoveFromClassList("gizli");
            // Perde parçaya erken eklendi; sonradan eklenen düğmelerin üstünde dursun.
            perde.BringToFront();
        }

        static void Yap(IOyunAkisi akis, VisualElement perde, OyuncuId kim, System.Action<string> hataYaz)
        {
            perde.AddToClassList("gizli");
            var s = akis.Sucla(kim);
            if (s != Sonuc.Tamam) hataYaz?.Invoke(HataMetni.Al(s));
        }

        /// <summary>
        /// Biten oylamanın duyurusu. Oyun sürüyorsa suçlanan köstebek (casus) değildi: yakalansaydı oyun biterdi
        /// (Casus'ta son şansa geçilirdi, orada bu duyuru gösterilmez).
        /// </summary>
        public static string SonucMetni(IOyunAkisi akis, OyuncuId? suclanan) =>
            suclanan.HasValue ? OylamaMetni(akis.Ayarlar.Oyun, "sonuc_yanlis", akis.Isim(suclanan)) : Metin.Al("kostebek.sonuc_yok");

        /// <summary>Oyun sonunun en üstü: köstebek kimdi, kim kazandı, neden, köstebeğin görevi.</summary>
        public static VisualElement Acilis(IOyunAkisi akis, OyunOzeti ozet)
        {
            var k = ozet.Kostebek;
            bool grup = k.Kazanan == KostebekKazanan.Grup;
            var kart = Kutu("kostebek-acilis", grup ? "kazanan-grup" : "kazanan-kostebek");
            kart.Add(Yazi(Metin.Al("kostebek.acilis_baslik"), "ipucu", "orta"));
            var kim = Kutu("satir", "orta-satir");
            kim.Add(RenkNoktasi(akis.Sira(k.Kostebek)));
            kim.Add(Yazi(akis.Isim(k.Kostebek), "kostebek-isim"));
            kart.Add(kim);
            kart.Add(Yazi(Metin.Al(grup ? "kostebek.kazanan.Grup" : "kostebek.kazanan.Kostebek"), "kostebek-kazanan"));
            kart.Add(Yazi(Metin.Al("kostebek.neden." + k.Neden), "aciklama", "orta"));
            foreach (var gs in ozet.Gorevler)
            {
                if (gs.Kim != k.Kostebek) continue;
                kart.Add(Yazi(Metin.Al("kostebek.gorevi", Tekrar.GorevMetni(akis, gs.Gorev)), "ipucu", "orta"));
                kart.Add(Yazi(Metin.Al(gs.Basarili ? "son.gorev_basarili" : "son.gorev_basarisiz"), "gorev-durum"));
            }
            return kart;
        }

        /// <summary>"Kim?" oylamasında gösterilebilecekler: oyundaki herkes, kendin ve oyundan çıkanlar hariç (sunucu da denetler).</summary>
        public static List<OyuncuId> Adaylar(Gorunum g)
        {
            var l = new List<OyuncuId>();
            foreach (var o in g.Oyuncular)
                if (o.Id != g.Sen && !o.Cikti) l.Add(o.Id);
            return l;
        }
    }

    /// <summary>
    /// Elden ele, "Köstebek kim?" ("Casus kim?") oylamasından önce: telefon ortada, herkes tartışır. Süre istemcinin; kural motorunun saati
    /// durur. Süre dolunca ya da "Oylamaya geç"e basınca telefon herkese sırayla gelir.
    /// </summary>
    sealed class SuclamaTartismaParcasi : IOyunParcasi
    {
        readonly Label sure;

        public VisualElement Kok { get; }

        public SuclamaTartismaParcasi(IOyunAkisi akis, EkranDurumu e)
        {
            var g = e.Gorunum;
            Kok = Kutu("parca", "perde");

            var bas = Kutu("satir");
            bas.Add(Esnek());
            sure = Yazi("", "sure");
            bas.Add(sure);
            Kok.Add(bas);

            Kok.Add(Esnek());
            // Çakmak Gecesi'nde linç de böyle başlar: telefon ortada, önce dünün ve gecenin duyuruları.
            if (g.Asama == Asama.Linc)
            {
                Gece.Duyurular(Kok, akis, g, e.GeceDuyurulari);
                Kok.Add(Yazi(Metin.Al("gece.linc_kim"), "perde-buyuk"));
                Kok.Add(Yazi(Metin.Al("gece.linc_aciklama"), "aciklama", "orta"));
                Kok.Add(Yazi(Metin.Al("kostebek.tartis"), "aciklama", "orta"));
                Kok.Add(Esnek());
                var gecL = Buton(Metin.Al("kostebek.oylamaya_gec"), akis.TartismayiBitir, "buton-birincil", "buton-buyuk");
                GecikmeliAc(gecL);
                Kok.Add(gecL);
                Guncelle(e);
                return;
            }
            if (g.SonSuclama) Kok.Add(Yazi(Metin.Al("kostebek.son_oylama"), "banner"));
            Kok.Add(Yazi(Kostebek.OylamaMetni(g.Oyun, "kim"), "perde-buyuk"));
            Kok.Add(Yazi(e.SuclamaBaslatan.HasValue
                ? Metin.Al("kostebek.erken_yapti", akis.Isim(e.SuclamaBaslatan))
                : g.SonSuclama ? Kostebek.OylamaMetni(g.Oyun, "son_aciklama")
                : Kostebek.OylamaMetni(g.Oyun, "aralik", Kostebek.Aralik(g.Oyun)), "aciklama", "orta"));
            if (g.Supheliler.Count > 0)
            {
                var isimler = new List<string>();
                foreach (var s in g.Supheliler) isimler.Add(akis.Isim(s));
                Kok.Add(Yazi(Metin.Al("kostebek.supheliler", string.Join(", ", isimler)), "ipucu", "orta"));
            }
            Kok.Add(Yazi(Metin.Al("kostebek.tartis"), "aciklama", "orta"));
            Kok.Add(Esnek());

            var gec = Buton(Metin.Al("kostebek.oylamaya_gec"), akis.TartismayiBitir, "buton-birincil", "buton-buyuk");
            GecikmeliAc(gec);
            Kok.Add(gec);
            Guncelle(e);
        }

        public void Guncelle(EkranDurumu e) => SureyiYaz(sure, e.TartismaKalanMs, 10000);
    }

    /// <summary>
    /// "Köstebek kim?" oyu. Elden elede telefon herkese sırayla gelir (perde arkasında), online'da herkes kendi telefonunda.
    /// Kimin kime oy verdiği hiçbir yerde görünmez.
    /// </summary>
    sealed class SuclamaParcasi : IOyunParcasi
    {
        readonly IOyunAkisi akis;
        readonly Label sure;
        readonly Label hata;
        readonly Button ver;
        readonly List<OyuncuId> ciptekiler = new List<OyuncuId>();
        readonly List<Button> cipler = new List<Button>();
        OyuncuId? secili;

        public VisualElement Kok { get; }

        public SuclamaParcasi(IOyunAkisi akis, EkranDurumu e)
        {
            this.akis = akis;
            var g = e.Gorunum;
            Kok = Kutu("parca", "perde");

            var bas = Kutu("satir");
            bas.Add(Yazi(Metin.Al("kostebek.oy_baslik", akis.Isim(g.Sen)), "baslik"));
            bas.Add(Esnek());
            sure = Yazi("", "sure");
            // Elden elede saat durur: süre gösterilmez.
            sure.EnableInClassList("gizli", !akis.Online);
            bas.Add(sure);
            Kok.Add(bas);

            if (g.SonSuclama) Kok.Add(Yazi(Metin.Al("kostebek.son_oylama"), "banner"));
            Kok.Add(Yazi(Kostebek.OylamaMetni(g.Oyun, "kim"), "perde-buyuk"));
            Kok.Add(Yazi(Kostebek.OylamaMetni(g.Oyun, "oy_aciklama"), "aciklama", "orta"));

            var cipKutusu = Kutu("cipler");
            foreach (var id in Kostebek.Adaylar(g))
            {
                var secim = id;
                var cip = OyuncuCipi(Tekrar.IsimEtiketi(akis, g, id), akis.Sira(id), () => { secili = secim; Durum(); });
                ciptekiler.Add(id);
                cipler.Add(cip);
                cipKutusu.Add(cip);
            }
            Kok.Add(cipKutusu);

            Kok.Add(Esnek());
            Kok.Add(Yazi(Metin.Al("oy.gizli"), "ipucu"));
            hata = Yazi("", "hata");
            Kok.Add(hata);
            ver = Buton(Metin.Al("kostebek.oy_ver"), Ver, "buton-birincil", "buton-buyuk");
            Kok.Add(ver);
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
            var s = akis.SuclamaOyu(secili.Value);
            if (s != Sonuc.Tamam) hata.text = HataMetni.Al(s);
            else if (akis.Online) Kok.SetEnabled(false); // ekran sonraki durum'la "oy verildi"ye döner
        }

        public void Guncelle(EkranDurumu e)
        {
            if (akis.Online) SureyiYaz(sure, e.Gorunum.KalanMs, 10000);
        }
    }
}
