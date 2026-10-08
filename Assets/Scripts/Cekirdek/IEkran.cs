using UnityEngine.UIElements;

namespace TheLighter.Istemci
{
    /// <summary>Tam ekranlardan biri: ana menü, kurulum, oyun.</summary>
    public interface IEkran
    {
        VisualElement Kok { get; }

        /// <summary>Her karede çağrılır.</summary>
        void Guncelle(float gecenSn);

        /// <summary>Uygulama arka plana geçti (biri WhatsApp'a baktı). Oyun ekranı duraklar.</summary>
        void ArkaPlanaGecti();
    }
}
