using System;
using System.Collections.Generic;
using Cakmak.Kurallar;
using TheLighter.Ag;
using TheLighter.Akis;
using UnityEngine;
using UnityEngine.UIElements;
using static TheLighter.Istemci.Arayuz;

namespace TheLighter.Istemci
{
    /// <summary>
    /// Online lobi: oda kodu, oyuncular (renk, bağlı mı, kurucu), ayarlar. Ayarları sadece kurucu değiştirir;
    /// her değişiklikte sadece o alan gönderilir, ekran sunucudan gelen durumu çizer (tahmin yok).
    /// Kurucu olmayan ayarların özetini görür. "Başlat" en az 4 kişiyle. Reaksiyon online'da yok.
    /// Grubu tanınan kurucu "Bizim paket"i seçebilir: önce sunucuya parça parça yüklenir (Temiz modda yok).
    /// PC'de geniş pencerede oyuncular solda, sohbet sağda (Ana Plan 6.2).
    /// Takım Düellosu'nda takımlar herkese görünür; kurucu dokunarak dağıtır, "Rastgele" dağılımı siler.
    /// </summary>
    public sealed class LobiEkrani : IEkran, IOnlineEkran, IYanliEkran, IYenidenKurulur
    {
        static readonly OyunModu[] ModSecenekleri = { OyunModu.Normal, OyunModu.Temiz, OyunModu.Curcuna };
        static readonly MiniOyunTuru[] MiniSecenekleri = { MiniOyunTuru.Tkm, MiniOyunTuru.TekCift, MiniOyunTuru.Zar, MiniOyunTuru.Karisik };
        static readonly int[] TurSecenekleri = { 10, 15, 20, 0 };
        static readonly int[] SoruSnSecenekleri = { 30, 60, 90 };
        static readonly int[] CevapSnSecenekleri = { 10, 20, 30 };
        static readonly int[] HamleSnSecenekleri = { 5, 10, 15 };
        static readonly CezaSeviyesi[] CezaSecenekleri = { CezaSeviyesi.Kapali, CezaSeviyesi.Hafif, CezaSeviyesi.Cesur };

        readonly Uygulama uygulama;
        readonly OnlineYonetici online;
        readonly Label kodYazisi;
        readonly VisualElement oyuncuListesi;
        readonly VisualElement ayarKutusu;
        readonly Label ozet;
        readonly Label oyunTarifi;
        readonly TakimDagitmaParcasi takimParcasi;
        readonly Label hata;
        readonly Button basla;
        readonly Label baslaIpucu;
        readonly VisualElement atPerdesi;
        readonly List<Action<OdaAyarlari>> yenileyiciler = new List<Action<OdaAyarlari>>();
        readonly List<KeyValuePair<string, Button>> paketButonlari = new List<KeyValuePair<string, Button>>();
        readonly SohbetPaneli sohbet;
        readonly Button sohbetDugmesi;
        readonly AyarlarPaneli ayarlar;
        readonly VisualElement oyuncuBolumu;
        readonly VisualElement oyuncuYeri;
        Button bizimButon;      // AyarlariKur kurar
        Label bizimDurum;
        GrupDosyasi grup;
        string grupIsimleri;
        GrupPaketiYukleyici yukleyici;
        /// <summary>Görülen herkesin ismi: atılan listeden düştükten sonra "atıldı" olayı gelince ismi lazım.</summary>
        readonly Dictionary<OyuncuId, string> gorulenIsimler = new Dictionary<OyuncuId, string>();
        int sonSurum = -1;

        public VisualElement Kok { get; }

        OnlineIstemci Istemci => online.Istemci;
        DurumMesaji D => Istemci.SonDurum;
        bool Kurucu => Istemci.Kurucuyum;

