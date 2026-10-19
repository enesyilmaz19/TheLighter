using System;
using Cakmak.Kurallar;
using TheLighter.Ag;
using UnityEngine.UIElements;
using static TheLighter.Istemci.Arayuz;

namespace TheLighter.Istemci
{
    /// <summary>
    /// Bir oyuncuya uzun basınca (ya da yanındaki "…") açılan katman: Engelle / Engeli kaldır, Raporla
    /// (Google Play kullanıcı içeriği şartı; tasks/g2-android.md 6, docs/Protokol.md "Rapor ve engelleme").
    /// Sadece online: elden elede kimlik ve sohbet yok. Kendinde açılmaz.
    /// Engel onaysız ve hemen; rapor onay ister. Engel listesi sunucudan (<see cref="OnlineIstemci.Engellenenler"/>).
    /// </summary>
    public sealed class OyuncuIslemleri
    {
        enum Adim { Secim, RaporOnay, RaporGidiyor, RaporAlindi, RaporHata }

        readonly OnlineIstemci istemci;
        OyuncuId kim;
        string isim;
        Adim adim;
        int cizilenEngel = -1;

        public VisualElement Kok { get; } = Kutu("menu-perde", "gizli");

        public bool Acik => !Kok.ClassListContains("gizli");

        public OyuncuIslemleri(OnlineIstemci istemci)
        {
            this.istemci = istemci;
            // Ekran kapanınca olaydan çıkar (ekranlar sık yeniden kurulur).
            Kok.RegisterCallback<AttachToPanelEvent>(_ => istemci.RaporCevabi += RaporCevabi);
            Kok.RegisterCallback<DetachFromPanelEvent>(_ => istemci.RaporCevabi -= RaporCevabi);
        }

        /// <summary>Listede ve sohbette uzun basınca. Kendin ya da odada değilsen bir şey olmaz.</summary>
        public void Ac(OyuncuId kisi, string ad)
        {
            if (!istemci.Sen.HasValue || kisi == istemci.Sen.Value) return;
            kim = kisi;
            isim = ad ?? "";
            adim = Adim.Secim;
            Ciz();
            Kok.RemoveFromClassList("gizli");
            Kok.BringToFront();
        }

        public void Kapat() => Kok.AddToClassList("gizli");

        /// <summary>Her karede: engel listesi değiştiyse (sunucunun cevabı geldi) düğmeler yenilenir.</summary>
        public void Guncelle()
        {
            if (Acik && istemci.EngelSurumu != cizilenEngel) Ciz();
        }

        void RaporCevabi(OyuncuId kisi, bool alindi)
        {
            if (!Acik || kisi != kim || adim != Adim.RaporGidiyor) return;
            adim = alindi ? Adim.RaporAlindi : Adim.RaporHata;
            Ciz();
        }

        void Ciz()
        {
            cizilenEngel = istemci.EngelSurumu;
            bool engelli = istemci.EngelliMi(kim);
            Kok.Clear();
            var kutu = Kutu("menu-kutu");
            kutu.Add(Yazi(isim, "baslik"));
            switch (adim)
            {
                case Adim.Secim:
                    // Engel: onaysız, hemen; geri alınabilir. Oyun aynen sürer.
                    kutu.Add(Buton(Metin.Al(engelli ? "rapor.engeli_kaldir" : "rapor.engelle"),
                        () => istemci.Engelle(kim, !engelli), "buton-ikincil"));
                    kutu.Add(Yazi(Metin.Al(engelli ? "rapor.engelli_ipucu" : "rapor.engel_ipucu"), "ipucu"));
                    kutu.Add(Buton(Metin.Al("rapor.raporla"), () => { adim = Adim.RaporOnay; Ciz(); }, "buton-ikincil"));
                    kutu.Add(Buton(Metin.Al("genel.kapat"), Kapat, "buton-birincil"));
                    break;

                case Adim.RaporOnay:
                    kutu.Add(Yazi(Metin.Al("rapor.onay"), "aciklama"));
                    kutu.Add(Buton(Metin.Al("rapor.raporla"), Raporla, "buton-birincil"));
                    kutu.Add(Buton(Metin.Al("genel.geri"), () => { adim = Adim.Secim; Ciz(); }, "buton-ikincil"));
                    break;

                case Adim.RaporGidiyor:
                    kutu.Add(Yazi(Metin.Al("rapor.gidiyor"), "aciklama"));
                    kutu.Add(Buton(Metin.Al("genel.kapat"), Kapat, "buton-ikincil"));
                    break;

                case Adim.RaporAlindi:
                    kutu.Add(Yazi(Metin.Al("rapor.alindi"), "aciklama"));
                    if (engelli) kutu.Add(Yazi(Metin.Al("rapor.engelli_ipucu"), "ipucu"));
                    else
                    {
                        // Kısayol: rapordan sonra engellemek isteyen için.
                        kutu.Add(Yazi(Metin.Al("rapor.engellemek_ister_misin"), "alt-baslik"));
                        kutu.Add(Buton(Metin.Al("rapor.engelle"), () => istemci.Engelle(kim), "buton-ikincil"));
                    }
                    kutu.Add(Buton(Metin.Al("genel.kapat"), Kapat, "buton-birincil"));
                    break;

                case Adim.RaporHata:
                    kutu.Add(Yazi(Metin.Al("rapor.gonderilemedi"), "hata"));
                    kutu.Add(Buton(Metin.Al("rapor.tekrar"), Raporla, "buton-birincil"));
                    kutu.Add(Buton(Metin.Al("genel.kapat"), Kapat, "buton-ikincil"));
                    break;
            }
            Kok.Add(kutu);
        }

        void Raporla()
        {
            adim = istemci.Raporla(kim) ? Adim.RaporGidiyor : Adim.RaporHata;
            Ciz();
        }

        // ---------- Listeler için ----------

        /// <summary>
        /// Oyuncu satırına "…" düğmesi ve uzun basma ekler (kendi satırında değil). Engellediysen "engelli" rozeti.
        /// </summary>
        public static void SatiraEkle(VisualElement satir, OnlineIstemci istemci, OyuncuId id, string ad, Action<OyuncuId, string> ac)
        {
            if (ac == null || !istemci.Sen.HasValue || id == istemci.Sen.Value) return;
            if (istemci.EngelliMi(id)) satir.Add(Yazi(Metin.Al("rapor.engelli_etiket"), "lobi-rozet", "soluk"));
            satir.Add(Buton("…", () => ac(id, ad), "buton-kucuk", "islem-dugmesi"));
            UzunBas(satir, () => ac(id, ad));
        }
    }
}
