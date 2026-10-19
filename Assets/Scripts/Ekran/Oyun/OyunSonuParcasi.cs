using System;
using System.Collections.Generic;
using System.Globalization;
using Cakmak.Kurallar;
using TheLighter.Akis;
using UnityEngine.UIElements;
using static TheLighter.Istemci.Arayuz;

namespace TheLighter.Istemci
{
    /// <summary>
    /// Oyun sonu (Ana Plan 8.5, 8.6): unvanlar, açılan gizli görevler, ifşa olan sorular, sayaçlar,
    /// grup kaydı ve paylaşım kartı. Hepsi kural motorunun özetinden (<see cref="OyunOzeti"/>);
    /// güme giden soru özette olmadığı için ne ekrana ne cihaza yazılır.
    /// </summary>
    sealed class OyunSonuParcasi : IOyunParcasi
    {
        /// <summary>Aynı oyun grup kaydına iki kez işlenmesin (ekran yeniden kurulsa bile).</summary>
        static IOyunAkisi sonIslenen;
        static GrupDosyasi sonGrup;

        readonly Uygulama uygulama;
        readonly IOyunAkisi akis;
        readonly OyunOzeti ozet;
        readonly VisualElement grupKutusu;
        readonly VisualElement kartPerdesi;
        GrupDosyasi grup;

        public VisualElement Kok { get; }

        /// <param name="online">Online'da: "Tekrar oyna" sadece kurucuda (oda lobiye döner), "Ana menü" odadan ayrılır.</param>
        public OyunSonuParcasi(Uygulama uygulama, IOyunAkisi akis, EkranDurumu e, OnlineYonetici online = null)
        {
            this.uygulama = uygulama;
            this.akis = akis;
            ozet = e.Ozet;
            var g = e.Gorunum;

            Kok = Kutu("parca", "oyun-sonu");
            Kok.Add(Yazi(Metin.Al("son.baslik"), "logo"));
            Kok.Add(Yazi(Metin.Al("son.tur", g.Tur), "aciklama", "orta"));

            var kaydirma = new ScrollView(ScrollViewMode.Vertical);
            kaydirma.AddToClassList("kaydirma");

            // Köstebek: önce köstebek açılışı (kim, kim kazandı, neden).
            if (ozet != null && ozet.Kostebek != null) kaydirma.Add(Kostebek.Acilis(akis, ozet));
            // Rus Ruleti: kazanan (son kalan) ve elenme sırası.
            if (ozet != null && ozet.RusRuleti != null) kaydirma.Add(RusRuleti.Acilis(akis, ozet.RusRuleti));
            // Takım Düellosu: kazanan takım (ya da berabere), puanlar, üyeler.
            if (ozet != null && ozet.Takim != null)
                kaydirma.Add(Takim.Acilis(akis, ozet.Takim, ozet.Oyuncular.Count > 0 ? ozet.Oyuncular : g.Oyuncular));

            grupKutusu = Kutu("grup-kutusu");
            kaydirma.Add(grupKutusu);
            GrupuIsle();

            // Unvanlar: en uç 3 (kural motoru seçer).
            kaydirma.Add(Yazi(Metin.Al("son.unvanlar"), "alt-baslik"));
            if (ozet == null || ozet.Unvanlar.Count == 0) kaydirma.Add(Yazi(Metin.Al("son.unvan_yok"), "aciklama"));
            else foreach (var u in ozet.Unvanlar) kaydirma.Add(UnvanKarti(akis, u));

            // Gizli görevler oyun bitince herkese açılır.
            if (ozet != null && ozet.Gorevler.Count > 0)
            {
                kaydirma.Add(Yazi(Metin.Al("son.gorevler"), "alt-baslik"));
                foreach (var gs in ozet.Gorevler)
                {
                    var kart = Kutu("son-kart", gs.Basarili ? "gorev-basarili" : "gorev-basarisiz");
                    var ust = Kutu("satir");
                    ust.Add(RenkNoktasi(akis.Sira(gs.Kim)));
                    ust.Add(Yazi(akis.Isim(gs.Kim), "son-soru"));
                    ust.Add(Esnek());
                    ust.Add(Yazi(Metin.Al(gs.Basarili ? "son.gorev_basarili" : "son.gorev_basarisiz"), "gorev-durum"));
                    kart.Add(ust);
                    kart.Add(Yazi(Tekrar.GorevMetni(akis, gs.Gorev), "ipucu"));
                    kaydirma.Add(kart);
                }
            }

            kaydirma.Add(Yazi(Metin.Al("son.ifsalar"), "alt-baslik"));
            if (e.IfsaOlanlar == null || e.IfsaOlanlar.Count == 0) kaydirma.Add(Yazi(Metin.Al("son.ifsa_yok"), "aciklama"));
            else
            {
                foreach (var k in e.IfsaOlanlar)
                {
                    var kart = Kutu("son-kart");
                    kart.Add(Yazi(k.Soru != null ? Metin.Al("sonuc.ifsa_soru", k.Soru) : Metin.Al("son.fisilti"), "son-soru"));
                    kart.Add(Yazi(Metin.Al("sonuc.soran_cevap", akis.Isim(k.Soran), akis.Isim(k.Cevap)), "ipucu"));
                    kaydirma.Add(kart);
                }
            }

            kaydirma.Add(Yazi(Metin.Al("son.sayaclar"), "alt-baslik"));
            foreach (var o in g.Oyuncular)
            {
                var satir = Kutu("satir", "son-satir");
                satir.Add(RenkNoktasi(akis.Sira(o.Id)));
                satir.Add(Yazi(Metin.Al("son.sayac", o.Isim, o.Gosterilme, o.IfsaEttirme, o.Saklama), "aciklama"));
                kaydirma.Add(satir);
            }
            Kok.Add(kaydirma);

            Kok.Add(Buton(Metin.Al("son.paylas"), KartiAc, "buton-ikincil"));
            if (!akis.Online)
            {
                Kok.Add(Buton(Metin.Al("son.tekrar"), () => uygulama.TekrarOyna(), "buton-birincil", "buton-buyuk"));
                Kok.Add(Buton(Metin.Al("oyun.ana_menu"), uygulama.AnaMenu, "buton-ikincil"));
            }
            else
            {
                var oa = akis as TheLighter.Ag.OnlineAkis;
                if (akis.Kurucuyum)
                    Kok.Add(Buton(Metin.Al("son.tekrar"), () => oa?.TekrarOyna(), "buton-birincil", "buton-buyuk"));
                else
                    Kok.Add(Yazi(Metin.Al("online.son.tekrar_bekle"), "ipucu", "orta"));
                Kok.Add(Buton(Metin.Al("online.ayril"), () => online?.Ayril(), "buton-ikincil"));
            }

            kartPerdesi = Kutu("menu-perde", "kart-perdesi", "gizli");
            Kok.Add(kartPerdesi);
        }

