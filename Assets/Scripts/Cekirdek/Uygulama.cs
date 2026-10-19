using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Cakmak.Kurallar;
using TheLighter.Akis;
using UnityEngine;
using UnityEngine.UIElements;

namespace TheLighter.Istemci
{
    /// <summary>
    /// Oyunun girişi. Sahneye bir şey koymak gerekmez: Play'e basınca (ya da build açılınca)
    /// kendini kurar, arayüzü koddan oluşturur. Tek sahne yeterli (şablondaki SampleScene).
    /// </summary>
    public sealed class Uygulama : MonoBehaviour
    {
        const string TemaYolu = "TheLighter/Tema";
        const string StilYolu = "TheLighter/Stil";
        static readonly Vector2Int ReferansCozunurluk = new Vector2Int(1080, 1920);
        /// <summary>
        /// Panel biriminde bu genişlikten itibaren PC yerleşimi: solda oyuncular, ortada oyun, sağda sohbet (Ana Plan 6.2).
        /// İki yan sütun (640) + orta sütun (1200) + boşluklar. 16:9 yatay pencere ≈ 3400.
        /// </summary>
        const float GenisEsik = 2600f;

        static Uygulama ornek;

        PanelSettings panel;
        UIDocument belge;
        VisualElement kok;
        VisualElement sahne;
        VisualElement solYan;
        VisualElement sagYan;
        bool genis;
        IEkran ekran;
        VisualElement baglaniyor;
        Rect sonGuvenliAlan;
        int sonGenislik;
        int sonYukseklik;

        /// <summary>"Tekrar oyna" ve kurulum ekranı için son kullanılan isimler ve ayarlar.</summary>
        public List<string> SonIsimler { get; private set; } = new List<string>();

        public Ayarlar SonAyarlar { get; private set; } = new Ayarlar();

        /// <summary>Son seçilen paketler (dosya adıyla; grubun paketi <see cref="PaketBilgisi.Bizim"/>).</summary>
        public List<string> SonPaketler { get; private set; } = new List<string> { PaketBilgisi.Genel };

        /// <summary>Assets/Icerik paketleri (PaketListesi üzerinden). Açılışta bir kez okunur.</summary>
        public List<PaketBilgisi> Paketler { get; private set; } = new List<PaketBilgisi>();

        List<string> sesliCezalar = new List<string>();

        /// <summary>Online mod: oturum ve online ekranlar arası geçiş.</summary>
        public OnlineYonetici Online { get; private set; }

        /// <summary>Şu an gösterilen ekran.</summary>
        public IEkran Ekran => ekran;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Baslat()
        {
            if (ornek != null) return;
            var go = new GameObject("TheLighter");
            DontDestroyOnLoad(go);
            ornek = go.AddComponent<Uygulama>();
        }

        void Awake()
        {
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep; // elden elede telefon masada duruyor
            if (Application.isMobilePlatform) Screen.orientation = ScreenOrientation.Portrait;

            Tercihler.Uygula();
            Metin.Yukle();
            PaketleriYukle();
            ArayuzuKur();
            Online = new OnlineYonetici(this);
            AnaMenu();
        }

        void ArayuzuKur()
        {
            panel = ScriptableObject.CreateInstance<PanelSettings>();
            panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panel.referenceResolution = ReferansCozunurluk;
            panel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            panel.match = 0f;

            var tema = Resources.Load<ThemeStyleSheet>(TemaYolu);
            if (tema != null) panel.themeStyleSheet = tema;
            else Debug.LogError("[TheLighter] Tema bulunamadı: Resources/" + TemaYolu + ".tss");

            // UIDocument, panelSettings atanmadan etkinleşmesin diye kapalı bir alt nesnede kurulur.
            var arayuz = new GameObject("Arayuz");
            arayuz.SetActive(false);
            arayuz.transform.SetParent(transform, false);
            belge = arayuz.AddComponent<UIDocument>();
            belge.panelSettings = panel;
            arayuz.SetActive(true);

            var root = belge.rootVisualElement;
            var stil = Resources.Load<StyleSheet>(StilYolu);
            if (stil != null) root.styleSheets.Add(stil);
            else Debug.LogError("[TheLighter] Stil bulunamadı: Resources/" + StilYolu + ".uss");

            kok = Arayuz.Kutu("kok");
            solYan = Arayuz.Kutu("yan", "yan-sol", "gizli");
            sahne = Arayuz.Kutu("sahne");
            sagYan = Arayuz.Kutu("yan", "yan-sag", "gizli");
            kok.Add(solYan);
            kok.Add(sahne);
            kok.Add(sagYan);
            baglaniyor = BaglaniyorKatmani();
            kok.Add(baglaniyor);
            root.Add(kok);
            OlcekVeGuvenliAlan(true);
        }

