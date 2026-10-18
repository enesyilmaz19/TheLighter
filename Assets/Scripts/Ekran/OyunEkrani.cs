using System;
using Cakmak.Kurallar;
using TheLighter.Ag;
using TheLighter.Akis;
using UnityEngine.UIElements;
using static TheLighter.Istemci.Arayuz;

namespace TheLighter.Istemci
{
    /// <summary>Oyun ekranının bir bölümü: perde, soru sorma, mini oyun...</summary>
    public interface IOyunParcasi
    {
        VisualElement Kok { get; }

        /// <summary>Ekran baştan çizilmeden değişen şeyler (kalan süre).</summary>
        void Guncelle(EkranDurumu e);
    }

    /// <summary>
    /// Oyunun tek ekranı (elden ele ve online). Her karede akışı ilerletir. Akışın istediği ekran değişince
    /// (EkranDurumu.Anahtar) ilgili parçayı baştan kurar, değişmediyse sadece süreleri günceller.
    /// Online'da ayrıca: kısa bildirimler (sıra atlandı, AFK...), "Buradayım" düğmesi, "oyuncular bekleniyor" katmanı.
    /// </summary>
    public sealed class OyunEkrani : IEkran, IOnlineEkran, IYanliEkran, IYenidenKurulur
    {
        const float BildirimSn = 3.5f;

        readonly Uygulama uygulama;
        readonly IOyunAkisi akis;
        readonly OnlineAkis onlineAkis;
        readonly OnlineYonetici online;
        readonly Label bildirim;
        readonly VisualElement afkKatmani;
        readonly VisualElement beklemeKatmani;
        readonly Label beklemeYazisi;
        readonly SohbetPaneli sohbet;
        readonly Button sohbetDugmesi;
        readonly OyuncuSutunu oyuncuSutunu;
        /// <summary>Casus: liste, rolüm (online), "Mekânı biliyorum". Diğer oyunlarda null.</summary>
        readonly CasusSeridi casusSeridi;
        /// <summary>Online, Çakmak Gecesi: "Rolüm" (rol kartı ve bütün bulgular).</summary>
        readonly VisualElement rolPerdesi;
        /// <summary>Online, Çakmak Gecesi: gece ölenler (sabah duyurusu için) ve sonraki durum'da duyurulacaklar (roller açılmış olsun).</summary>
        readonly System.Collections.Generic.List<OyuncuId> geceOlenler = new System.Collections.Generic.List<OyuncuId>();
        readonly System.Collections.Generic.List<GeceDuyurusu> bekleyenDuyurular = new System.Collections.Generic.List<GeceDuyurusu>();
        int duyuruSurumu = -1;
        float bildirimKalan;
        float buradayimBekle;
        /// <summary>Online: biten "kim?" oylamasının sonucu, sonraki durum gelince duyurulur (oyun bittiyse açılış gösterir).</summary>
        bool suclamaBitti;
        OyuncuId? suclanan;
        readonly VisualElement ustCubuk;
        readonly Label turYazisi;
        readonly VisualElement skorKutusu;
        readonly Label durumYazisi;
        readonly VisualElement govde;
        readonly VisualElement menu;
        readonly VisualElement onayPerdesi;
        readonly AyarlarPaneli ayarlar;
        IOyunParcasi parca;
        string anahtar;

        public VisualElement Kok { get; }

