using System.Collections.Generic;
using TheLighter.Akis;
using Cakmak.Kurallar;
using UnityEngine.UIElements;
using static TheLighter.Istemci.Arayuz;

namespace TheLighter.Istemci
{
    /// <summary>
    /// A'nın ekranı: kime soracağını seçer, soruyu yazar (en fazla 140 harf) ya da kulağına fısıldar.
    /// Yazı kutusu üstte: mobilde klavye ekranın alt yarısını kapatıyor (Ana Plan 6.2).
    /// Soru A'ya atandıysa (Curcuna, "havuzdan sor" cezası) yazı kutusu yerine atanan soru görünür;
    /// Kör Soru turunda A soruyu hiç görmez. Hangisi olduğu görünümden okunur, kural hesaplanmaz.
    /// </summary>
    sealed class SoruSormaParcasi : IOyunParcasi, IHataGosteren
    {
        const int SoruSiniri = Oyun.SoruEnFazlaKarakter;

        enum Bicim { Yazili, Atanan, Kor }

        readonly IOyunAkisi akis;
        readonly Bicim bicim;
        readonly Label sure;
        readonly Label sayac;
        readonly Label hata;
        readonly TextField kutu;
        readonly Label atananYazi;
        readonly Button degistir;
        readonly Button gonder;
        readonly Button fisilda;
        readonly List<OyuncuId> ciptekiler = new List<OyuncuId>();
        readonly List<Button> cipler = new List<Button>();
        OyuncuId? secili;
        string sonOneri;
        string sonAtanan;
        bool gonderildi;

        public VisualElement Kok { get; }

        public SoruSormaParcasi(IOyunAkisi akis, EkranDurumu e)
        {
            this.akis = akis;
            var g = e.Gorunum;
            bicim = g.Kaos == KaosKurali.KorSoru ? Bicim.Kor : g.AtananSoru != null ? Bicim.Atanan : Bicim.Yazili;
            sonOneri = g.Oneri;
            sonAtanan = g.AtananSoru;
            // Curcuna'da değiştirme hakkı var; "havuzdan sor" cezasında yok.
            bool curcuna = akis.Ayarlar.Mod == OyunModu.Curcuna;

            Kok = Kutu("parca", "soru-sorma");

            var bas = Kutu("satir");
            bas.Add(Yazi(Metin.Al("soru.baslik", akis.Isim(g.Sen)), "baslik"));
            bas.Add(Esnek());
            sure = Yazi("", "sure");
            bas.Add(sure);
            Kok.Add(bas);
            if (g.Kaos != KaosKurali.Yok) Kok.Add(Tekrar.KaosSeridi(g.Kaos));

            switch (bicim)
            {
                case Bicim.Kor:
                    Kok.Add(Yazi(Metin.Al("soru.kor"), "aciklama"));
                    break;

                case Bicim.Atanan:
                    if (!curcuna) Kok.Add(Yazi(Metin.Al("soru.havuzdan_ceza"), "banner"));
                    Kok.Add(Yazi(Metin.Al("soru.atanan_baslik"), "aciklama"));
                    var kart = Kutu("okuma-alani");
                    atananYazi = Yazi(g.AtananSoru, "okuma-soru");
                    kart.Add(atananYazi);
                    Kok.Add(kart);
                    if (curcuna)
                    {
                        var satir = Kutu("satir");
                        degistir = Buton(Metin.Al("soru.degistir"), Degistir, "buton-kucuk");
                        degistir.SetEnabled(g.DegistirmeHakki);
                        satir.Add(degistir);
                        satir.Add(Esnek());
                        Kok.Add(satir);
                    }
                    break;

                default:
                    Kok.Add(Yazi(Metin.Al("soru.aciklama"), "aciklama"));
                    kutu = SoruKutusu.Kur();
                    kutu.RegisterValueChangedCallback(_ => Durum());
                    SoruKutusu.EnterIleGonder(kutu, () => { if (gonder.enabledSelf) Gonder(); });
                    Kok.Add(kutu);

                    var alt = Kutu("satir");
                    alt.Add(Buton(Metin.Al("soru.oneri"), OneriCek, "buton-kucuk"));
                    alt.Add(Esnek());
                    sayac = Yazi("", "sayac");
                    alt.Add(sayac);
                    Kok.Add(alt);
                    break;
            }

            Kok.Add(Yazi(Metin.Al("soru.kime"), "alt-baslik"));
            // Takım Düellosu: rakibe mi soracaksın, takım arkadaşına mı? Herkesin takımı çipte.
            var takimim = Takim.SeninTakimin(g);
            if (takimim != null) Kok.Add(takimim);
            var cipKutusu = Kutu("cipler");
            foreach (var id in g.Secilebilir)
            {
                var secim = id;
                var cip = OyuncuCipi(Tekrar.IsimEtiketi(akis, g, id), akis.Sira(id), () => { secili = secim; Durum(); });
                Takim.Boya(cip, g, id);
                ciptekiler.Add(id);
                cipler.Add(cip);
                cipKutusu.Add(cip);
            }
            Kok.Add(cipKutusu);

            Kok.Add(Esnek());
            hata = Yazi("", "hata");
            // Köstebek: erken suçlama (hakkı olan varsa). Elden elede "Kim suçluyor?" sorulur.
            Kok.Add(Kostebek.SuclamaDugmesi(akis, Kok, m => hata.text = m));
            Kok.Add(hata);
            gonder = Buton(Metin.Al("soru.gonder"), Gonder, "buton-birincil", "buton-buyuk");
            Kok.Add(gonder);
            // Online'da fısıltı yok (protokol): soru yazılır.
            if (bicim == Bicim.Yazili && !akis.Online)
            {
                fisilda = Buton(Metin.Al("soru.fisildadim"), Fisilda, "buton-ikincil");
                Kok.Add(fisilda);
                Kok.Add(Yazi(Metin.Al("soru.fisildadim_ipucu"), "ipucu"));
            }

            Durum();
        }

