using TheLighter.Akis;
using UnityEngine.UIElements;
using static TheLighter.Istemci.Arayuz;

namespace TheLighter.Istemci
{
    /// <summary>
    /// Kullanım şartlarının kabulü (Google Play: kullanıcı içerik yazmadan önce; tasks/g2-android.md 6).
    /// İlk açılışta bir kez, oyuna girmeden önce. Metin docs/yasal/KullanimSartlari.md'nin sonundaki "Uygulama içindeki
    /// kabul ekranı". Kabul cihazda saklanır (<see cref="Tercihler.KabulEdilenSartlar"/>); şartların sürümü artınca yeniden çıkar.
    /// Dil buradan da seçilebilir (ilk açılışta İngilizce isteyen için).
    /// </summary>
    public sealed class KabulEkrani : IEkran, IGeriTusu, IYenidenKurulur
    {
        readonly Uygulama uygulama;
        readonly AyarlarPaneli ayarlar;
        readonly OnayPerdesi onay = new OnayPerdesi();

        public VisualElement Kok { get; }

        public KabulEkrani(Uygulama uygulama)
        {
            this.uygulama = uygulama;
            Kok = Kutu("ekran", "kabul");

            var ust = Kutu("satir");
            ust.Add(Esnek());
            ayarlar = new AyarlarPaneli(uygulama);
            ust.Add(Buton(Metin.Al("menu.ayarlar"), ayarlar.Ac, "buton-kucuk"));
            Kok.Add(ust);

            var kaydirma = new ScrollView(ScrollViewMode.Vertical);
            kaydirma.AddToClassList("kaydirma");
            kaydirma.Add(Yazi(Metin.Al("menu.baslik"), "logo"));
            kaydirma.Add(Yazi(Metin.Al("sartlar.baslik"), "baslik", "orta"));
            kaydirma.Add(Yazi(Metin.Al("sartlar.metin"), "aciklama"));
            kaydirma.Add(Yazi(Metin.Al("sartlar.devam"), "aciklama"));
            kaydirma.Add(AyarlarPaneli.YasalBaglantilar());
            Kok.Add(kaydirma);

            Kok.Add(Esnek());
            Kok.Add(Buton(Metin.Al("sartlar.kabul"), uygulama.SartlariKabulEt, "buton-birincil", "buton-buyuk"));

            Kok.Add(ayarlar.Kok);
            Kok.Add(onay.Kok);
        }

        public void Guncelle(float gecenSn) { }

        public void ArkaPlanaGecti() { }

        public GeriEkrani GeriTuru => GeriEkrani.Kabul;

        /// <summary>Kabul etmeden geri: uygulamadan çıkılsın mı? (habersiz kapanmaz).</summary>
        public void CikisSor() => onay.Sor("oyun.cikis_soru", "oyun.cikis_ipucu", "oyun.cikis_evet", uygulama.OyundanCik);

        public IEkran YenidenKur()
        {
            var e = new KabulEkrani(uygulama);
            e.ayarlar.Ac();
            return e;
        }
    }
}
