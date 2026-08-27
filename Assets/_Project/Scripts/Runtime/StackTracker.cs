using System.Collections.Generic;
using UnityEngine;

namespace PhysicsStack
{
    /// <summary>
    /// Yığına giren kutuların kaydını tutar ve iki soruyu cevaplar:
    /// "her şey durdu mu" ve "yığının tepesi nerede".
    ///
    /// Kazanma kontrolünü bu sınıfa yaptırmıyorum; burası sadece ölçüyor,
    /// kararı <see cref="StackGameController"/> veriyor. Ölçüm ile kural
    /// ayrı durunca kuralı değiştirmek ölçümü bozmuyor.
    /// </summary>
    public sealed class StackTracker : MonoBehaviour
    {
        [Tooltip("Bu hızın altındaki kutu durmuş sayılır (m/s).")]
        [SerializeField] float restSpeedThreshold = 0.05f;

        [Tooltip("Bu açısal hızın altındaki kutu dönmüyor sayılır (rad/s).")]
        [SerializeField] float restAngularThreshold = 0.1f;

        [Tooltip("Kazanma kontrolü için sıkı hız eşiği (m/s).")]
        [SerializeField] float steadySpeedThreshold = 0.015f;

        [Tooltip("Kazanma kontrolü için sıkı açısal hız eşiği (rad/s). 0.02 rad/s ≈ 1°/sn.")]
        [SerializeField] float steadyAngularThreshold = 0.02f;

        readonly List<DraggableBody> bodies = new();

        /// <summary>
        /// Bir kez yere/kuleye oturmuş kutular. Kule yüksekliği yalnızca bunlara
        /// bakıyor.
        ///
        /// Sebebi kameranın hoplaması: kutu bırakıldığı anda yığının parçası
        /// sayılıyordu, ama o an kutu havada ve bırakma mesafesi kadar yukarıda.
        /// Kamera "kule iki birim uzadı" deyip yukarı çıkıyor, kutu iniyor,
        /// kamera geri iniyor. Bırakma mesafesini büyüttükçe zıplama da büyüdü.
        ///
        /// "Bir kez oturmuş" olmak kalıcı: sonradan sallanan kutu listeden
        /// düşmüyor. Düşseydi kule sallandığında yükseklik anlık olarak azalır,
        /// kamera bu kez aşağı hoplardı.
        /// </summary>
        readonly HashSet<DraggableBody> settled = new();

        /// <summary>Yığına kaydedilmiş toplam kutu sayısı; eldeki kutu da dahil.</summary>
        public int Count => bodies.Count;

        /// <summary>
        /// Yerleştirilmiş kutu sayısı: elde tutulan kutu sayılmıyor. Skor bunu
        /// kullanıyor — oyuncu kutuyu havada tutarken skorun bir artıp geri
        /// düşmesi, sayının ne anlama geldiğini bulanıklaştırırdı.
        /// </summary>
        public int PlacedCount
        {
            get
            {
                int count = 0;

                for (int i = 0; i < bodies.Count; i++)
                {
                    var body = bodies[i];
                    if (body != null && !body.IsDragged)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public void Register(DraggableBody body)
        {
            if (!bodies.Contains(body))
            {
                bodies.Add(body);
            }
        }

        /// <summary>Yığındaki her şey durdu mu? Sıradaki kutunun geleceği an budur.</summary>
        public bool AllResting() => AllBelow(restSpeedThreshold, restAngularThreshold);

        /// <summary>
        /// Yığın **gerçekten** kıpırdamıyor mu? Kazanma kontrolü bunu soruyor.
        ///
        /// İki ayrı eşiğin sebebi oynarken çıktı: hedefi geçtikten sonra hafifçe
        /// kayan bir kule gevşek eşiğin altında kalıp "durdu" sayılıyor, oyuncu
        /// kazanıyor, kule on saniye sonra devriliyordu. Gevşek eşik 0.1 rad/s'e
        /// izin veriyor — saniyede 5.7°, yani on saniyede 57°. Duran bir kule değil
        /// o, yavaş devrilen bir kule.
        ///
        /// Gevşek eşiği sıkılaştırıp tek eşikle yürümedim çünkü ikisi farklı iş
        /// yapıyor: biri "sıradaki kutu gelebilir" diyor (yanılırsa oyuncu bir
        /// saniye erken kutu alır), diğeri "bu tur kazanıldı" diyor (yanılırsa
        /// oyun yalan söyler).
        /// </summary>
        public bool AllSteady() => AllBelow(steadySpeedThreshold, steadyAngularThreshold);

        bool AllBelow(float speed, float angular)
        {
            for (int i = 0; i < bodies.Count; i++)
            {
                var body = bodies[i];
                if (body == null || body.IsDragged)
                {
                    return false;
                }

                if (!IsBelow(body.Body, speed, angular))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Önce <c>IsSleeping()</c>'e bakıyoruz: fizik motoru bir cismi uykuya
        /// aldıysa zaten "bu artık hareket etmiyor" demiş oluyor, bizim eşik
        /// tahminimizden daha güvenilir. Uyku eşiğine hiç inmeyen ama pratikte
        /// duran cisimler için de kendi eşiğimiz yedekte duruyor.
        /// </summary>
        static bool IsBelow(Rigidbody rb, float speed, float angular) =>
            rb.IsSleeping() ||
            (rb.linearVelocity.sqrMagnitude <= speed * speed &&
             rb.angularVelocity.sqrMagnitude <= angular * angular);

        /// <summary>
        /// Kulenin tepe noktası: yalnızca bir kez oturmuş kutular sayılıyor.
        /// Elde tutulan ve henüz havada olan kutular kulenin parçası değil.
        ///
        /// Transform pozisyonu değil collider sınırları kullanılıyor: kutu yan
        /// yattığında merkezi alçalır ama üst kenarı yükselir, kule yüksekliği
        /// dediğimiz şey ikincisi.
        /// </summary>
        public float HighestSettledPointY()
        {
            float highest = 0f;

            for (int i = 0; i < bodies.Count; i++)
            {
                var body = bodies[i];
                if (body == null || !settled.Contains(body))
                {
                    continue;
                }

                if (!body.TryGetComponent(out Collider collider))
                {
                    continue;
                }

                highest = Mathf.Max(highest, collider.bounds.max.y);
            }

            return highest;
        }

        /// <summary>
        /// Havadaki kutuların oturup oturmadığını takip eder. Ölçümü çağrıldığı
        /// anda hesaplamak yerine burada biriktirmemin sebebi, "bir kez oturmuş
        /// olmak"ın bir olay olması: geçmişi bilmeden cevaplanamaz.
        /// </summary>
        void Update()
        {
            for (int i = 0; i < bodies.Count; i++)
            {
                var body = bodies[i];
                if (body == null || body.IsDragged || settled.Contains(body))
                {
                    continue;
                }

                if (IsBelow(body.Body, restSpeedThreshold, restAngularThreshold))
                {
                    settled.Add(body);
                }
            }
        }

        /// <summary>Bir parça verilen yüksekliğin altına düştü mü?</summary>
        public bool AnyBelow(float y)
        {
            for (int i = 0; i < bodies.Count; i++)
            {
                var body = bodies[i];
                if (body != null && body.transform.position.y < y)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
