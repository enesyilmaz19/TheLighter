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
            return v;
        }

        public static Label Yazi(string metin, params string[] siniflar)
        {
            var l = new Label(metin);
            foreach (var s in siniflar) l.AddToClassList(s);
            return l;
        }

        public static Button Buton(string metin, Action tik, params string[] siniflar)
        {
            var b = new Button(tik) { text = metin };
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
            return v;
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
            etiket.EnableInClassList("sure-az", kalanMs <= azEsikMs);
        }
    }
}
