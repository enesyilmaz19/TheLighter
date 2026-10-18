using System;
using System.Collections.Generic;
using Cakmak.Kurallar;
using TheLighter.Akis;
using UnityEngine.UIElements;
using static TheLighter.Istemci.Arayuz;

namespace TheLighter.Istemci
{
    /// <summary>
    /// Casus ekranlarının ortak parçaları (docs/Oyunlar.md 4, tasks/g2-casus.md): rol kartı, liste, "Mekânı biliyorum",
    /// oyun sonundaki açılış. Kural hesaplanmaz: kim casus, sır, liste ve kategori kural motorunun görünümünden.
    /// </summary>
    static class Casus
    {
        static bool Kelime(Gorunum g) => g != null && g.CasusKategori != null;

        /// <summary>
        /// Rol kartı. Casusa "Casussun!" (kelimede kategori), diğerlerine mekân ve rolü (kelimede kelime ve kategori).
        /// İkisi aynı biçimde ve aynı satır sayısında: kartın boyundan kimse anlamasın.
        /// </summary>
        public static VisualElement RolKarti(Gorunum g)
        {
            bool casus = g.Rolun == Rol.Casus;
            bool kelime = Kelime(g);
            var kart = Kutu("rol-karti", casus ? "rol-kostebek" : "rol-masum");
            if (casus)
            {
                kart.Add(Yazi(Metin.Al("casus.rol_sen"), "rol-ad"));
                kart.Add(Yazi(kelime ? Metin.Al("casus.kategori", g.CasusKategori) : Metin.Al("casus.mekani_bilmiyorsun"), "aciklama", "orta"));
                kart.Add(Yazi(Metin.Al(kelime ? "casus.rol_sen_aciklama_kelime" : "casus.rol_sen_aciklama"), "ipucu", "orta"));
            }
            else
            {
                kart.Add(Yazi(g.Sirrin ?? "", "rol-ad"));
                kart.Add(Yazi(kelime ? Metin.Al("casus.kategori", g.CasusKategori) : Metin.Al("casus.rolun", g.MekanRolun ?? ""), "aciklama", "orta"));
                kart.Add(Yazi(Metin.Al(kelime ? "casus.rol_degil_aciklama_kelime" : "casus.rol_degil_aciklama"), "ipucu", "orta"));
            }
            return kart;
        }

        /// <summary>
        /// Kendi gizli ekranında (elden elede telefon onda) dokununca açılan rol kartı. Casus oyunu değilse ya da görünümde
        /// kişiye özel bir şey yoksa null.
        /// </summary>
        public static VisualElement RolumDugmesi(Gorunum g)
        {
            if (g == null || g.Oyun != OyunTuru.Casus || (g.Rolun != Rol.Casus && g.Sirrin == null)) return null;
            var kap = Kutu("casus-rolum");
            var kart = RolKarti(g);
            kart.AddToClassList("gizli");
            Button dugme = null;
            dugme = Buton(Metin.Al("casus.rolume_bak"), () =>
            {
                bool acik = kart.ClassListContains("gizli");
                kart.EnableInClassList("gizli", !acik);
                dugme.text = Metin.Al(acik ? "casus.rolu_gizle" : "casus.rolume_bak");
            }, "buton-kucuk");
            kap.Add(dugme);
            kap.Add(kart);
            return kap;
        }

        /// <summary>Listenin başlığı: kelimede kategori, mekânda "Mekânlar".</summary>
        public static string ListeBasligi(Gorunum g) =>
            Kelime(g) ? Metin.Al("casus.liste_kategori", g.CasusKategori) : Metin.Al("casus.liste_mekanlar");

