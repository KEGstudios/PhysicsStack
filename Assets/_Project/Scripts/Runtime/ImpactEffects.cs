using System.Collections;
using UnityEngine;

namespace PhysicsStack
{
    /// <summary>
    /// Carpma ve cokus geri bildirimleri: toz, kamera sarsintisi, kisa zaman
    /// yavaslamasi.
    ///
    /// Hepsi tamamen gorsel. Fizige tek dokunusu cokus anindaki zaman
    /// yavaslamasi ve o da simulasyonun hizini degistiriyor, sonucunu degil.
    /// Bu ayrimi korumak onemli: "his" katmani oyunu daha iyi hissettirmeli,
    /// baska bir oyun haline getirmemeli.
    ///
    /// Efektler tek yerde toplandi cunku hepsi ayni olaylari dinliyor. Her
    /// bilesenin kendi carpisma dinleyicisi olsaydi ayni carpma uc kez
    /// islenirdi.
    /// </summary>
    public sealed class ImpactEffects : MonoBehaviour
    {
        [SerializeField] StackGameController controller;
        [SerializeField] BoxQueue queue;
        [SerializeField] StackCamera stackCamera;
        [SerializeField] ParticleSystem dust;

        [Tooltip("Bu hizin altindaki carpmalar sessiz gecer (m/s).")]
        [SerializeField] float minImpactSpeed = 4.5f;

        [Tooltip("Carpma hizi basina kamera sarsintisi.")]
        [SerializeField] float shakePerSpeed = 0.008f;

        [Tooltip("Kule coktugunde uygulanan sarsinti.")]
        [SerializeField] float collapseShake = 0.22f;

        [Tooltip("Cokuste zamanin yavasladigi sure (gercek sn).")]
        [SerializeField] float hitStopDuration = 0.14f;

        [Tooltip("Cokuste zaman olcegi.")]
        [SerializeField] float hitStopScale = 0.35f;

        GameState lastSeen = GameState.Menu;

        void Start()
        {
            if (queue != null)
            {
                queue.BoxSpawned += OnSpawned;
            }
        }

        void OnDestroy()
        {
            if (queue != null)
            {
                queue.BoxSpawned -= OnSpawned;
            }

            // Sahne yeniden yuklenirken zaman yavaslamasi devam ediyor olabilir.
            // Coroutine nesneyle birlikte olur ama Time.timeScale global: geri
            // yazilmazsa oyun kalici olarak agir cekimde acilir.
            Time.timeScale = 1f;
        }

        void OnSpawned(DraggableBody body)
        {
            body.Landed += OnLanded;
        }

        void OnLanded(DraggableBody body, Vector3 point, float speed)
        {
            if (speed < minImpactSpeed)
            {
                return;
            }

            if (stackCamera != null)
            {
                stackCamera.Shake(speed * shakePerSpeed);
            }

            Burst(point, speed);
        }

        /// <summary>
        /// Toz tek bir sistemden cikiyor, her carpmada yeni bir nesne
        /// uretilmiyor: bir turda onlarca carpma oluyor ve her biri icin
        /// Instantiate/Destroy yapmak coplugu bosuna mesgul eder.
        /// </summary>
        void Burst(Vector3 point, float speed)
        {
            if (dust == null)
            {
                return;
            }

            dust.transform.position = point;

            int count = Mathf.RoundToInt(Mathf.Lerp(4f, 14f, Mathf.InverseLerp(minImpactSpeed, 10f, speed)));
            dust.Emit(count);
        }

        void Update()
        {
            if (controller.State == lastSeen)
            {
                return;
            }

            lastSeen = controller.State;

            if (lastSeen == GameState.Lost)
            {
                if (stackCamera != null)
                {
                    stackCamera.Shake(collapseShake);
                }

                StartCoroutine(HitStop());
            }
        }

        /// <summary>
        /// Kisa zaman yavaslamasi. Gercek sureyle bekleniyor, yoksa yavaslatilmis
        /// zamanda beklemek sureyi de uzatir ve etki agir cekim gibi okunur.
        /// </summary>
        IEnumerator HitStop()
        {
            Time.timeScale = hitStopScale;
            yield return new WaitForSecondsRealtime(hitStopDuration);
            Time.timeScale = 1f;
        }
    }
}
