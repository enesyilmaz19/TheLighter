using TheLighter.Akis;
using UnityEngine.UIElements;
using static TheLighter.Istemci.Arayuz;

namespace TheLighter.Istemci
{
    /// <summary>
    /// Sunucu bu sürümü kabul etmedi (<c>GUNCELLE</c>, docs/Protokol.md "Sürüm"): online için oyunun güncellenmesi lazım.
    /// Elden ele ağsız, oynanmaya devam eder.
    /// </summary>
    public sealed class GuncelleEkrani : IEkran, IGeriTusu
    {
        public VisualElement Kok { get; }

        public GuncelleEkrani(Uygulama uygulama)
        {
            Kok = Kutu("ekran", "guncelle");
            Kok.Add(Esnek());
            Kok.Add(Yazi(Metin.Al("guncelle.baslik"), "baslik", "orta"));
            Kok.Add(Yazi(Metin.Al("guncelle.aciklama"), "aciklama", "orta"));
            Kok.Add(Esnek());
            Kok.Add(Buton(Metin.Al("guncelle.elden_ele"), uygulama.Kurulum, "buton-birincil", "buton-buyuk"));
            Kok.Add(Buton(Metin.Al("oyun.ana_menu"), uygulama.AnaMenu, "buton-ikincil"));
        }

        public void Guncelle(float gecenSn) { }

        public void ArkaPlanaGecti() { }

        public GeriEkrani GeriTuru => GeriEkrani.Guncelle;

        public void CikisSor() { }
    }
}
