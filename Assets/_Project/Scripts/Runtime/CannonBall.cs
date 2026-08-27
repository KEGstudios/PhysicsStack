using UnityEngine;

namespace PhysicsStack
{
    /// <summary>
    /// Top atıcının mermisi. Yatay gidiyor, yerçekimi yok.
    ///
    /// Yerçekimsiz olması bilinçli: düşen bir mermi parabol çizer ve oyuncunun
    /// nereye gideceğini tahmin etmesi için ayrı bir sezgi gerekir. Düz çizgi
    /// bakınca okunuyor — tehdidin adil olması, görülebilir olmasından geçiyor.
    ///
    /// İlk çarpışmada yok oluyor: sahnede biriken mermiler kuleye yeni bir zemin
    /// olurdu ve oyun kendi kurallarını çiğnerdi.
    /// </summary>
    public sealed class CannonBall : MonoBehaviour
    {
        [Tooltip("Kimseye çarpmazsa bu süre sonunda yok olur (sn).")]
        [SerializeField] float lifetime = 3f;

        void Start()
        {
            Destroy(gameObject, lifetime);
        }

        void OnCollisionEnter(Collision collision)
        {
            Destroy(gameObject);
        }
    }
}
