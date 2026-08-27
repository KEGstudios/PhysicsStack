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

        ImpactEffects effects;

        /// <summary>
        /// Efekt sunucusu disaridan veriliyor, mermi kendi bulmuyor.
        /// <c>FindObjectOfType</c> her atista sahneyi taramak demek olurdu ve
        /// mermi saniyede birden fazla uretiliyor.
        /// </summary>
        public void Bind(ImpactEffects value)
        {
            effects = value;
        }

        void OnCollisionEnter(Collision collision)
        {
            if (effects != null)
            {
                effects.BallHit(collision.GetContact(0).point);
            }

            Destroy(gameObject);
        }
    }
}
