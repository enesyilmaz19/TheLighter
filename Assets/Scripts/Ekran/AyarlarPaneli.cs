using UnityEngine.UIElements;
using static TheLighter.Istemci.Arayuz;

namespace TheLighter.Istemci
{
    /// <summary>
    /// Ayarlar katmanı (ana menü, lobi ve oyun menüsünden): ses, müzik, dil. Değerler <see cref="Tercihler"/>'de saklanır.
    /// Dil değişince ekran yeni dille baştan kurulur (<see cref="Uygulama.DilDegisti"/>).
    /// </summary>
    public sealed class AyarlarPaneli
    {
        readonly Uygulama uygulama;
        readonly Button tr;
        readonly Button en;

        public VisualElement Kok { get; }

        public AyarlarPaneli(Uygulama uygulama)
        {
            this.uygulama = uygulama;
            Kok = Kutu("menu-perde", "ayarlar", "gizli");
            var kutu = Kutu("menu-kutu");
            kutu.Add(Yazi(Metin.Al("ayarlar.baslik"), "baslik"));

            kutu.Add(Kaydirici("ayarlar.ses", Tercihler.SesSeviyesi, v => Tercihler.SesSeviyesi = v));
            kutu.Add(Kaydirici("ayarlar.muzik", Tercihler.MuzikSeviyesi, v => Tercihler.MuzikSeviyesi = v));

            kutu.Add(Yazi(Metin.Al("ayarlar.dil"), "alt-baslik"));
            var diller = Kutu("secenekler");
            tr = Buton(Metin.Al("dil.tr"), () => DilSec(Tercihler.Turkce), "secenek");
            en = Buton(Metin.Al("dil.en"), () => DilSec(Tercihler.Ingilizce), "secenek");
            diller.Add(tr);
            diller.Add(en);
            kutu.Add(diller);
            kutu.Add(Yazi(Metin.Al("ayarlar.dil_ipucu"), "ipucu"));

            // Kullanım Şartları ve Gizlilik Politikası (Play şartı; adresler yayınlanınca gelir).
            kutu.Add(Yazi(Metin.Al("ayarlar.yasal"), "alt-baslik"));
            kutu.Add(YasalBaglantilar());

            kutu.Add(Buton(Metin.Al("genel.kapat"), Kapat, "buton-birincil"));
            Kok.Add(kutu);
            DilleriCiz();
        }

        static VisualElement Kaydirici(string baslik, float deger, System.Action<float> degisti)
        {
            var bolum = Kutu("ayar-satiri");
            var ust = Kutu("satir");
            ust.Add(Yazi(Metin.Al(baslik), "alt-baslik"));
            ust.Add(Esnek());
            var yuzde = Yazi(Yuzde(deger), "sayac");
            ust.Add(yuzde);
            bolum.Add(ust);
            var k = new Slider(0f, 100f) { value = deger * 100f };
            k.AddToClassList("ayar-kaydirici");
            k.RegisterValueChangedCallback(ev =>
            {
                yuzde.text = Yuzde(ev.newValue / 100f);
                degisti(ev.newValue / 100f);
            });
            bolum.Add(k);
            return bolum;
        }

        /// <summary>Kullanım Şartları ve Gizlilik Politikası düğmeleri. Adres yoksa "(yakında)" ve basılmaz. Kabul ekranında da.</summary>
        public static VisualElement YasalBaglantilar()
        {
            var satir = Kutu("secenekler");
            satir.Add(BaglantiDugmesi("sartlar.kullanim", Baglantilar.KullanimSartlari));
            satir.Add(BaglantiDugmesi("sartlar.gizlilik", Baglantilar.Gizlilik));
            return satir;
        }

        static Button BaglantiDugmesi(string ad, string adres)
        {
            bool acik = Baglantilar.Var(adres);
            var b = Buton(acik ? Metin.Al(ad) : Metin.Al("sartlar.yakinda", Metin.Al(ad)), () => Baglantilar.Ac(adres), "secenek");
            b.SetEnabled(acik);
            return b;
        }

        static string Yuzde(float v) => UnityEngine.Mathf.RoundToInt(v * 100f) + "%";

        void DilleriCiz()
        {
            tr.EnableInClassList("secili", Tercihler.Dil == Tercihler.Turkce);
            en.EnableInClassList("secili", Tercihler.Dil == Tercihler.Ingilizce);
        }

        void DilSec(string dil)
        {
            if (dil == Tercihler.Dil) return;
            Tercihler.Dil = dil;
            DilleriCiz();
            uygulama.DilDegisti();
        }

        public void Ac() => Kok.RemoveFromClassList("gizli");

        public void Kapat() => Kok.AddToClassList("gizli");
    }
}
