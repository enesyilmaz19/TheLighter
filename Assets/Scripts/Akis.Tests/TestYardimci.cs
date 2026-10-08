using System.Collections.Generic;
using Cakmak.Kurallar;
using NUnit.Framework;
using TheLighter.Akis;

namespace TheLighter.Tests
{
    static class TestYardimci
    {
        static readonly string[] Isimler = { "Ali", "Ayşe", "Mehmet", "Zeynep", "Can", "Elif", "Burak", "Deniz", "Emre", "Selin" };

        public static List<Oyuncu> Oyuncular(int sayi)
        {
            var l = new List<Oyuncu>();
            for (int i = 0; i < sayi; i++) l.Add(new Oyuncu(new OyuncuId(i + 1), Isimler[i]));
            return l;
        }

        public static Ayarlar Ayar(MiniOyunTuru mini = MiniOyunTuru.Tkm, int tur = 10, IReadOnlyList<string> oneriler = null)
        {
            var a = new Ayarlar { MiniOyun = mini, TurSayisi = tur };
            if (oneriler != null) a.OneriSorulari = oneriler;
            return a;
        }

        public static EldenEleAkisi YeniAkis(int tohum = 1, MiniOyunTuru mini = MiniOyunTuru.Tkm, int tur = 10, int oyuncu = 5,
            IReadOnlyList<string> oneriler = null)
        {
            var s = EldenEleAkisi.Kur(Ayar(mini, tur, oneriler), Oyuncular(oyuncu), tohum, out var akis);
            Assert.AreEqual(Sonuc.Tamam, s);
            return akis;
        }
    }
}
