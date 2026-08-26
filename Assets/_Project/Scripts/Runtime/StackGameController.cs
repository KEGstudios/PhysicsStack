using UnityEngine;

namespace PhysicsStack
{
    /// <summary>
    /// Oyunun kuralları: ne zaman kazandık, ne zaman kaybettik, sıradaki kutu ne zaman gelir.
    ///
    /// Ölçüm <see cref="StackTracker"/>'da, üretim <see cref="BoxQueue"/>'da,
    /// karar burada. Üçünü ayırmamın sebebi Gün 4: his ayarlarken kuralları
    /// değiştirmeden ölçüm eşiklerini kurcalayabilmek istiyorum.
    /// </summary>
    public sealed class StackGameController : MonoBehaviour
    {
        [SerializeField] BoxQueue queue;
        [SerializeField] StackTracker tracker;

        [Tooltip("Kulenin tepesi bu yüksekliği geçerse ve yığın oturmuşsa kazanıldı.")]
        [SerializeField] float targetHeight = 4f;

        [Tooltip("Bu yüksekliğin altına düşen parça kaybettirir. Zemin üstü y = 0.")]
        [SerializeField] float killHeight = -1f;

        [Tooltip("Yığın bu kadar süre kesintisiz durursa 'oturdu' sayılır (sn).")]
        [SerializeField] float settleGraceTime = 0.3f;

        public GameState State { get; private set; } = GameState.WaitingForDrag;

        /// <summary>Gün 4'teki debug paneli için: yığın ne kadar süredir kesintisiz duruyor.</summary>
        public float RestTimer { get; private set; }

        public float TargetHeight => targetHeight;

        /// <summary>Debug paneli ölçümleri buradan okuyor; kural sınıfı zaten tracker'ı tutuyor.</summary>
        public StackTracker Tracker => tracker;

        void Start()
        {
            queue.BoxSpawned += OnBoxSpawned;
            queue.SpawnNext();
        }

        void OnDestroy()
        {
            if (queue != null)
            {
                queue.BoxSpawned -= OnBoxSpawned;
            }
        }

        void OnBoxSpawned(DraggableBody body)
        {
            body.Grabbed += OnGrabbed;
            body.Released += OnReleased;
        }

        void OnGrabbed(DraggableBody body)
        {
            if (State is GameState.Won or GameState.Lost)
            {
                return;
            }

            // Kutuyu bırakıldığında değil yakalandığında kaydediyoruz: oyuncu
            // kutuyu havada tutarken zeminin altına sürüklerse bu da bir kayıp,
            // yığının parçası sayılmalı.
            tracker.Register(body);

            State = GameState.Dragging;
            RestTimer = 0f;
        }

        void OnReleased(DraggableBody body)
        {
            if (State is GameState.Won or GameState.Lost)
            {
                return;
            }

            body.Grabbed -= OnGrabbed;
            body.Released -= OnReleased;

            State = GameState.Settling;
            RestTimer = 0f;
        }

        void Update()
        {
            if (State is GameState.Won or GameState.Lost)
            {
                return;
            }

            // Kayıp kontrolü durumdan bağımsız: sürükleme sırasında devrilen
            // eski bir kutu da oyunu bitirir.
            if (tracker.AnyBelow(killHeight))
            {
                Finish(GameState.Lost);
                return;
            }

            if (State != GameState.Settling)
            {
                return;
            }

            // Tek kare "duruyor" görmek yetmiyor: yığın sallanırken hız sıfırdan
            // geçtiği anlar oluyor. Kesintisiz süre şartı bu yanlış pozitifi eliyor.
            RestTimer = tracker.AllResting() ? RestTimer + Time.deltaTime : 0f;

            if (RestTimer < settleGraceTime)
            {
                return;
            }

            // Kazanma kontrolü ancak burada yapılıyor. "Tepe hedefi geçti mi" değil,
            // "oturduktan sonra hedefin üstünde mi" sorusunu soruyoruz: sallanan
            // kule bir kare için hedefi geçip sonra devrilebilir, o kazanma değil.
            if (tracker.HighestPointY() >= targetHeight)
            {
                Finish(GameState.Won);
                return;
            }

            State = GameState.WaitingForDrag;
            RestTimer = 0f;
            queue.SpawnNext();
        }

        void Finish(GameState result)
        {
            State = result;
            RestTimer = 0f;

            // Gün 4'te ekrana basılacak; şimdilik konsol yeterli.
            Debug.Log($"[StackGameController] {result} · kule {tracker.HighestPointY():0.00} / hedef {targetHeight:0.00} · {tracker.Count} kutu");
        }
    }
}