        public OyunEkrani(Uygulama uygulama, IOyunAkisi akis, OnlineYonetici online = null)
        {
            this.uygulama = uygulama;
            this.akis = akis;
            this.online = online;
            onlineAkis = akis as OnlineAkis;

            Kok = Kutu("ekran", "oyun-ekrani");

            ustCubuk = Kutu("ust-cubuk");
            turYazisi = Yazi("", "ust-cubuk-yazi");
            ustCubuk.Add(turYazisi);
            // Takım Düellosu: iki takımın puanı ve hedef her ekranda.
            skorKutusu = Kutu("takim-ust", "gizli");
            ustCubuk.Add(skorKutusu);
            ustCubuk.Add(Esnek());
            durumYazisi = Yazi("", "ust-cubuk-yazi", "ust-cubuk-durum");
            ustCubuk.Add(durumYazisi);
            if (online != null && OnlineYonetici.SohbetVar)
            {
                sohbet = online.SohbetPaneliKur();
                sohbetDugmesi = Buton(Metin.Al("sohbet.ac"), sohbet.Ac, "buton-kucuk");
                ustCubuk.Add(sohbetDugmesi);
            }
            if (online != null) oyuncuSutunu = new OyuncuSutunu(online.Istemci);
            if (online != null && akis.Ayarlar.Oyun == OyunTuru.CakmakGecesi)
            {
                rolPerdesi = Kutu("menu-perde", "gizli");
                ustCubuk.Add(Buton(Metin.Al("casus.rolum"), RolumuAc, "buton-kucuk"));
            }
            ustCubuk.Add(Buton(Metin.Al("oyun.menu"), MenuyuAc, "buton-kucuk"));
            Kok.Add(ustCubuk);

            if (akis.Ayarlar.Oyun == OyunTuru.Casus)
            {
                casusSeridi = new CasusSeridi(akis);
                Kok.Add(casusSeridi.Cubuk);
            }

            bildirim = Yazi("", "bildirim", "gizli");
            Kok.Add(bildirim);

            govde = Kutu("govde");
            Kok.Add(govde);

            // Online: AFK olduysa büyük "Buradayım". Ekrana dokunmak da yeter.
            afkKatmani = Kutu("menu-perde", "gizli");
            var afkKutu = Kutu("menu-kutu");
            afkKutu.Add(Yazi(Metin.Al("online.afk_baslik"), "baslik"));
            afkKutu.Add(Yazi(Metin.Al("online.afk_aciklama"), "aciklama"));
            afkKutu.Add(Buton(Metin.Al("online.buradayim"), Buradayim, "buton-birincil", "buton-buyuk"));
            afkKatmani.Add(afkKutu);
            Kok.Add(afkKatmani);

            beklemeKatmani = Kutu("menu-perde", "gizli");
            var bekKutu = Kutu("menu-kutu");
            bekKutu.Add(Yazi(Metin.Al("online.bekleme_baslik"), "baslik"));
            beklemeYazisi = Yazi("", "aciklama");
            bekKutu.Add(beklemeYazisi);
            beklemeKatmani.Add(bekKutu);
            Kok.Add(beklemeKatmani);

            ayarlar = new AyarlarPaneli(uygulama);
            menu = MenuKur();
            Kok.Add(menu);
            Kok.Add(ayarlar.Kok);
            onayPerdesi = Kutu("menu-perde", "gizli");
            Kok.Add(onayPerdesi);
            if (sohbet != null) Kok.Add(sohbet.Kok);
            if (casusSeridi != null) Kok.Add(casusSeridi.Perde);
            if (rolPerdesi != null) Kok.Add(rolPerdesi);

            if (onlineAkis != null)
                Kok.RegisterCallback<PointerDownEvent>(_ => { if (onlineAkis.BenAfkyim) Buradayim(); }, TrickleDown.TrickleDown);

            Ciz();
        }

