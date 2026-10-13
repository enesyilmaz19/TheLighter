using System;
using System.Collections.Generic;

namespace Cakmak.Kurallar
{
    // Rus Ruleti (docs/Oyunlar.md, 2): çakmak bir tabanca. Turlar normal çakmak; mini oyunu kaybeden, sonuç ekranından
    // sonra tetiği çeker ya da (oyun boyu bir kez) devreder. Patlarsa bir can gider, can biterse elenir ve izler.
    // 3 kişi kalınca final: sorusuz ikili düellolar. Son kalan kazanır. Ceza, görev ve kaos bu oyunda kapalı.

    /// <summary>Oyun sonunda: kazanan ve elenme sırası.</summary>
    public sealed class RusRuletiSonucu
    {
        /// <summary>Son kalan. Oyun erken bittiyse en çok canı olan; eşitlikte ya da kimse kalmadıysa null.</summary>
        public OyuncuId? Kazanan;
        /// <summary>Elenenler, elendikleri sırayla.</summary>
        public IReadOnlyList<OyuncuId> ElenmeSirasi = Array.Empty<OyuncuId>();

        internal RusRuletiSonucu Kopya() => new RusRuletiSonucu { Kazanan = Kazanan, ElenmeSirasi = new List<OyuncuId>(ElenmeSirasi) };
    }

    public sealed partial class Oyun
    {
        public const int HazneSayisi = 6;
        public const int EnFazlaCan = 3;
        /// <summary>Bu kadar (ya da daha az) oyuncu kalınca çakmak turu biter, final başlar.</summary>
        public const int FinalSiniri = 3;

        /// <summary>Merminin haznesi (0..5). Hiçbir görünümde yok, sadece ısı ipucu.</summary>
        int mermi;
        int bosalan;
        /// <summary>Isıya her tetikte -1/0/+1 gürültü: mermi ısının değişiminden hesaplanamasın.</summary>
        int tabancaRuhu;
        OyuncuId? tetikCeken;
        /// <summary>Tetikten sonra soruyu soracak (sonuç ekranındaki C, ya da Yön Değişti'deki rastgele).</summary>
        OyuncuId sonrakiA;
        bool final;
        readonly List<OyuncuId> elenenler = new List<OyuncuId>();
        RusRuletiSonucu rusRuletiSonucu;

        bool RusRuletli => ayar.Oyun == OyunTuru.RusRuleti;

        // ------------------------------------------------------------ Komutlar

        public Sonuc TetikCek(OyuncuId kim)
        {
            if (asama != Asama.Tetik) return Sonuc.YanlisAsama;
            if (kim != tetikCeken) return Sonuc.SiraSendeDegil;
            TetigiCek();
            return Sonuc.Tamam;
        }

        /// <summary>Oyun boyu 1 kez: çekeceğin tetiği oyundaki aktif birine ver, o çeker (o da hakkı varsa devredebilir).</summary>
        public Sonuc TetikDevret(OyuncuId kim, OyuncuId hedef)
        {
            if (asama != Asama.Tetik) return Sonuc.YanlisAsama;
            if (kim != tetikCeken) return Sonuc.SiraSendeDegil;
            var o = Bul(kim);
            if (!o.DevretmeHakki) return Sonuc.HakKalmadi;
            if (hedef == kim || Bul(hedef) == null || !Aktif(hedef)) return Sonuc.Secilemez;

            o.DevretmeHakki = false;
            tetikCeken = hedef;
            Yay(new Olay { Tur = OlayTuru.TetikDevredildi, Kim = kim, Hedef = hedef });
            kalanMs = ayar.HamleSuresiSn * 1000.0; // devralanın karar süresi
            return Sonuc.Tamam;
        }

        // ------------------------------------------------------------ Akış

        void RusRuletiKur()
        {
            if (!RusRuletli) return;
            foreach (var o in oyuncular)
            {
                o.Can = ayar.Can;
                o.DevretmeHakki = true;
            }
            TabancayiDoldur();
        }

        void TabancayiDoldur()
        {
            mermi = rastgele.Next(HazneSayisi);
            bosalan = 0;
            tabancaRuhu = 0;
        }

        /// <summary>Elenmemiş ve oyundan çıkmamış olanlar.</summary>
        List<OyuncuId> Oynayanlar()
        {
            var liste = new List<OyuncuId>();
            foreach (var o in oyuncular)
                if (!o.Elendi && !cikan.Contains(o.Id)) liste.Add(o.Id);
            return liste;
        }

        /// <summary>Tur sonucu belli (TuruBitir): düelloyu kaybeden, sonuç ekranından sonra tetiği çekecek.</summary>
        void RusRuletiTurSonu(OyuncuId? kaybeden)
        {
            if (RusRuletli) tetikCeken = kaybeden;
        }

        /// <summary>Sonuç ekranı bitti: tetik bekliyorsa Tetik aşaması başlar. true: başladı (ya da tetiksiz geçildi).</summary>
        bool TetikBekliyor(OyuncuId sonraki)
        {
            if (!RusRuletli || !tetikCeken.HasValue) return false;
            sonrakiA = sonraki;
            SoruyuKapat();
            TetigeGec();
            return true;
        }

