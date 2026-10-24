using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace TheLighter.Istemci
{
    /// <summary>
    /// Arka planda ağır ağır yükselen kıvılcımlar (süs). Ekranların arkasında durur, dokunmaya kapalı, düzeni etkilemez
    /// (sadece translate ve opacity değişir). Gece ekranlarında ve oyunda sakinleşir (<see cref="Sakin"/>).
    /// </summary>
    public sealed class Kivilcimlar
    {
        sealed class Kivilcim
        {
            public VisualElement Oge;
            public float X, Y, Hiz, Salinim, Faz, Omur, Yas, Parlaklik;
        }

        static readonly Color[] Renkler =
        {
            new Color32(0xFF, 0x8A, 0x1F, 0xFF), new Color32(0xFF, 0xB2, 0x4E, 0xFF), new Color32(0xFF, 0xD1, 0x66, 0xFF),
            new Color32(0xFF, 0x6B, 0x3D, 0xFF),
        };

        readonly List<Kivilcim> kivilcimlar = new List<Kivilcim>();
        readonly System.Random r = new System.Random();
        bool sakin;
        float kutlama;

        public VisualElement Kok { get; }

        public Kivilcimlar(int adet)
        {
            Kok = new VisualElement();
            Kok.AddToClassList("kivilcimlar");
            Kok.pickingMode = PickingMode.Ignore;
            for (int i = 0; i < adet; i++)
            {
                var k = new Kivilcim { Oge = new VisualElement() };
                k.Oge.AddToClassList("kivilcim");
                k.Oge.pickingMode = PickingMode.Ignore;
                float boy = 8f + (float)r.NextDouble() * 14f;
                k.Oge.style.width = boy;
                k.Oge.style.height = boy;
                k.Oge.style.borderTopLeftRadius = k.Oge.style.borderTopRightRadius =
                    k.Oge.style.borderBottomLeftRadius = k.Oge.style.borderBottomRightRadius = boy / 2f;
                k.Oge.style.backgroundColor = Renkler[r.Next(Renkler.Length)];
                Yenile(k, ilk: true);
                kivilcimlar.Add(k);
                Kok.Add(k.Oge);
            }
            Kok.schedule.Execute(t => Ilerle(t.deltaTime / 1000f)).Every(33);
        }

        /// <summary>Oyun ve gece ekranlarında daha az ve daha soluk: dikkat dağıtmasın.</summary>
        public bool Sakin
        {
            get => sakin;
            set
            {
                sakin = value;
                Kok.EnableInClassList("kivilcimlar-sakin", value);
            }
        }

        /// <summary>Oyun sonu: birkaç saniye kıvılcımlar hızlanır ve parlar (sakin olsa da).</summary>
        public void Kutla(float sn = 3.5f)
        {
            kutlama = sn;
            Kok.RemoveFromClassList("kivilcimlar-sakin");
        }

        void Yenile(Kivilcim k, bool ilk)
        {
            k.X = (float)r.NextDouble();
            k.Y = ilk ? (float)r.NextDouble() : 1.05f;
            k.Hiz = 0.03f + (float)r.NextDouble() * 0.05f;   // ekran yüksekliği / sn
            k.Salinim = 0.6f + (float)r.NextDouble() * 1.2f;
            k.Faz = (float)(r.NextDouble() * Math.PI * 2);
            k.Omur = 6f + (float)r.NextDouble() * 8f;
            k.Yas = ilk ? (float)r.NextDouble() * k.Omur : 0f;
            k.Parlaklik = 0.35f + (float)r.NextDouble() * 0.55f;
        }

        void Ilerle(float dt)
        {
            float w = Kok.resolvedStyle.width, h = Kok.resolvedStyle.height;
            if (float.IsNaN(w) || float.IsNaN(h) || w <= 0 || h <= 0) return;
            dt = Mathf.Clamp(dt, 0f, 0.1f);
            float hiz = 1f;
            if (kutlama > 0f)
            {
                kutlama -= dt;
                hiz = 1f + 5f * Mathf.Clamp01(kutlama / 1.5f);
                if (kutlama <= 0f) Kok.EnableInClassList("kivilcimlar-sakin", sakin);
            }
            foreach (var k in kivilcimlar)
            {
                k.Yas += dt * hiz;
                k.Y -= k.Hiz * dt * hiz;
                if (k.Yas >= k.Omur || k.Y < -0.05f) Yenile(k, ilk: false);
                float salinim = Mathf.Sin(k.Faz + k.Yas * k.Salinim) * 0.02f;
                k.Oge.style.translate = new Translate((k.X + salinim) * w, k.Y * h);
                // Doğarken belirir, ölürken söner.
                float hayat = Mathf.Sin(Mathf.PI * Mathf.Clamp01(k.Yas / k.Omur));
                k.Oge.style.opacity = hayat * k.Parlaklik;
            }
        }
    }
}
