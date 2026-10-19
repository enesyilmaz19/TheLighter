using System.Collections.Generic;
using Cakmak.Kurallar;
using TheLighter.Akis;
using UnityEngine.UIElements;
using static TheLighter.Istemci.Arayuz;

namespace TheLighter.Istemci
{
    /// <summary>
    /// Takım Düellosu ekranlarının ortak parçaları (docs/Oyunlar.md 3, tasks/g2-takim.md): takım adı ve rengi (motor sadece
    /// 0 ve 1 bilir), skor tabelası, puan anı, oyun sonu. Puanlar ve takımlar kural motorunun görünümünden.
    /// </summary>
    static class Takim
    {
        public static string Ad(int takim) => Metin.Al("takim.ad." + takim);

        public static string Sinif(int takim) => "takim-" + takim;

        /// <summary>İsmin yanına takım adı. Takım oyunu değilse null.</summary>
        public static string Etiket(OyuncuDurumu o) => o != null && o.Takim.HasValue ? Ad(o.Takim.Value) : null;

        public static int? TakimiNe(Gorunum g, OyuncuId? id)
        {
            if (g == null || !id.HasValue) return null;
            foreach (var o in g.Oyuncular) if (o.Id == id.Value) return o.Takim;
            return null;
        }

        /// <summary>Seçim çipine takım rengi (kenar). Takım oyunu değilse dokunmaz.</summary>
        public static void Boya(VisualElement cip, Gorunum g, OyuncuId id)
        {
            var t = TakimiNe(g, id);
            if (!t.HasValue) return;
            cip.AddToClassList("takim-cip");
            cip.AddToClassList(Sinif(t.Value));
        }

        /// <summary>"Kırmızı takımdasın." (görünümün sahibi). Takım oyunu değilse null.</summary>
        public static Label SeninTakimin(Gorunum g)
        {
            var t = TakimiNe(g, g?.Sen);
            if (!t.HasValue) return null;
            return Yazi(Metin.Al("takim.senin", Ad(t.Value)), "ipucu", "takim-ust-puan", Sinif(t.Value));
        }

        static bool Var(Gorunum g) => g != null && g.TakimPuanlari.Count >= Oyun.TakimSayisi;

        /// <summary>
        /// Tek sayıda kişide küçük takım 1 puanla başlar (kural motoru verir): "+1 avans" hangi takımın yanında. Takımlar
        /// eşitse null. Puan görünümden; burada sadece hangi takımın küçük olduğuna bakılır.
        /// </summary>
        public static int? Avansli(Gorunum g)
        {
            if (!Var(g)) return null;
            var sayi = new int[Oyun.TakimSayisi];
            foreach (var o in g.Oyuncular) if (o.Takim.HasValue && o.Takim.Value >= 0 && o.Takim.Value < sayi.Length) sayi[o.Takim.Value]++;
            if (sayi[0] == sayi[1]) return null;
            return sayi[0] < sayi[1] ? 0 : 1;
        }

        /// <summary>Üst çubuk için küçük skor: iki renkli sayı ve hedef. Takım oyunu değilse boş ve gizli.</summary>
        public static void UstSkoruYaz(VisualElement kutu, Gorunum g)
        {
            kutu.Clear();
            kutu.EnableInClassList("gizli", !Var(g));
            if (!Var(g)) return;
            kutu.Add(Yazi(Ad(0) + " " + g.TakimPuanlari[0], "takim-ust-puan", Sinif(0)));
            kutu.Add(Yazi("–", "ust-cubuk-yazi"));
            kutu.Add(Yazi(g.TakimPuanlari[1] + " " + Ad(1), "takim-ust-puan", Sinif(1)));
            kutu.Add(Yazi(Metin.Al("takim.hedef_kisa", Oyun.HedefPuan), "ust-cubuk-yazi"));
        }