        void TetigeGec()
        {
            // Kaybeden bu arada oyundan çıktıysa tetik çekilmez.
            if (cikan.Contains(tetikCeken.Value)) { TetikSonrasi(); return; }
            AsamayaGec(Asama.Tetik, ayar.HamleSuresiSn);
            // Aşama değişikliği olaysız kalmasın: sunucu yeni durumu olaydan sonra yollar.
            Yay(new Olay { Tur = OlayTuru.TetikBasladi, Kim = tetikCeken });
        }

        void TetigiCek()
        {
            var o = Bul(tetikCeken.Value);
            if (bosalan == mermi)
            {
                o.Can--;
                Yay(new Olay { Tur = OlayTuru.TetikPatladi, Kim = o.Id });
                TabancayiDoldur();
                if (o.Can <= 0)
                {
                    o.Elendi = true;
                    elenenler.Add(o.Id);
                    Yay(new Olay { Tur = OlayTuru.Elendi, Kim = o.Id });
                }
            }
            else
            {
                bosalan++;
                tabancaRuhu = rastgele.Next(-1, 2);
                Yay(new Olay { Tur = OlayTuru.TetikBos, Kim = o.Id });
            }
            TetikSonrasi();
        }

        void TetikSonrasi()
        {
            tetikCeken = null;
            if (final) { FinalTuru(); return; }
            TurBaslat(Oynayanlar().Contains(sonrakiA) ? sonrakiA : RastgeleOyuncu(sonrakiA));
        }

        /// <summary>Yeni tur başlamadan: son kalan kazanır, 3 kişi kaldıysa final. true: tur başlamadı.</summary>
        bool RusRuletiAraKontrol()
        {
            if (!RusRuletli) return false;
            if (RusRuletiBitti()) return true;
            if (Oynayanlar().Count > FinalSiniri) return false;
            FinalTuru();
            return true;
        }

        bool RusRuletiBitti()
        {
            var kalan = Oynayanlar();
            if (kalan.Count > 1) return false;
            rusRuletiSonucu = new RusRuletiSonucu
            {
                Kazanan = kalan.Count == 1 ? kalan[0] : (OyuncuId?)null,
                ElenmeSirasi = new List<OyuncuId>(elenenler),
            };
            Bitir();
            return true;
        }

        /// <summary>Final: sorusuz ikili düello, kalanlardan rastgele iki kişi. Kaybeden doğrudan tetiğe.</summary>
        void FinalTuru()
        {
            if (RusRuletiBitti()) return;
            if (!final)
            {
                final = true;
                Yay(new Olay { Tur = OlayTuru.FinalBasladi });
            }
            var kalan = Oynayanlar();
            int i = rastgele.Next(kalan.Count);
            int j = rastgele.Next(kalan.Count - 1);
            if (j >= i) j++;

            TurSonuIsleri();
            tur++;
            a = kalan[i]; // soru yok; A, B ile aynı (C ne A ne B kuralı bozulmasın)
            TurDurumunuSifirla();
            b = kalan[i];
            c = kalan[j];
            AsamayaGec(Asama.MiniOyun, ayar.HamleSuresiSn);
            Yay(new Olay { Tur = OlayTuru.TurBasladi, B = b, C = c });
        }

        /// <summary>Final düellosu bitti: kaybeden doğrudan tetiğe (soru yok, sonuç ekranı yok). true: final düellosuydu.</summary>
        bool FinalDuellosuBitti(bool cKazandi)
        {
            if (!final) return false;
            tetikCeken = cKazandi ? b : c;
            TetigeGec();
            return true;
        }

        /// <summary>Oyun bitiyor ve sonuç yazılmadı (kurucu erken bitirdi): en çok canı olan kazanır, eşitlikte kimse.</summary>
        void RusRuletiSonucunuYaz()
        {
            if (!RusRuletli || rusRuletiSonucu != null) return;
            OyuncuId? kazanan = null;
            int enCok = 0;
            bool esit = false;
            foreach (var id in Oynayanlar())
            {
                int can = Bul(id).Can;
                if (can > enCok) { enCok = can; kazanan = id; esit = false; }
                else if (can == enCok) esit = true;
            }
            rusRuletiSonucu = new RusRuletiSonucu { Kazanan = esit ? null : kazanan, ElenmeSirasi = new List<OyuncuId>(elenenler) };
        }

        /// <summary>
        /// Patlamadan önceki boş hazne sayısından, gürültüyle. Sonraki tetik patlayacaksa hep "kızgın";
        /// ama "kızgın" her zaman patlayacak demek değil.
        /// </summary>
        IsiSeviyesi TabancaIsisi()
        {
            int kalan = Math.Max(0, mermi - bosalan + tabancaRuhu);
            if (kalan <= 1) return IsiSeviyesi.Kizgin;
            if (kalan <= 2) return IsiSeviyesi.Sicak;
            if (kalan <= 3) return IsiSeviyesi.Ilik;
            return IsiSeviyesi.Soguk;
        }

        // ------------------------------------------------------------ Görünüm

        void RusRuletiGorunumu(Gorunum g)
        {
            if (!RusRuletli) return;
            g.ToplamTur = 0; // son kalana kadar sürer, tur sayısı yok
            g.Isi = TabancaIsisi();
            g.TetikCeken = asama == Asama.Tetik ? tetikCeken : null;
            g.Final = final;
        }
    }
}
