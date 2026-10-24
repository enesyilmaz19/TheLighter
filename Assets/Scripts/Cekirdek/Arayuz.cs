using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace TheLighter.Istemci
{
    /// <summary>UI Toolkit öğelerini kısa yoldan kurmak için. Görünüş Stil.uss'te, burada sadece sınıf adları.</summary>
    public static class Arayuz
    {
        static readonly Color32[] OyuncuRenkleri =
        {
            new Color32(0xFF, 0x6B, 0x6B, 0xFF),
            new Color32(0x4E, 0xCD, 0xC4, 0xFF),
            new Color32(0xFF, 0xD1, 0x66, 0xFF),
            new Color32(0x6A, 0x8D, 0xFF, 0xFF),
            new Color32(0xC7, 0x7D, 0xFF, 0xFF),
            new Color32(0x06, 0xD6, 0xA0, 0xFF),
            new Color32(0xFF, 0x9F, 0x1C, 0xFF),
            new Color32(0xF1, 0x5B, 0xB5, 0xFF),
            new Color32(0x8A, 0xC9, 0x26, 0xFF),
            new Color32(0x00, 0xBB, 0xF9, 0xFF),
        };

        public static VisualElement Kutu(params string[] siniflar)
        {
            var v = new VisualElement();
            foreach (var s in siniflar) v.AddToClassList(s);
            Canlandir(v);
            return v;
        }

        // ---------- Animasyon (USS geçişleriyle; kütüphane yok) ----------

        /// <summary>
        /// Sınıfına göre kendiliğinden oynayan animasyonlar: katman (menu-perde) her açılışta solup büyüyerek gelir,
        /// ifşa kartı patlayarak, güme kartı dumanla. Görünüş Stil.uss'te (".acilis", ".giris").
        /// </summary>
        static void Canlandir(VisualElement v)
        {
            if (v.ClassListContains("menu-perde"))
            {
                // Gizliyken (display: none) boyutu sıfır; göründüğü an animasyon başlar.
                v.RegisterCallback<GeometryChangedEvent>(e =>
                {
                    if (e.oldRect.width <= 0f && e.newRect.width > 0f) Belir(v, "acilis");
                });
            }
            if (v.ClassListContains("ifsa-karti") || v.ClassListContains("gume-karti")) Belir(v);
        }

        /// <summary>
        /// Giriş animasyonu: sınıf hemen eklenir, birkaç kare sonra kalkar; USS'teki geçiş başlangıçtan son hâline oynar.
        /// Öğe henüz ekranda değilse eklenince başlar.
        /// </summary>
        public static void Belir(VisualElement e, string sinif = "giris")
        {
            e.AddToClassList(sinif);
            e.schedule.Execute(() => e.RemoveFromClassList(sinif)).StartingIn(40);
        }

        /// <summary>Sınıfı belli aralıkla açıp kapatır (nabız, titreşim). Öğe ekrandan çıkınca kendiliğinden durur.</summary>
        public static void Nabiz(VisualElement e, string sinif, long ms)
        {
            e.schedule.Execute(() => e.ToggleInClassList(sinif)).Every(ms);
        }

        /// <summary>
        /// Titreyen alev: logonun üstündeki alev resmi, arkasında hale. Ana menüde ve kabul ekranında.
        /// </summary>
        public static VisualElement Alev(string sinif = "logo-alev")
        {
            var kap = Kutu(sinif);
            kap.pickingMode = PickingMode.Ignore;
            var hale = Kutu("alev-hale");
            hale.pickingMode = PickingMode.Ignore;
            var resim = Kutu("alev-resim");
            resim.pickingMode = PickingMode.Ignore;
            kap.Add(hale);
            kap.Add(resim);
            var r = new System.Random();
            resim.schedule.Execute(() =>
            {
                float s = 0.95f + (float)r.NextDouble() * 0.1f;
                resim.style.scale = new Scale(new Vector3(s, 0.96f + (float)r.NextDouble() * 0.1f, 1f));
                resim.style.rotate = new Rotate(new Angle(-3f + (float)r.NextDouble() * 6f, AngleUnit.Degree));
                hale.style.opacity = 0.55f + (float)r.NextDouble() * 0.45f;
            }).Every(140);
            return kap;
        }

        public static Label Yazi(string metin, params string[] siniflar)
        {
            var l = new Label(metin);
            // Oyuncunun yazdığı isim ve soru da etiketlere düşer: "<size=500>" herkesin ekranını bozmasın (Guvenlik.md).
            l.enableRichText = false;
            foreach (var s in siniflar) l.AddToClassList(s);
            return l;
        }

        public static Button Buton(string metin, Action tik, params string[] siniflar)
        {
            var b = new Button(tik) { text = metin, enableRichText = false };
            b.AddToClassList("buton");
            foreach (var s in siniflar) b.AddToClassList(s);
            return b;
        }

        /// <summary>Kalan boşluğu dolduran öğe.</summary>
        public static VisualElement Esnek() => Kutu("esnek");

        public static Color OyuncuRengi(int renk)
        {
            int n = OyuncuRenkleri.Length;
            return OyuncuRenkleri[((renk % n) + n) % n];
        }

        public static VisualElement RenkNoktasi(int renk, string sinif = "renk-noktasi")
        {
            var v = Kutu(sinif);
            v.style.backgroundColor = OyuncuRengi(renk);
            v.pickingMode = PickingMode.Ignore;
            if (sinif != "perde-renk") return v;

            // Perdedeki büyük renk: "telefon sende" diye atar, arkasında kendi renginde bir hale.
            var kap = Kutu("perde-renk-kap");
            kap.pickingMode = PickingMode.Ignore;
            var hale = Kutu("perde-hale");
            hale.pickingMode = PickingMode.Ignore;
            hale.style.unityBackgroundImageTintColor = OyuncuRengi(renk);
            kap.Add(hale);
            kap.Add(v);
            Nabiz(v, "nabiz", 900);
            Nabiz(hale, "nabiz", 900);
            return kap;
        }

        /// <summary>Oyuncu seçme çipi: renk noktası + isim.</summary>
        public static Button OyuncuCipi(string isim, int renk, Action tik)
        {
            var b = Buton("", tik, "cip");
            b.Add(RenkNoktasi(renk));
            var y = Yazi(isim, "cip-yazi");
            y.pickingMode = PickingMode.Ignore;
            b.Add(y);
            return b;
        }

        /// <summary>
        /// Katman (menu-perde) geri tuşuyla kapatılınca sadece gizlenmesin, bu çalışsın (ör. oyun menüsü süreyi yeniden başlatır).
        /// </summary>
        public static void Kapaninca(VisualElement katman, Action kapat) => katman.userData = kapat;

        /// <summary>Geri tuşu bu katmanı kapatmaz (AFK, "oyuncu bekleniyor": durumdan açılıp kapanırlar).</summary>
        public static void Kapanmaz(VisualElement katman) => katman.AddToClassList("kapanmaz");

        /// <summary>
        /// Uzun basınca (yarım saniye, parmak kaymadan): sohbet satırında ve oyuncu listesinde "Engelle / Raporla" için.
        /// Kısa dokunuş öğenin kendi işini yapar.
        /// </summary>
        public static void UzunBas(VisualElement e, Action eylem, long ms = 500)
        {
            IVisualElementScheduledItem bekleyen = null;
            Vector2 baslangic = default;
            e.RegisterCallback<PointerDownEvent>(ev =>
            {
                // Satırdaki bir düğmeye basılıyorsa (at, …) o düğmenin işi: uzun basma sayılmaz.
                if (ev.target is VisualElement hedef && hedef != e && hedef.GetFirstOfType<Button>() != null) return;
                baslangic = ev.position;
                bekleyen?.Pause();
                bekleyen = e.schedule.Execute(() => { bekleyen = null; eylem(); }).StartingIn(ms);
            });
            e.RegisterCallback<PointerMoveEvent>(ev =>
            {
                if (bekleyen != null && ((Vector2)ev.position - baslangic).sqrMagnitude > 40f * 40f)
                {
                    bekleyen.Pause();
                    bekleyen = null;
                }
            });
            EventCallback<PointerUpEvent> birak = _ => { bekleyen?.Pause(); bekleyen = null; };
            e.RegisterCallback(birak);
            e.RegisterCallback<PointerLeaveEvent>(_ => { bekleyen?.Pause(); bekleyen = null; });
            e.RegisterCallback<PointerCancelEvent>(_ => { bekleyen?.Pause(); bekleyen = null; });
        }

        /// <summary>
        /// Butonu kısa bir süre kapalı tutar. Önceki ekranda art arda basılan bir parmak
        /// perdeyi yanlışlıkla kaldırmasın diye.
        /// </summary>
        public static void GecikmeliAc(VisualElement e, long ms = 800)
        {
            e.SetEnabled(false);
            e.schedule.Execute(() => e.SetEnabled(true)).StartingIn(ms);
        }

        /// <summary>Kalan süre, yukarı yuvarlanmış saniye.</summary>
        public static string Sure(int kalanMs)
        {
            return Mathf.CeilToInt(Mathf.Max(0, kalanMs) / 1000f).ToString();
        }

        public static void SureyiYaz(Label etiket, int kalanMs, int azEsikMs = 5000)
        {
            etiket.text = Sure(kalanMs);
            bool az = kalanMs <= azEsikMs;
            etiket.EnableInClassList("sure-az", az);
            // Süre azken saniyede iki kez büyüyüp küçülür.
            etiket.EnableInClassList("sure-nabiz", az && kalanMs > 0 && (Environment.TickCount / 250) % 2 == 0);
        }
    }
}