        public void Guncelle(float gecenSn)
        {
            akis.Ilerle(TimeSpan.FromSeconds(gecenSn));
            if (akis.Ekran.Anahtar != anahtar) Ciz();
            else parca.Guncelle(akis.Ekran);

            sohbet?.Guncelle(gecenSn);
            oyuncuSutunu?.Guncelle();
            // Online, Çakmak Gecesi: sabah/linç/suikast duyurusu, olaydan sonraki durum gelince (açılan roller onda).
            if (bekleyenDuyurular.Count > 0 && (online?.Istemci.SonDurum?.Surum ?? -1) != duyuruSurumu)
            {
                var metinler = new System.Collections.Generic.List<string>();
                foreach (var d in bekleyenDuyurular) metinler.Add(Gece.DuyuruMetni(akis, akis.Ekran.Gorunum, d));
                bekleyenDuyurular.Clear();
                Bildir(string.Join("  ", metinler));
            }
            if (suclamaBitti && akis.Ekran.Gorunum != null && akis.Ekran.Gorunum.Asama != Asama.Suclama)
            {
                // Oyun sürüyorsa suçlanan köstebek değildi; bittiyse oyun sonu açılışı söyler.
                // Casus yakalandıysa son şans ekranı kendisi söyler.
                suclamaBitti = false;
                if (!akis.Bitti && akis.Ekran.Gorunum.Asama != Asama.CasusTahmini) Bildir(Kostebek.SonucMetni(akis, suclanan));
            }
            if (bildirimKalan > 0)
            {
                bildirimKalan -= gecenSn;
                if (bildirimKalan <= 0) bildirim.AddToClassList("gizli");
            }
            if (onlineAkis != null)
            {
                if (buradayimBekle > 0) buradayimBekle -= gecenSn;
                afkKatmani.EnableInClassList("gizli", !onlineAkis.BenAfkyim);
                var bekleme = onlineAkis.BeklemeKalanMs;
                beklemeKatmani.EnableInClassList("gizli", !bekleme.HasValue || akis.Bitti);
                if (bekleme.HasValue) beklemeYazisi.text = Metin.Al("online.bekleme_aciklama", DakikaSaniye(bekleme.Value));
            }
        }

        static string DakikaSaniye(int ms)
        {
            int sn = (int)Math.Ceiling(Math.Max(0, ms) / 1000.0);
            return (sn / 60) + ":" + (sn % 60).ToString("00");
        }

        /// <summary>Elden elede menüyü açar (süre durur). Online'da saat sunucuda: bir şey yapmaz.</summary>
        public void ArkaPlanaGecti()
        {
            if (!akis.Online && !akis.Bitti) MenuyuAc();
        }

        void Buradayim()
        {
            // Dokunuşlar birleşir: saniyede bir kezden fazla gitmez (sunucunun mesaj sınırı).
            if (onlineAkis == null || buradayimBekle > 0) return;
            buradayimBekle = 1f;
            onlineAkis.Buradayim();
        }

        void Bildir(string metin)
        {
            if (string.IsNullOrEmpty(metin)) return;
            bildirim.RemoveFromClassList("takim-0");
            bildirim.RemoveFromClassList("takim-1");
            bildirim.text = metin;
            bildirim.RemoveFromClassList("gizli");
            bildirimKalan = BildirimSn;
        }

        public void HataGeldi(HataMesaji h)
        {
            // Eski tur: ekran zaten yeni durumu çiziyor, kullanıcıya söylemeye gerek yok.
            if (h.Kod == "ESKI_TUR") return;
            // Son giden sohbetse hata sohbetindir (Temiz modda UYGUNSUZ, COK_HIZLI...).
            if (sohbet != null && online != null && online.Istemci.SonGidenSohbet)
            {
                sohbet.HataGoster(HataMetni.Mesaj(h));
                return;
            }
            // Soru ekranı kendisi gösterir ve gönder düğmesini yeniden açar (Temiz: tur aynen sürer).
            if (parca is IHataGosteren hg && hg.HataGoster(h)) return;
            Bildir(HataMetni.Mesaj(h));
        }

