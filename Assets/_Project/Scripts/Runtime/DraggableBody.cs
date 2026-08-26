using UnityEngine;

namespace PhysicsStack
{
    /// <summary>
    /// Sürüklenebilir kutu. Üç takip yaklaşımı da burada duruyor; hangisinin
    /// neden elendiği ancak sırayla denenince anlaşılıyor, o yüzden elenenleri
    /// silmek yerine Inspector'dan seçilebilir bıraktım.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class DraggableBody : MonoBehaviour
    {
        public enum FollowMode
        {
            /// <summary>1. yaklaşım: rb.position doğrudan set edilir.</summary>
            DirectPosition,

            /// <summary>2. yaklaşım: kinematik hale getirilip MovePosition ile taşınır.</summary>
            KinematicMovePosition,

            /// <summary>3. yaklaşım (seçilen): dinamik kalır, hedefe doğru hız atanır.</summary>
            VelocityFollow,
        }

        [SerializeField] FollowMode mode = FollowMode.VelocityFollow;

        [Header("Hız tabanlı takip")]
        [Tooltip("1 = parmağa tam yetişmeye çalışır, 0.2 = ağır ve gecikmeli hisseder.")]
        [SerializeField, Range(0.05f, 1f)] float followStrength = 0.35f;

        [Tooltip("Kutunun ulaşabileceği en yüksek hız. Ağırlık hissi buradan geliyor.")]
        [SerializeField] float maxSpeed = 14f;

        Rigidbody rb;
        bool isDragged;

        /// <summary>
        /// Parmağın son pozisyonu. Update'te yazılır, FixedUpdate'te okunur.
        /// İkisini karıştırmamak bu prototipin en kritik kuralı: girdi kare hızında
        /// gelir, fizik sabit adımda çalışır. Girdiyi doğrudan fiziğe bağlarsak aynı
        /// hareket bazı fizik adımlarında iki kez, bazılarında hiç işlenmez ve his
        /// kare hızına bağlı hale gelir.
        /// </summary>
        Vector3 targetPoint;

        public bool IsDragged => isDragged;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        public void BeginDrag(Vector3 point)
        {
            targetPoint = point;
            isDragged = true;

            if (mode == FollowMode.KinematicMovePosition)
            {
                rb.isKinematic = true;
            }
        }

        /// <summary>Update'ten çağrılır; sadece hedefi günceller, fiziğe dokunmaz.</summary>
        public void MoveTarget(Vector3 point)
        {
            targetPoint = point;
        }

        public void EndDrag()
        {
            isDragged = false;

            switch (mode)
            {
                case FollowMode.DirectPosition:
                    // Kutu her kare ışınlandığı için rigidbody'nin üstünde anlamlı bir
                    // hız birikmiyor; sıfırlamasak da bırakınca olduğu yerde düşüyor.
                    rb.linearVelocity = Vector3.zero;
                    break;

                case FollowMode.KinematicMovePosition:
                    // Kinematikten dinamiğe dönerken hız sıfır: fırlatma yok, taş gibi düşüş.
                    rb.isKinematic = false;
                    rb.linearVelocity = Vector3.zero;
                    break;

                case FollowMode.VelocityFollow:
                    // Hiçbir şey yapmıyoruz — ve bu yaklaşımın en güzel tarafı bu.
                    // Bırakma anında rigidbody'nin üstünde zaten doğru hız var,
                    // fırlatma ayrı bir kod yazmadan geliyor.
                    break;
            }
        }

        void FixedUpdate()
        {
            if (!isDragged)
            {
                return;
            }

            switch (mode)
            {
                case FollowMode.DirectPosition:
                    // Kutu kareler arasında ışınlanıyor. Çarpışma çözücüsü araya
                    // giremediği için zeminden ve yığından geçiyor.
                    rb.position = targetPoint;
                    break;

                case FollowMode.KinematicMovePosition:
                    // Çarpışmalara saygılı ama kinematik cisim itilemez: sürüklenen
                    // kutu yığını ezip geçiyor, kendisi hiç zorlanmıyor.
                    rb.MovePosition(targetPoint);
                    break;

                case FollowMode.VelocityFollow:
                    ApplyVelocityFollow();
                    break;
            }
        }

        void ApplyVelocityFollow()
        {
            Vector3 delta = targetPoint - rb.position;

            // delta / fixedDeltaTime = "tek fizik adımında oraya varmak için gereken hız".
            // followStrength bunun ne kadarını uygulayacağımızı söylüyor; 1'e yaklaştıkça
            // kutu parmağa yapışıyor, düşük değerlerde arkadan sürüklenen bir ağırlık gibi.
            Vector3 desired = delta / Time.fixedDeltaTime * followStrength;

            // Asıl his buradan geliyor: parmağı hızlı çekince kutu yetişemiyor ve geride
            // kalıyor. Bu gecikme "ağır cisim" olarak okunuyor. Aynı zamanda kutunun
            // çılgın hızlara çıkıp sahneden fırlamasını da engelliyor.
            rb.linearVelocity = Vector3.ClampMagnitude(desired, maxSpeed);
        }
    }
}