        /// <summary>Oyun sonunun en üstü: casus kimdi, kim kazandı, neden, sır neydi, tahmini neydi.</summary>
        public static VisualElement Acilis(IOyunAkisi akis, CasusSonucu c)
        {
            bool grup = c.Kazanan == CasusKazanan.Grup;
            var kart = Kutu("kostebek-acilis", grup ? "kazanan-grup" : "kazanan-kostebek");
            kart.Add(Yazi(Metin.Al("casus.acilis_baslik"), "ipucu", "orta"));
            var kim = Kutu("satir", "orta-satir");
            kim.Add(RenkNoktasi(akis.Sira(c.Casus)));
            kim.Add(Yazi(akis.Isim(c.Casus), "kostebek-isim"));
            kart.Add(kim);
            kart.Add(Yazi(Metin.Al(grup ? "casus.kazanan.Grup" : "casus.kazanan.Casus"), "kostebek-kazanan"));
            kart.Add(Yazi(Metin.Al("casus.neden." + c.Neden), "aciklama", "orta"));
            bool kelime = akis.Ayarlar.Casus == CasusTuru.Kelime;
            if (c.Sir != null) kart.Add(Yazi(Metin.Al(kelime ? "casus.kelime_neydi" : "casus.mekan_neydi", c.Sir), "aciklama", "orta"));
            if (c.Tahmin != null) kart.Add(Yazi(Metin.Al("casus.tahmini", c.Tahmin), "ipucu", "orta"));
            return kart;
        }

        /// <summary>
        /// Listeden seçim ızgarası. <paramref name="sec"/> null ise sadece okunur. <paramref name="isaretler"/> doluysa
        /// (online) dokununca "bu olamaz" çizgisi: sadece bu cihazda, motora gitmez.
        /// </summary>
        public static VisualElement Izgara(IReadOnlyList<string> liste, Action<string> sec, HashSet<string> isaretler)
        {
            var kutu = Kutu("secenekler", "casus-liste");
            foreach (var oge in liste)
            {
                var o = oge;
                if (sec != null)
                {
                    kutu.Add(Buton(o, () => sec(o), "secenek", "casus-oge"));
                    continue;
                }
                if (isaretler != null)
                {
                    Button b = null;
                    b = Buton(o, () =>
                    {
                        if (!isaretler.Remove(o)) isaretler.Add(o);
                        b.EnableInClassList("casus-isaretli", isaretler.Contains(o));
                    }, "secenek", "casus-oge");
                    b.EnableInClassList("casus-isaretli", isaretler.Contains(o));
                    kutu.Add(b);
                    continue;
                }
                kutu.Add(Yazi(o, "secenek", "casus-oge", "casus-salt"));
            }
            return kutu;
        }

        /// <summary>
        /// Tahminden önce son soru: "Uçak mı? Emin misin? Yanlışsa kaybedersin." Herkese aynı (casus olmayana da):
        /// yanındaki ekrana bakarak anlamasın.
        /// </summary>
        public static void Onayla(VisualElement perde, Gorunum g, string tahmin, Action evet, Action geri)
        {
            perde.Clear();
            var kutu = Kutu("menu-kutu");
            kutu.Add(Yazi(Metin.Al("casus.emin_misin", tahmin), "baslik"));
            kutu.Add(Yazi(Metin.Al(Kelime(g) ? "casus.emin_ipucu_kelime" : "casus.emin_ipucu"), "ipucu"));
            kutu.Add(Buton(Metin.Al("casus.evet_tahmin"), evet, "buton-birincil"));
            kutu.Add(Buton(Metin.Al("genel.geri"), geri, "buton-ikincil"));
            perde.Add(kutu);
            perde.RemoveFromClassList("gizli");
            perde.BringToFront();
        }
    }

    /// <summary>
    /// Casus'ta oyun ekranının üstünde bir şerit: "Liste" (herkeste aynı), online'da "Rolüm", ve herkeste "Mekânı biliyorum".
    /// Casus olmayan da basar, listeden seçer, onaylar; sonra hiçbir şey olmaz (kural motoru ya da akış sessizce yutar).
    /// Elden elede telefonu kimin tuttuğu belli değilse (perde, ortadaki ekranlar) önce "Kim?" sorulur.
    /// </summary>
    sealed class CasusSeridi
    {
        /// <summary>Online işaretleri: oyun boyunca bu cihazda (aynı akış), yeni oyunda sıfırdan.</summary>
        static IOyunAkisi isaretAkisi;
        static readonly HashSet<string> isaretler = new HashSet<string>();

        readonly IOyunAkisi akis;
        readonly Button biliyorum;