        /// <summary>Büyük skor tabelası: iki renkli kutu, hedef, küçük takımın avansı.</summary>
        public static VisualElement Tabela(Gorunum g)
        {
            var kutu = Kutu("satir", "takim-tabela");
            if (!Var(g)) return kutu;
            var avans = Avansli(g);
            for (int t = 0; t < Oyun.TakimSayisi; t++)
            {
                var k = Kutu("takim-skor", Sinif(t));
                k.Add(Yazi(Ad(t), "takim-ad"));
                k.Add(Yazi(g.TakimPuanlari[t].ToString(), "takim-puan"));
                if (avans == t) k.Add(Yazi(Metin.Al("takim.avans"), "takim-avans"));
                kutu.Add(k);
                if (t == 0) kutu.Add(Yazi(Metin.Al("takim.hedef", Oyun.HedefPuan), "ipucu", "takim-hedef"));
            }
            return kutu;
        }

        public static string PuanMetni(IOyunAkisi akis, PuanAni p) =>
            p.IfsaBonusu
                ? Metin.Al("takim.ifsa_bonusu", Ad(p.Takim), akis.Isim(p.Kim))
                : Metin.Al("takim.duello_puani", Ad(p.Takim), akis.Isim(p.Kim));

        /// <summary>"+1 Mavi" duyurusu, kısa bir büyüme animasyonuyla.</summary>
        public static Label PuanDuyurusu(IOyunAkisi akis, PuanAni p)
        {
            var l = Yazi(PuanMetni(akis, p), "banner", "puan-ani", Sinif(p.Takim));
            Canlandir(l);
            return l;
        }

        /// <summary>Kısa "pop": büyük başlar, USS geçişiyle normale iner (Stil.uss .puan-ani).</summary>
        public static void Canlandir(VisualElement v)
        {
            v.AddToClassList("puan-ani");
            v.AddToClassList("puan-ani-bas");
            v.schedule.Execute(() => v.RemoveFromClassList("puan-ani-bas")).StartingIn(60);
        }

        /// <summary>Oyun sonunun en üstü: kazanan takım (ya da berabere), puanlar, takımların üyeleri.</summary>
        public static VisualElement Acilis(IOyunAkisi akis, TakimSonucu t, IReadOnlyList<OyuncuDurumu> oyuncular)
        {
            var kart = Kutu("kostebek-acilis", t.Kazanan.HasValue ? Sinif(t.Kazanan.Value) : "takim-berabere", "takim-acilis");
            kart.Add(Yazi(t.Kazanan.HasValue ? Metin.Al("takim.kazandi", Ad(t.Kazanan.Value)) : Metin.Al("takim.berabere"), "kostebek-kazanan"));
            if (t.Puanlar.Count >= Oyun.TakimSayisi)
                kart.Add(Yazi(Metin.Al("takim.son_skor", Ad(0), t.Puanlar[0], t.Puanlar[1], Ad(1)), "aciklama", "orta"));
            for (int takim = 0; takim < Oyun.TakimSayisi; takim++)
            {
                var uyeler = new List<string>();
                if (oyuncular != null)
                    foreach (var o in oyuncular) if (o.Takim == takim) uyeler.Add(akis.Isim(o.Id));
                var satir = Kutu("satir", "orta-satir");
                satir.Add(Yazi(Ad(takim), "takim-ust-puan", Sinif(takim)));
                satir.Add(Yazi(string.Join(", ", uyeler), "aciklama"));
                kart.Add(satir);
            }
            return kart;
        }
    }

    /// <summary>
    /// Takım dağıtma (elden ele kurulum ve online lobi): iki sütun, dokununca takım değişir, "Rastgele" dağılımı siler.
    /// Geçerli mi kural motoruna sorulur (<see cref="TakimDagitimi.Gecerli"/>). Kurucu olmayan sadece görür.
    /// </summary>
    sealed class TakimDagitmaParcasi
    {
        public readonly VisualElement Kok;
        readonly VisualElement icerik;
        readonly Label uyari;