        public void Guncelle(EkranDurumu e)
        {
            var g = e.Gorunum;
            SureyiYaz(sure, g.KalanMs, 10000);

            // Online'da öneri ve değişen soru sonraki durum'la gelir.
            if (kutu != null && g.Oneri != null && g.Oneri != sonOneri)
            {
                sonOneri = g.Oneri;
                kutu.value = g.Oneri;
            }
            if (atananYazi != null && g.AtananSoru != null && g.AtananSoru != sonAtanan)
            {
                sonAtanan = g.AtananSoru;
                atananYazi.text = g.AtananSoru;
            }
            if (degistir != null) degistir.SetEnabled(g.DegistirmeHakki);
        }

        void Durum()
        {
            for (int i = 0; i < cipler.Count; i++)
                cipler[i].EnableInClassList("secili", secili.HasValue && ciptekiler[i] == secili.Value);

            if (bicim == Bicim.Yazili)
            {
                string metin = kutu.value ?? "";
                sayac.text = metin.Length + "/" + SoruSiniri;
                gonder.SetEnabled(!gonderildi && secili.HasValue && metin.Trim().Length > 0);
                if (fisilda != null) fisilda.SetEnabled(secili.HasValue);
            }
            else
            {
                gonder.SetEnabled(!gonderildi && secili.HasValue);
            }
            hata.text = "";
        }

        void OneriCek()
        {
            var s = akis.OneriCek();
            if (s != Sonuc.Tamam) { hata.text = HataMetni.Al(s); return; }
            if (akis.Online) return; // öneri sonraki durum'la gelir (Guncelle)
            var oneri = akis.Ekran.Gorunum != null ? akis.Ekran.Gorunum.Oneri : null;
            if (oneri != null) { sonOneri = oneri; kutu.value = oneri; }
            else hata.text = HataMetni.Al(Sonuc.HavuzBos);
        }

        void Degistir()
        {
            var s = akis.SoruyuDegistir();
            if (s != Sonuc.Tamam) { hata.text = HataMetni.Al(s); return; }
            if (akis.Online) { degistir.SetEnabled(false); return; } // yenisi sonraki durum'la gelir
            var g = akis.Ekran.Gorunum;
            if (g != null && g.AtananSoru != null) { sonAtanan = g.AtananSoru; atananYazi.text = g.AtananSoru; }
            if (g != null) degistir.SetEnabled(g.DegistirmeHakki);
        }

        void Gonder()
        {
            if (!secili.HasValue) return;
            // Atanan ya da kör soruda metin kural motorunda yok sayılır.
            var s = akis.SoruGonder(secili.Value, kutu != null ? kutu.value : "");
            if (s != Sonuc.Tamam) hata.text = HataMetni.Al(s);
            else if (akis.Online) { gonderildi = true; Durum(); } // cevap gelene kadar ikinci kez gitmesin
        }

        /// <summary>
        /// Online: soruSor'a sunucudan hata geldi (Temiz modda UYGUNSUZ, GECERSIZ_GIRDI...). Tur aynen sürüyor:
        /// gönder yeniden açılır, A başka soru yazabilir.
        /// </summary>
        public bool HataGoster(TheLighter.Ag.HataMesaji h)
        {
            gonderildi = false;
            Durum();
            hata.text = HataMetni.Mesaj(h);
            return true;
        }

        void Fisilda()
        {
            if (!secili.HasValue) return;
            var s = akis.Fisildadim(secili.Value);
            if (s != Sonuc.Tamam) hata.text = HataMetni.Al(s);
        }
    }

    /// <summary>Sunucudan gelen hatayı kendi içinde gösterebilen parça (gösterdiyse true).</summary>
    interface IHataGosteren
    {
        bool HataGoster(TheLighter.Ag.HataMesaji h);
    }

    /// <summary>Kural motorunun döndürdüğü hata kodunun oyuncuya görünen metni.</summary>
    static class HataMetni
    {
        /// <summary>
        /// Sunucunun hata mesajı. Temiz modda UYGUNSUZ: "Gönderilmedi… 3. seferde atılırsın (1/3)", sınır mesajdan.
        /// </summary>
        public static string Mesaj(TheLighter.Ag.HataMesaji h)
        {
            if (h == null) return "";
            if (h.Sonuc == Sonuc.Uygunsuz && h.Sinir > 0) return Metin.Al("online.uygunsuz", h.Ihlal, h.Sinir);
            return Kod(h.Kod);
        }

        /// <summary>Biri atıldı: Temiz modda mı, kurucu mu attı.</summary>
        public static string Atildi(TheLighter.Ag.OlayMesaji o, string isim)
        {
            if (o.Neden == "temiz") return Metin.Al("online.olay.atildi_temiz", isim);
            if (o.Neden == "kurucu") return Metin.Al("online.olay.atildi_kurucu", isim, TurkceEk.Belirtme(isim));
            return Metin.Al("online.olay.atildi", isim);
        }

        public static string Al(Sonuc s) => Metin.Al("hata." + s);

        /// <summary>Sunucudan gelen kod: kural motorununsa onun metni, sunucununsa "hata.ODA_YOK" gibi.</summary>
        public static string Kod(string kod)
        {
            var s = TheLighter.Ag.Cevirici.SonucKodu(kod);
            return s.HasValue ? Al(s.Value) : Metin.Al("hata." + kod);
        }
    }
}
