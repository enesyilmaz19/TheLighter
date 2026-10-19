using System.Collections.Generic;
using Cakmak.Kurallar;
using TheLighter.Akis;
using UnityEngine.UIElements;
using static TheLighter.Istemci.Arayuz;

namespace TheLighter.Istemci
{
    /// <summary>
    /// S3 (tekrar oynatma) ekranlarının ortak parçaları: kaos kartı, ısı, ceza kartları, görev metni,
    /// palyaço etiketi. Kural hesaplanmaz: hepsi <see cref="Gorunum"/>'dan okunur, metinler Metinler.csv'de.
    /// </summary>
    static class Tekrar
    {
        public static string KaosAdi(KaosKurali k) => Metin.Al("kaos.ad." + k);

        public static string IsiYazisi(IsiSeviyesi i) => Metin.Al("isi." + i);

        /// <summary>Tur başında büyük duyuru (Ana Plan 8.3).</summary>
        public static VisualElement KaosKarti(KaosKurali k)
        {
            var kart = Kutu("kaos-karti");
            kart.Add(Yazi(Metin.Al("perde.kaos_baslik"), "kaos-ust"));
            kart.Add(Yazi(KaosAdi(k), "kaos-ad"));
            kart.Add(Yazi(Metin.Al("kaos.aciklama." + k), "kaos-aciklama"));
            return kart;
        }

        /// <summary>Kaos turunda küçük hatırlatma şeridi.</summary>
        public static VisualElement KaosSeridi(KaosKurali k)
        {
            return Yazi(KaosAdi(k) + ": " + Metin.Al("kaos.aciklama." + k), "banner", "kaos-serit");
        }

        /// <summary>Kızgın Çakmak açıksa ısı etiketi, kapalıysa null.</summary>
        public static Label IsiEtiketi(Gorunum g)
        {
            if (!g.Isi.HasValue) return null;
            var l = Yazi(Metin.Al("isi.etiket", IsiYazisi(g.Isi.Value)), "isi", "isi-" + g.Isi.Value.ToString().ToLowerInvariant());
            return l;
        }

        /// <summary>
        /// İsim + herkese açık etiketleri: palyaço cezası; online'da Bağlanıyor…, AFK, Ayrıldı.
        /// </summary>
        public static string IsimEtiketi(IOyunAkisi akis, Gorunum g, OyuncuId id)
        {
            string isim = akis.Isim(id);
            if (g == null) return isim;
            foreach (var o in g.Oyuncular)
            {
                if (o.Id != id) continue;
                foreach (var k in o.Kisitlamalar)
                    if (k.Tur == CezaTuru.Palyaco) { isim += " · " + Metin.Al("palyaco.etiket"); break; }
                foreach (var s in g.Supheliler)
                    if (s == id) { isim += " · " + Metin.Al("kostebek.supheli_etiket"); break; }
                if (o.Cikti) return isim + " · " + Metin.Al("online.etiket.ayrildi");
                if (akis is TheLighter.Ag.OnlineAkis online && !online.Bagli(id)) return isim + " · " + Metin.Al("online.etiket.baglaniyor");
                if (o.Afk) return isim + " · " + Metin.Al("online.etiket.afk");
            }
            return isim;
        }

        public static int BedelHakki(Gorunum g, OyuncuId? id)
        {
            if (g == null || !id.HasValue) return 0;
            foreach (var o in g.Oyuncular)
                if (o.Id == id.Value) return o.BedelHakki;
            return 0;
        }

        /// <summary>Görev metni: türü + hedef + adet geliyor, cümle Metinler.csv'de.</summary>
        public static string GorevMetni(IOyunAkisi akis, Gorev g)
        {
            if (g == null) return "";
            string hedef = g.Hedef.HasValue ? akis.Isim(g.Hedef) : "";
            return Metin.Al("gorev.tur." + g.Tur, g.Adet, hedef,
                TurkceEk.Belirtme(hedef), TurkceEk.Yonelme(hedef), TurkceEk.Ilgi(hedef));
        }

        public static string CezaMetni(Ceza c)
        {
            if (c == null) return "";
            return c.Tur == CezaTuru.Sesli ? (c.Metin ?? "") : Metin.Al("ceza.metin." + c.Tur);
        }