        public void OlayGeldi(OlayMesaji o)
        {
            string kim = akis.Isim(o.Kim);
            if (o.Atildi)
            {
                Bildir(HataMetni.Atildi(o, kim));
                return;
            }
            switch (o.Olay.Tur)
            {
                case OlayTuru.SuclamaBasladi:
                    Bildir(o.Kim.HasValue ? Metin.Al("kostebek.erken_yapti", kim) : Kostebek.OylamaMetni(akis.Ayarlar.Oyun, "basladi"));
                    break;
                case OlayTuru.SuclamaBitti:
                    suclamaBitti = true;
                    suclanan = o.Kim;
                    break;
                case OlayTuru.TetikBos:
                    if (o.Kim.HasValue) Bildir(RusRuleti.TetikMetni(akis, new TetikSonucu { Kim = o.Kim.Value }));
                    break;
                case OlayTuru.TetikPatladi:
                    if (o.Kim.HasValue) Bildir(RusRuleti.TetikMetni(akis, new TetikSonucu { Kim = o.Kim.Value, Patladi = true }));
                    break;
                case OlayTuru.Elendi:
                    if (o.Kim.HasValue && akis.Ayarlar.Oyun == OyunTuru.RusRuleti)
                        Bildir(RusRuleti.TetikMetni(akis, new TetikSonucu { Kim = o.Kim.Value, Patladi = true, Elendi = true }));
                    // Çakmak Gecesi: gece ölenler birer birer gelir, sabah toplu duyurulur.
                    if (o.Kim.HasValue && akis.Ayarlar.Oyun == OyunTuru.CakmakGecesi) geceOlenler.Add(o.Kim.Value);
                    break;
                case OlayTuru.TetikDevredildi:
                    Bildir(Metin.Al("rus.devretti", kim, akis.Isim(o.Olay.Hedef)));
                    break;
                case OlayTuru.FinalBasladi:
                    Bildir(Metin.Al("rus.final"));
                    break;
                case OlayTuru.CasusYakalandi:
                    Bildir(Metin.Al("casus.yakalandi", kim));
                    break;
                // ---- Çakmak Gecesi ----
                case OlayTuru.LincBasladi:
                    Bildir(Metin.Al("gece.linc_basladi"));
                    break;
                case OlayTuru.LincBitti:
                    Duyur(new GeceDuyurusu
                    {
                        Tur = GeceDuyurusu.Turu.Linc,
                        Olenler = o.Kim.HasValue ? new[] { o.Kim.Value } : new OyuncuId[0],
                    });
                    break;
                case OlayTuru.Suikast:
                    if (o.Kim.HasValue) Duyur(new GeceDuyurusu { Tur = GeceDuyurusu.Turu.Suikast, Olenler = new[] { o.Kim.Value } });
                    break;
                case OlayTuru.GeceBasladi:
                    geceOlenler.Clear();
                    Bildir(Metin.Al("gece.gece_basladi"));
                    break;
                case OlayTuru.GeceBitti:
                    Duyur(new GeceDuyurusu { Tur = GeceDuyurusu.Turu.Sabah, Olenler = new System.Collections.Generic.List<OyuncuId>(geceOlenler) });
                    break;
                case OlayTuru.HakemKarari:
                    Bildir(Metin.Al("gece.hakem_karari"));
                    break;
                case OlayTuru.DuelloSonduruldu:
                    Bildir(Metin.Al("gece.sonduruldu"));
                    break;
                case OlayTuru.DuelloPuani:
                case OlayTuru.IfsaBonusu:
                    // Puan anı: "+1 Mavi". Puanın kendisi sonraki durumdan çizilir.
                    if (o.Olay.Takim.HasValue)
                    {
                        Bildir(Takim.PuanMetni(akis, new PuanAni { IfsaBonusu = o.Olay.Tur == OlayTuru.IfsaBonusu, Kim = o.Kim, Takim = o.Olay.Takim.Value }));
                        bildirim.EnableInClassList("takim-0", o.Olay.Takim == 0);
                        bildirim.EnableInClassList("takim-1", o.Olay.Takim == 1);
                        Takim.Canlandir(bildirim);
                    }
                    break;
                case OlayTuru.SiraAtlandi: Bildir(Metin.Al("online.olay.sira_atlandi", kim, TurkceEk.Ilgi(kim))); break;
                case OlayTuru.Afk: Bildir(Metin.Al("online.olay.afk", kim)); break;
                case OlayTuru.AfkBitti: Bildir(Metin.Al("online.olay.afk_bitti", kim)); break;
                case OlayTuru.OyuncuCikti: Bildir(Metin.Al("online.olay.cikti", kim)); break;
                case OlayTuru.SureDoldu:
                    if (o.Kim.HasValue) Bildir(Metin.Al("online.olay.sure_doldu", kim, TurkceEk.Ilgi(kim)));
                    break;
            }
        }