        public LobiEkrani(Uygulama uygulama, OnlineYonetici online)
        {
            this.uygulama = uygulama;
            this.online = online;
            Kok = Kutu("ekran", "kurulum", "lobi");

            var ust = Kutu("satir");
            ust.Add(Buton(Metin.Al("lobi.ayril"), online.Ayril, "buton-kucuk"));
            ust.Add(Esnek());
            ayarlar = new AyarlarPaneli(uygulama);
            ust.Add(Buton(Metin.Al("menu.ayarlar"), ayarlar.Ac, "buton-kucuk"));
            if (OnlineYonetici.SohbetVar)
            {
                sohbet = online.SohbetPaneliKur();
                sohbetDugmesi = Buton(Metin.Al("sohbet.ac"), sohbet.Ac, "buton-kucuk");
                ust.Add(sohbetDugmesi);
            }
            Kok.Add(ust);

            var kaydirma = new ScrollView(ScrollViewMode.Vertical);
            kaydirma.AddToClassList("kaydirma");
            Kok.Add(kaydirma);

            // Oda kodu büyük: arkadaşlar bununla katılır.
            var kodKutusu = Kutu("hafiza-karti", "oda-kodu-karti");
            kodKutusu.Add(Yazi(Metin.Al("lobi.kod_baslik"), "ipucu", "orta"));
            kodYazisi = Yazi("", "oda-kodu");
            kodKutusu.Add(kodYazisi);
            kodKutusu.Add(Buton(Metin.Al("lobi.kopyala"), () => GUIUtility.systemCopyBuffer = Istemci.Oda ?? "", "buton-kucuk"));
            kaydirma.Add(kodKutusu);

            // PC'de bu bölüm sol sütuna taşınır (Yerlesim).
            oyuncuYeri = Kutu("lobi-oyuncu-yeri");
            kaydirma.Add(oyuncuYeri);
            oyuncuBolumu = Kutu("lobi-oyuncu-bolumu");
            oyuncuBolumu.Add(Yazi(Metin.Al("lobi.oyuncular"), "alt-baslik"));
            oyuncuListesi = Kutu("isim-listesi");
            oyuncuBolumu.Add(oyuncuListesi);
            oyuncuYeri.Add(oyuncuBolumu);

            ozet = Yazi("", "aciklama");
            kaydirma.Add(ozet);
            // Seçili oyunun kısa tarifi herkese (kurucu olmayan da ne oynayacağını bilsin).
            oyunTarifi = Yazi("", "ipucu");
            kaydirma.Add(oyunTarifi);
            // Takım Düellosu: takımlar herkese açık (kurucu olmayan da görür), sadece kurucu değiştirir.
            takimParcasi = new TakimDagitmaParcasi();
            kaydirma.Add(takimParcasi.Kok);

            ayarKutusu = Kutu("lobi-ayarlar");
            kaydirma.Add(ayarKutusu);
            AyarlariKur();

            hata = Yazi("", "hata");
            Kok.Add(hata);
            baslaIpucu = Yazi("", "ipucu", "orta");
            Kok.Add(baslaIpucu);
            basla = Buton(Metin.Al("lobi.basla"), () => Gonder(Cevirici.Baslat()), "buton-birincil", "buton-buyuk");
            Kok.Add(basla);

            atPerdesi = Kutu("menu-perde", "gizli");
            Kok.Add(atPerdesi);
            Kok.Add(ayarlar.Kok);
            if (sohbet != null) Kok.Add(sohbet.Kok);

            Yenile();
        }

        // ---------- Ayarlar (sadece kurucu dokunur) ----------