        /// <summary>Tek bir ceza kartı: kim, neden, türü ve metni.</summary>
        public static VisualElement CezaKarti(IOyunAkisi akis, CezaCekimi cek)
        {
            var kart = Kutu("ceza-karti", cek.Ceza.Tur == CezaTuru.Sesli ? "ceza-sesli" : "ceza-kisit");
            var ust = Kutu("satir");
            ust.Add(RenkNoktasi(akis.Sira(cek.Kim)));
            ust.Add(Yazi(Metin.Al("ceza.baslik", akis.Isim(cek.Kim)), "ceza-kim"));
            kart.Add(ust);
            kart.Add(Yazi(Metin.Al("ceza.neden." + cek.Neden), "ipucu"));
            kart.Add(Yazi(Metin.Al("ceza.tur." + cek.Ceza.Tur), "ceza-tur"));
            kart.Add(Yazi(CezaMetni(cek.Ceza), "ceza-metin"));
            if (cek.Ceza.Tur == CezaTuru.Sesli) kart.Add(Yazi(Metin.Al("ceza.sesli_ipucu"), "ipucu"));
            return kart;
        }

        public static List<CezaCekimi> Filtrele(IReadOnlyList<CezaCekimi> liste, bool yanma)
        {
            var l = new List<CezaCekimi>();
            if (liste == null) return l;
            foreach (var c in liste)
                if ((c.Neden == CekimNedeni.Yandi) == yanma) l.Add(c);
            return l;
        }

        /// <summary>
        /// Çark: ceza türleri bir süre döner, sonra kartlar açılır. Sadece görüntü; kartlar kural motorundan geldi.
        /// </summary>
        public static VisualElement Cark(IOyunAkisi akis, IReadOnlyList<CezaCekimi> cezalar, System.Action bitti)
        {
            var kap = Kutu("ceza-alani");
            var donen = Yazi(Metin.Al("ceza.cark"), "ceza-cark");
            kap.Add(donen);
            var turler = (CezaTuru[])System.Enum.GetValues(typeof(CezaTuru));
            int i = 0;
            var is1 = kap.schedule.Execute(() =>
            {
                donen.text = Metin.Al("ceza.tur." + turler[i % turler.Length]);
                i++;
            }).Every(90);
            kap.schedule.Execute(() =>
            {
                is1.Pause();
                kap.Clear();
                foreach (var c in cezalar) kap.Add(CezaKarti(akis, c));
                bitti?.Invoke();
            }).StartingIn(1300);
            return kap;
        }
    }

    /// <summary>Curcuna başı: telefonu tutan kişi havuza 2 soru yazar. Saat durur (elden ele).</summary>
    sealed class SoruToplamaParcasi : IOyunParcasi
    {
        static readonly System.Random Fikir = new System.Random();

        readonly IOyunAkisi akis;
        readonly TextField kutu;
        readonly Label sayac;
        readonly Label hata;
        readonly Button ekle;

        public VisualElement Kok { get; }

        public SoruToplamaParcasi(IOyunAkisi akis, EkranDurumu e)
        {
            this.akis = akis;
            var g = e.Gorunum;
            int yazdigi = 0;
            foreach (var o in g.Oyuncular)
                if (o.Id == g.Sen) yazdigi = o.HavuzaEkledigi;

            Kok = Kutu("parca", "soru-sorma");
            Kok.Add(Yazi(Metin.Al("toplama.baslik", akis.Isim(g.Sen)), "baslik"));
            Kok.Add(Yazi(Metin.Al("toplama.aciklama", yazdigi + 1, Oyun.KisiBasiHavuzSorusu), "aciklama"));

            kutu = SoruKutusu.Kur();
            kutu.RegisterValueChangedCallback(_ => Durum());
            SoruKutusu.EnterIleGonder(kutu, () => { if (ekle.enabledSelf) Ekle(); });
            Kok.Add(kutu);

            var alt = Kutu("satir");
            alt.Add(Buton(Metin.Al("toplama.oneri"), FikirVer, "buton-kucuk"));
            alt.Add(Esnek());
            sayac = Yazi("", "sayac");
            alt.Add(sayac);
            Kok.Add(alt);

            Kok.Add(Esnek());
            Kok.Add(Yazi(Metin.Al("toplama.kayit"), "ipucu"));
            hata = Yazi("", "hata");
            Kok.Add(hata);
            ekle = Buton(Metin.Al("toplama.ekle"), Ekle, "buton-birincil", "buton-buyuk");
            Kok.Add(ekle);
            Durum();
        }

        void FikirVer()
        {
            var sorular = akis.Ayarlar.OneriSorulari;
            if (sorular == null || sorular.Count == 0)
            {
                hata.text = HataMetni.Al(Sonuc.HavuzBos);
                return;
            }
            kutu.value = sorular[Fikir.Next(sorular.Count)];
        }

