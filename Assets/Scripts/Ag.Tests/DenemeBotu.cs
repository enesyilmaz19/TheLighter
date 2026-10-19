using System;
using Cakmak.Kurallar;
using TheLighter.Ag;

namespace TheLighter.Testler
{
    /// <summary>
    /// Yerel sunucuyla deneme için oyuncu yerine geçen bot (sadece editörde: test ve "Online deneme botları" penceresi).
    /// Gerçek istemcinin kullandığı <see cref="OnlineIstemci"/> ile bağlanır; sırası gelince <c>durum</c>'daki
    /// seçeneklerden rastgele birini yollar. Kural hesaplamaz: neyin seçilebileceği durum'dan.
    /// </summary>
    public sealed class DenemeBotu
    {
        readonly Random rastgele;
        int sonIsledigiSurum = -1;
        double bekle;

        public OnlineIstemci Istemci { get; }
        public string Isim { get; }
        /// <summary>Son hata kodu (testte göstermek için).</summary>
        public string SonHata { get; private set; }
        public int YolladigiKomut { get; private set; }
        /// <summary>Köstebek: bot ilk soru sırasında erken suçlama hakkını kullansın (testte bir bota açılır).</summary>
        public bool ErkenSuclasin;
        public int OyVerdigiSuclama { get; private set; }
        public int Devretti { get; private set; }

        public DenemeBotu(string isim, int tohum, Func<ITasima> tasima = null)
        {
            Isim = isim;
            rastgele = new Random(tohum);
            Istemci = new OnlineIstemci(tasima ?? (() => new WebSocketTasima()), null);
            Istemci.HataGeldi += h => SonHata = h.Kod;
        }

        public bool OyunBitti => Istemci.SonDurum?.Gorunum?.Asama == Asama.OyunSonu;

        public void Guncelle(double sn)
        {
            Istemci.Guncelle(TimeSpan.FromSeconds(sn));
            var d = Istemci.SonDurum;
            if (d == null || d.Yer != OdaYeri.Oyun || !Istemci.Sen.HasValue) return;
            if (d.Surum == sonIsledigiSurum) return;

            // İnsan gibi biraz düşünsün; aynı durumda iki kez komut yollamasın.
            if (bekle <= 0) bekle = 0.3 + rastgele.NextDouble() * 0.7;
            bekle -= sn;
            if (bekle > 0) return;
            bekle = 0;
            sonIsledigiSurum = d.Surum;
            Oyna(d.Gorunum, Istemci.Sen.Value);
        }

        void Yolla(string json)
        {
            if (Istemci.Gonder(json)) YolladigiKomut++;
        }

        OyuncuId Sec(System.Collections.Generic.IReadOnlyList<OyuncuId> liste) => liste[rastgele.Next(liste.Count)];

        void Oyna(Gorunum g, OyuncuId ben)
        {
            int tur = g.Tur;
            foreach (var o in g.Oyuncular)
                if (o.Id == ben && o.Afk) Yolla(Cevirici.Buradayim());

            switch (g.Asama)
            {
                case Asama.SoruToplama:
                    foreach (var o in g.Oyuncular)
                        if (o.Id == ben && o.HavuzaEkledigi < Oyun.KisiBasiHavuzSorusu)
                            Yolla(Cevirici.HavuzaEkle(0, Isim + " soruyor: grupta en çok kim güler? (" + o.HavuzaEkledigi + ")"));
                    break;

                case Asama.SoruSorma:
                    // Hak bitene kadar dener: A soruyu önce sorarsa sunucu YANLIS_ASAMA der, sonraki turda yine.
                    if (ErkenSuclasin && g.SuclamaHakkin)
                    {
                        Yolla(Cevirici.Sucla(tur));
                        break;
                    }
                    if (g.A == ben && g.Secilebilir.Count > 0)
                        Yolla(Cevirici.SoruSor(tur, Sec(g.Secilebilir), Isim + " soruyor: grupta en geç kim uyur?"));
                    break;

                case Asama.CevapSecme:
                    if (g.B == ben && g.Secilebilir.Count > 0)
                        Yolla(Cevirici.CevapSec(tur, Sec(g.Secilebilir)));
                    break;

                case Asama.MiniOyun:
                    bool b = g.B == ben, c = g.C == ben;
                    if ((b && !g.BHamleYapti) || (c && !g.CHamleYapti))
                        Yolla(Cevirici.HamleYap(tur, Hamle(g.MiniOyun)));
                    break;

                case Asama.Oylama:
                    foreach (var id in g.Oylayanlar)
                        if (id == ben && !g.OyVerdin) Yolla(Cevirici.Oy(tur, rastgele.Next(2) == 0));
                    break;

                case Asama.Suclama:
                    // "Köstebek kim?": kendisi ve oyundan çıkan dışında rastgele biri (sunucu da denetler).
                    foreach (var id in g.Oylayanlar)
                    {
                        if (id != ben || g.OyVerdin) continue;
                        var adaylar = new System.Collections.Generic.List<OyuncuId>();
                        foreach (var o in g.Oyuncular) if (o.Id != ben && !o.Cikti) adaylar.Add(o.Id);
                        if (adaylar.Count == 0) break;
                        Yolla(Cevirici.SuclamaOyu(tur, Sec(adaylar)));
                        OyVerdigiSuclama++;
                    }
                    break;

                case Asama.Tetik:
                    // Rus Ruleti: arada bir devreder (hakkı varsa), yoksa çeker.
                    if (g.TetikCeken != ben) break;
                    OyuncuDurumu benim = null;
                    var adaylar2 = new System.Collections.Generic.List<OyuncuId>();
                    foreach (var o in g.Oyuncular)
                    {
                        if (o.Id == ben) benim = o;
                        else if (!o.Elendi && !o.Cikti && !o.Afk) adaylar2.Add(o.Id);
                    }
                    if (benim != null && benim.DevretmeHakki && adaylar2.Count > 0 && rastgele.Next(4) == 0)
                    {
                        Yolla(Cevirici.TetikDevret(tur, Sec(adaylar2)));
                        Devretti++;
                    }
                    else Yolla(Cevirici.TetikCek(tur));
                    break;

                case Asama.IkiyeKatla:
                case Asama.Bedel:
                    if (g.KararVeren == ben) Yolla(rastgele.Next(2) == 0 ? Cevirici.KarariGec(tur)
                        : g.Asama == Asama.Bedel ? Cevirici.BedelOde(tur) : Cevirici.IkiyeKatla(tur));
                    break;
            }
        }

        Hamle Hamle(MiniOyunTuru tur)
        {
            switch (tur)
            {
                case MiniOyunTuru.TekCift: return Cakmak.Kurallar.Hamle.TekCift(1 + rastgele.Next(5), rastgele.Next(2) == 0);
                case MiniOyunTuru.Zar: return Cakmak.Kurallar.Hamle.ZarAt();
                default: return Cakmak.Kurallar.Hamle.TasKagitMakas((TkmSecim)rastgele.Next(3));
            }
        }
    }
}