        void Update()
        {
            OlcekVeGuvenliAlan(false);
            // Önce ağ: gelen durumlar bu karede ekrana yansısın (ekran değişebilir).
            Online.Guncelle(Time.unscaledDeltaTime);
            if (ekran != null) ekran.Guncelle(Time.deltaTime);
        }

        void OnApplicationPause(bool duraklatildi)
        {
            if (duraklatildi && ekran != null) ekran.ArkaPlanaGecti();
            // Android: WhatsApp'a bakıp dönen oyuncu. Bağlantı sessizce ölmüş olabilir, hemen yeniden bağlan.
            if (!duraklatildi && Online != null) Online.UygulamaDondu();
        }

        // ---------- Gezinme ----------

        public void Goster(IEkran yeni)
        {
            sahne.Clear();
            ekran = yeni;
            sahne.Add(yeni.Kok);
            YanlariKur();
        }

        /// <summary>Geniş pencerede (PC) ekranın yan sütunları; dar ekranda ekran her şeyi kendi sütununa alır.</summary>
        void YanlariKur()
        {
            solYan.Clear();
            sagYan.Clear();
            if (ekran is IYanliEkran y) y.Yerlesim(genis, solYan, sagYan);
            solYan.EnableInClassList("gizli", !genis || solYan.childCount == 0);
            sagYan.EnableInClassList("gizli", !genis || sagYan.childCount == 0);
        }

        public void AnaMenu() => Goster(new AnaMenuEkrani(this));

        public void Kurulum() => Goster(new KurulumEkrani(this));

        /// <param name="neden">Odadan neden çıkıldı (sunucu kodu ya da oturumun nedeni), yoksa null.</param>
        public void OnlineMenu(string neden) => Goster(new OnlineMenuEkrani(this, Online, neden));

        /// <summary>
        /// Ayarlardan dil değişti: metinler yeni dilden okunur, ekran baştan kurulur (ayarlar açık gelir).
        /// Baştan kurulamayan ekranlar yeni dili bir sonraki ekranda gösterir.
        /// </summary>
        public void DilDegisti()
        {
            Metin.Yukle();
            bool acik = baglaniyor != null && !baglaniyor.ClassListContains("gizli");
            if (baglaniyor != null) baglaniyor.RemoveFromHierarchy();
            baglaniyor = BaglaniyorKatmani();
            kok.Add(baglaniyor);
            BaglaniyorGoster(acik);
            if (ekran is IYenidenKurulur y) Goster(y.YenidenKur());
        }

