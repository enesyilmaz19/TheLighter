using System;
using System.Collections.Generic;
using Cakmak.Kurallar;
using TheLighter.Ag;
using UnityEngine;

namespace TheLighter.Istemci
{
    /// <summary>Online ekranlarından biri: oturumdan gelen hata ve olayları gösterir.</summary>
    public interface IOnlineEkran
    {
        void HataGeldi(HataMesaji h);
        void OlayGeldi(OlayMesaji o);
    }

    /// <summary>Cihazda kalıcı küçük ayarlar: PlayerPrefs.</summary>
    sealed class PlayerPrefsKalici : IKalici
    {
        public string Oku(string ad) => PlayerPrefs.HasKey(ad) ? PlayerPrefs.GetString(ad) : null;

        public void Yaz(string ad, string deger)
        {
            PlayerPrefs.SetString(ad, deger ?? "");
            PlayerPrefs.Save();
        }

        public void Sil(string ad)
        {
            PlayerPrefs.DeleteKey(ad);
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// Online modun Unity tarafı: oturumu her karede ilerletir, odanın yerine göre ekranı seçer
    /// (lobi, bekleme odası, oyun), kopunca "Bağlanıyor…" katmanını açar.
    /// Sunucu tek otorite: ekranlar <see cref="OnlineIstemci.SonDurum"/>'dan çizilir.
    /// </summary>
    public sealed class OnlineYonetici
    {
        /// <summary>
        /// Yerel deneme adresi (Sunucu/Cakmak.Sunucu, dotnet run). Yayındaki adres koda yazılmaz:
        /// online menüsündeki "Sunucu" alanından girilir ve cihazda saklanır.
        /// </summary>
        public const string YerelAdres = "ws://localhost:5000/ws";
        const string KayitSunucu = "online.sunucu";
        const string KayitSonIsim = "online.son_isim";

        readonly Uygulama uygulama;
        readonly IKalici kalici = new PlayerPrefsKalici();
        OdaAyarlari sonAyarlar;
        OnlineAkis akis;

        public OnlineIstemci Istemci { get; }

        /// <summary>
        /// Sohbet ve DM protokolü sunucuda henüz yok (docs/Protokol.md, "Henüz yok"). Gelince true olur,
        /// gelen mesajlar <see cref="Sohbet"/>'e eklenir. O zamana kadar sohbet düğmesi görünmez.
        /// </summary>
        public static bool SohbetVar => false;

        /// <summary>Bu odanın sohbeti. Cihazda saklanmaz, odadan çıkınca silinir.</summary>
        public SohbetDefteri Sohbet { get; } = new SohbetDefteri();

        /// <summary>Sohbet mesajı gönderir. Protokol gelene kadar false (gönderilmedi).</summary>
        public bool SohbetGonder(OyuncuId? kanal, string metin)
        {
            // Protokol gelince: SohbetVar ve Odada ise Istemci.Gonder(Cevirici.Sohbet(kanal, metin)), sonra Sohbet.Ekle(benim mesajım).
            return false;
        }

        /// <summary>Lobi ya da oyun ekranına eklenecek sohbet katmanı.</summary>
        public SohbetPaneli SohbetPaneliKur() => new SohbetPaneli(Sohbet, SohbetGonder,
            () => (IReadOnlyList<OdaOyuncusu>)Istemci.SonDurum?.Oyuncular ?? new List<OdaOyuncusu>(), () => Istemci.Sen);

        /// <summary>Lobide son görülen ayarlar (oyun ekranı mod ve öneriler için kullanır).</summary>
        public OdaAyarlari SonAyarlar => sonAyarlar;

        public OnlineYonetici(Uygulama uygulama)
        {
            this.uygulama = uygulama;
            // Anahtar günlüğe hiç girmez (OnlineIstemci yazmaz). Günlük sadece geliştirirken editörde.
            Istemci = new OnlineIstemci(() => new WebSocketTasima(), kalici, Application.isEditor ? (Action<string>)Debug.Log : null);
            Istemci.DurumGeldi += DurumGeldi;
            Istemci.Ayrildi += Ayrildi;
            Istemci.HataGeldi += h => (uygulama.Ekran as IOnlineEkran)?.HataGeldi(h);
            Istemci.OlayGeldi += o => (uygulama.Ekran as IOnlineEkran)?.OlayGeldi(o);
        }

        public string SunucuAdresi
        {
            get
            {
                var s = kalici.Oku(KayitSunucu);
                return string.IsNullOrWhiteSpace(s) ? YerelAdres : s;
            }
        }

        /// <summary>ws:// ya da wss:// adresi kaydeder. Bozuksa false.</summary>
        public bool SunucuAdresiKaydet(string adres)
        {
            if (!AdresGecerli(adres, out _)) return false;
            kalici.Yaz(KayitSunucu, adres.Trim());
            return true;
        }

        public static bool AdresGecerli(string adres, out Uri uri)
        {
            uri = null;
            if (string.IsNullOrWhiteSpace(adres)) return false;
            if (!Uri.TryCreate(adres.Trim(), UriKind.Absolute, out uri)) return false;
            return uri.Scheme == "ws" || uri.Scheme == "wss";
        }

        public string SonIsim
        {
            get => kalici.Oku(KayitSonIsim) ?? "";
            set => kalici.Yaz(KayitSonIsim, value ?? "");
        }

        public void OdaKur(string isim)
        {
            if (!AdresGecerli(SunucuAdresi, out var uri)) return;
            SonIsim = isim;
            Istemci.OdaKur(uri, isim);
        }

        public void Katil(string kod, string isim)
        {
            if (!AdresGecerli(SunucuAdresi, out var uri)) return;
            SonIsim = isim;
            Istemci.Katil(uri, kod, isim);
        }

        /// <summary>Odadan kendi isteğiyle çık. Ekran online menüsüne döner.</summary>
        public void Ayril()
        {
            if (Istemci.Durum == BaglantiDurumu.Kapali) uygulama.OnlineMenu(null);
            else Istemci.Ayril();
        }

        public void Guncelle(float gecenSn)
        {
            Istemci.Guncelle(TimeSpan.FromSeconds(gecenSn));
            uygulama.BaglaniyorGoster(Istemci.Durum == BaglantiDurumu.YenidenBaglaniyor);
        }

        /// <summary>Android: uygulama arka plandan döndü. Bağlantı sessizce ölmüş olabilir.</summary>
        public void UygulamaDondu() => Istemci.UygulamaDondu();

        /// <summary>
        /// Lobide seçili paketlerin soruları, cihazdaki paket dosyalarından ("fikir ver" için). Sunucu da aynı dosyaları okuyor.
        /// </summary>
        List<string> OneriSorulari(OdaAyarlari a)
        {
            var sorular = new List<string>();
            if (a == null) return sorular;
            foreach (var p in uygulama.Paketler)
                if (a.Paketler.Contains(p.Ad)) sorular.AddRange(p.Sorular);
            return sorular;
        }

        Ayarlar OyunAyarlari()
        {
            var a = sonAyarlar ?? new OdaAyarlari();
            return new Ayarlar
            {
                Mod = a.Mod,
                MiniOyun = a.MiniOyun,
                TurSayisi = a.TurSayisi,
                SoruSuresiSn = a.SoruSn,
                CevapSuresiSn = a.CevapSn,
                HamleSuresiSn = a.HamleSn,
                CezaSeviyesi = a.Ceza,
                GizliGorevler = a.Gorevler,
                KaosTurlari = a.Kaos,
                OneriSorulari = OneriSorulari(a),
            };
        }

        void DurumGeldi(DurumMesaji d)
        {
            switch (d.Yer)
            {
                case OdaYeri.Lobi:
                    sonAyarlar = d.Ayarlar;
                    akis = null;
                    if (!(uygulama.Ekran is LobiEkrani)) uygulama.Goster(new LobiEkrani(uygulama, this));
                    break;

                case OdaYeri.BeklemeOdasi:
                    akis = null;
                    if (!(uygulama.Ekran is BeklemeOdasiEkrani)) uygulama.Goster(new BeklemeOdasiEkrani(uygulama, this));
                    break;

                case OdaYeri.Oyun:
                    if (akis == null || !(uygulama.Ekran is OyunEkrani))
                    {
                        akis = new OnlineAkis(Istemci, OyunAyarlari());
                        uygulama.Goster(new OyunEkrani(uygulama, akis, this));
                    }
                    break;
            }
        }

        void Ayrildi(string neden)
        {
            akis = null;
            Sohbet.Temizle();
            uygulama.BaglaniyorGoster(false);
            uygulama.OnlineMenu(neden == OnlineIstemci.NedenAyrildin ? null : neden);
        }

        /// <summary>Çıkış/hata nedeninin oyuncuya görünen metni.</summary>
        public static string NedenMetni(string neden)
        {
            if (string.IsNullOrEmpty(neden)) return "";
            if (neden == OnlineIstemci.NedenBaglanamadi || neden == OnlineIstemci.NedenAyrildin)
                return Metin.Al("online.cikis." + neden);
            return HataMetni.Kod(neden);
        }
    }
}
