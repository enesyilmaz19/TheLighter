using System;
using System.Collections.Generic;

namespace Cakmak.Kurallar
{
    // Gizli kişili oyunların ortak "kim?" oylaması (Köstebek, Casus; docs/Oyunlar.md 1 ve 4). Aralıkla ve oyun biterken
    // son kez yapılır. En çok oyu gizli kişi alırsa ya da gizli kişi kaçarsa oyunun kuralı işler (Oyun.Kancalar.cs);
    // almazsa yanlış suçlanan şüpheli olur, oyun sürer. Oy sayımı (EnCokOyAlanlar) Vampir'in linci ve gecesiyle ortak.
    public sealed partial class Oyun
    {
        /// <summary>Gizli kişi: köstebek ya da casus.</summary>
        OyuncuId hain;
        readonly List<OyuncuId> supheliler = new List<OyuncuId>();
        readonly Dictionary<OyuncuId, OyuncuId> suclamaOylari = new Dictionary<OyuncuId, OyuncuId>();
        bool sonSuclama;

        bool Suclamali => Kostebekli || Casuslu;

        /// <summary>"Kim?" oyu: oyundan çıkmamış herkes bir kez, kendisi dışında oyundaki birine.</summary>
        public Sonuc SuclamaOyu(OyuncuId kim, OyuncuId hedef)
        {
            if (asama != Asama.Suclama) return Sonuc.YanlisAsama;
            if (!oylayanlar.Contains(kim)) return Sonuc.SiraSendeDegil;
            if (suclamaOylari.ContainsKey(kim)) return Sonuc.HakKalmadi;
            if (hedef == kim || Bul(hedef) == null || cikan.Contains(hedef)) return Sonuc.Secilemez;

            suclamaOylari[kim] = hedef;
            Yay(new Olay { Tur = OlayTuru.OyVerildi, Kim = kim });
            if (suclamaOylari.Count == oylayanlar.Count) SuclamayiBitir();
            return Sonuc.Tamam;
        }

        /// <summary>Yeni tur başladı: aralıkla oylama (Köstebek 5, Casus 4 turda bir).</summary>
        void PeriyodikSuclama()
        {
            if (!Suclamali || tur <= 1) return;
            int aralik = Casuslu ? CasusSuclamaAraligi : SuclamaAraligi;
            if ((tur - 1) % aralik == 0) SuclamayiBaslat(null);
        }

        /// <summary>
        /// Oyun bitecek (tur sayısı doldu ya da kurucu bitirdi): önce son oylama. Kurucu da gizli kişi olabilir:
        /// oyunu kendisi bitirip kazanamasın. Görevini bu turda bitirmiş köstebek oylamaya kalmadan kazanır.
        /// true: oylama başladı ya da oyun bitti.
        /// </summary>
        bool SonSuclamayiBaslat()
        {
            if (!Suclamali || sonSuclama) return false;
            if (KostebekGoreviBitti()) return true;
            SoruyuKapat();
            sonSuclama = true;
            SuclamayiBaslat(null);
            return true;
        }

        void SuclamayiBaslat(OyuncuId? suclayan)
        {
            oylayanlar.Clear();
            suclamaOylari.Clear();
            foreach (var o in oyuncular)
                if (!cikan.Contains(o.Id)) oylayanlar.Add(o.Id);
            AsamayaGec(Asama.Suclama, ayar.SuclamaSuresiSn);
            Yay(new Olay { Tur = OlayTuru.SuclamaBasladi, Kim = suclayan });
        }

        /// <summary>En çok oyu alan suçlanır; eşitlikte ya da hiç oy yoksa kimse.</summary>
        void SuclamayiBitir()
        {
            var enCok = EnCokOyAlanlar(suclamaOylari.Values);
            OyuncuId? suclanan = enCok.Count == 1 ? enCok[0] : (OyuncuId?)null;
            suclamaOylari.Clear();
            oylayanlar.Clear();
            Yay(new Olay { Tur = OlayTuru.SuclamaBitti, Kim = suclanan });

            if (suclanan == hain) { HainYakalandi(); return; }
            if (suclanan.HasValue && !supheliler.Contains(suclanan.Value)) supheliler.Add(suclanan.Value);
            if (sonSuclama) { HainYakalanmadi(); return; }

            // A'nın turu baştan. A bu arada oyundan çıktıysa, soru sormadan çıkan A gibi turu yanar.
            if (cikan.Contains(a))
            {
                OncekiTuruUnut();
                TurBaslat(RastgeleOyuncu(a));
            }
            else AsamayaGec(Asama.SoruSorma, ayar.SoruSuresiSn);
        }

        /// <summary>En çok oyu alan(lar), oy sırasıyla. Hiç oy yoksa boş; birden çoksa eşitlik.</summary>
        static List<OyuncuId> EnCokOyAlanlar(IEnumerable<OyuncuId> oylar)
        {
            var sayilar = new Dictionary<OyuncuId, int>();
            foreach (var hedef in oylar) Artir(sayilar, hedef);
            int enCok = 0;
            foreach (var s in sayilar) enCok = Math.Max(enCok, s.Value);
            var sonuc = new List<OyuncuId>();
            foreach (var s in sayilar)
                if (s.Value == enCok) sonuc.Add(s.Key);
            return sonuc;
        }

        /// <summary>Oyundan çıkan gizli kişiyse grup kazanır. true: oyun bitti.</summary>
        bool HainCikti(OyuncuId kim)
        {
            if (!Suclamali || kim != hain) return false;
            HainKacti();
            return true;
        }

        void SuclamaGorunumu(Gorunum g, OyuncuId kim)
        {
            if (!Suclamali) return;
            g.Supheliler = new List<OyuncuId>(supheliler);
            if (asama != Asama.Suclama) return;
            g.Oylayanlar = new List<OyuncuId>(oylayanlar);
            g.OyVerdin = suclamaOylari.ContainsKey(kim);
            g.OyKullanan = suclamaOylari.Count;
            g.SonSuclama = sonSuclama;
        }
    }
}