        /// <summary>Oyunu kapatır. Odadaysa önce ayrılır (yeri boşuna 60 sn tutulmasın).</summary>
        public void OyundanCik()
        {
            if (Online != null && Online.Istemci.Durum != TheLighter.Ag.BaglantiDurumu.Kapali) Online.Istemci.Ayril();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        /// <summary>Bağlantı koptu, kendiliğinden yeniden deneniyor: bütün ekranın üstünde "Bağlanıyor…".</summary>
        public void BaglaniyorGoster(bool acik)
        {
            if (baglaniyor != null) baglaniyor.EnableInClassList("gizli", !acik);
        }

        VisualElement BaglaniyorKatmani()
        {
            var perde = Arayuz.Kutu("menu-perde", "baglaniyor", "gizli");
            var kutu = Arayuz.Kutu("menu-kutu");
            kutu.Add(Arayuz.Yazi(Metin.Al("online.baglaniyor"), "baslik"));
            kutu.Add(Arayuz.Yazi(Metin.Al("online.baglaniyor_ipucu"), "aciklama"));
            kutu.Add(Arayuz.Buton(Metin.Al("online.vazgec"), () => Online.Ayril(), "buton-ikincil"));
            perde.Add(kutu);
            return perde;
        }

        /// <summary>
        /// Kural motorunu kurar ve oyun ekranına geçer. Kur hata verirse ekran değişmez, hata döner.
        /// Temiz modda sadece Aile Dostu paketi ve en fazla Hafif ceza (Ana Plan 8.1, 8.4).
        /// </summary>
        public Sonuc OyunuBaslat(IReadOnlyList<string> isimler, Ayarlar ayarlar, IReadOnlyList<string> paketler)
        {
            var oyuncular = new List<Oyuncu>();
            for (int i = 0; i < isimler.Count; i++)
                oyuncular.Add(new Oyuncu(new OyuncuId(i + 1), isimler[i]));

            bool temiz = ayarlar.Mod == OyunModu.Temiz;
            var secili = temiz ? new List<string> { PaketBilgisi.AileDostu } : new List<string>(paketler ?? new string[0]);
            var sorular = new List<string>();
            foreach (var p in Paketler)
                if (secili.Contains(p.Ad)) sorular.AddRange(p.Sorular);
            if (!temiz && secili.Contains(PaketBilgisi.Bizim))
            {
                var grup = GrupDeposu.Eslesen(isimler);
                if (grup != null) sorular.AddRange(grup.Kayit.GrupPaketi);
            }

            var ceza = ayarlar.CezaSeviyesi;
            if (temiz && ceza == CezaSeviyesi.Cesur) ceza = CezaSeviyesi.Hafif;

            var oyunAyari = new Ayarlar
            {
                Oyun = ayarlar.Oyun,
                SuclamaSuresiSn = ayarlar.SuclamaSuresiSn,
                Can = ayarlar.Can,
                Mod = ayarlar.Mod,
                MiniOyun = ayarlar.MiniOyun,
                TurSayisi = ayarlar.TurSayisi,
                SoruSuresiSn = ayarlar.SoruSuresiSn,
                CevapSuresiSn = ayarlar.CevapSuresiSn,
                HamleSuresiSn = ayarlar.HamleSuresiSn,
                SonucSuresiSn = ayarlar.SonucSuresiSn,
                CezaSeviyesi = ceza,
                GizliGorevler = ayarlar.GizliGorevler,
                KaosTurlari = ayarlar.KaosTurlari,
                SesliCezalar = sesliCezalar,
                // Elden ele: herkes telefonu kimin tuttuğunu görüyor, "Soran Gizli" çıkmasın.
                TekCihaz = true,
                OneriSorulari = sorular,
            };

            var s = EldenEleAkisi.Kur(oyunAyari, oyuncular, Environment.TickCount, out var akis);
            if (s != Sonuc.Tamam) return s;

            SonIsimler = new List<string>(isimler);
            SonAyarlar = ayarlar;
            SonPaketler = new List<string>(paketler ?? new string[0]);
            Goster(new OyunEkrani(this, akis));
            return Sonuc.Tamam;
        }

        public Sonuc TekrarOyna() => OyunuBaslat(SonIsimler, SonAyarlar, SonPaketler);

        /// <summary>
        /// Enes'in Assets/Icerik dosyaları, <see cref="PaketListesi"/> üzerinden: soru paketleri ve sesli cezalar.
        /// Paket yoksa öneri çek "henüz soru paketi yok" der.
        /// </summary>
        void PaketleriYukle()
        {
            Paketler = new List<PaketBilgisi>();
            var liste = Resources.Load<PaketListesi>(PaketListesi.ResourcesYolu);
            if (liste != null)
            {
                foreach (var paket in liste.Paketler)
                {
                    if (paket == null) continue;
                    Paketler.Add(new PaketBilgisi
                    {
                        Ad = paket.name,
                        GorunenAd = PaketBilgisi.BaslikBul(paket.text, paket.name),
                        Sorular = Paket.Ayristir(paket.text),
                    });
                }
                if (liste.SesliCezalar != null) sesliCezalar = Paket.Ayristir(liste.SesliCezalar.text);
            }
            if (Paketler.Count == 0) Debug.LogWarning("[TheLighter] Soru paketi yok. Menü → TheLighter → Soru paketlerini güncelle.");
            if (Paketler.Find(p => p.Ad == PaketBilgisi.Genel) == null && Paketler.Count > 0)
                SonPaketler = new List<string> { Paketler[0].Ad };
        }

        // ---------- Paylaşım kartı ----------

        /// <summary>
        /// Ekranın görüntüsünü PNG olarak kaydeder (paylaşım kartı). <paramref name="gizlenecek"/> (butonlar)
        /// görüntü alınırken gizlenir. PC'de Resimler/TheLighter, telefonda uygulamanın klasörü.
        /// Sonuç: dosya yolu ya da null.
        /// </summary>
        public void EkranGoruntusuKaydet(VisualElement gizlenecek, Action<string> bitti)
        {
            StartCoroutine(GoruntuAl(gizlenecek, bitti));
        }

        IEnumerator GoruntuAl(VisualElement gizlenecek, Action<string> bitti)
        {
            if (gizlenecek != null) gizlenecek.AddToClassList("gizli");
            yield return null;
            yield return new WaitForEndOfFrame();

            string yol = null;
            Texture2D resim = null;
            try
            {
                resim = ScreenCapture.CaptureScreenshotAsTexture();
                byte[] png = resim.EncodeToPNG();
                string klasor = Application.isMobilePlatform
                    ? Path.Combine(Application.persistentDataPath, "Kartlar")
                    : Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                if (string.IsNullOrEmpty(klasor)) klasor = Path.Combine(Application.persistentDataPath, "Kartlar");
                else if (!Application.isMobilePlatform) klasor = Path.Combine(klasor, "TheLighter");
                Directory.CreateDirectory(klasor);
                yol = Path.Combine(klasor, "thelighter-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".png");
                File.WriteAllBytes(yol, png);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[TheLighter] Kart kaydedilemedi (" + e.GetType().Name + ")");
                yol = null;
            }
            finally
            {
                if (resim != null) Destroy(resim);
            }

            if (gizlenecek != null) gizlenecek.RemoveFromClassList("gizli");
            if (bitti != null) bitti(yol);
        }

        // ---------- Ölçek ve çentik ----------

        /// <summary>
        /// Telefonda dikey: genişliğe göre ölçekle. PC'de yatay: yüksekliğe göre ölçekle,
        /// sütun ortada kalır (Ana Plan 6.2). Çentik ve yuvarlak köşeler için safe area kadar iç boşluk.
        /// </summary>
        void OlcekVeGuvenliAlan(bool zorla)
        {
            var alan = Screen.safeArea;
            if (!zorla && alan == sonGuvenliAlan && Screen.width == sonGenislik && Screen.height == sonYukseklik) return;
            sonGuvenliAlan = alan;
            sonGenislik = Screen.width;
            sonYukseklik = Screen.height;
            if (Screen.width <= 0 || Screen.height <= 0) return;

            panel.match = Screen.width > Screen.height ? 1f : 0f;

            // ScaleWithScreenSize + MatchWidthOrHeight formülü: bir panel biriminin kaç piksel olduğu.
            float logG = Mathf.Log(Screen.width / (float)ReferansCozunurluk.x, 2f);
            float logY = Mathf.Log(Screen.height / (float)ReferansCozunurluk.y, 2f);
            float piksel = Mathf.Pow(2f, Mathf.Lerp(logG, logY, panel.match));
            if (piksel <= 0f) return;

            kok.style.paddingLeft = alan.xMin / piksel;
            kok.style.paddingRight = (Screen.width - alan.xMax) / piksel;
            kok.style.paddingTop = (Screen.height - alan.yMax) / piksel;
            kok.style.paddingBottom = alan.yMin / piksel;

            bool simdiGenis = alan.width / piksel >= GenisEsik;
            if (simdiGenis != genis)
            {
                genis = simdiGenis;
                kok.EnableInClassList("genis", genis);
                YanlariKur();
            }
        }
    }
}