        void AyarlariKur()
        {
            ayarKutusu.Add(Buton(Metin.Al("kurulum.hizli"), HizliBasla, "buton-birincil", "buton-buyuk"));
            ayarKutusu.Add(Yazi(Metin.Al("lobi.hizli_ipucu"), "ipucu"));

            // Oyun ve mod iki ayrı eksen (docs/Oyunlar.md 0).
            Bolum("kurulum.oyun", OyunSecimi.Secenekler, o => Metin.Al("oyun.ad." + o), a => a.Oyun, OyunSec);
            // Rus Ruleti: herkesin canı.
            var canBolumu = Kutu("ayar-bolumu");
            ayarKutusu.Add(canBolumu);
            Bolum("rus.can_baslik", OyunSecimi.CanSecenekleri, KurulumEkrani.CanYazisi, a => a.Can,
                c => Gonder(Cevirici.AyarDegistir(can: c)), yer: canBolumu);
            yenileyiciler.Add(a => canBolumu.EnableInClassList("gizli", a.Oyun != OyunTuru.RusRuleti));
            // Casus: mekân ya da kelime.
            var casusBolumu = Kutu("ayar-bolumu");
            ayarKutusu.Add(casusBolumu);
            Bolum("casus.tur_baslik", OyunSecimi.CasusSecenekleri, c => Metin.Al("casus.tur." + c), a => a.Casus,
                c => Gonder(Cevirici.AyarDegistir(casus: c)), yer: casusBolumu);
            var casusAciklama = Yazi("", "ipucu");
            casusBolumu.Add(casusAciklama);
            yenileyiciler.Add(a =>
            {
                casusBolumu.EnableInClassList("gizli", a.Oyun != OyunTuru.Casus);
                casusAciklama.text = Metin.Al("casus.tur_aciklama." + a.Casus);
            });

            var modButonlari = Bolum("kurulum.mod", ModSecenekleri, m => Metin.Al("mod.ad." + m), a => a.Mod, ModSec);
            // Oyuna uymayan mod (Casus'ta Curcuna) kapalı: kural motoruna sorulur.
            yenileyiciler.Add(a =>
            {
                var ka = OyunSecimi.OdadanAyarlar(a);
                for (int i = 0; i < ModSecenekleri.Length; i++)
                {
                    var m = ModSecenekleri[i];
                    modButonlari[i].SetEnabled(m == a.Mod || OyunSecimi.Uyumlu(ka, x => x.Mod = m));
                }
            });
            var modAciklama = Yazi("", "ipucu");
            ayarKutusu.Add(modAciklama);
            yenileyiciler.Add(a => modAciklama.text = Metin.Al("mod.aciklama." + a.Mod));

            Bolum("kurulum.mini_oyun", MiniSecenekleri, m => Metin.Al("mini.ad." + m), a => a.MiniOyun,
                m => Gonder(Cevirici.AyarDegistir(miniOyun: m)));
            var turBolumu = Kutu("ayar-bolumu");
            ayarKutusu.Add(turBolumu);
            Bolum("kurulum.tur", TurSecenekleri,
                t => t == 0 ? Metin.Al("kurulum.tur_sinirsiz") : Metin.Al("kurulum.tur_sayi", t), a => a.TurSayisi,
                t => Gonder(Cevirici.AyarDegistir(turSayisi: t)), yer: turBolumu);
            var turYok = Yazi("", "ipucu");
            ayarKutusu.Add(turYok);
            yenileyiciler.Add(a =>
            {
                bool yok = OyunSecimi.TurSayisiYok(a.Oyun);
                turBolumu.EnableInClassList("gizli", yok);
                turYok.EnableInClassList("gizli", !yok);
                turYok.text = yok ? Metin.Al("kurulum.tur_yok." + a.Oyun) : "";
            });

            // Çakmak Gecesi: rol açılması, gecelik kurban, roller. Bu oyunda paket seçimi yok (öneriler sunucunun suçlama sorularından).
            var geceAyarlari = new GeceAyarlari(
                a => Gonder(Cevirici.AyarDegistir(rolAcilir: a)),
                k => Gonder(Cevirici.AyarDegistir(gecelikKurban: k)),
                r =>
                {
                    var yeni = GeceAyarlari.Degistir(D.Ayarlar.KapaliRoller, r);
                    if (OyunSecimi.Uyumlu(OyunSecimi.OdadanAyarlar(D.Ayarlar), x => x.KapaliRoller = yeni))
                        Gonder(Cevirici.AyarDegistir(kapaliRoller: yeni));
                });
            ayarKutusu.Add(geceAyarlari.Kok);
            var geceSoruNotu = Yazi(Metin.Al("gece.paket_yok"), "ipucu");
            ayarKutusu.Add(geceSoruNotu);
            yenileyiciler.Add(a =>
            {
                bool gece = a.Oyun == OyunTuru.CakmakGecesi;
                geceAyarlari.Kok.EnableInClassList("gizli", !gece);
                geceSoruNotu.EnableInClassList("gizli", !gece);
                geceAyarlari.Ciz(a.RolAcilir, a.GecelikKurban, a.KapaliRoller);
            });

            var paketBolumu = Kutu("ayar-bolumu");
            ayarKutusu.Add(paketBolumu);
            yenileyiciler.Add(a => paketBolumu.EnableInClassList("gizli", a.Oyun == OyunTuru.CakmakGecesi));
            paketBolumu.Add(Yazi(Metin.Al("kurulum.paketler"), "alt-baslik"));
            var paketKutusu = Kutu("secenekler");
            foreach (var p in uygulama.Paketler)
            {
                string ad = p.Ad;
                var b = Buton(Metin.Al("kurulum.paket_sayi", p.GorunenAd, p.Sorular.Count), () => PaketDegistir(ad), "secenek");
                paketButonlari.Add(new KeyValuePair<string, Button>(ad, b));
                paketKutusu.Add(b);
            }
            bizimButon = Buton("", BizimPaketDegistir, "secenek", "gizli");
            paketKutusu.Add(bizimButon);
            paketBolumu.Add(paketKutusu);
            bizimDurum = Yazi("", "ipucu");
            paketBolumu.Add(bizimDurum);
            var paketNotu = Yazi("", "ipucu");
            paketBolumu.Add(paketNotu);
            yenileyiciler.Add(a =>
            {
                bool temiz = a.Mod == OyunModu.Temiz;
                foreach (var pb in paketButonlari)
                {
                    pb.Value.EnableInClassList("secili", a.Paketler.Contains(pb.Key));
                    pb.Value.SetEnabled(!temiz || a.Paketler.Contains(pb.Key));
                }
                paketNotu.text = temiz ? Metin.Al("kurulum.paket_temiz") : "";
                BizimCiz(a);
            });

            var cezaButonlari = Bolum("kurulum.ceza", CezaSecenekleri, c => Metin.Al("ceza.seviye." + c), a => a.Ceza,
                c => Gonder(Cevirici.AyarDegistir(ceza: c)));
            var cezaAciklama = Yazi("", "ipucu");
            ayarKutusu.Add(cezaAciklama);
            yenileyiciler.Add(a =>
            {
                bool temiz = a.Mod == OyunModu.Temiz;
                var ka = OyunSecimi.OdadanAyarlar(a);
                for (int i = 0; i < CezaSecenekleri.Length; i++)
                {
                    var c = CezaSecenekleri[i];
                    cezaButonlari[i].SetEnabled(c == a.Ceza || OyunSecimi.Uyumlu(ka, x => x.CezaSeviyesi = c));
                }
                cezaAciklama.text = Metin.Al("ceza.seviye_aciklama." + a.Ceza) + (temiz ? "  " + Metin.Al("kurulum.ceza_temiz") : "");
            });

            ayarKutusu.Add(Yazi(Metin.Al("kurulum.ekstra"), "alt-baslik"));
            var ekstra = Kutu("secenekler");
            var gorev = Buton("", () =>
            {
                if (!OyunSecimi.GorevlerHerkeste(D.Ayarlar.Oyun)) Gonder(Cevirici.AyarDegistir(gorevler: !D.Ayarlar.Gorevler));
            }, "secenek");
            var kaos = Buton("", () => Gonder(Cevirici.AyarDegistir(kaos: !D.Ayarlar.Kaos)), "secenek");
            ekstra.Add(gorev);
            ekstra.Add(kaos);
            ayarKutusu.Add(ekstra);
            yenileyiciler.Add(a =>
            {
                var ka = OyunSecimi.OdadanAyarlar(a);
                bool herkeste = OyunSecimi.GorevlerHerkeste(a.Oyun);
                bool gorevAcik = herkeste || a.Gorevler;
                gorev.text = Metin.Al("kurulum.gorevler") + ": " + Metin.Al(herkeste ? "kurulum.herkeste" : gorevAcik ? "kurulum.acik" : "kurulum.kapali");
                gorev.EnableInClassList("secili", gorevAcik);
                gorev.SetEnabled(herkeste || a.Gorevler || OyunSecimi.Uyumlu(ka, x => x.GizliGorevler = true));
                kaos.text = Metin.Al("kurulum.kaos") + ": " + Metin.Al(a.Kaos ? "kurulum.acik" : "kurulum.kapali");
                kaos.EnableInClassList("secili", a.Kaos);
                kaos.SetEnabled(a.Kaos || OyunSecimi.Uyumlu(ka, x => x.KaosTurlari = true));
            });

            ayarKutusu.Add(Yazi(Metin.Al("lobi.sureler"), "alt-baslik"));
            Bolum("lobi.soru_sn", SoruSnSecenekleri, s => Metin.Al("lobi.sn", s), a => a.SoruSn, s => Gonder(Cevirici.AyarDegistir(soruSn: s)), altBaslik: false);
            Bolum("lobi.cevap_sn", CevapSnSecenekleri, s => Metin.Al("lobi.sn", s), a => a.CevapSn, s => Gonder(Cevirici.AyarDegistir(cevapSn: s)), altBaslik: false);
            Bolum("lobi.hamle_sn", HamleSnSecenekleri, s => Metin.Al("lobi.sn", s), a => a.HamleSn, s => Gonder(Cevirici.AyarDegistir(hamleSn: s)), altBaslik: false);
        }

