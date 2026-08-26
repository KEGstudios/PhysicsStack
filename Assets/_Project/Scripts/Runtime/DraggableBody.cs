using System;
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

        [Tooltip("His ayarları. Boş bırakılırsa kod içindeki varsayılanlarla çalışır ama uyarı basar.")]
        [SerializeField] DragSettings settings;

        Rigidbody rb;
        bool isDragged;

        /// <summary>
        /// Kuyruktaki kutu sırasını beklerken havada asılı duruyor: kinematik ve
        /// yerçekimsiz. İlk dokunuşta kendini serbest bırakıyor.
        /// </summary>
        bool isWaiting;

        /// <summary>
        /// Parmağın son pozisyonu. Update'te yazılır, FixedUpdate'te okunur.
        /// İkisini karıştırmamak bu prototipin en kritik kuralı: girdi kare hızında
        /// gelir, fizik sabit adımda çalışır. Girdiyi doğrudan fiziğe bağlarsak aynı
        /// hareket bazı fizik adımlarında iki kez, bazılarında hiç işlenmez ve his
        /// kare hızına bağlı hale gelir.
        /// </summary>
        Vector3 targetPoint;

        public bool IsDragged => isDragged;

        /// <summary>Yerleşme tespiti rigidbody'nin kendisine bakıyor; dışarı açıyoruz.</summary>
        public Rigidbody Body => rb;

        public event Action<DraggableBody> Grabbed;
        public event Action<DraggableBody> Released;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();

            if (settings == null)
            {
                // Sessizce çalışmasındansa bağırarak çalışsın: varlığı prefab'a
                // vermeyi unutmak, "değeri değiştiriyorum ama hiçbir şey olmuyor"
                // diye yarım saat harcanacak türden bir hata.
                Debug.LogWarning($"[DraggableBody] {name}: DragSettings atanmamış, varsayılan değerlerle çalışıyorum.", this);
                settings = ScriptableObject.CreateInstance<DragSettings>();
            }
        }

        /// <summary>
        /// Kuyrukta sırasını bekleyen kutuyu havada tutar. Dinamik bırakırsak
        /// oyuncu dokunmadan düşer; kinematik yapmak "sıradaki kutu" fikrini
        /// ayrı bir bekleme nesnesi icat etmeden veriyor.
        /// </summary>
        public void HoldInPlace()
        {
            isWaiting = true;
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        public void BeginDrag(Vector3 point)
        {
            if (isWaiting)
            {
                isWaiting = false;
                rb.isKinematic = false;
                rb.useGravity = true;
            }

            targetPoint = point;
            isDragged = true;

            Grabbed?.Invoke(this);

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
                    // Bırakma anında rigidbody'nin üstünde zaten doğru hız var,
                    // fırlatma ayrı bir kod yazmadan geliyor — bu yaklaşımın en güzel
                    // tarafı buydu. Ama tamamen serbest bırakınca sorun çıkıyordu:
                    // kuleye yaklaşırken parmağı hızlı oynatıp bırakınca kutu maksimum
                    // hızla kulenin içine giriyor ve altındaki her şeyi süpürüyordu.
                    // Kırpma fırlatmayı öldürmüyor, sadece üst sınırını "kuleyi
                    // yıkmayacak" seviyeye çekiyor.
                    rb.linearVelocity = Vector3.ClampMagnitude(rb.linearVelocity, settings.releaseSpeedClamp);
                    break;
            }

            Released?.Invoke(this);
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
            Vector3 desired = delta / Time.fixedDeltaTime * settings.followStrength;

            // Parmağı hızlı çekince kutu yetişemiyor ve geride kalıyor; bu gecikme
            // "ağır cisim" olarak okunuyor. Aynı zamanda kutunun çılgın hızlara
            // çıkıp sahneden fırlamasını da engelliyor.
            desired = Vector3.ClampMagnitude(desired, settings.maxSpeed);

            // Hızı doğrudan atamak yerine ona doğru yürüyoruz. Doğrudan atama fiziğin
            // karşı koyma hakkını elinden alıyordu: kutu kulenin üstündeki kutuya
            // değdiği anda çözücü hızı sıfırlıyor, biz bir sonraki adımda aynı
            // hızı geri yazıyoruz — yani her fizik adımında kuleye yeni bir darbe.
            // Adım başına değişimi sınırlayınca kutu engele dayandığında hız
            // birikemiyor: itiş sertliğinin üst sınırı artık maxAcceleration.
            //
            // Bu, AddForce(ForceMode.Acceleration) ile aynı hesabın açık yazılmış hali.
            // AddForce'u tercih etmememin sebebi şu: kuvvet uygulayıp sonucu fiziğe
            // bırakınca kutunun ne kadar hızlanacağını kütle, sürtünme ve temas
            // belirliyor, dolayısıyla "parmağa ne kadar yetişecek" sorusunun cevabı
            // elimden çıkıyor. Burada hedef hız benim, ona ulaşma sertliği fiziğin.
            rb.linearVelocity = Vector3.MoveTowards(
                rb.linearVelocity, desired, settings.maxAcceleration * Time.fixedDeltaTime);
        }
    }
}
