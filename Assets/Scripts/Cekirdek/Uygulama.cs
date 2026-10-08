using System;
using System.Collections.Generic;
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

        static Uygulama ornek;

        PanelSettings panel;
        UIDocument belge;
        VisualElement kok;
        VisualElement sahne;
        IEkran ekran;
        Rect sonGuvenliAlan;
        int sonGenislik;
        int sonYukseklik;

        /// <summary>"Tekrar oyna" ve kurulum ekranı için son kullanılan isimler ve ayarlar.</summary>
        public List<string> SonIsimler { get; private set; } = new List<string>();

        public Ayarlar SonAyarlar { get; private set; } = new Ayarlar();

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

            Metin.Yukle();
            ArayuzuKur();
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
            sahne = Arayuz.Kutu("sahne");
            kok.Add(sahne);
            root.Add(kok);
            OlcekVeGuvenliAlan(true);
        }

        void Update()
        {
            OlcekVeGuvenliAlan(false);
            if (ekran != null) ekran.Guncelle(Time.deltaTime);
        }

        void OnApplicationPause(bool duraklatildi)
        {
            if (duraklatildi && ekran != null) ekran.ArkaPlanaGecti();
        }

        // ---------- Gezinme ----------

        public void Goster(IEkran yeni)
        {
            sahne.Clear();
            ekran = yeni;
            sahne.Add(yeni.Kok);
        }

        public void AnaMenu() => Goster(new AnaMenuEkrani(this));

        public void Kurulum() => Goster(new KurulumEkrani(this));

        /// <summary>Kural motorunu kurar ve oyun ekranına geçer. Kur hata verirse ekran değişmez, hata döner.</summary>
        public Sonuc OyunuBaslat(IReadOnlyList<string> isimler, Ayarlar ayarlar)
        {
            var oyuncular = new List<Oyuncu>();
            for (int i = 0; i < isimler.Count; i++)
                oyuncular.Add(new Oyuncu(new OyuncuId(i + 1), isimler[i]));

            var oyunAyari = new Ayarlar
            {
                Mod = OyunModu.Normal,
                MiniOyun = ayarlar.MiniOyun,
                TurSayisi = ayarlar.TurSayisi,
                SoruSuresiSn = ayarlar.SoruSuresiSn,
                CevapSuresiSn = ayarlar.CevapSuresiSn,
                HamleSuresiSn = ayarlar.HamleSuresiSn,
                SonucSuresiSn = ayarlar.SonucSuresiSn,
                OneriSorulari = OneriSorulariniYukle(),
            };

            var s = EldenEleAkisi.Kur(oyunAyari, oyuncular, Environment.TickCount, out var akis);
            if (s != Sonuc.Tamam) return s;

            SonIsimler = new List<string>(isimler);
            SonAyarlar = ayarlar;
            Goster(new OyunEkrani(this, akis));
            return Sonuc.Tamam;
        }

        /// <summary>
        /// "Öneri çek" için soru paketleri: Enes'in Assets/Icerik/*.txt dosyaları, <see cref="PaketListesi"/> üzerinden.
        /// Paket yoksa öneri çek "henüz soru paketi yok" der.
        /// </summary>
        static List<string> OneriSorulariniYukle()
        {
            var sorular = new List<string>();
            var liste = Resources.Load<PaketListesi>(PaketListesi.ResourcesYolu);
            if (liste != null)
                foreach (var paket in liste.Paketler)
                    if (paket != null) sorular.AddRange(Paket.Ayristir(paket.text));
            if (sorular.Count == 0) Debug.LogWarning("[TheLighter] Soru paketi yok. Menü → TheLighter → Soru paketlerini güncelle.");
            return sorular;
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
        }
    }
}