        /// <summary>Bir ayar satırı: başlık + seçenek butonları. Seçili olan sunucunun durumundan.</summary>
        List<Button> Bolum<T>(string baslik, T[] secenekler, Func<T, string> yazi, Func<OdaAyarlari, T> secili, Action<T> sec,
            bool altBaslik = true, VisualElement yer = null)
        {
            yer = yer ?? ayarKutusu;
            yer.Add(Yazi(Metin.Al(baslik), altBaslik ? "alt-baslik" : "ipucu"));
            var satir = Kutu("secenekler");
            var butonlar = new List<Button>();
            foreach (var s in secenekler)
            {
                var secim = s;
                var b = Buton(yazi(s), () => sec(secim), "secenek");
                butonlar.Add(b);
                satir.Add(b);
            }
            yer.Add(satir);
            yenileyiciler.Add(a =>
            {
                var su = secili(a);
                for (int i = 0; i < secenekler.Length; i++)
                    butonlar[i].EnableInClassList("secili", EqualityComparer<T>.Default.Equals(secenekler[i], su));
            });
            return butonlar;
        }

        /// <summary>
        /// Temiz: sadece Aile Dostu ve en fazla Hafif (Ana Plan 8.1). Sunucu Temiz + Cesur'u reddeder:
        /// ceza aynı mesajda düşürülür, paket aynı mesajda Aile Dostu'na çekilir.
        /// </summary>
        void ModSec(OyunModu m)
        {
            var a = D.Ayarlar;
            if (m == OyunModu.Temiz)
            {
                bool aileVar = uygulama.Paketler.Exists(p => p.Ad == PaketBilgisi.AileDostu);
                Gonder(Cevirici.AyarDegistir(
                    mod: m,
                    ceza: a.Ceza == CezaSeviyesi.Cesur ? CezaSeviyesi.Hafif : (CezaSeviyesi?)null,
                    paketler: aileVar ? new[] { PaketBilgisi.AileDostu } : null));
            }
            else Gonder(Cevirici.AyarDegistir(mod: m));
        }

