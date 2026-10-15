using System.Collections.Generic;
using Cakmak.Kurallar;
using TheLighter.Akis;
using UnityEngine.UIElements;
using static TheLighter.Istemci.Arayuz;

namespace TheLighter.Istemci
{
    /// <summary>
    /// Rus Ruleti ekranlarının ortak parçaları (docs/Oyunlar.md 2, tasks/g2-rus-ruleti.md): can etiketi, tabanca ısısı,
    /// tetik sonucu duyurusu, oyun sonundaki kazanan ve elenme sırası. Kural hesaplanmaz: can, elenme, ısı görünümden.
    /// </summary>
    static class RusRuleti
    {
        /// <summary>İsmin yanına: "2 can" ya da "elendi". Çakmak'ta boş.</summary>
        public static string CanEtiketi(Gorunum g, OyuncuDurumu o)
        {
            if (g == null || g.Oyun != OyunTuru.RusRuleti || o == null) return null;
            return o.Elendi ? Metin.Al("rus.elendi_etiket") : Metin.Al("rus.can_sayi", o.Can);
        }

        /// <summary>Az önce çekilen tetik: "Tık… boş", "BAM! −1 can", "BAM! Elendi".</summary>
        public static string TetikMetni(IOyunAkisi akis, TetikSonucu t)
        {
            string kim = akis.Isim(t.Kim);
            if (!t.Patladi) return Metin.Al("rus.tik", kim);
            return Metin.Al(t.Elendi ? "rus.elendi" : "rus.bam", kim);
        }

        public static Label TetikDuyurusu(IOyunAkisi akis, TetikSonucu t) =>
            Yazi(TetikMetni(akis, t), "banner", t.Patladi ? "tetik-bam" : "tetik-tik");

        /// <summary>Herkesin canı: renk, isim, can (ya da elendi), devret hakkı. Elenenler soluk.</summary>
        public static VisualElement CanListesi(IOyunAkisi akis, Gorunum g)
        {
            var kutu = Kutu("cipler", "can-listesi");
            foreach (var o in g.Oyuncular)
            {
                var cip = Kutu("cip", "cip-salt");
                cip.Add(RenkNoktasi(akis.Sira(o.Id)));
                string yazi = akis.Isim(o.Id) + " · " + CanEtiketi(g, o);
                if (!o.Elendi && o.DevretmeHakki) yazi += " · " + Metin.Al("rus.devret_etiket");
                cip.Add(Yazi(yazi, "cip-yazi"));
                cip.EnableInClassList("soluk", o.Elendi || o.Cikti);
                kutu.Add(cip);
            }
            return kutu;
        }

        /// <summary>Oyun sonunun en üstü: kazanan (ya da yok), elenme sırası (ilk elenen en altta).</summary>
        public static VisualElement Acilis(IOyunAkisi akis, RusRuletiSonucu r)
        {
            var kart = Kutu("kostebek-acilis", "rus-acilis");
            if (r.Kazanan.HasValue)
            {
                kart.Add(Yazi(Metin.Al("rus.kazanan_baslik"), "ipucu", "orta"));
                var kim = Kutu("satir", "orta-satir");
                kim.Add(RenkNoktasi(akis.Sira(r.Kazanan.Value)));
                kim.Add(Yazi(akis.Isim(r.Kazanan), "kostebek-isim"));
                kart.Add(kim);
                kart.Add(Yazi(Metin.Al("rus.son_kalan"), "kostebek-kazanan"));
            }
            else kart.Add(Yazi(Metin.Al("rus.kazanan_yok"), "kostebek-kazanan"));

            if (r.ElenmeSirasi.Count > 0)
            {
                kart.Add(Yazi(Metin.Al("rus.elenme_sirasi"), "alt-baslik"));
                for (int i = r.ElenmeSirasi.Count - 1; i >= 0; i--)
                {
                    var satir = Kutu("satir");
                    satir.Add(RenkNoktasi(akis.Sira(r.ElenmeSirasi[i])));
                    satir.Add(Yazi(Metin.Al("rus.elenen_satir", i + 1, akis.Isim(r.ElenmeSirasi[i])), "aciklama"));
                    kart.Add(satir);
                }
            }
            return kart;
        }

        /// <summary>Devredilebilecekler: oyunda, elenmemiş, AFK ya da kopuk olmayan biri, kendin değil (sunucu da denetler).</summary>
        public static List<OyuncuId> DevretAdaylari(IOyunAkisi akis, Gorunum g, OyuncuId ben)
        {
            var l = new List<OyuncuId>();
            var online = akis as TheLighter.Ag.OnlineAkis;
            foreach (var o in g.Oyuncular)
                if (o.Id != ben && !o.Elendi && !o.Cikti && !o.Afk && (online == null || online.Bagli(o.Id))) l.Add(o.Id);
            return l;
        }
    }

