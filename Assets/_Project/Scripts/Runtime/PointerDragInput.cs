using UnityEngine;
using UnityEngine.InputSystem;

namespace PhysicsStack
{
    /// <summary>
    /// Parmağı (veya editörde fareyi) okur, altındaki kutuyu yakalar ve sürüklerken
    /// hedef noktayı besler. Fiziğe hiç dokunmuyor — o iş <see cref="DraggableBody"/>'de.
    ///
    /// Ayrım bilinçli: girdi okuma platforma bağlı ve kare hızında çalışıyor,
    /// takip mantığı platformdan bağımsız ve sabit adımda. İkisini aynı sınıfa
    /// koyarsam telefonda bozulan şeyin hangisi olduğunu ayırt edemem.
    /// </summary>
    public sealed class PointerDragInput : MonoBehaviour
    {
        [SerializeField] Camera targetCamera;

        [Tooltip("Raycast ön elemesi. Asıl filtre DraggableBody bileşeni: zemin onu taşımadığı için zaten yakalanmıyor.")]
        [SerializeField] LayerMask pickableMask = ~0;

        [SerializeField] float maxPickDistance = 100f;

        DraggableBody held;

        /// <summary>
        /// Sürükleme düzlemi: kameraya bakan, dünya orijininden geçen sabit bir düzlem.
        ///
        /// Neden ScreenToWorldPoint + sabit uzaklık değil? O yöntem kameranın konumuna
        /// ve FOV'una bağlı; kamerayı bir santim oynattığım anda ayarladığım his bozulur.
        /// Düzlem + Plane.Raycast ise kamera nereye giderse gitsin sürüklemeyi hep aynı
        /// dünya düzleminde tutuyor.
        ///
        /// Düzlemin sabit olması (kutunun yakalandığı noktadan değil, orijinden geçmesi)
        /// bu oyun için ayrıca isabetli: tek bir yığın var, bütün kutuların aynı derinlikte
        /// kalması gerekiyor. Kutu başına ayrı düzlem kullansaydım yığın derinlemesine
        /// dağılır, kule kamera açısından "duruyor" görünüp aslında birbirine değmezdi.
        /// </summary>
        Plane dragPlane;

        void Awake()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            dragPlane = new Plane(-targetCamera.transform.forward, Vector3.zero);
        }

        void Update()
        {
            // Pointer.current fareyi de dokunmatiği de kapsıyor: editörde fareyle test
            // ettiğim kod telefonda parmakla aynı yoldan geçiyor.
            var pointer = Pointer.current;
            if (pointer == null)
            {
                return;
            }

            Vector2 screenPosition = pointer.position.ReadValue();

            if (pointer.press.wasPressedThisFrame)
            {
                TryPick(screenPosition);
            }
            else if (pointer.press.isPressed && held != null)
            {
                held.MoveTarget(PointOnDragPlane(screenPosition, held.transform.position));
            }
            else if (pointer.press.wasReleasedThisFrame && held != null)
            {
                held.EndDrag();
                held = null;
            }
        }

        void TryPick(Vector2 screenPosition)
        {
            Ray ray = targetCamera.ScreenPointToRay(screenPosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, maxPickDistance, pickableMask))
            {
                return;
            }

            // Collider çocuk nesnede olabilir; bileşeni yukarı doğru arıyoruz.
            var body = hit.collider.GetComponentInParent<DraggableBody>();
            if (body == null)
            {
                return;
            }

            held = body;
            held.BeginDrag(PointOnDragPlane(screenPosition, hit.point));
        }

        Vector3 PointOnDragPlane(Vector2 screenPosition, Vector3 fallback)
        {
            Ray ray = targetCamera.ScreenPointToRay(screenPosition);

            // Işın düzleme paralel kalırsa (kamera aşırı yatarsa) kesişim yok.
            // Böyle bir karede hedefi zıplatmak yerine son bilinen noktada bırakıyoruz.
            return dragPlane.Raycast(ray, out float distance) ? ray.GetPoint(distance) : fallback;
        }
    }
}