        // ---------- Bizim paket (grup paketi) ----------

        /// <summary>Lobidekilerin grubu (elden eledeki eşleşme kuralı). İsimler değişmedikçe dosyalar yeniden okunmaz.</summary>
        void GrubuBul(DurumMesaji d)
        {
            var isimler = new List<string>();
            foreach (var o in d.Oyuncular) isimler.Add(o.Isim);
            string anahtar = string.Join("\n", isimler);
            if (anahtar == grupIsimleri) return;
            grupIsimleri = anahtar;
            grup = GrupDeposu.Eslesen(isimler);
        }

        int YerelBizimSayisi => grup != null ? grup.Kayit.GrupPaketi.Count : 0;

        /// <summary>Sadece grubu tanınan kurucuya, paketi doluysa ve Temiz değilse. Seçili olan da (gruptan biri çıktıysa) görünür.</summary>
        void BizimCiz(OdaAyarlari a)
        {
            bool secili = a.Paketler.Contains(PaketBilgisi.Bizim);
            bool goster = a.Mod != OyunModu.Temiz && (YerelBizimSayisi > 0 || secili);
            bizimButon.EnableInClassList("gizli", !goster);
            int sayi = D != null && D.GrupPaketi > 0 ? D.GrupPaketi : YerelBizimSayisi;
            bizimButon.text = Metin.Al("kurulum.bizim_paket", sayi);
            bizimButon.EnableInClassList("secili", secili);
            bizimButon.SetEnabled(yukleyici == null || yukleyici.Durum != GrupPaketiYukleyici.Hal.Yukleniyor);
        }

        void BizimPaketDegistir()
        {
            hata.text = "";
            var a = D?.Ayarlar;
            if (a == null || !Kurucu) return;
            if (a.Paketler.Contains(PaketBilgisi.Bizim))
            {
                PaketDegistir(PaketBilgisi.Bizim);
                return;
            }
            // Sunucuda bu odanın paketi varsa (tekrar oyna) hemen seçilir; yoksa önce yüklenir.
            if (D.GrupPaketi > 0 || YerelBizimSayisi == 0)
            {
                if (D.GrupPaketi > 0) PaketDegistir(PaketBilgisi.Bizim);
                return;
            }
            yukleyici = new GrupPaketiYukleyici(grup.Kayit.GrupPaketi, Istemci.Gonder);
            yukleyici.Basla(D.Surum);
            BizimYuklemeDurumu();
        }