        public void Guncelle(EkranDurumu e) { }

        static VisualElement UnvanKarti(IOyunAkisi akis, Unvan u)
        {
            var kart = Kutu("unvan-karti");
            kart.Add(Yazi(Metin.Al("unvan.ad." + u.Tur), "unvan-ad"));
            var kim = Kutu("satir");
            kim.Add(RenkNoktasi(akis.Sira(u.Kim)));
            kim.Add(Yazi(akis.Isim(u.Kim), "unvan-kim"));
            kart.Add(kim);
            kart.Add(Yazi(Metin.Al("unvan.aciklama." + u.Tur, u.Deger), "ipucu"));
            return kart;
        }

        // ---------- Grup kaydı ----------

        List<string> Isimler()
        {
            var l = new List<string>();
            foreach (var o in akis.Oyuncular) l.Add(o.Isim);
            return l;
        }

        /// <summary>Bugünkü oyuncular kayıtlı bir grupsa oyun ona kendiliğinden işlenir; değilse "Bu grubu kaydet".</summary>
        void GrupuIsle()
        {
            grupKutusu.Clear();
            if (ozet == null) return;

            if (sonIslenen == akis)
            {
                grup = sonGrup;
                if (grup != null) GrupYazisi();
                else KaydetFormu();
                return;
            }

            var eslesen = GrupDeposu.Eslesen(Isimler());
            if (eslesen != null && GrupDeposu.Isle(eslesen, ozet))
            {
                sonIslenen = akis;
                sonGrup = eslesen;
                grup = eslesen;
                GrupYazisi();
                return;
            }
            KaydetFormu();
        }

