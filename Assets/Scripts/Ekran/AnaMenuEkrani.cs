using TheLighter.Akis;
using UnityEngine.UIElements;
using static TheLighter.Istemci.Arayuz;

namespace TheLighter.Istemci
{
    /// <summary>Ana menü: elden ele, online, nasıl oynanır, ayarlar (ses, müzik, dil), oyundan çık.</summary>
    public sealed class AnaMenuEkrani : IEkran, IYenidenKurulur, IGeriTusu
    {
        readonly Uygulama uygulama;
        readonly VisualElement nasil;
        readonly AyarlarPaneli ayarlar;
        readonly OnayPerdesi onay = new OnayPerdesi();

        public VisualElement Kok { get; }

        public AnaMenuEkrani(Uygulama uygulama)
        {
            this.uygulama = uygulama;
            Kok = Kutu("ekran", "ana-menu");

            Kok.Add(Esnek());
            Kok.Add(Yazi(Metin.Al("menu.baslik"), "logo"));
            Kok.Add(Yazi(Metin.Al("menu.alt_baslik"), "logo-alt"));
            Kok.Add(Esnek());

            Kok.Add(Buton(Metin.Al("menu.elden_ele"), uygulama.Kurulum, "buton-birincil", "buton-buyuk"));

            Kok.Add(Buton(Metin.Al("menu.online"), () => uygulama.OnlineMenu(null), "buton-ikincil"));

            Kok.Add(Buton(Metin.Al("menu.nasil_oynanir"), NasilAcKapa, "buton-ikincil"));

            ayarlar = new AyarlarPaneli(uygulama);
            Kok.Add(Buton(Metin.Al("menu.ayarlar"), ayarlar.Ac, "buton-ikincil"));
            Kok.Add(Buton(Metin.Al("menu.cikis"), uygulama.OyundanCik, "buton-ikincil"));

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
            Kok.Add(ayarlar.Kok);
            Kok.Add(onay.Kok);
        }

        public GeriEkrani GeriTuru => GeriEkrani.AnaMenu;

        /// <summary>Geri tuşu ana menüde: uygulamadan çıkılsın mı? (habersiz kapanmaz).</summary>
        public void CikisSor() => onay.Sor("oyun.cikis_soru", "oyun.cikis_ipucu", "oyun.cikis_evet", uygulama.OyundanCik);

        public IEkran YenidenKur()
        {
            var e = new AnaMenuEkrani(uygulama);
            e.ayarlar.Ac();
            return e;
        }

        void NasilAcKapa()
        {
            nasil.EnableInClassList("gizli", !nasil.ClassListContains("gizli"));
        }

        public void Guncelle(float gecenSn) { }

        public void ArkaPlanaGecti() { }
    }
}