    /// <summary>
    /// Rus Ruleti, tetik: düelloyu kaybeden çeker ya da (oyun boyu bir kez) devreder. Açık bilgi: herkes izler.
    /// Düğmeler elden elede telefonu tutanda, online'da sadece çekecek kişide. Süre dolunca tetik kendiliğinden çekilir.
    /// </summary>
    sealed class TetikParcasi : IOyunParcasi
    {
        readonly IOyunAkisi akis;
        readonly Label sure;
        readonly Label hata;
        readonly VisualElement devretKutusu;

        public VisualElement Kok { get; }

        public TetikParcasi(IOyunAkisi akis, EkranDurumu e)
        {
            this.akis = akis;
            var g = e.Gorunum;
            var cekecek = e.Kime ?? g.TetikCeken;
            bool benim = cekecek.HasValue && (!akis.Online || akis.Ben == cekecek);
            string isim = akis.Isim(cekecek);

            Kok = Kutu("parca", "perde", "tetik");

            var bas = Kutu("satir");
            var isi = Tekrar.IsiEtiketi(g);
            if (isi != null) bas.Add(isi);
            bas.Add(Esnek());
            sure = Yazi("", "sure");
            bas.Add(sure);
            Kok.Add(bas);

            Kok.Add(Esnek());
            // Kim kaybetti: düello sonucu tetik aşamasında da görünümde.
            if (g.SonMiniOyun != null && g.B.HasValue && g.C.HasValue)
            {
                var kaybeden = g.SonMiniOyun.CKazandi ? g.B : g.C;
                if (kaybeden != cekecek) Kok.Add(Yazi(Metin.Al("rus.devralan", isim, akis.Isim(kaybeden)), "aciklama", "orta"));
                else Kok.Add(Yazi(Metin.Al("rus.kaybetti", isim), "aciklama", "orta"));
            }
            if (cekecek.HasValue) Kok.Add(RenkNoktasi(akis.Sira(cekecek.Value), "perde-renk"));
            Kok.Add(Yazi(Metin.Al(benim ? "rus.tetik_sende" : "rus.tetik_onda", isim), "perde-buyuk"));
            Kok.Add(Yazi(Metin.Al("rus.tetik_ipucu"), "aciklama", "orta"));
            Kok.Add(RusRuleti.CanListesi(akis, g));
            Kok.Add(Esnek());

            hata = Yazi("", "hata");
            devretKutusu = Kutu("cipler", "gizli");
            if (benim)
            {
                var ben = cekecek.Value;
                bool hakVar = false;
                foreach (var o in g.Oyuncular) if (o.Id == ben) hakVar = o.DevretmeHakki;

                Kok.Add(devretKutusu);
                Kok.Add(hata);
                var cek = Buton(Metin.Al("rus.tetigi_cek"), Cek, "buton-birincil", "buton-buyuk");
                GecikmeliAc(cek);
                Kok.Add(cek);
                if (hakVar)
                {
                    foreach (var id in RusRuleti.DevretAdaylari(akis, g, ben))
                    {
                        var hedef = id;
                        devretKutusu.Add(OyuncuCipi(Tekrar.IsimEtiketi(akis, g, hedef), akis.Sira(hedef), () => Devret(hedef)));
                    }
                    Kok.Add(Buton(Metin.Al("rus.devret"), () => devretKutusu.ToggleInClassList("gizli"), "buton-ikincil"));
                    Kok.Add(Yazi(Metin.Al("rus.devret_ipucu"), "ipucu"));
                }
            }
            else Kok.Add(Yazi(Metin.Al("rus.izle"), "ipucu", "orta"));
            Guncelle(e);
        }

        void Cek()
        {
            var s = akis.TetikCek();
            if (s != Sonuc.Tamam) hata.text = HataMetni.Al(s);
            else if (akis.Online) Kok.SetEnabled(false);
        }

        void Devret(OyuncuId hedef)
        {
            var s = akis.TetikDevret(hedef);
            if (s != Sonuc.Tamam) hata.text = HataMetni.Al(s);
            else if (akis.Online) Kok.SetEnabled(false);
        }

        public void Guncelle(EkranDurumu e) => SureyiYaz(sure, e.Gorunum.KalanMs, 3000);
    }
}
