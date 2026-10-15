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
    public sealed class OyunEkrani : IEkran, IOnlineEkran, IYanliEkran
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
        float bildirimKalan;
        float buradayimBekle;
        readonly VisualElement ustCubuk;
        readonly Label turYazisi;
        readonly Label durumYazisi;
        readonly VisualElement govde;
        readonly VisualElement menu;
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
            ustCubuk.Add(Buton(Metin.Al("oyun.menu"), MenuyuAc, "buton-kucuk"));
            Kok.Add(ustCubuk);

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

            menu = MenuKur();
            Kok.Add(menu);
            if (sohbet != null) Kok.Add(sohbet.Kok);

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
                case OlayTuru.SiraAtlandi: Bildir(Metin.Al("online.olay.sira_atlandi", kim, TurkceEk.Ilgi(kim))); break;
                case OlayTuru.Afk: Bildir(Metin.Al("online.olay.afk", kim)); break;
                case OlayTuru.AfkBitti: Bildir(Metin.Al("online.olay.afk_bitti", kim)); break;
                case OlayTuru.OyuncuCikti: Bildir(Metin.Al("online.olay.cikti", kim)); break;
                case OlayTuru.SureDoldu:
                    if (o.Kim.HasValue) Bildir(Metin.Al("online.olay.sure_doldu", kim, TurkceEk.Ilgi(kim)));
                    break;
            }
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

            // Elden ele mini oyunda ekranın üst yarısı ters döndüğü için üst çubuk gizlenir.
            ustCubuk.EnableInClassList("gizli", (!akis.Online && e.Tur == EkranTuru.MiniOyun) || e.Tur == EkranTuru.OyunSonu);
            var g = e.Gorunum;
            // Curcuna soru toplama ve görev turu ilk turdan önce: tur numarası yok.
            turYazisi.text = g.Tur <= 0 ? ""
                : g.ToplamTur > 0 ? Metin.Al("oyun.tur_sinirli", g.Tur, g.ToplamTur)
                : Metin.Al("oyun.tur_sinirsiz", g.Tur);

            // Kaos turu ve Kızgın Çakmak'ın ısısı herkese açık: üst çubukta.
            var parcalar = new System.Collections.Generic.List<string>();
            if (g.Kaos != KaosKurali.Yok) parcalar.Add(Metin.Al("oyun.kaos_etiket", Tekrar.KaosAdi(g.Kaos)));
            if (g.Isi.HasValue) parcalar.Add(Metin.Al("isi.etiket", Tekrar.IsiYazisi(g.Isi.Value)));
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
                case EkranTuru.Karar: return new KararParcasi(akis, e);
                case EkranTuru.OyunSonu: return new OyunSonuParcasi(uygulama, akis, e, online);
                default: return akis.Online ? (IOyunParcasi)new OnlineBekleParcasi(akis, e) : new BekleParcasi();
            }
        }

        // ---------- Menü ----------

        VisualElement MenuKur()
        {
            var m = Kutu("menu-perde", "gizli");
            var kutu = Kutu("menu-kutu");
            if (!akis.Online)
            {
                kutu.Add(Yazi(Metin.Al("oyun.menu_baslik"), "baslik"));
                kutu.Add(Buton(Metin.Al("oyun.devam"), MenuyuKapat, "buton-birincil"));
                kutu.Add(Buton(Metin.Al("oyun.bitir"), () => { akis.OyunuBitir(); MenuyuKapat(); }, "buton-ikincil"));
                kutu.Add(Buton(Metin.Al("oyun.ana_menu"), uygulama.AnaMenu, "buton-ikincil"));
                kutu.Add(Yazi(Metin.Al("oyun.menu_ipucu"), "ipucu"));
            }
            else
            {
                // Online'da oyun durmaz: süre sunucuda akar. Oyunu sadece kurucu bitirir.
                kutu.Add(Yazi(Metin.Al("online.menu_baslik", online?.Istemci.Oda ?? ""), "baslik"));
                kutu.Add(Buton(Metin.Al("oyun.devam"), MenuyuKapat, "buton-birincil"));
                var bitir = Buton(Metin.Al("oyun.bitir"), () => { akis.OyunuBitir(); MenuyuKapat(); }, "buton-ikincil");
                bitir.AddToClassList("menu-bitir");
                kutu.Add(bitir);
                kutu.Add(Buton(Metin.Al("online.ayril"), () => online?.Ayril(), "buton-ikincil"));
                kutu.Add(Yazi(Metin.Al("online.menu_ipucu"), "ipucu"));
            }
            m.Add(kutu);
            return m;
        }

        void MenuyuAc()
        {
            // Kuruculuk oyun içinde el değiştirebilir (kurucu koptuysa): her açılışta bakılır.
            menu.Q(className: "menu-bitir")?.EnableInClassList("gizli", !akis.Kurucuyum);
            akis.Duraklatildi = true;
            menu.RemoveFromClassList("gizli");
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
