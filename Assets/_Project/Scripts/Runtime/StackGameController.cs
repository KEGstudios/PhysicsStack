using UnityEngine;

namespace PhysicsStack
{
    /// <summary>
    /// Turun akışını yürütür: girdiyi dinler, sıradaki kutuyu ister, yığının
    /// oturmasını bekler ve durumu yayınlar.
    ///
    /// Gün 7'de bu sınıftan bir şey çıkarıldı: kararın kendisi. Kazanma, kaybetme
    /// ve hedef artık <see cref="IStackRules"/>'un işi. Burada kalan üç iş —
    /// ölçümü toplamak, kurala sormak, cevaba göre sahneyi ilerletmek — iki modda
    /// da birebir aynı çalıştığı için mod eklemek bu dosyaya dokunmuyor.
    ///
    /// Ölçüm <see cref="StackTracker"/>'da, üretim <see cref="BoxQueue"/>'da,
    /// kural <see cref="IStackRules"/>'ta, sıralama burada.
    /// </summary>
    public sealed class StackGameController : MonoBehaviour
    {
        [SerializeField] BoxQueue queue;
        [SerializeField] StackTracker tracker;

        [Tooltip("Sahne hangi kural setiyle açılacak. Gün 9'da bunu mod seçim ekranı belirleyecek.")]
        [SerializeField] StackMode mode = StackMode.Level;

        [Tooltip("Seviyelerin sırası. Seviye modunda bütün sayılar buradan geliyor.")]
        [SerializeField] LevelLibrary levelLibrary;

        [Tooltip("Kaçıncı seviye oynanıyor (0 tabanlı). Gün 9'da seviye listesi bunu belirleyecek.")]
        [SerializeField] int levelIndex;

        [Tooltip("Bu yüksekliğin altına düşen parça 'düştü' sayılır. Zemin üstü y = 0.")]
        [SerializeField] float killHeight = -1f;

        [Tooltip("Yığın bu kadar süre kesintisiz durursa 'oturdu' sayılır (sn).")]
        [SerializeField] float settleGraceTime = 0.3f;

        [Tooltip("Sonsuz modda kule zirvesinin bu kadar altına düşerse çökmüş sayılır. Seviye modunda bu sayı seviyenin verisinde.")]
        [SerializeField] float endlessCollapseDrop = 0.6f;

        IStackRules rules;

        float peakHeight;

        public GameState State { get; private set; } = GameState.WaitingForDrag;

        /// <summary>Debug paneli için: yığın ne kadar süredir kesintisiz duruyor (gevşek eşik).</summary>
        public float RestTimer { get; private set; }

        /// <summary>Kulenin sıkı eşiğe göre kıpırdamadan geçirdiği süre. Kazanma buna bakıyor.</summary>
        public float SteadyTimer { get; private set; }

        /// <summary>Tutunma için gereken süre; panel ilerlemeyi bunun üstünden gösteriyor.</summary>
        public float HoldTime => rules != null ? rules.HoldTime : 0f;

        /// <summary>Turun o anki skoru. Neyin sayıldığına kural karar veriyor.</summary>
        public float Score { get; private set; }

        /// <summary>Skorun ekranda görünecek hâli; birimi de kuraldan geliyor.</summary>
        public string ScoreText => rules != null ? rules.DescribeScore(Score) : string.Empty;

        /// <summary>
        /// Tur bittiğinde kulenin ulaştığı yükseklik. Hedefi olmayan modda
        /// gösterilecek tek anlamlı sayı bu: "buraya kadar geldin".
        /// </summary>
        public float FinalHeight { get; private set; }

        /// <summary>Yürürlükteki kural seti. Panel ve hedef çizgisi buradan okuyor.</summary>
        public IStackRules Rules => rules;

        /// <summary>Hedef yükseklik; sıfır ise bu modda hedef yok.</summary>
        public float TargetHeight => rules != null ? rules.TargetHeight : 0f;

        /// <summary>Debug paneli ölçümleri buradan okuyor.</summary>
        public StackTracker Tracker => tracker;

        void Awake()
        {
            // Kural nesnesi Start'tan önce hazır olmalı: hedef çizgisi kendini
            // hedefe göre yerleştirirken bunu okuyor.
            rules = mode == StackMode.Endless
                ? new EndlessRules(endlessCollapseDrop)
                : new LevelRules(ResolveLevel());
        }