        /// <summary>Duyuru bir sonraki durum'la gösterilir: açılan roller ve ölüler o durumda.</summary>
        void Duyur(GeceDuyurusu d)
        {
            bekleyenDuyurular.Add(d);
            duyuruSurumu = online?.Istemci.SonDurum?.Surum ?? -1;
        }

        /// <summary>Online, Çakmak Gecesi: kendi rolüm ve bütün bulgularım (kendi telefonum).</summary>
        void RolumuAc()
        {
            var g = akis.Ekran.Gorunum;
            rolPerdesi.Clear();
            var kutu = Kutu("menu-kutu");
            var kaydirma = new ScrollView(ScrollViewMode.Vertical);
            kaydirma.AddToClassList("kaydirma");
            if (g != null && g.Rolun != Rol.Yok) kaydirma.Add(Gece.RolKarti(akis, g));
            if (g != null) kaydirma.Add(Gece.TumBulgular(akis, g));
            kutu.Add(kaydirma);
            kutu.Add(Buton(Metin.Al("genel.tamam"), () => rolPerdesi.AddToClassList("gizli"), "buton-birincil"));
            rolPerdesi.Add(kutu);
            rolPerdesi.RemoveFromClassList("gizli");
            rolPerdesi.BringToFront();
        }

        /// <summary>PC'de geniş pencere: solda oyuncular, sağda sohbet hep açık. Dar ekranda sohbet düğmeyle açılır.</summary>
        public void Yerlesim(bool genis, VisualElement sol, VisualElement sag)
        {
            if (oyuncuSutunu != null && genis) sol.Add(oyuncuSutunu.Kok);
            if (sohbet == null) return;
            sohbet.YanaAl(genis);
            sohbetDugmesi.EnableInClassList("gizli", genis);
            if (genis) sag.Add(sohbet.Kok);
            else Kok.Add(sohbet.Kok);
        }

        void Ciz()
        {
            var e = akis.Ekran;
            anahtar = e.Anahtar;
            govde.Clear();
            parca = ParcaKur(e);
            govde.Add(parca.Kok);
            parca.Guncelle(e);

            casusSeridi?.Guncelle(e);
            // Elden ele mini oyunda ekranın üst yarısı ters döndüğü için üst çubuk gizlenir.
            ustCubuk.EnableInClassList("gizli", (!akis.Online && e.Tur == EkranTuru.MiniOyun) || e.Tur == EkranTuru.OyunSonu);
            var g = e.Gorunum;
            // Curcuna soru toplama ve görev turu ilk turdan önce: tur numarası yok.
            turYazisi.text = g.Tur <= 0 ? ""
                : g.ToplamTur > 0 ? Metin.Al("oyun.tur_sinirli", g.Tur, g.ToplamTur)
                : Metin.Al("oyun.tur_sinirsiz", g.Tur);
            Takim.UstSkoruYaz(skorKutusu, g);
            // Çakmak Gecesi: tur sayısı yok; gün ve linçe kalan tur.
            if (Gece.Mi(g) && g.Gun > 0) turYazisi.text = Gece.GunYazisi(g);

            // Kaos turu ve Kızgın Çakmak'ın ısısı herkese açık: üst çubukta.
            var parcalar = new System.Collections.Generic.List<string>();
            if (g.Kaos != KaosKurali.Yok) parcalar.Add(Metin.Al("oyun.kaos_etiket", Tekrar.KaosAdi(g.Kaos)));
            if (g.Isi.HasValue)
                parcalar.Add(Metin.Al(g.Oyun == OyunTuru.RusRuleti ? "rus.isi_etiket" : "isi.etiket", Tekrar.IsiYazisi(g.Isi.Value)));
            if (g.Final) parcalar.Add(Metin.Al("rus.final_etiket"));
            durumYazisi.text = string.Join("  ·  ", parcalar);
        }

