using System;
using UnityEngine.UIElements;
using static TheLighter.Istemci.Arayuz;

namespace TheLighter.Istemci
{
    /// <summary>
    /// "Emin misin?" katmanı: soru, kısa açıklama, evet ve geri. Geri tuşu ve "Geri" kapatır.
    /// Ekranın en sonuna eklenir ki her şeyin üstünde dursun.
    /// </summary>
    public sealed class OnayPerdesi
    {
        public VisualElement Kok { get; } = Kutu("menu-perde", "gizli");

        public bool Acik => !Kok.ClassListContains("gizli");

        /// <param name="soru">Metin anahtarı.</param>
        /// <param name="ipucu">Metin anahtarı.</param>
        /// <param name="evet">Metin anahtarı.</param>
        public void Sor(string soru, string ipucu, string evet, Action tamam)
        {
            Kok.Clear();
            var kutu = Kutu("menu-kutu");
            kutu.Add(Yazi(Metin.Al(soru), "baslik"));
            kutu.Add(Yazi(Metin.Al(ipucu), "ipucu"));
            kutu.Add(Buton(Metin.Al(evet), () => { Kapat(); tamam(); }, "buton-birincil"));
            kutu.Add(Buton(Metin.Al("genel.geri"), Kapat, "buton-ikincil"));
            Kok.Add(kutu);
            Kok.RemoveFromClassList("gizli");
            Kok.BringToFront();
        }

        public void Kapat() => Kok.AddToClassList("gizli");
    }
}