        /// <summary>
        /// Oynanacak seviyeyi bulur. Kütüphane bağlı değilse sessizce çalışmak
        /// yerine bağırıp varsayılan bir seviye üretiyor: "değeri değiştiriyorum
        /// ama hiçbir şey olmuyor" diye yarım saat harcanacak türden bir hata bu.
        /// </summary>
        LevelDefinition ResolveLevel()
        {
            var level = levelLibrary != null ? levelLibrary.Get(levelIndex) : null;

            if (level != null)
            {
                return level;
            }

            Debug.LogWarning($"[StackGameController] Seviye bulunamadı (indeks {levelIndex}), varsayılanla oynuyorum.", this);
            return ScriptableObject.CreateInstance<LevelDefinition>();
        }

        void Start()
        {
            queue.BoxSpawned += OnBoxSpawned;
            RequestNextBox();
        }

        /// <summary>Sıradaki kutuyu ister; nasıl olacağını kural söylüyor.</summary>
        void RequestNextBox()
        {
            queue.SpawnNext(rules.NextBox(Read(settled: false)));
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

            // Sayaçlar hem yerleşme hem tutunma sırasında işliyor: Holding, kutu
            // bırakıldıktan sonraki sürecin devamı, ayrı bir bölüm değil.
            bool measuring = State is GameState.Settling or GameState.Holding;
            bool settled = false;

            if (measuring)
            {
                // Tek kare "duruyor" görmek yetmiyor: yığın sallanırken hız sıfırdan
                // geçtiği anlar oluyor. Kesintisiz süre şartı bu yanlış pozitifi eliyor.
                RestTimer = tracker.AllResting() ? RestTimer + Time.deltaTime : 0f;
                SteadyTimer = tracker.AllSteady() ? SteadyTimer + Time.deltaTime : 0f;
                settled = RestTimer >= settleGraceTime;
            }

            var snapshot = Read(settled);
            Score = rules.Score(snapshot);

            switch (rules.Evaluate(snapshot))
            {
                case RunOutcome.Won:
                    Finish(GameState.Won, snapshot);
                    return;

                case RunOutcome.Lost:
                    Finish(GameState.Lost, snapshot);
                    return;

                case RunOutcome.Pending:
                    // Kule hedefin üstünde ama henüz tutunmadı. Sıradaki kutuyu
                    // vermiyoruz: oyuncunun elinde kutu varken kaybetmek, seyrederken
                    // kaybetmekten farklı bir şey olurdu.
                    State = GameState.Holding;
                    return;
            }

            if (!settled)
            {
                return;
            }

            State = GameState.WaitingForDrag;
            RestTimer = 0f;
            SteadyTimer = 0f;
            RequestNextBox();
        }

        /// <summary>
        /// Ölçümleri tek seferde okuyup dondurur. Yükseklik olarak elde tutulan
        /// kutuyu saymayan değeri veriyoruz: yeni kutu kulenin üstünde belirdiği
        /// için, oyuncu ona dokunduğu anda "kule boyu" hedefi geçmiş gibi
        /// görünürdü. Oturma anında ikisi zaten aynı sayı.
        /// </summary>
        StackSnapshot Read(bool settled)
        {
            float height = tracker.HighestSettledPointY();

            // Zirve yalnızca oturmuş ölçümle güncelleniyor. Sallanan kule bir kare
            // için olduğundan yüksek okunabiliyor; o sahte zirve yazılsaydı sonraki
            // her ölçüm ona göre "çökmüş" görünürdü.
            if (settled)
            {
                peakHeight = Mathf.Max(peakHeight, height);
            }

            return new StackSnapshot(
                height,
                peakHeight,
                tracker.PlacedCount,
                tracker.AnyBelow(killHeight),
                settled,
                SteadyTimer);
        }

        void Finish(GameState result, in StackSnapshot snapshot)
        {
            State = result;
            RestTimer = 0f;
            SteadyTimer = 0f;
            FinalHeight = snapshot.Height;

            Debug.Log($"[StackGameController] {rules.Title} · {result} · " +
                      $"kule {snapshot.Height:0.00} · zirve {snapshot.PeakHeight:0.00} · skor {ScoreText}");
        }
    }
}