        IOyunParcasi ParcaKur(EkranDurumu e)
        {
            switch (e.Tur)
            {
                case EkranTuru.Perde: return new PerdeParcasi(akis, e);
                case EkranTuru.SoruSorma: return new SoruSormaParcasi(akis, e);
                case EkranTuru.CevapSecme: return new CevapSecmeParcasi(akis, e);
                case EkranTuru.MiniOyunPerdesi: return new MiniOyunPerdeParcasi(akis, e);
                case EkranTuru.MiniOyun:
                    if (akis.Online) return new OnlineMiniOyunParcasi(akis, e);
                    if (e.Gorunum.MiniOyun == MiniOyunTuru.Reaksiyon) return new ReaksiyonParcasi(akis, e);
                    return new MiniOyunParcasi(akis, e);
                case EkranTuru.Ifsa:
                case EkranTuru.Gume: return new SonucParcasi(akis, e);
                case EkranTuru.SoruToplama: return new SoruToplamaParcasi(akis, e);
                case EkranTuru.GorevGoster: return new GorevParcasi(akis, e);
                case EkranTuru.Oylama: return new OylamaParcasi(akis, e);
                case EkranTuru.SuclamaTartisma: return new SuclamaTartismaParcasi(akis, e);
                case EkranTuru.Suclama: return new SuclamaParcasi(akis, e);
                case EkranTuru.Tetik: return new TetikParcasi(akis, e);
                case EkranTuru.CasusTahmini: return new CasusTahminiParcasi(akis, e);
                case EkranTuru.Linc: return new LincParcasi(akis, e);
                case EkranTuru.Gece: return new GeceParcasi(akis, e);
                case EkranTuru.Karar: return new KararParcasi(akis, e);
                case EkranTuru.OyunSonu: return new OyunSonuParcasi(uygulama, akis, e, online);
                default: return akis.Online ? (IOyunParcasi)new OnlineBekleParcasi(akis, e) : new BekleParcasi();
            }
        }

        // ---------- Menü ----------

        /// <summary>
        /// Oyun menüsü. Elden ele: devam, ayarlar, oyunu bitir, lobiye (oyuncu ve ayar ekranına) dön, ana menü, oyundan çık.
        /// Online: devam, ayarlar, oyunu bitir ve lobiye dön (sadece kurucu), odadan ayrıl, oyundan çık.
        /// </summary>
        VisualElement MenuKur()
        {
            var m = Kutu("menu-perde", "gizli");
            var kutu = Kutu("menu-kutu");
            var kaydirma = new ScrollView(ScrollViewMode.Vertical);
            kaydirma.AddToClassList("kaydirma");
            if (!akis.Online)
            {
                kaydirma.Add(Yazi(Metin.Al("oyun.menu_baslik"), "baslik"));
                kaydirma.Add(Buton(Metin.Al("oyun.devam"), MenuyuKapat, "buton-birincil"));
                kaydirma.Add(Buton(Metin.Al("menu.ayarlar"), ayarlar.Ac, "buton-ikincil"));
                kaydirma.Add(Buton(Metin.Al(BitirAnahtari), () => { akis.OyunuBitir(); MenuyuKapat(); }, "buton-ikincil"));
                kaydirma.Add(Buton(Metin.Al("oyun.lobiye_don"), () => Onayla("oyun.lobiye_soru", "oyun.lobiye_ipucu_elden", "oyun.lobiye_evet",
                    uygulama.Kurulum), "buton-ikincil"));
                kaydirma.Add(Buton(Metin.Al("oyun.ana_menu"), uygulama.AnaMenu, "buton-ikincil"));
                kaydirma.Add(Buton(Metin.Al("menu.cikis"), () => Onayla("oyun.cikis_soru", "oyun.cikis_ipucu", "oyun.cikis_evet",
                    uygulama.OyundanCik), "buton-ikincil"));
                kaydirma.Add(Yazi(Metin.Al("oyun.menu_ipucu"), "ipucu"));
            }
            else
            {
                // Online'da oyun durmaz: süre sunucuda akar. Oyunu bitirmek ve lobiye dönmek sadece kurucunun.
                kaydirma.Add(Yazi(Metin.Al("online.menu_baslik", online?.Istemci.Oda ?? ""), "baslik"));
                kaydirma.Add(Buton(Metin.Al("oyun.devam"), MenuyuKapat, "buton-birincil"));
                kaydirma.Add(Buton(Metin.Al("menu.ayarlar"), ayarlar.Ac, "buton-ikincil"));
                var bitir = Buton(Metin.Al(BitirAnahtari), () => { akis.OyunuBitir(); MenuyuKapat(); }, "buton-ikincil");
                bitir.AddToClassList("menu-kurucu");
                kaydirma.Add(bitir);
                var lobi = Buton(Metin.Al("oyun.lobiye_don"), () => Onayla("oyun.lobiye_soru", "oyun.lobiye_ipucu", "oyun.lobiye_evet",
                    LobiyeDon), "buton-ikincil");
                lobi.AddToClassList("menu-kurucu");
                kaydirma.Add(lobi);
                kaydirma.Add(Buton(Metin.Al("online.ayril"), () => online?.Ayril(), "buton-ikincil"));
                kaydirma.Add(Buton(Metin.Al("menu.cikis"), () => Onayla("oyun.cikis_soru", "oyun.cikis_ipucu_online", "oyun.cikis_evet",
                    uygulama.OyundanCik), "buton-ikincil"));
                kaydirma.Add(Yazi(Metin.Al("online.menu_ipucu"), "ipucu"));
            }
            kutu.Add(kaydirma);
            m.Add(kutu);
            return m;
        }