        void BizimYuklemeDurumu()
        {
            if (yukleyici == null) return;
            switch (yukleyici.Durum)
            {
                case GrupPaketiYukleyici.Hal.Yukleniyor:
                    bizimDurum.text = Metin.Al("lobi.bizim_yukleniyor", yukleyici.Biten, yukleyici.Toplam);
                    break;
                case GrupPaketiYukleyici.Hal.Bitti:
                    yukleyici = null;
                    bizimDurum.text = "";
                    if (D?.Ayarlar != null && D.GrupPaketi > 0 && !D.Ayarlar.Paketler.Contains(PaketBilgisi.Bizim))
                        PaketDegistir(PaketBilgisi.Bizim);
                    break;
                case GrupPaketiYukleyici.Hal.Hata:
                    bizimDurum.text = Metin.Al("lobi.bizim_hata") +
                        (yukleyici.HataKodu != null ? " " + HataMetni.Kod(yukleyici.HataKodu) : "");
                    yukleyici = null;
                    break;
            }
            if (D?.Ayarlar != null) BizimCiz(D.Ayarlar);
        }

        /// <summary>
        /// Oyun değişince uymayan ayarlar aynı mesajda kapanır (sunucu uyumsuz sonucu GECERSIZ_GIRDI ile reddeder,
        /// hangi sırayla gönderilirse gönderilsin). Neyin uymadığı kural motoruna sorulur.
        /// </summary>
        void OyunSec(OyunTuru o)
        {
            var once = OyunSecimi.OdadanAyarlar(D.Ayarlar);
            var sonra = OyunSecimi.OdadanAyarlar(D.Ayarlar);
            sonra.Oyun = o;
            OyunSecimi.Uyumla(sonra);
            Gonder(Cevirici.AyarDegistir(
                oyun: o,
                kaos: sonra.KaosTurlari != once.KaosTurlari ? sonra.KaosTurlari : (bool?)null,
                gorevler: sonra.GizliGorevler != once.GizliGorevler ? sonra.GizliGorevler : (bool?)null,
                ceza: sonra.CezaSeviyesi != once.CezaSeviyesi ? sonra.CezaSeviyesi : (CezaSeviyesi?)null,
                mod: sonra.Mod != once.Mod ? sonra.Mod : (OyunModu?)null));
        }

        void PaketDegistir(string ad)
        {
            var secili = new List<string>(D.Ayarlar.Paketler);
            if (!secili.Remove(ad)) secili.Add(ad);
            if (secili.Count == 0)
            {
                hata.text = Metin.Al("kurulum.hata_paket");
                return;
            }
            Gonder(Cevirici.AyarDegistir(paketler: secili));
        }

        /// <summary>Elden eledeki "Hızlı başla"nın aynısı; 4 kişi varsa oyunu da başlatır.</summary>
        void HizliBasla()
        {
            bool genelVar = uygulama.Paketler.Exists(p => p.Ad == PaketBilgisi.Genel);
            Gonder(Cevirici.AyarDegistir(oyun: OyunTuru.Cakmak, mod: OyunModu.Normal, miniOyun: MiniOyunTuru.Tkm, turSayisi: 15,
                ceza: CezaSeviyesi.Cesur, gorevler: true, kaos: true,
                paketler: genelVar ? new[] { PaketBilgisi.Genel } : null));
            if (D.Oyuncular.Count >= Oyun.EnAzOyuncu) Gonder(Cevirici.Baslat());
        }

        void Gonder(string json)
        {
            hata.text = "";
            if (!Kurucu) return;
            Istemci.Gonder(json);
        }

        // ---------- Oyuncular ----------

        void OyunculariCiz(DurumMesaji d)
        {
            oyuncuListesi.Clear();
            foreach (var o in d.Oyuncular)
            {
                gorulenIsimler[o.Id] = o.Isim;
                var oyuncu = o;
                var satir = Kutu("satir", "lobi-oyuncu");
                satir.Add(RenkNoktasi(o.Renk));
                string ad = o.Isim;
                if (o.Id == Istemci.Sen) ad += " " + Metin.Al("lobi.sen");
                var isim = Yazi(ad, "lobi-isim");
                satir.Add(isim);
                if (d.Kurucu == o.Id) satir.Add(Yazi(Metin.Al("lobi.kurucu"), "lobi-rozet"));
                if (!o.Bagli) satir.Add(Yazi(Metin.Al("online.baglaniyor_kisa"), "lobi-rozet", "soluk"));
                satir.Add(Esnek());
                if (Kurucu && o.Id != Istemci.Sen)
                    satir.Add(Buton(Metin.Al("lobi.at"), () => AtSor(oyuncu), "buton-kucuk"));
                oyuncuListesi.Add(satir);
            }
        }

