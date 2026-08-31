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

        [Tooltip("Menüyü atlayıp aşağıdaki mod/seviye ile doğrudan başla. Editor'de tek seviye denerken işe yarıyor.")]
        [SerializeField] bool startImmediately;

        [Tooltip("Menü atlanırsa hangi kural seti çalışacak.")]
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

        public GameState State { get; private set; } = GameState.Menu;

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
        /// Tur boyunca kulenin **ulaştığı** yükseklik. Hedefi olmayan modda
        /// gösterilecek tek anlamlı sayı bu: "buraya kadar geldin".
        ///
        /// Zirve okunuyor, bitiş anındaki boy değil. Uzun süre yanlıştı ve
        /// belirtisi şuydu: 15 birimlik kule çöktüğünde tur sonu ekranı "kule
        /// 5.00" yazıyordu. Çünkü tur tam da kule kısaldığı için bitiyor — yani
        /// bitiş anındaki boy, hep kaybettiren çöküşün sonrasını gösteriyor.
        ///
        /// Cümlenin kendisi zaten doğruyu söylüyormuş: "ulaştığı yükseklik".
        /// Kodun onunla uyuşmadığını fark etmek bir tur oynamak kadar sürdü ama
        /// yazıyı okumak daha kısa sürerdi.
        /// </summary>
        public float FinalHeight { get; private set; }

        /// <summary>
        /// Kulede duran kutu sayısı ve yere düşenler. Panel bunları gösteriyor:
        /// seviyenin hedefi artık kutu sayısı ve yıldız da düşen kutuyu
        /// sayıyor, yani oyuncunun tur boyunca merak ettiği iki sayı bunlar.
        /// </summary>
        public int TowerBoxes { get; private set; }

        public int DroppedBoxes { get; private set; }

        /// <summary>Yürürlükteki kural seti. Menüdeyken null.</summary>
        public IStackRules Rules => rules;

        /// <summary>Oynanan mod; sonuç ekranı "sonraki seviye" düğmesi için soruyor.</summary>
        public StackMode Mode => mode;

        /// <summary>Oynanan seviyenin sırası.</summary>
        public int LevelIndex => levelIndex;

        /// <summary>Hedef yükseklik; sıfır ise bu modda hedef yok.</summary>
        public float TargetHeight => rules != null ? rules.TargetHeight : 0f;

        /// <summary>Debug paneli ölçümleri buradan okuyor.</summary>
        public StackTracker Tracker => tracker;

        /// <summary>
        /// Turun o anki tehditleri. Sahnedeki <see cref="Wind"/> ve
        /// <see cref="Cannon"/> burayı okuyor.
        ///
        /// Neden kural nesnesinden değil de buradan: sonsuz modda tehdit tur
        /// içinde büyüyor ve kural bunu anlık görüntüye bakarak hesaplıyor.
        /// Anlık görüntüyü okumak controller'ın işi — tehditlerin her karede
        /// kendi başlarına yığını ölçmesi, aynı ölçümün üç yerde yapılması
        /// demekti. Değer yeni kutu istenirken bir kez hesaplanıyor.
        /// </summary>
        public HazardSettings Hazards { get; private set; }

        /// <summary>
        /// Sıradaki kontrol noktasının yüksekliği. Sonsuzsa bu modda kontrol
        /// noktası yok. Debug paneli de bunu okuyor.
        /// </summary>
        public float NextCheckpoint { get; private set; } = float.PositiveInfinity;

        /// <summary>Son dondurmanın yapıldığı kule boyu; sıfır ise henüz dondurulmadı.</summary>
        public float LastCheckpoint { get; private set; }

        void Awake()
        {
            // Menüden bir istek geldiyse tur onunla başlıyor; gelmediyse ya
            // Inspector'daki ayarlar (Editor kolaylığı) ya da hiç — o zaman
            // sahne menüyle açılıyor.
            if (RunRequest.HasRequest)
            {
                mode = RunRequest.Mode;
                levelIndex = RunRequest.LevelIndex;
            }
            else if (!startImmediately)
            {
                return;
            }

            // Kural nesnesi Start'tan önce hazır olmalı: hedef çizgisi kendini
            // hedefe göre yerleştirirken bunu okuyor.
            rules = mode == StackMode.Endless
                ? new EndlessRules(endlessCollapseDrop)
                : new LevelRules(ResolveLevel());

            // Baslangic degeri bos bir anlik goruntuden geliyor. Seviye modunda
            // cevap zaten anlik goruntuden bagimsiz, yani ruzgar ilk kareden
            // itibaren dogru; sonsuz modda ilk kutuda tehdit zaten yok. Burada
            // gercek yigini okumuyorum, cunku Awake'te tracker'in hazir olup
            // olmadigi betik sirasina bagli ve o siraya guvenmek istemiyorum.
            Hazards = rules.HazardsFor(default);
            NextCheckpoint = rules.CheckpointAfter(0f);

            State = GameState.WaitingForDrag;
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
            if (rules == null)
            {
                return;
            }

            queue.BoxSpawned += OnBoxSpawned;
            RequestNextBox();
        }

        /// <summary>
        /// Sıradaki kutuyu ister; nasıl olacağını kural söylüyor. Tehditler de
        /// burada tazeleniyor: ikisi de "zorluk şu an ne olmalı" sorusunun
        /// cevabı ve aynı anlık görüntüden okunmaları gerekiyor.
        ///
        /// Kutu başına bir kez, kare başına değil. Sonsuz modda rüzgâr böylece
        /// kutu aralarında basamak basamak artıyor; oyuncu bir kutuyu bir
        /// rüzgârla indirip aynı kutu inerken başka bir rüzgârla karşılaşmıyor.
        /// </summary>
        void RequestNextBox()
        {
            var snapshot = Read(settled: false);

            Hazards = rules.HazardsFor(snapshot);
            queue.SpawnNext(rules.NextBox(snapshot));
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
            if (State is GameState.Menu or GameState.Won or GameState.Lost)
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
            DroppedBoxes = snapshot.DroppedCount;
            TowerBoxes = snapshot.TowerBoxes;

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

            TryCheckpoint(snapshot);

            State = GameState.WaitingForDrag;
            RestTimer = 0f;
            SteadyTimer = 0f;
            RequestNextBox();
        }

        /// <summary>
        /// Kontrol noktasına gelindiyse kulenin oturmuş kısmını dondurur.
        ///
        /// Kule tam durduğu anda çağrılıyor, kutu bırakıldığında değil: sallanan
        /// bir kuleyi dondurmak eğikliği kalıcı hâle getirir ve oyuncunun
        /// düzeltme şansı olmadan verilmiş bir cezaya dönüşür.
        ///
        /// Ölçü kule boyu, atılan kutu sayısı değil. Kutu sayısıyla çalışırken
        /// kutuları kulenin yanına atmak da sayacı ilerletiyordu: ödül, ödülü
        /// hak eden şeyden bağımsız veriliyordu.
        ///
        /// Sıradaki eşik hemen bir sonrakine taşınıyor, çünkü yükseklik sürekli
        /// bir sayı: "bu değer bir kontrol noktası mı" diye sorulamıyor, kule
        /// 9.98'den 10.03'e geçiyor.
        /// </summary>
        void TryCheckpoint(in StackSnapshot snapshot)
        {
            if (!snapshot.Reached(NextCheckpoint))
            {
                return;
            }

            LastCheckpoint = snapshot.Height;
            NextCheckpoint = rules.CheckpointAfter(snapshot.Height);

            // Ses yalnızca gerçekten bir şey donduysa çalıyor: olmayan bir olayı
            // duyurmak, oyuncuya sistemin ne yaptığını yanlış öğretir.
            if (tracker.FreezeSettled() > 0)
            {
                SfxPlayer.Play(Sfx.Checkpoint);
            }
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
                tracker.GroundedCount(),
                tracker.AnyBelow(killHeight),
                settled,
                SteadyTimer);
        }

        void Finish(GameState result, in StackSnapshot snapshot)
        {
            State = result;
            RestTimer = 0f;
            SteadyTimer = 0f;
            FinalHeight = snapshot.PeakHeight;

            // İlerleme burada yazılıyor, sonuç ekranında değil: kaydın arayüze
            // bağlı olması, arayüzü değiştirdiğimde kaydı da bozma riski demek.
            if (result == GameState.Won && mode == StackMode.Level)
            {
                Progress.CompleteLevel(levelIndex, snapshot.PlacedCount);
            }
            else if (mode == StackMode.Endless)
            {
                Progress.ReportEndless(snapshot.PeakHeight);
            }

            Debug.Log($"[StackGameController] {rules.Title} · {result} · " +
                      $"kule {snapshot.Height:0.00} · zirve {snapshot.PeakHeight:0.00} · skor {ScoreText}");
        }
    }
}