        public VisualElement Cubuk { get; }
        public VisualElement Perde { get; }

        public CasusSeridi(IOyunAkisi akis)
        {
            this.akis = akis;
            if (isaretAkisi != akis)
            {
                isaretAkisi = akis;
                isaretler.Clear();
            }
            Cubuk = Kutu("satir", "casus-seridi");
            Cubuk.Add(Buton(Metin.Al("casus.liste"), ListeyiAc, "buton-kucuk"));
            if (akis.Online) Cubuk.Add(Buton(Metin.Al("casus.rolum"), RolumuAc, "buton-kucuk"));
            Cubuk.Add(Esnek());
            biliyorum = Buton(Metin.Al(akis.Ayarlar.Casus == CasusTuru.Kelime ? "casus.biliyorum_kelime" : "casus.biliyorum"),
                Biliyorum, "buton-kucuk", "buton-casus");
            Cubuk.Add(biliyorum);
            Perde = Kutu("menu-perde", "gizli");
        }

        /// <summary>Şerit oyun sürerken görünür; oyun sonunda, son şans ekranında (kendi listesi var) ve elden elede ters dönen mini oyunda gizli.</summary>
        public void Guncelle(EkranDurumu e)
        {
            bool goster = e.Gorunum != null && e.Gorunum.Oyun == OyunTuru.Casus && e.Tur != EkranTuru.OyunSonu
                && e.Tur != EkranTuru.CasusTahmini && !(!akis.Online && e.Tur == EkranTuru.MiniOyun);
            Cubuk.EnableInClassList("gizli", !goster);
            if (!goster) Kapat();
        }

        void Kapat() => Perde.AddToClassList("gizli");

        Gorunum G => akis.Ekran.Gorunum;

        void Ac(VisualElement kutu)
        {
            Perde.Clear();
            Perde.Add(kutu);
            Perde.RemoveFromClassList("gizli");
            Perde.BringToFront();
        }

        void ListeyiAc()
        {
            var kutu = Kutu("menu-kutu");
            kutu.Add(Yazi(Casus.ListeBasligi(G), "baslik"));
            if (akis.Online) kutu.Add(Yazi(Metin.Al("casus.isaret_ipucu"), "ipucu"));
            var kaydirma = new ScrollView(ScrollViewMode.Vertical);
            kaydirma.AddToClassList("kaydirma");
            // Elden elede telefon herkesin: işaret, koyanın düşüncesini ele verir; sadece online'da.
            kaydirma.Add(Casus.Izgara(G.CasusListesi, null, akis.Online ? isaretler : null));
            kutu.Add(kaydirma);
            kutu.Add(Buton(Metin.Al("genel.tamam"), Kapat, "buton-birincil"));
            Ac(kutu);
        }

        void RolumuAc()
        {
            var kutu = Kutu("menu-kutu");
            kutu.Add(Casus.RolKarti(G));
            kutu.Add(Buton(Metin.Al("genel.tamam"), Kapat, "buton-birincil"));
            Ac(kutu);
        }

        /// <summary>Elden elede telefon kimdeyse o (kendi gizli ekranında). Ortadaki ekranlarda ve perdede belli değil: null.</summary>
        OyuncuId? Tutan()
        {
            if (akis.Online) return akis.Ben;
            var e = akis.Ekran;
            switch (e.Tur)
            {
                case EkranTuru.SoruSorma:
                case EkranTuru.CevapSecme:
                case EkranTuru.Suclama:
                case EkranTuru.GorevGoster:
                case EkranTuru.Oylama:
                case EkranTuru.SoruToplama:
                    return e.Kime;
                default:
                    return null;
            }
        }

        void Biliyorum()
        {
            var kim = Tutan();
            if (kim.HasValue) Liste(kim.Value);
            else KimSor();
        }