        void GrupYazisi()
        {
            grupKutusu.Clear();
            grupKutusu.Add(Yazi(Metin.Al("son.grup_kaydedildi", grup.Kayit.Ad, grup.Kayit.OyunSayisi), "banner"));
        }

        void KaydetFormu()
        {
            grupKutusu.Clear();
            grupKutusu.Add(Yazi(Metin.Al("son.grup_kaydet"), "alt-baslik"));
            var ad = new TextField { maxLength = 32 };
            ad.AddToClassList("isim-kutusu");
            grupKutusu.Add(Yazi(Metin.Al("son.grup_ad"), "ipucu"));
            grupKutusu.Add(ad);
            var hata = Yazi("", "hata");
            grupKutusu.Add(hata);
            grupKutusu.Add(Buton(Metin.Al("son.grup_kaydet"), () =>
            {
                string isim = (ad.value ?? "").Trim();
                if (isim.Length == 0)
                {
                    hata.text = Metin.Al("son.grup_ad_bos");
                    return;
                }
                var yeni = GrupDeposu.Yeni(isim);
                if (!GrupDeposu.Isle(yeni, ozet))
                {
                    hata.text = Metin.Al("son.grup_hata");
                    return;
                }
                sonIslenen = akis;
                sonGrup = yeni;
                grup = yeni;
                GrupYazisi();
            }, "buton-ikincil"));
        }

        // ---------- Paylaşım kartı ----------

        void KartiAc()
        {
            kartPerdesi.Clear();
            kartPerdesi.Add(PaylasimKarti());

            var butonlar = Kutu("satir", "kart-butonlari");
            var durum = Yazi("", "ipucu");
            butonlar.Add(Buton(Metin.Al("genel.geri"), () => kartPerdesi.AddToClassList("gizli"), "buton-ikincil"));
            butonlar.Add(Esnek());
            butonlar.Add(Buton(Metin.Al("kart.kaydet"), () =>
            {
                durum.text = "";
                uygulama.EkranGoruntusuKaydet(butonlar, yol =>
                    durum.text = yol != null ? Metin.Al("kart.kaydedildi", yol) : Metin.Al("kart.kaydedilemedi"));
            }, "buton-birincil"));
            kartPerdesi.Add(butonlar);
            kartPerdesi.Add(durum);
            kartPerdesi.RemoveFromClassList("gizli");
        }

        /// <summary>
        /// Dikey kart: grup adı, tarih, 3 unvan, görevini yapanlar, oyunun adı (Ana Plan 8.6).
        /// İfşa olan sorular karta konmaz: grubun içinde komik olan, dışarıda birini kırabilir.
        /// </summary>
        VisualElement PaylasimKarti()
        {
            var kart = Kutu("paylasim-karti");
            kart.Add(Yazi(Metin.Al("menu.baslik"), "logo"));
            string baslik = grup != null && !string.IsNullOrEmpty(grup.Kayit.Ad) ? grup.Kayit.Ad : "";
            if (baslik.Length > 0) kart.Add(Yazi(baslik, "kart-grup"));
            kart.Add(Yazi(DateTime.Now.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture), "ipucu", "orta"));

            if (ozet != null && ozet.Unvanlar.Count > 0)
                foreach (var u in ozet.Unvanlar) kart.Add(UnvanKarti(akis, u));
            else kart.Add(Yazi(Metin.Al("son.unvan_yok"), "aciklama", "orta"));

            if (ozet != null && ozet.Gorevler.Count > 0)
            {
                kart.Add(Yazi(Metin.Al("kart.gorevi_yapanlar"), "alt-baslik"));
                var yapanlar = new List<string>();
                foreach (var gs in ozet.Gorevler)
                    if (gs.Basarili) yapanlar.Add(akis.Isim(gs.Kim));
                kart.Add(Yazi(yapanlar.Count > 0 ? string.Join(", ", yapanlar) : Metin.Al("kart.kimse"), "aciklama", "orta"));
            }

            kart.Add(Esnek());
            kart.Add(Yazi(Metin.Al("kart.alt"), "ipucu", "orta"));
            return kart;
        }
    }
}
