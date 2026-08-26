using UnityEngine;

namespace PhysicsStack
{
    /// <summary>
    /// Oyunun bittiğini hedef çizgisinin rengiyle söyler: kazanınca yeşil,
    /// kaybedince kırmızı.
    ///
    /// Neden bu? Beş kutuyu üst üste koyup kazandığımda oyunun bittiğini
    /// anlamadım — köşedeki debug panelinde tek kelime yazıyordu ama oynarken
    /// oraya bakılmıyor. Bitişin oyuncunun zaten baktığı yerde, kulenin tepesinde
    /// olması gerekiyordu. Menü ya da yazı eklemeden bunu yapmanın en ucuz yolu
    /// sahnede zaten duran bir nesnenin rengini değiştirmek.
    /// </summary>
    public sealed class ResultIndicator : MonoBehaviour
    {
        [SerializeField] StackGameController controller;
        [SerializeField] Renderer targetLine;

        [SerializeField] Color idleColor = new(0.30f, 0.30f, 0.32f);
        [SerializeField] Color wonColor = new(0.25f, 0.75f, 0.35f);
        [SerializeField] Color lostColor = new(0.80f, 0.25f, 0.25f);

        /// <summary>
        /// Renk MaterialPropertyBlock ile veriliyor, <c>renderer.material</c> ile değil.
        /// İkincisi materyalin çalışma zamanı kopyasını çıkarır: hem sahnedeki
        /// varlığa dokunmuş oluruz hem de kopya ayrı bir draw call'a düşer.
        /// PropertyBlock materyali hiç kopyalamadan tek nesnenin rengini değiştiriyor.
        /// </summary>
        MaterialPropertyBlock block;

        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        GameState lastSeen = (GameState)(-1);

        void Awake()
        {
            block = new MaterialPropertyBlock();
        }

        void Update()
        {
            // Her karede renk yazmanın anlamı yok; sadece durum değişince.
            if (controller.State == lastSeen)
            {
                return;
            }

            lastSeen = controller.State;

            targetLine.GetPropertyBlock(block);
            block.SetColor(BaseColor, ColorFor(lastSeen));
            targetLine.SetPropertyBlock(block);
        }

        Color ColorFor(GameState state) => state switch
        {
            GameState.Won => wonColor,
            GameState.Lost => lostColor,
            _ => idleColor,
        };
    }
}
