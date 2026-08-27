using UnityEngine;

namespace PhysicsStack
{
    /// <summary>
    /// Bırakma çizgisini çizer: kutunun altına inemeyeceği yükseklik.
    ///
    /// Kısıtı yeniden hesaplamıyor, kutunun kendisinden okuyor. Aynı sayıyı iki
    /// yerde hesaplasaydım ekrandaki çizgi ile fiilen uygulanan kural zamanla
    /// birbirinden ayrılırdı — ve oyuncunun göreceği tek şey "çizgiye kadar
    /// indiremiyorum" olurdu.
    ///
    /// Çizgi yalnızca kutu elde ya da beklerken görünüyor: kutu düşerken kural
    /// zaten devrede değil, duran bir çizgi orada sadece gürültü olurdu.
    /// </summary>
    public sealed class DropLineView : MonoBehaviour
    {
        [SerializeField] StackGameController controller;
        [SerializeField] BoxQueue queue;
        [SerializeField] Renderer line;

        [Tooltip("Hedef çizgisinden ayrışsın diye farklı renk: biri hedef, diğeri kısıt.")]
        [SerializeField] Color color = new(0.30f, 0.50f, 0.68f);

        MaterialPropertyBlock block;

        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        void Awake()
        {
            block = new MaterialPropertyBlock();

            line.GetPropertyBlock(block);
            block.SetColor(BaseColor, color);
            line.SetPropertyBlock(block);
        }

        void LateUpdate()
        {
            var current = queue.Current;
            bool visible = current != null &&
                           controller.State is GameState.WaitingForDrag or GameState.Dragging;

            line.enabled = visible;

            if (!visible)
            {
                return;
            }

            var position = line.transform.position;
            line.transform.position = new Vector3(position.x, current.DropLineY, position.z);
        }
    }
}
