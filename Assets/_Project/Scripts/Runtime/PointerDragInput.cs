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
        /// Sürükleme düzlemi: yığının durduğu dünya düzlemi (z = 0).
        ///
        /// Neden ScreenToWorldPoint + sabit uzaklık değil? O yöntem kameranın konumuna
        /// ve FOV'una bağlı; kamerayı bir santim oynattığım anda ayarladığım his bozulur.
        /// Düzlem + Plane.Raycast ise kamera nereye giderse gitsin sürüklemeyi hep aynı
        /// dünya düzleminde tutuyor.
        ///
        /// Önce düzlemi kameraya baktırmıştım (<c>-camera.forward</c> normali). Kamera
        /// 8° aşağı baktığı için o düzlem de 8° yatıktı: parmağı yukarı sürüklemek
        /// kutuyu yukarı **ve arkaya** taşıyordu. Kutular farklı yüksekliklerde
        /// bırakıldığı için her biri farklı derinlikte kalıyor, kule kameradan düzgün
        /// görünürken aslında derinlemesine kayıyor ve arkaya deviriliyordu.
        ///
        /// Düzlemi dünyanın XY düzlemine sabitlemek bunu kökünden kesiyor: derinlik
        /// girdiyle hiç değişmiyor. Tek yığın var, bütün kutuların aynı derinlikte
        /// kalması zaten gereken şeydi.
        /// </summary>
        Plane dragPlane;

        void Awake()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            // Normal +z'ye bakıyor, düzlem orijinden geçiyor: yani z = 0 düzlemi.
            dragPlane = new Plane(Vector3.back, Vector3.zero);
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
                // Arayüz önce. Oyunun içinde tek bir düğme var (duraklatma
                // dişlisi) ama o düğmeye dokunmak aynı anda altındaki kutuyu da
                // yakalıyordu: iki okuyucu aynı basışı görüyor ve ikisi de kendi
                // işini yapıyor. uGUI'de bunu EventSystem çözüyor; burada
                // dokunuşu zaten kendimiz okuduğumuz için tek gereken kayıtlı
                // dikdörtgenlere bakmak.
                if (!UIBlocker.Blocks(screenPosition))
                {
                    TryPick(screenPosition);
                }
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

            // Yerleştirilmiş kutuya dokunulamıyor: sadece sıradaki kutu alınabilir.
            if (body == null || !body.CanGrab)
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
