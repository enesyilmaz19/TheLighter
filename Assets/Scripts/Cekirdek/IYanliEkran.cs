using UnityEngine.UIElements;

namespace TheLighter.Istemci
{
    /// <summary>
    /// PC'de geniş pencerede yan sütunları olan ekran (Ana Plan 6.2): solda oyuncular, sağda sohbet, ortada oyun.
    /// Ekran gösterilince ve pencere genişleyip daralınca çağrılır. Yan sütunlar her çağrıda boş gelir.
    /// </summary>
    public interface IYanliEkran
    {
        /// <param name="genis">true: <paramref name="sol"/> ve <paramref name="sag"/>'a koy. false: hepsini kendi sütununa geri al.</param>
        void Yerlesim(bool genis, VisualElement sol, VisualElement sag);
    }
}