        /// <summary>Atmadan önce sor: geri alınamaz (atılan bu odaya aynı isimle dönemez).</summary>
        void AtSor(OdaOyuncusu o)
        {
            atPerdesi.Clear();
            var kutu = Kutu("menu-kutu");
            kutu.Add(Yazi(Metin.Al("lobi.at_soru", o.Isim, TurkceEk.Belirtme(o.Isim)), "baslik"));
            kutu.Add(Yazi(Metin.Al("lobi.at_ipucu"), "ipucu"));
            kutu.Add(Buton(Metin.Al("lobi.at_evet"), () =>
            {
                atPerdesi.AddToClassList("gizli");
                Gonder(Cevirici.At(o.Id));
            }, "buton-birincil"));
            kutu.Add(Buton(Metin.Al("genel.geri"), () => atPerdesi.AddToClassList("gizli"), "buton-ikincil"));
            atPerdesi.Add(kutu);
            atPerdesi.RemoveFromClassList("gizli");
        }

        // ---------- Durum ----------

        void Yenile()
        {
            var d = D;
            if (d == null || d.Yer != OdaYeri.Lobi || d.Ayarlar == null) return;
            sonSurum = d.Surum;

            kodYazisi.text = d.Oda ?? Istemci.Oda ?? "";
            GrubuBul(d);
            OyunculariCiz(d);

            var a = d.Ayarlar;
            foreach (var y in yenileyiciler) y(a);

            // Kurucu ayarları değiştirir; diğerleri sadece özeti görür.
            ayarKutusu.EnableInClassList("gizli", !Kurucu);
            ozet.text = Ozet(a, d.GrupPaketi);
            oyunTarifi.text = Metin.Al("oyun.aciklama." + a.Oyun);
            ozet.EnableInClassList("gizli", Kurucu);

            string takimUyarisi = TakimlariCiz(d);

            int kisi = d.Oyuncular.Count;
            basla.EnableInClassList("gizli", !Kurucu);
            // Çakmak Gecesi 6–18, diğer oyunlar 4–10 (oda 18 kişilik).
            int enAz = OyuncuSayisi.EnAz(a.Oyun), enFazla = OyuncuSayisi.EnFazla(a.Oyun);
            basla.SetEnabled(kisi >= enAz && kisi <= enFazla && takimUyarisi == null);
            baslaIpucu.text = !Kurucu ? Metin.Al("lobi.kurucu_bekleniyor", Isim(d.Kurucu))
                : kisi < enAz ? Metin.Al("lobi.en_az", kisi, enAz)
                : kisi > enFazla ? Metin.Al("lobi.en_fazla", kisi, enFazla)
                : takimUyarisi ?? "";
        }

        // ---------- Takımlar (Takım Düellosu) ----------

        static List<OyuncuId> Kisiler(DurumMesaji d)
        {
            var l = new List<OyuncuId>();
            foreach (var o in d.Oyuncular) l.Add(o.Id);
            return l;
        }

        /// <summary>Sunucunun dağılımı (lobiden çıkan zaten düşmüş). Takım oyunu değilse gizli. Geçersizse uyarı metni.</summary>
        string TakimlariCiz(DurumMesaji d)
        {
            bool takimli = d.Ayarlar.Oyun == OyunTuru.Takim;
            takimParcasi.Kok.EnableInClassList("gizli", !takimli);
            if (!takimli) return null;
            var kisiler = Kisiler(d);
            var dag = new TakimDagitimi(d.Ayarlar.Takimlar);
            dag.Esitle(kisiler);
            return takimParcasi.Ciz(dag, kisiler, id => Isim(id), id => D.Bul(id)?.Renk ?? 0,
                Kurucu ? TakimDokun : (Action<OyuncuId>)null,
                Kurucu ? () => Gonder(Cevirici.AyarDegistir(takimlar: new Dictionary<OyuncuId, int>())) : (Action)null);
        }

        /// <summary>Dokunulanın takımı değişir; dağılımın tamamı gider (sunucu baştan yazar), ekran gelen durumu çizer.</summary>
        void TakimDokun(OyuncuId id)
        {
            var d = D;
            if (d?.Ayarlar == null) return;
            var dag = new TakimDagitimi(d.Ayarlar.Takimlar);
            dag.Esitle(Kisiler(d));
            dag.Dokun(id);
            Gonder(Cevirici.AyarDegistir(takimlar: dag.Dagilim));
        }

