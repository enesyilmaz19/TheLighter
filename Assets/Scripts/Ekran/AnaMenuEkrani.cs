using UnityEngine.UIElements;
using static TheLighter.Istemci.Arayuz;

namespace TheLighter.Istemci
{
    public sealed class AnaMenuEkrani : IEkran
    {
        readonly VisualElement nasil;

        public VisualElement Kok { get; }

        public AnaMenuEkrani(Uygulama uygulama)
        {
            Kok = Kutu("ekran", "ana-menu");

            Kok.Add(Esnek());
            Kok.Add(Yazi(Metin.Al("menu.baslik"), "logo"));
            Kok.Add(Yazi(Metin.Al("menu.alt_baslik"), "logo-alt"));
            Kok.Add(Esnek());

            Kok.Add(Buton(Metin.Al("menu.elden_ele"), uygulama.Kurulum, "buton-birincil", "buton-buyuk"));

            var online = Buton(Metin.Al("menu.online"), null, "buton-ikincil");
            online.SetEnabled(false);
            Kok.Add(online);
            Kok.Add(Yazi(Metin.Al("menu.online_yakinda"), "ipucu"));

            Kok.Add(Buton(Metin.Al("menu.nasil_oynanir"), NasilAcKapa, "buton-ikincil"));

            nasil = Kutu("menu-perde", "gizli");
            var kutu = Kutu("menu-kutu");
            var kaydirma = new ScrollView(ScrollViewMode.Vertical);
            kaydirma.AddToClassList("kaydirma");
            kaydirma.Add(Yazi(Metin.Al("nasil.baslik"), "baslik"));
            kaydirma.Add(Yazi(Metin.Al("nasil.metin"), "aciklama"));
            kutu.Add(kaydirma);
            kutu.Add(Buton(Metin.Al("genel.tamam"), NasilAcKapa, "buton-birincil"));
            nasil.Add(kutu);
            Kok.Add(nasil);
        }

        void NasilAcKapa()
        {
            nasil.EnableInClassList("gizli", !nasil.ClassListContains("gizli"));
        }

        public void Guncelle(float gecenSn) { }

        public void ArkaPlanaGecti() { }
    }
}
