using System;
using System.Collections.Generic;
using System.IO;
using Cakmak.Kurallar;
using UnityEngine;

namespace TheLighter.Istemci
{
    /// <summary>Cihazdaki bir grup kaydı ve dosyası.</summary>
    public sealed class GrupDosyasi
    {
        public string Yol;
        public GrupKaydi Kayit;
    }

    /// <summary>
    /// Grup hafızası cihazda (Ana Plan 8.5): her grup Application.persistentDataPath/Gruplar altında bir JSON.
    /// Yazılan şey kural motorunun <see cref="GrupKaydi"/>'dır; içine sadece özet girer, güme giden soru hiç gelmez.
    /// Hesap yok. Grup, bugünkü oyuncuların isimleriyle eşlenir.
    /// </summary>
    public static class GrupDeposu
    {
        /// <summary>Eşleşme için bugünkü oyunculardan en az bu kadarı grupta olmalı (ve en az yarısı).</summary>
        const int EnAzOrtak = 3;

        static string Klasor => Path.Combine(Application.persistentDataPath, "Gruplar");

        public static List<GrupDosyasi> Hepsi()
        {
            var liste = new List<GrupDosyasi>();
            try
            {
                if (!Directory.Exists(Klasor)) return liste;
                foreach (var yol in Directory.GetFiles(Klasor, "*.json"))
                {
                    try
                    {
                        var kayit = JsonUtility.FromJson<GrupKaydi>(File.ReadAllText(yol));
                        if (kayit != null) liste.Add(new GrupDosyasi { Yol = yol, Kayit = kayit });
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning("[TheLighter] Grup kaydı okunamadı: " + Path.GetFileName(yol) + " (" + e.GetType().Name + ")");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[TheLighter] Grup klasörü okunamadı (" + e.GetType().Name + ")");
            }
            return liste;
        }

        /// <summary>
        /// Bugünkü oyunculara en çok benzeyen grup: en az 3 kişi ve bugünkülerin en az yarısı ortak olmalı.
        /// ponytail: aynı isim = aynı kişi (Ana Plan 8.5). Bir iki kişi eksik ya da fazla gelse de grup tanınır.
        /// </summary>
        public static GrupDosyasi Eslesen(IEnumerable<string> isimler, List<GrupDosyasi> gruplar)
        {
            var bugun = new HashSet<string>();
            foreach (var i in isimler)
                if (!string.IsNullOrWhiteSpace(i)) bugun.Add(Oyuncu.IsimAnahtari(i));
            if (bugun.Count == 0 || gruplar == null) return null;

            GrupDosyasi en = null;
            int enIyi = 0;
            foreach (var g in gruplar)
            {
                int ortak = 0;
                foreach (var u in g.Kayit.Uyeler)
                    if (bugun.Contains(Oyuncu.IsimAnahtari(u.Isim))) ortak++;
                if (ortak < EnAzOrtak || ortak * 2 < bugun.Count) continue;
                if (ortak > enIyi || (ortak == enIyi && en != null && g.Kayit.OyunSayisi > en.Kayit.OyunSayisi))
                {
                    en = g;
                    enIyi = ortak;
                }
            }
            return en;
        }

        public static GrupDosyasi Eslesen(IEnumerable<string> isimler) => Eslesen(isimler, Hepsi());

        public static GrupDosyasi Yeni(string ad)
        {
            string dosya = DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N").Substring(0, 6) + ".json";
            return new GrupDosyasi { Yol = Path.Combine(Klasor, dosya), Kayit = new GrupKaydi { Ad = ad ?? "" } };
        }

        /// <summary>Biten oyunu gruba işler (<see cref="GrupKaydi.Isle"/>) ve dosyaya yazar.</summary>
        public static bool Isle(GrupDosyasi grup, OyunOzeti ozet)
        {
            if (grup == null || ozet == null) return false;
            var yeni = GrupKaydi.Isle(grup.Kayit, ozet);
            if (!Yaz(grup.Yol, yeni)) return false;
            grup.Kayit = yeni;
            return true;
        }

        static bool Yaz(string yol, GrupKaydi kayit)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(yol));
                string gecici = yol + ".tmp";
                File.WriteAllText(gecici, JsonUtility.ToJson(kayit));
                if (File.Exists(yol)) File.Delete(yol);
                File.Move(gecici, yol);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[TheLighter] Grup kaydı yazılamadı (" + e.GetType().Name + ")");
                return false;
            }
        }
    }
}
