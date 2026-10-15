using System.Collections.Generic;

namespace Cakmak.Kurallar
{
    // S5 — Kopma, AFK, oyundan çıkma (Ana Plan 6.1). Online'da sunucu çağırır. Elden elede hiçbiri olmaz:
    // kimse kopmaz, telefon herkesin elinde olduğu için AFK da sayılmaz.
    public sealed partial class Oyun
    {
        /// <summary>Üst üste bu kadar kez süresini dolduran AFK sayılır.</summary>
        public const int AfkSiniri = 2;

        readonly HashSet<OyuncuId> kopuk = new HashSet<OyuncuId>();
        readonly HashSet<OyuncuId> afk = new HashSet<OyuncuId>();
        readonly HashSet<OyuncuId> cikan = new HashSet<OyuncuId>();
        readonly Dictionary<OyuncuId, int> ardArdaSure = new Dictionary<OyuncuId, int>();

        /// <summary>Seçilebilir ve sırası gelince oynar. Kopuk, AFK, çıkmış ya da (Rus Ruleti'nde) elenmiş oyuncu atlanır.</summary>
        bool Aktif(OyuncuId id) => !kopuk.Contains(id) && !afk.Contains(id) && !cikan.Contains(id) && !Bul(id).Elendi;

        /// <summary>Bağlantısı koptu. Yeri tutulur ve süreler işler, ama B/C seçilemez, sırası gelirse atlanır.</summary>
        public Sonuc OyuncuKoptu(OyuncuId kim)
        {
            if (Bul(kim) == null) return Sonuc.GecersizGirdi;
            if (!cikan.Contains(kim)) kopuk.Add(kim);
            return Sonuc.Tamam;
        }

        /// <summary>Geri bağlandı: yine seçilebilir, AFK da kalkar. Oyundan çıkmış oyuncu geri girmez, izler.</summary>
        public Sonuc OyuncuDondu(OyuncuId kim)
        {
            if (Bul(kim) == null) return Sonuc.GecersizGirdi;
            kopuk.Remove(kim);
            return Buradayim(kim);
        }

        /// <summary>
        /// Oyuncu başında: AFK etiketi kalkar, üst üste süre dolma sayısı sıfırlanır.
        /// Online'da sunucu oyuncudan gelen her mesajda çağırır ("ekrana dokununca geri gelir").
        /// </summary>
        public Sonuc Buradayim(OyuncuId kim)
        {
            if (Bul(kim) == null) return Sonuc.GecersizGirdi;
            ardArdaSure[kim] = 0;
            if (afk.Remove(kim)) Yay(new Olay { Tur = OlayTuru.AfkBitti, Kim = kim });
            return Sonuc.Tamam;
        }

        /// <summary>
        /// Oyundan çıktı (online'da 60 sn içinde dönmedi). Bir daha seçilmez, sırası gelmez, sayaçları özette kalır.
        /// Elindeki iş hemen biter: sormadan çıkan A'nın turu yanar, seçmeden çıkan B'nin sorusu güme gider,
        /// mini oyundan kaçan kaybeder, oy vermeden çıkan oylamadan düşer, karar vermeden çıkan "hayır" demiş sayılır.
        /// </summary>
        public Sonuc OyuncuCikti(OyuncuId kim)
        {
            if (Bul(kim) == null) return Sonuc.GecersizGirdi;
            if (asama == Asama.OyunSonu || !cikan.Add(kim)) return Sonuc.Tamam;
            kopuk.Remove(kim);
            afk.Remove(kim);
            Yay(new Olay { Tur = OlayTuru.OyuncuCikti, Kim = kim });
            if (OyuncuCiktiOyunKarari(kim)) return Sonuc.Tamam;
            EldekiIsiBitir(kim);
            return Sonuc.Tamam;
        }

        void EldekiIsiBitir(OyuncuId kim)
        {
            switch (asama)
            {
                case Asama.SoruToplama:
                    if (ToplamaTamam()) ToplamayiBitir();
                    break;
                // Soran Gizli'de A hemen düşerse turun bitişinden kim olduğu anlaşılır: süresi dolsun.
                case Asama.SoruSorma when kim == a && !SoranGizli:
                    OncekiTuruUnut();
                    TurBaslat(RastgeleOyuncu(a));
                    break;
                case Asama.CevapSecme when kim == b:
                    GumeyeGitti();
                    OncekiTuruUnut();
                    TurBaslat(RastgeleOyuncu(a, kim));
                    break;
                case Asama.MiniOyun when kim == b || kim == c:
                    MiniOyunBitti(cKazandi: kim == b, sureDoldu: true, zarlaKarar: false, 0, 0);
                    break;
                case Asama.Oylama when !oylar.ContainsKey(kim) && oylayanlar.Remove(kim):
                    if (oylar.Count == oylayanlar.Count) OylamayiBitir();
                    break;
                // "Kim?" oylaması: oy vermeden çıkan düşer, verdiyse oyu sayılır.
                case Asama.Suclama when !suclamaOylari.ContainsKey(kim) && oylayanlar.Remove(kim):
                    if (suclamaOylari.Count == oylayanlar.Count) SuclamayiBitir();
                    break;
                case Asama.Linc when !Secti(kim) && oylayanlar.Remove(kim):
                case Asama.Gece when !Secti(kim) && oylayanlar.Remove(kim):
                    if (SecenSayisi == oylayanlar.Count) GeceOylamasiBitti();
                    break;
                case Asama.IkiyeKatla when kim == kararVeren:
                case Asama.Bedel when kim == kararVeren:
                    KarardanVazgecildi();
                    break;
                // Tetiği çekmeden çıkan çekmez: oyundan zaten çıktı.
                case Asama.Tetik when kim == tetikCeken:
                    TetikSonrasi();
                    break;
            }
        }

        /// <summary>Curcuna toplaması: oyunda kalan herkes sorularını yazdı.</summary>
        bool ToplamaTamam() => oyuncular.TrueForAll(x => cikan.Contains(x.Id) || x.HavuzaEkledigi >= KisiBasiHavuzSorusu);

        /// <summary>
        /// Süresi doldu (soru sormadı, C seçmedi, hamle yapmadı): Uykucu sayacı, ve üst üste AfkSiniri kez olursa AFK.
        /// Soran Gizli'de A sayılmaz: AFK olması gizli A'nın kim olduğunu söylerdi.
        /// </summary>
        void SuresiDoldu(OyuncuId kim)
        {
            IzAl(kim).SureDoldu++;
            if (ayar.TekCihaz || cikan.Contains(kim) || (SoranGizli && kim == a)) return;
            ardArdaSure.TryGetValue(kim, out var n);
            ardArdaSure[kim] = ++n;
            if (n >= AfkSiniri && afk.Add(kim)) Yay(new Olay { Tur = OlayTuru.Afk, Kim = kim });
        }
    }
}