        void Durum()
        {
            string metin = kutu.value ?? "";
            sayac.text = metin.Length + "/" + Oyun.SoruEnFazlaKarakter;
            ekle.SetEnabled(metin.Trim().Length > 0);
            hata.text = "";
        }

        void Ekle()
        {
            var s = akis.HavuzaSoruEkle(kutu.value);
            if (s != Sonuc.Tamam) hata.text = HataMetni.Al(s);
            else if (akis.Online) ekle.SetEnabled(false); // sunucunun cevabı gelene kadar ikinci kez gitmesin
        }

        public void Guncelle(EkranDurumu e) { }
    }

    /// <summary>Oyun başı: telefonu tutan kişi kendi gizli görevine bakar (Ana Plan 8.2).</summary>
    sealed class GorevParcasi : IOyunParcasi
    {
        public VisualElement Kok { get; }

        public GorevParcasi(IOyunAkisi akis, EkranDurumu e)
        {
            var g = e.Gorunum;
            Kok = Kutu("parca", "perde");
            Kok.Add(Yazi(Metin.Al("gorev.baslik", akis.Isim(g.Sen)), "baslik"));
            Kok.Add(Esnek());
            // Köstebek: görevle birlikte rol. Herkese aynı biçimde (köstebek olmayana "değilsin").
            bool rollu = g.Oyun == OyunTuru.Kostebek;
            if (rollu) Kok.Add(Kostebek.RolBasligi(g));
            var kart = Kutu("gorev-karti");
            if (g.Gorevin != null)
            {
                kart.Add(Yazi(Metin.Al("zorluk." + g.Gorevin.Zorluk), "gorev-zorluk"));
                kart.Add(Yazi(Tekrar.GorevMetni(akis, g.Gorevin), "gorev-metin"));
            }
            Kok.Add(kart);
            Kok.Add(Yazi(Metin.Al("gorev.aciklama"), "aciklama", "orta"));
            Kok.Add(Esnek());
            // Online'da telefon elden ele dolaşmıyor: "telefonu ver" demez.
            var tamam = Buton(Metin.Al(akis.Online ? "gorev.gordum_online" : "gorev.gordum"), akis.GorevGordum, "buton-birincil", "buton-buyuk");
            // Rollü oyunda herkes aynı süre bekler: kimin uzun baktığından rolü anlaşılmasın.
            GecikmeliAc(tamam, rollu ? Kostebek.RolPerdesiMs : 800);
            Kok.Add(tamam);
        }

        public void Guncelle(EkranDurumu e) { }
    }

    /// <summary>Grup Kararı: telefonu tutan kişi gizlice oy verir. Soruyu görmez.</summary>
    sealed class OylamaParcasi : IOyunParcasi
    {
        readonly Label hata;

        public VisualElement Kok { get; }

        public OylamaParcasi(IOyunAkisi akis, EkranDurumu e)
        {
            var g = e.Gorunum;
            string b = akis.Isim(g.B);
            string c = akis.Isim(g.C);

            Kok = Kutu("parca", "perde");
            Kok.Add(Yazi(Metin.Al("oy.baslik", akis.Isim(g.Sen)), "baslik"));
            Kok.Add(Esnek());
            Kok.Add(Tekrar.KaosKarti(KaosKurali.GrupKarari));
            Kok.Add(Yazi(Metin.Al("oy.aciklama", b, TurkceEk.Yonelme(c), c), "aciklama", "orta"));
            Kok.Add(Esnek());
            Kok.Add(Yazi(Metin.Al("oy.gizli"), "ipucu"));
            hata = Yazi("", "hata");
            Kok.Add(hata);

            var ifsa = Buton(Metin.Al("oy.ifsa"), () => Ver(akis, true), "buton-birincil", "buton-buyuk");
            var gume = Buton(Metin.Al("oy.gume"), () => Ver(akis, false), "buton-ikincil", "buton-buyuk");
            GecikmeliAc(ifsa);
            GecikmeliAc(gume);
            Kok.Add(ifsa);
            Kok.Add(gume);
        }

        void Ver(IOyunAkisi akis, bool ifsa)
        {
            var s = akis.Oy(ifsa);
            if (s != Sonuc.Tamam) hata.text = HataMetni.Al(s);
            else if (akis.Online) Kok.SetEnabled(false);
        }

        public void Guncelle(EkranDurumu e) { }
    }

    /// <summary>İkiye Katla ya da Bedel: karar veren 5 saniyede seçer. Telefon ortada, herkes görür.</summary>
    sealed class KararParcasi : IOyunParcasi
    {
        readonly Label sure;
        readonly Label hata;
        readonly bool online;

