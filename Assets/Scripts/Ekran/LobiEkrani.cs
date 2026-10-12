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
    /// </summary>
    public sealed class LobiEkrani : IEkran, IOnlineEkran
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
        readonly Label hata;
        readonly Button basla;
        readonly Label baslaIpucu;
        readonly VisualElement atPerdesi;
        readonly List<Action<OdaAyarlari>> yenileyiciler = new List<Action<OdaAyarlari>>();
        readonly List<KeyValuePair<string, Button>> paketButonlari = new List<KeyValuePair<string, Button>>();
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

            kaydirma.Add(Yazi(Metin.Al("lobi.oyuncular"), "alt-baslik"));
            oyuncuListesi = Kutu("isim-listesi");
            kaydirma.Add(oyuncuListesi);

            ozet = Yazi("", "aciklama");
            kaydirma.Add(ozet);

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

            Yenile();
        }

        // ---------- Ayarlar (sadece kurucu dokunur) ----------

        void AyarlariKur()
        {
            ayarKutusu.Add(Buton(Metin.Al("kurulum.hizli"), HizliBasla, "buton-birincil", "buton-buyuk"));
            ayarKutusu.Add(Yazi(Metin.Al("lobi.hizli_ipucu"), "ipucu"));

            Bolum("kurulum.mod", ModSecenekleri, m => Metin.Al("mod.ad." + m), a => a.Mod, ModSec);
            var modAciklama = Yazi("", "ipucu");
            ayarKutusu.Add(modAciklama);
            yenileyiciler.Add(a => modAciklama.text = Metin.Al("mod.aciklama." + a.Mod));

            Bolum("kurulum.mini_oyun", MiniSecenekleri, m => Metin.Al("mini.ad." + m), a => a.MiniOyun,
                m => Gonder(Cevirici.AyarDegistir(miniOyun: m)));
            Bolum("kurulum.tur", TurSecenekleri,
                t => t == 0 ? Metin.Al("kurulum.tur_sinirsiz") : Metin.Al("kurulum.tur_sayi", t), a => a.TurSayisi,
                t => Gonder(Cevirici.AyarDegistir(turSayisi: t)));

            ayarKutusu.Add(Yazi(Metin.Al("kurulum.paketler"), "alt-baslik"));
            var paketKutusu = Kutu("secenekler");
            foreach (var p in uygulama.Paketler)
            {
                string ad = p.Ad;
                var b = Buton(Metin.Al("kurulum.paket_sayi", p.GorunenAd, p.Sorular.Count), () => PaketDegistir(ad), "secenek");
                paketButonlari.Add(new KeyValuePair<string, Button>(ad, b));
                paketKutusu.Add(b);
            }
            ayarKutusu.Add(paketKutusu);
            var paketNotu = Yazi("", "ipucu");
            ayarKutusu.Add(paketNotu);
            yenileyiciler.Add(a =>
            {
                bool temiz = a.Mod == OyunModu.Temiz;
                foreach (var pb in paketButonlari)
                {
                    pb.Value.EnableInClassList("secili", a.Paketler.Contains(pb.Key));
                    pb.Value.SetEnabled(!temiz || a.Paketler.Contains(pb.Key));
                }
                paketNotu.text = temiz ? Metin.Al("kurulum.paket_temiz") : "";
            });

            var cezaButonlari = Bolum("kurulum.ceza", CezaSecenekleri, c => Metin.Al("ceza.seviye." + c), a => a.Ceza,
                c => Gonder(Cevirici.AyarDegistir(ceza: c)));
            var cezaAciklama = Yazi("", "ipucu");
            ayarKutusu.Add(cezaAciklama);
            yenileyiciler.Add(a =>
            {
                bool temiz = a.Mod == OyunModu.Temiz;
                for (int i = 0; i < CezaSecenekleri.Length; i++)
                    cezaButonlari[i].SetEnabled(!temiz || CezaSecenekleri[i] != CezaSeviyesi.Cesur);
                cezaAciklama.text = Metin.Al("ceza.seviye_aciklama." + a.Ceza) + (temiz ? "  " + Metin.Al("kurulum.ceza_temiz") : "");
            });

            ayarKutusu.Add(Yazi(Metin.Al("kurulum.ekstra"), "alt-baslik"));
            var ekstra = Kutu("secenekler");
            var gorev = Buton("", () => Gonder(Cevirici.AyarDegistir(gorevler: !D.Ayarlar.Gorevler)), "secenek");
            var kaos = Buton("", () => Gonder(Cevirici.AyarDegistir(kaos: !D.Ayarlar.Kaos)), "secenek");
            ekstra.Add(gorev);
            ekstra.Add(kaos);
            ayarKutusu.Add(ekstra);
            yenileyiciler.Add(a =>
            {
                gorev.text = Metin.Al("kurulum.gorevler") + ": " + Metin.Al(a.Gorevler ? "kurulum.acik" : "kurulum.kapali");
                gorev.EnableInClassList("secili", a.Gorevler);
                kaos.text = Metin.Al("kurulum.kaos") + ": " + Metin.Al(a.Kaos ? "kurulum.acik" : "kurulum.kapali");
                kaos.EnableInClassList("secili", a.Kaos);
            });

            ayarKutusu.Add(Yazi(Metin.Al("lobi.sureler"), "alt-baslik"));
            Bolum("lobi.soru_sn", SoruSnSecenekleri, s => Metin.Al("lobi.sn", s), a => a.SoruSn, s => Gonder(Cevirici.AyarDegistir(soruSn: s)), altBaslik: false);
            Bolum("lobi.cevap_sn", CevapSnSecenekleri, s => Metin.Al("lobi.sn", s), a => a.CevapSn, s => Gonder(Cevirici.AyarDegistir(cevapSn: s)), altBaslik: false);
            Bolum("lobi.hamle_sn", HamleSnSecenekleri, s => Metin.Al("lobi.sn", s), a => a.HamleSn, s => Gonder(Cevirici.AyarDegistir(hamleSn: s)), altBaslik: false);
        }

        /// <summary>Bir ayar satırı: başlık + seçenek butonları. Seçili olan sunucunun durumundan.</summary>
        List<Button> Bolum<T>(string baslik, T[] secenekler, Func<T, string> yazi, Func<OdaAyarlari, T> secili, Action<T> sec, bool altBaslik = true)
        {
            ayarKutusu.Add(Yazi(Metin.Al(baslik), altBaslik ? "alt-baslik" : "ipucu"));
            var satir = Kutu("secenekler");
            var butonlar = new List<Button>();
            foreach (var s in secenekler)
            {
                var secim = s;
                var b = Buton(yazi(s), () => sec(secim), "secenek");
                butonlar.Add(b);
                satir.Add(b);
            }
            ayarKutusu.Add(satir);
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
            Gonder(Cevirici.AyarDegistir(mod: OyunModu.Normal, miniOyun: MiniOyunTuru.Tkm, turSayisi: 15,
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
            OyunculariCiz(d);

            var a = d.Ayarlar;
            foreach (var y in yenileyiciler) y(a);

            // Kurucu ayarları değiştirir; diğerleri sadece özeti görür.
            ayarKutusu.EnableInClassList("gizli", !Kurucu);
            ozet.text = Ozet(a);
            ozet.EnableInClassList("gizli", Kurucu);

            int kisi = d.Oyuncular.Count;
            basla.EnableInClassList("gizli", !Kurucu);
            basla.SetEnabled(kisi >= Oyun.EnAzOyuncu);
            baslaIpucu.text = !Kurucu ? Metin.Al("lobi.kurucu_bekleniyor", Isim(d.Kurucu))
                : kisi < Oyun.EnAzOyuncu ? Metin.Al("lobi.en_az", kisi, Oyun.EnAzOyuncu)
                : "";
        }

        string Isim(OyuncuId? id)
        {
            if (!id.HasValue || D == null) return "";
            var o = D.Bul(id.Value);
            return o?.Isim ?? "";
        }

        static string Ozet(OdaAyarlari a)
        {
            var parca = new List<string>
            {
                Metin.Al("mod.ad." + a.Mod),
                Metin.Al("mini.ad." + a.MiniOyun),
                a.TurSayisi == 0 ? Metin.Al("kurulum.tur_sinirsiz") : Metin.Al("kurulum.tur_sayi", a.TurSayisi),
                Metin.Al("ceza.seviye." + a.Ceza),
            };
            if (a.Gorevler) parca.Add(Metin.Al("kurulum.gorevler"));
            if (a.Kaos) parca.Add(Metin.Al("kurulum.kaos"));
            return Metin.Al("lobi.ozet", string.Join(" · ", parca));
        }

        public void Guncelle(float gecenSn)
        {
            if (D != null && D.Surum != sonSurum) Yenile();
        }

        public void ArkaPlanaGecti() { }

        public void HataGeldi(HataMesaji h) => hata.text = HataMetni.Kod(h.Kod);

        public void OlayGeldi(OlayMesaji o) { }
    }
}