        void KimSor()
        {
            var kutu = Kutu("menu-kutu");
            kutu.Add(Yazi(Metin.Al("casus.kim_basti"), "baslik"));
            kutu.Add(Yazi(Metin.Al("casus.kim_basti_ipucu"), "ipucu"));
            var cipler = Kutu("cipler");
            foreach (var o in akis.Ekran.Gorunum.Oyuncular)
            {
                if (o.Cikti) continue;
                var id = o.Id;
                cipler.Add(OyuncuCipi(akis.Isim(id), akis.Sira(id), () => Liste(id)));
            }
            kutu.Add(cipler);
            kutu.Add(Buton(Metin.Al("genel.geri"), Kapat, "buton-ikincil"));
            Ac(kutu);
        }

        void Liste(OyuncuId kim)
        {
            var kutu = Kutu("menu-kutu");
            kutu.Add(Yazi(Metin.Al(akis.Ayarlar.Casus == CasusTuru.Kelime ? "casus.hangisi_kelime" : "casus.hangisi"), "baslik"));
            kutu.Add(Yazi(Casus.ListeBasligi(G), "ipucu"));
            var kaydirma = new ScrollView(ScrollViewMode.Vertical);
            kaydirma.AddToClassList("kaydirma");
            kaydirma.Add(Casus.Izgara(G.CasusListesi, t => Casus.Onayla(Perde, G, t, () => Tahmin(kim, t), () => Liste(kim)), null));
            kutu.Add(kaydirma);
            kutu.Add(Buton(Metin.Al("genel.geri"), Kapat, "buton-ikincil"));
            Ac(kutu);
        }

        void Tahmin(OyuncuId kim, string tahmin)
        {
            Kapat();
            // Casus değilse SiraSendeDegil (elden ele) ya da hiç gönderilmez (online): bilerek bir şey gösterilmez.
            akis.CasusTahmini(kim, tahmin);
        }
    }

    /// <summary>
    /// Casus yakalandı: son şans. Herkes izler (casus açıldı), listeden sadece casus seçer, onaylar. 30 sn; süre dolarsa
    /// grup kazanır (kural motoru).
    /// </summary>
    sealed class CasusTahminiParcasi : IOyunParcasi
    {
        readonly Label sure;
        readonly VisualElement perde;

        public VisualElement Kok { get; }

        public CasusTahminiParcasi(IOyunAkisi akis, EkranDurumu e)
        {
            var g = e.Gorunum;
            var casus = e.Kime ?? g.Yakalanan;
            bool benim = casus.HasValue && (!akis.Online || akis.Ben == casus);
            string isim = akis.Isim(casus);
            bool kelime = g.CasusKategori != null;

            Kok = Kutu("parca", "perde", "casus-son-sans");
            var bas = Kutu("satir");
            bas.Add(Esnek());
            sure = Yazi("", "sure");
            bas.Add(sure);
            Kok.Add(bas);

            Kok.Add(Yazi(Metin.Al("casus.yakalandi", isim), "banner", "tetik-bam"));
            if (casus.HasValue) Kok.Add(RenkNoktasi(akis.Sira(casus.Value), "perde-renk"));
            Kok.Add(Yazi(Metin.Al(kelime ? "casus.son_sans_kelime" : "casus.son_sans", isim), "aciklama", "orta"));
            Kok.Add(Yazi(Casus.ListeBasligi(g), "alt-baslik"));

            var kaydirma = new ScrollView(ScrollViewMode.Vertical);
            kaydirma.AddToClassList("kaydirma");
            perde = Kutu("menu-perde", "gizli");
            if (benim)
            {
                var kim = casus.Value;
                kaydirma.Add(Casus.Izgara(g.CasusListesi, t => Casus.Onayla(perde, g, t, () =>
                {
                    perde.AddToClassList("gizli");
                    var s = akis.CasusTahmini(kim, t);
                    if (s == Sonuc.Tamam && akis.Online) Kok.SetEnabled(false);
                }, () => perde.AddToClassList("gizli")), null));
            }
            else
            {
                kaydirma.Add(Casus.Izgara(g.CasusListesi, null, null));
            }
            Kok.Add(kaydirma);
            if (!benim) Kok.Add(Yazi(Metin.Al("casus.son_sans_bekle", isim), "ipucu", "orta"));
            Kok.Add(perde);
            Guncelle(e);
        }

        public void Guncelle(EkranDurumu e) => SureyiYaz(sure, e.Gorunum.KalanMs, 10000);
    }
}
