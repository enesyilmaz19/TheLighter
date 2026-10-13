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
