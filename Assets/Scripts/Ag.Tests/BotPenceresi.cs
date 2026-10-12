using System;
using System.Collections.Generic;
using System.Diagnostics;
using TheLighter.Ag;
using UnityEditor;
using UnityEngine;

namespace TheLighter.Testler
{
    /// <summary>
    /// Sadece editörde, deneme için: menü → TheLighter → Online deneme botları.
    /// Play'de online oda kur, kodu buraya yaz, "Katıl": botlar odaya girer ve sıraları gelince oynar.
    /// Böylece tek kişi gerçek ekranlardan 4 kişilik online oyunu deneyebilir. Yerel sunucu gerekir.
    /// </summary>
    public sealed class BotPenceresi : EditorWindow
    {
        string adres = "ws://localhost:5000/ws";
        string oda = "";
        int sayi = 3;
        readonly List<DenemeBotu> botlar = new List<DenemeBotu>();
        readonly Stopwatch saat = Stopwatch.StartNew();
        double son;

        [MenuItem("TheLighter/Online deneme botları")]
        static void Ac() => GetWindow<BotPenceresi>("Deneme botları");

        void OnEnable() => EditorApplication.update += Guncelle;
        void OnDisable()
        {
            EditorApplication.update -= Guncelle;
            Cikar();
        }

        void Guncelle()
        {
            double simdi = saat.Elapsed.TotalSeconds;
            double g = simdi - son;
            son = simdi;
            foreach (var b in botlar) b.Guncelle(g);
            if (botlar.Count > 0) Repaint();
        }

        void OnGUI()
        {
            adres = EditorGUILayout.TextField("Sunucu", adres);
            oda = EditorGUILayout.TextField("Oda kodu", oda).ToUpperInvariant();
            sayi = EditorGUILayout.IntSlider("Bot sayısı", sayi, 1, 9);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.enabled = oda.Trim().Length == 5;
                if (GUILayout.Button("Katıl")) Katil();
                GUI.enabled = botlar.Count > 0;
                if (GUILayout.Button("Hepsini çıkar")) Cikar();
                GUI.enabled = true;
            }

            foreach (var b in botlar)
            {
                var d = b.Istemci.SonDurum;
                string ne = d == null ? "-" : d.Yer + (d.Gorunum != null ? " " + d.Gorunum.Asama + " tur " + d.Gorunum.Tur : "");
                EditorGUILayout.LabelField(b.Isim, b.Istemci.Durum + "  " + ne + (b.SonHata != null ? "  son hata: " + b.SonHata : ""));
            }
        }

        void Katil()
        {
            if (!Uri.TryCreate(adres, UriKind.Absolute, out var uri)) return;
            for (int i = 0; i < sayi; i++)
            {
                var b = new DenemeBotu("Bot" + (botlar.Count + 1), Environment.TickCount + i);
                b.Istemci.Katil(uri, oda.Trim(), b.Isim);
                botlar.Add(b);
            }
        }

        void Cikar()
        {
            foreach (var b in botlar) b.Istemci.Ayril();
            botlar.Clear();
        }
    }
}
