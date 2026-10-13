namespace TheLighter.Istemci
{
    /// <summary>
    /// Dil değişince yeni dille baştan kurulabilen ekran. Yeni ekran ayarlar açık gelir (oyuncu oradaydı).
    /// Durum ekranda değil (akışta, oturumda): yeniden kurmak bir şey kaybettirmez.
    /// </summary>
    public interface IYenidenKurulur
    {
        IEkran YenidenKur();
    }
}