        public VisualElement Kok { get; }

        public KararParcasi(IOyunAkisi akis, EkranDurumu e)
        {
            var g = e.Gorunum;
            string karar = akis.Isim(e.Kime);
            bool bedel = g.Asama == Asama.Bedel;
            // Online'da sadece karar verenin ekranında düğme var; diğerleri izler.
            bool benKarar = !akis.Online || (akis.Ben.HasValue && akis.Ben == e.Kime);
            online = akis.Online;

            Kok = Kutu("parca", "perde");
            var bas = Kutu("satir");
            bas.Add(Esnek());
            sure = Yazi("", "sure");
            bas.Add(sure);
            Kok.Add(bas);

            Kok.Add(Esnek());
            if (e.Kime.HasValue) Kok.Add(RenkNoktasi(akis.Sira(e.Kime.Value), "perde-renk"));
            if (bedel)
            {
                Kok.Add(Yazi(Metin.Al("karar.bedel_baslik"), "perde-buyuk"));
                Kok.Add(Yazi(Metin.Al("karar.bedel_soru", karar, Tekrar.BedelHakki(g, e.Kime)), "aciklama", "orta"));
            }
            else
            {
                Kok.Add(Yazi(Metin.Al("karar.katla_baslik", karar), "perde-buyuk"));
                Kok.Add(Yazi(Metin.Al("karar.katla_soru", karar), "aciklama", "orta"));
            }
            Kok.Add(Esnek());

            hata = Yazi("", "hata");
            Kok.Add(hata);
            if (!benKarar)
            {
                Kok.Add(Yazi(Metin.Al("online.bekle.karar", karar), "aciklama", "orta"));
                return;
            }
            var evet = Buton(Metin.Al(bedel ? "karar.bedel_evet" : "karar.katla_evet"),
                () => Bildir(bedel ? akis.BedelOde() : akis.IkiyeKatla()), "buton-birincil", "buton-buyuk");
            var hayir = Buton(Metin.Al(bedel ? "karar.bedel_hayir" : "karar.katla_hayir"),
                () => Bildir(akis.KarariGec()), "buton-ikincil", "buton-buyuk");
            GecikmeliAc(evet, 400);
            GecikmeliAc(hayir, 400);
            Kok.Add(evet);
            Kok.Add(hayir);
        }

        void Bildir(Sonuc s)
        {
            if (s != Sonuc.Tamam) hata.text = HataMetni.Al(s);
            else if (online) Kok.SetEnabled(false);
        }

        public void Guncelle(EkranDurumu e)
        {
            SureyiYaz(sure, e.Gorunum.KalanMs, 3000);
        }
    }

    /// <summary>Soru yazı kutusu: uzun soru alt satıra geçer (Unity'nin stili USS'teki white-space'i eziyor).</summary>
    static class SoruKutusu
    {
        public static TextField Kur()
        {
            var kutu = new TextField { maxLength = Oyun.SoruEnFazlaKarakter, multiline = true };
            kutu.AddToClassList("soru-kutusu");
            kutu.verticalScrollerVisibility = ScrollerVisibility.Auto;
            kutu.style.whiteSpace = WhiteSpace.Normal;
            var icYazi = kutu.Q(className: TextField.inputUssClassName)?.Q<TextElement>();
            if (icYazi != null) icYazi.style.whiteSpace = WhiteSpace.Normal;
            return kutu;
        }

        /// <summary>
        /// PC: Enter gönderir (satır atlamaz), odak kutuda kalır. Shift+Enter yok sayılır: soru tek satır.
        /// Telefonda klavyenin "Enter"ı da aynı.
        /// </summary>
        public static void EnterIleGonder(TextField kutu, System.Action gonder)
        {
            kutu.RegisterCallback<KeyDownEvent>(e =>
            {
                bool enter = e.keyCode == UnityEngine.KeyCode.Return || e.keyCode == UnityEngine.KeyCode.KeypadEnter;
                if (!enter && e.character != '\n' && e.character != '\r') return;
                e.StopImmediatePropagation();
                if (enter) gonder();
                kutu.schedule.Execute(() => kutu.Focus());
            }, TrickleDown.TrickleDown);
            // Yapıştırılan satır sonları da tek boşluk olur.
            kutu.RegisterValueChangedCallback(ev =>
            {
                var v = ev.newValue ?? "";
                if (v.IndexOf('\n') >= 0 || v.IndexOf('\r') >= 0)
                    kutu.SetValueWithoutNotify(v.Replace("\r\n", " ").Replace('\n', ' ').Replace('\r', ' '));
            });
        }
    }
}