        string Isim(OyuncuId? id)
        {
            if (!id.HasValue || D == null) return "";
            var o = D.Bul(id.Value);
            return o?.Isim ?? "";
        }

        static string Ozet(OdaAyarlari a, int grupPaketi)
        {
            var parca = new List<string>
            {
                Metin.Al("oyun.ad." + a.Oyun),
                Metin.Al("mod.ad." + a.Mod),
                Metin.Al("mini.ad." + a.MiniOyun),
                a.Oyun == OyunTuru.RusRuleti ? KurulumEkrani.CanYazisi(a.Can)
                    : OyunSecimi.TurSayisiYok(a.Oyun) ? Metin.Al("kurulum.tur_yok." + a.Oyun)
                    : a.TurSayisi == 0 ? Metin.Al("kurulum.tur_sinirsiz") : Metin.Al("kurulum.tur_sayi", a.TurSayisi),
                Metin.Al("ceza.seviye." + a.Ceza),
            };
            if (a.Oyun == OyunTuru.Casus) parca.Insert(1, Metin.Al("casus.tur." + a.Casus));
            if (a.Oyun == OyunTuru.CakmakGecesi)
            {
                parca.Insert(1, Metin.Al(a.RolAcilir ? "gece.ayar_rol_acik" : "gece.ayar_rol_gizli"));
                parca.Insert(2, Metin.Al("gece.ayar_kurban_sayi", a.GecelikKurban));
                if (a.KapaliRoller.Count > 0) parca.Add(Metin.Al("gece.ozet_kapali", a.KapaliRoller.Count));
            }
            if (a.Gorevler || OyunSecimi.GorevlerHerkeste(a.Oyun)) parca.Add(Metin.Al("kurulum.gorevler"));
            if (a.Kaos) parca.Add(Metin.Al("kurulum.kaos"));
            if (grupPaketi > 0 && a.Mod != OyunModu.Temiz && a.Paketler.Contains(PaketBilgisi.Bizim))
                parca.Add(Metin.Al("kurulum.bizim_paket", grupPaketi));
            return Metin.Al("lobi.ozet", string.Join(" · ", parca));
        }

        public void Guncelle(float gecenSn)
        {
            if (D != null && D.Surum != sonSurum)
            {
                yukleyici?.DurumGeldi(D.Surum);
                Yenile();
            }
            if (yukleyici != null)
            {
                yukleyici.Ilerle(gecenSn);
                BizimYuklemeDurumu();
            }
            sohbet?.Guncelle(gecenSn);
        }

        public void ArkaPlanaGecti() { }

        public void HataGeldi(HataMesaji h)
        {
            if (sohbet != null && Istemci.SonGidenSohbet)
            {
                sohbet.HataGoster(HataMetni.Mesaj(h));
                return;
            }
            if (yukleyici != null)
            {
                yukleyici.HataGeldi(h.Kod);
                BizimYuklemeDurumu();
                return;
            }
            hata.text = HataMetni.Mesaj(h);
        }

        public void OlayGeldi(OlayMesaji o)
        {
            if (!o.Atildi) return;
            // Atılan listeden zaten düşüyor; neden kısaca yazılır (Temiz mod mu, kurucu mu).
            string isim = "";
            if (o.Kim.HasValue && gorulenIsimler.TryGetValue(o.Kim.Value, out var i)) isim = i;
            hata.text = HataMetni.Atildi(o, isim);
        }

        public IEkran YenidenKur()
        {
            var e = new LobiEkrani(uygulama, online);
            e.ayarlar.Ac();
            return e;
        }

        /// <summary>PC'de geniş pencere: oyuncular solda, sohbet sağda hep açık.</summary>
        public void Yerlesim(bool genis, VisualElement sol, VisualElement sag)
        {
            if (genis) sol.Add(oyuncuBolumu);
            else oyuncuYeri.Add(oyuncuBolumu);
            oyuncuBolumu.EnableInClassList("menu-kutu", genis);
            if (sohbet == null) return;
            sohbet.YanaAl(genis);
            sohbetDugmesi.EnableInClassList("gizli", genis);
            if (genis) sag.Add(sohbet.Kok);
            else Kok.Add(sohbet.Kok);
        }
    }
}