        /// <summary>Köstebek'te "Oyunu bitir" önce son oylamayı açar (kural motoru): düğme bunu söyler.</summary>
        string BitirAnahtari => akis.Ayarlar.Oyun == OyunTuru.Kostebek || akis.Ayarlar.Oyun == OyunTuru.Casus ? "kostebek.bitir" : "oyun.bitir";

        /// <summary>Online, sadece kurucu: oyun biter, oyun sonu beklenmeden herkes lobiye döner.</summary>
        void LobiyeDon()
        {
            MenuyuKapat();
            if (onlineAkis == null) return;
            var s = onlineAkis.LobiyeDon();
            if (s != Sonuc.Tamam) Bildir(HataMetni.Al(s));
        }

        /// <summary>Geri alınamayan seçimden önce sorar.</summary>
        void Onayla(string soru, string ipucu, string evet, Action tamam)
        {
            onayPerdesi.Clear();
            var kutu = Kutu("menu-kutu");
            kutu.Add(Yazi(Metin.Al(soru), "baslik"));
            kutu.Add(Yazi(Metin.Al(ipucu), "ipucu"));
            kutu.Add(Buton(Metin.Al(evet), () => { onayPerdesi.AddToClassList("gizli"); tamam(); }, "buton-birincil"));
            kutu.Add(Buton(Metin.Al("genel.geri"), () => onayPerdesi.AddToClassList("gizli"), "buton-ikincil"));
            onayPerdesi.Add(kutu);
            onayPerdesi.RemoveFromClassList("gizli");
        }

        void MenuyuAc()
        {
            // Kuruculuk oyun içinde el değiştirebilir (kurucu koptuysa): her açılışta bakılır.
            menu.Query(className: "menu-kurucu").ForEach(b => b.EnableInClassList("gizli", !akis.Kurucuyum));
            akis.Duraklatildi = true;
            menu.RemoveFromClassList("gizli");
        }

        /// <summary>Dil değişti: aynı akışla yeni ekran, menü ve ayarlar açık.</summary>
        public IEkran YenidenKur()
        {
            var e = new OyunEkrani(uygulama, akis, online);
            e.MenuyuAc();
            e.ayarlar.Ac();
            return e;
        }

        void MenuyuKapat()
        {
            akis.Duraklatildi = false;
            menu.AddToClassList("gizli");
        }
    }

    sealed class BekleParcasi : IOyunParcasi
    {
        public VisualElement Kok { get; } = Kutu("parca");

        public BekleParcasi()
        {
            Kok.Add(Yazi(Metin.Al("oyun.bekleniyor"), "aciklama"));
        }

        public void Guncelle(EkranDurumu e) { }
    }
}
