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

        readonly List<DraggableBody> bodies = new();

        public int Count => bodies.Count;

        public void Register(DraggableBody body)
        {
            if (!bodies.Contains(body))
            {
                bodies.Add(body);
            }
        }

        /// <summary>
        /// Yığındaki her şey durdu mu?
        ///
        /// Önce <c>IsSleeping()</c>'e bakıyoruz: fizik motoru bir cismi uykuya
        /// aldıysa zaten "bu artık hareket etmiyor" demiş oluyor, bizim eşik
        /// tahminimizden daha güvenilir. Uyku eşiğine hiç inmeyen ama pratikte
        /// duran cisimler için de kendi eşiğimiz yedekte duruyor.
        /// </summary>
        public bool AllResting()
        {
            for (int i = 0; i < bodies.Count; i++)
            {
                var body = bodies[i];
                if (body == null || body.IsDragged)
                {
                    return false;
                }

                var rb = body.Body;
                if (rb.IsSleeping())
                {
                    continue;
                }

                if (rb.linearVelocity.sqrMagnitude > restSpeedThreshold * restSpeedThreshold ||
                    rb.angularVelocity.sqrMagnitude > restAngularThreshold * restAngularThreshold)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Yığının en tepe noktası. Transform pozisyonu değil collider sınırları
        /// kullanılıyor: kutu yan yattığında merkezi alçalır ama üst kenarı
        /// yükselir, kule yüksekliği dediğimiz şey ikincisi.
        /// </summary>
        public float HighestPointY()
        {
            float highest = 0f;

            for (int i = 0; i < bodies.Count; i++)
            {
                var body = bodies[i];
                if (body == null || !body.TryGetComponent(out Collider collider))
                {
                    continue;
                }

                highest = Mathf.Max(highest, collider.bounds.max.y);
            }

            return highest;
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