        public TakimDagitmaParcasi()
        {
            Kok = Kutu("ayar-bolumu", "takim-dagitma");
            Kok.Add(Yazi(Metin.Al("takim.dagit_baslik"), "alt-baslik"));
            icerik = Kutu("takim-dagit-icerik");
            Kok.Add(icerik);
            uyari = Yazi("", "hata", "takim-uyari");
            Kok.Add(uyari);
            Kok.Add(Yazi(Metin.Al("takim.avans_ipucu"), "ipucu"));
        }

        /// <summary>
        /// Baştan çizer. <paramref name="kisiler"/>: sıralı. <paramref name="dokun"/> null ise sadece gösterir (online'da
        /// kurucu olmayan); <paramref name="rastgele"/> "Rastgele" düğmesi. Geçersizse uyarı metni döner (yoksa null).
        /// </summary>
        public string Ciz(TakimDagitimi d, IReadOnlyList<OyuncuId> kisiler, System.Func<OyuncuId, string> isim,
            System.Func<OyuncuId, int> renk, System.Action<OyuncuId> dokun, System.Action rastgele)
        {
            icerik.Clear();
            bool degisir = dokun != null;

            if (d.Rastgele)
                icerik.Add(Yazi(Metin.Al(degisir ? "takim.rastgele_ipucu" : "takim.rastgele_bilgi"), "ipucu"));
            else if (degisir)
                icerik.Add(Yazi(Metin.Al("takim.dokun_ipucu"), "ipucu"));

            var takimsiz = d.Takimsizlar(kisiler);
            if (takimsiz.Count > 0)
            {
                var kutu = Kutu("cipler", "takim-takimsiz");
                foreach (var id in takimsiz) kutu.Add(Cip(id, isim, renk, dokun));
                icerik.Add(kutu);
            }

            if (!d.Rastgele)
            {
                var sutunlar = Kutu("satir", "takim-sutunlar");
                for (int t = 0; t < Oyun.TakimSayisi; t++)
                {
                    var uyeler = d.Uyeler(kisiler, t);
                    var sutun = Kutu("takim-sutun", Takim.Sinif(t));
                    sutun.Add(Yazi(Metin.Al("takim.sutun_baslik", Takim.Ad(t), uyeler.Count), "takim-ad"));
                    foreach (var id in uyeler) sutun.Add(Cip(id, isim, renk, dokun));
                    sutunlar.Add(sutun);
                }
                icerik.Add(sutunlar);
                if (rastgele != null) icerik.Add(Buton(Metin.Al("takim.rastgele"), rastgele, "buton-ikincil"));
            }

            string metin = UyariMetni(d, kisiler);
            uyari.text = metin ?? "";
            uyari.EnableInClassList("gizli", metin == null);
            return metin;
        }

        static VisualElement Cip(OyuncuId id, System.Func<OyuncuId, string> isim, System.Func<OyuncuId, int> renk, System.Action<OyuncuId> dokun)
        {
            if (dokun != null) return OyuncuCipi(isim(id), renk(id), () => dokun(id));
            // Kurucu olmayan: düğme değil, salt çip (soluk kapalı düğme okunmaz).
            var cip = Kutu("cip-salt");
            cip.Add(RenkNoktasi(renk(id)));
            cip.Add(Yazi(isim(id), "cip-yazi"));
            return cip;
        }

        /// <summary>Kural motoru dağılımı kabul etmiyorsa neden: biri takımsız ya da denge bozuk. Bilinmiyorsa ya da geçerliyse null.</summary>
        public static string UyariMetni(TakimDagitimi d, IReadOnlyList<OyuncuId> kisiler)
        {
            if (d.Gecerli(kisiler) != false) return null;
            return Metin.Al(d.Takimsizlar(kisiler).Count > 0 ? "takim.uyari_eksik" : "takim.uyari_denge");
        }
    }
}
