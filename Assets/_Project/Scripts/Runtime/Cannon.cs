using UnityEngine;

namespace PhysicsStack
{
    /// <summary>
    /// Kulenin üstündeki dar bir bantta aşağı yukarı gezinen ve belirli aralıklarla
    /// yatay top atan tehdit. Oyuncuyu kutuyu **doğru anda** indirmeye zorluyor.
    ///
    /// Neden bu, sabit ya da hareketli bir engel çubuğu yerine: çubuk tek seferlik
    /// bir nişan alma problemi — bir kez çözersin, her seferinde aynı şekilde
    /// çözülür. Aralıklarla ateş eden bir namlu ise ritim problemi; aynı seviyeyi
    /// ikinci kez oynadığında da beklemek zorundasın.
    ///
    /// Namlu asla kulenin tepesinin altına inmiyor. Bu, çarpışma katmanıyla değil
    /// geometriyle sağlanan bir garanti: filtre unutulur, geometri unutulmaz.
    /// Duran kuleyi bozan tehdit, oyuncu hata yapmadan kaybettirir.
    /// </summary>
    public sealed class Cannon : MonoBehaviour
    {
        [SerializeField] StackGameController controller;
        [SerializeField] StackTracker tracker;
        [SerializeField] GameObject ballPrefab;
        [SerializeField] ImpactEffects effects;

        [Tooltip("Atis aninda namlunun geriye kacma mesafesi.")]
        [SerializeField] float recoilDistance = 0.35f;

        [Tooltip("Geri tepmenin sonme hizi (birim/sn).")]
        [SerializeField] float recoilRecovery = 1.6f;

        float recoil;

        [Tooltip("Namlunun görünen gövdesi; tehdit kapalıyken gizleniyor.")]
        [SerializeField] Renderer body;

        [Tooltip("Namlunun x konumu. Oyun alanının hemen dışında: BoxQueue'daki playHalfWidth + kutu yarısı + pay.")]
        [SerializeField] float sideX = -2.25f;

        [Tooltip("Seviye verisi bir değer vermezse kullanılan alt kenar payı.")]
        [SerializeField] float defaultBottomGap = 2f;

        [Tooltip("Bandın tabanı kule büyüdükçe bu sürede yetişiyor (sn).")]
        [SerializeField] float followSmoothTime = 0.5f;

        float bottomGap;
        float interval;
        float ballSpeed;
        float patrolSpeed;
        float patrolSpan;

        float timer;
        float travelled;

        /// <summary>
        /// Bandın yumuşatılmış tabanı.
        ///
        /// Doğrudan kule tepesini kullanınca namlu her kutu oturduğunda bir birim
        /// ışınlanıyordu. Kamera aynı sorunu aynı ilaçla çözüyor: ölçüm ani
        /// değişiyorsa gösterim ona yumuşayarak gitmeli.
        /// </summary>
        float smoothedBottom;
        float bottomVelocity;
        bool initialised;

        public bool Active { get; private set; }

        void Start()
        {
            // Menüdeyken kural nesnesi yok; namlu hiç görünmüyor.
            if (controller.Rules == null)
            {
                if (body != null)
                {
                    body.enabled = false;
                }

                return;
            }

            var hazards = controller.Rules.Hazards;

            Active = hazards.cannon;
            interval = Mathf.Max(0.25f, hazards.cannonInterval);
            ballSpeed = hazards.cannonBallSpeed;
            patrolSpeed = hazards.cannonPatrolSpeed;

            // Bandın yüksekliği sabit bir sayı, kadrajdan ya da spawn yüksekliğinden
            // türetilmiyor. İlk hâlinde bant "kule tepesi ile kutunun beliriş
            // yüksekliği arası" idi ve ikisi de her kutuda değiştiği için namlunun
            // gezinme aralığı her turda başka bir sayı oluyordu — PingPong da
            // aralık değişince çıktısını sıçratıyor. Sabit bant hem bu hatayı
            // kökünden kesiyor hem de tehdidi öngörülebilir kılıyor: oyuncu
            // namlunun nereye kadar çıkacağını biliyor.
            patrolSpan = Mathf.Max(1f, hazards.cannonPatrolSpan);

            // Bant kulenin hemen üstünde değil, kule ile kutunun bırakıldığı yerin
            // arasında duruyor. İlk denemede 0.9 idi ve namlu kuleye fazla yakın
            // kalıyordu: tehdit, kutuyu indirirken değil neredeyse yerleşirken
            // devreye giriyordu — yani oyuncunun düzeltme şansı olmayan bir anda.
            bottomGap = hazards.cannonBottomGap > 0f ? hazards.cannonBottomGap : defaultBottomGap;

            if (body != null)
            {
                body.enabled = Active;
            }
        }

        void Update()
        {
            if (!Active)
            {
                return;
            }

            // Tur bittiyse ateş kesiliyor. Kaybettikten sonra devam eden bir
            // saldırı, sonucu okumayı zorlaştırmaktan başka bir işe yaramıyor.
            if (controller.State is GameState.Won or GameState.Lost)
            {
                return;
            }

            float targetBottom = tracker.HighestSettledPointY() + bottomGap;

            if (!initialised)
            {
                smoothedBottom = targetBottom;
                initialised = true;
            }

            smoothedBottom = Mathf.SmoothDamp(
                smoothedBottom, targetBottom, ref bottomVelocity, followSmoothTime);

            // Aralık sabit olduğu için PingPong sürekli: namlu bandın altı ile üstü
            // arasında düzgün gidip geliyor, ritmi öğrenilebiliyor.
            travelled += patrolSpeed * Time.deltaTime;
            // Geri tepme namlunun disa dogru kacmasi: atisin cikmis oldugunu
            // gosteren en ucuz sey. Yon sideX'in isaretinden turetiliyor ki
            // namluyu karsi kenara tasimak tek bir sayi degistirmek olsun.
            recoil = Mathf.MoveTowards(recoil, 0f, recoilRecovery * Time.deltaTime);

            transform.position = new Vector3(
                sideX + Mathf.Sign(sideX) * recoil,
                smoothedBottom + Mathf.PingPong(travelled, patrolSpan),
                0f);

            timer += Time.deltaTime;

            if (timer >= interval)
            {
                timer = 0f;
                Fire();
            }
        }

        void Fire()
        {
            var ball = Instantiate(ballPrefab, transform.position, Quaternion.identity);

            // Namlu solda: mermi sağa gidiyor. İşareti konumdan türetiyorum ki
            // namluyu karşı kenara taşımak tek bir sayı değiştirmek olsun.
            ball.GetComponent<Rigidbody>().linearVelocity =
                new Vector3(Mathf.Sign(-sideX) * ballSpeed, 0f, 0f);

            ball.GetComponent<CannonBall>().Bind(effects);

            recoil = recoilDistance;

            // Perde her atışta biraz kayıyor. Namlu düzenli aralıklarla ateş
            // ettiği için tıpatıp aynı ses metronom gibi duyuluyor ve kulak
            // birkaç atıştan sonra onu takip etmeyi bırakıyor.
            SfxPlayer.Play(Sfx.CannonFire, 1f, Random.Range(0.92f, 1.08f));
        }
    }
}
