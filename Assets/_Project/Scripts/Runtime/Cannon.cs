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

        [Tooltip("Bırakma çizgisini okumak için: namlunun tavanı o çizgi.")]
        [SerializeField] BoxQueue queue;
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

        [Tooltip("Kaçıncı namlu (0 tabanlı). Tehditteki namlu sayısı bundan büyükse bu namlu açılır.")]
        [SerializeField] int index;

        [Tooltip("Seviye verisi bir değer vermezse kullanılan alt kenar payı.")]
        [SerializeField] float defaultBottomGap = 0.4f;

        [Tooltip("Namlunun bırakma çizgisinin altında kalma payı (birim). Merminin yarıçapını da kapsamalı.")]
        [SerializeField] float lineClearance = 0.4f;

        [Tooltip("Bırakma çizgisi okunamazsa kule tepesinin bu kadar üstü tavan sayılır.")]
        [SerializeField] float fallbackCeiling = 2.5f;

        [Tooltip("Bandın tabanı kule büyüdükçe bu sürede yetişiyor (sn).")]
        [SerializeField] float followSmoothTime = 0.5f;

        float bottomGap;
        float interval;
        float ballSpeed;
        float patrolSpeed;

        float timer;

        /// <summary>
        /// Gezinmenin evresi: 0-1 arası bir sayı, mesafe değil.
        ///
        /// Önce doğrudan yol biriktiriliyordu ve <c>PingPong</c> ham mesafe
        /// üzerinden çalışıyordu. Bandın yüksekliği sabitken bu doğruydu; artık
        /// bant kule ile bırakma çizgisi arasında ve her kutuda değişiyor, ham
        /// mesafeyle çalışan bir PingPong ise aralık değişince çıktısını
        /// sıçratıyor. Evre normalize olunca bant büyüyüp küçülürken namlu
        /// kendi yolunun aynı noktasında kalıyor.
        /// </summary>
        float phase;

        /// <summary>
        /// Bandın yumuşatılmış tabanı.
        ///
        /// Doğrudan kule tepesini kullanınca namlu her kutu oturduğunda bir birim
        /// ışınlanıyordu. Kamera aynı sorunu aynı ilaçla çözüyor: ölçüm ani
        /// değişiyorsa gösterim ona yumuşayarak gitmeli.
        /// </summary>
        float smoothedBottom;
        float bottomVelocity;
        float smoothedTop;
        float topVelocity;
        bool initialised;

        /// <summary>Bandın en az bu kadar yüksek olduğu varsayılıyor; sıfıra bölmeyi de bu engelliyor.</summary>
        const float MinSpan = 0.5f;

        public bool Active { get; private set; }

        /// <summary>Şu an uygulanmış olan ayar; değişince yeniden kuruluyor.</summary>
        HazardSettings applied;

        void Start()
        {
            // Menüdeyken kural nesnesi yok; namlu hiç görünmüyor. Apply bunu
            // zaten hallediyor (tehditsiz ayar = kapalı namlu), ama ilk kare
            // gelmeden önce bir kez çağrılması gerekiyor.
            Apply(Current());
        }

        /// <summary>
        /// Kuralın o an verdiği tehdit ayarı. Menüde kural nesnesi yok.
        /// </summary>
        HazardSettings Current() => controller != null && controller.Rules != null
            ? controller.Hazards
            : HazardSettings.None;

        /// <summary>
        /// Ayarı sahneye uygular. Eskiden bu iş <c>Start</c>'ta tek seferde
        /// yapılıyordu; sonsuz modda namlu turun ortasında devreye girdiği için
        /// artık her değişimde yeniden yapılıyor.
        ///
        /// Karşılaştırma alan alan (bkz. <see cref="HazardSettings.Equals"/>):
        /// controller değeri yalnızca yeni kutu istenirken hesapladığı için iki
        /// taraf da aynı hesabın çıktısı, değişmediyse bit bit aynı.
        /// </summary>
        void Apply(HazardSettings hazards)
        {
            bool wasActive = Active;

            applied = hazards;

            Active = hazards.cannonCount > index;
            interval = Mathf.Max(0.25f, hazards.cannonInterval);
            ballSpeed = hazards.cannonBallSpeed;
            patrolSpeed = hazards.cannonPatrolSpeed;

            // Bandın tabanı kule tepesinin bu kadar üstünde. Küçük bir sayı:
            // namlu artık kulenin tepesine kadar iniyor.
            bottomGap = hazards.cannonBottomGap > 0f ? hazards.cannonBottomGap : defaultBottomGap;

            if (body != null)
            {
                body.enabled = Active;
            }

            if (Active && !wasActive)
            {
                // Namlu tur ortasında beliriyor olabilir. Bant, kulenin o anki
                // tepesinden başlıyor: yumuşatma sıfırdan tırmansaydı namlu
                // kulenin içinden geçerek yukarı çıkardı.
                initialised = false;

                // Sayaçlar da sıfırlanıyor. Beliren namlunun ilk atışı için tam
                // süre var; belirdiği anda ateş eden bir tehdit öğrenilecek bir
                // ritim değil, kaza olurdu.
                //
                // İkinci namlu yarım tur kaymış başlıyor: hem gezinmesi hem
                // atışı. Aynı fazda başlasalardı iki namlu tek bir tehdit gibi
                // davranırdı — aynı anda, aynı yükseklikten iki mermi, oyuncu
                // için tek bir mermiyle aynı problem. Kaydırınca iki namlu
                // koridoru bölüşüyor ve ortaya bir ritim çıkıyor.
                float offset = (index % 2) * 0.5f;

                timer = interval * offset;
                phase = offset;
                recoil = 0f;
            }
        }

        void Update()
        {
            var current = Current();

            if (current != applied)
            {
                Apply(current);
            }

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

            // Bandın iki ucu da artık oyunun kendi işaretlerinden geliyor:
            // tabanı kulenin tepesi, tavanı bırakma çizgisi. Yani namlu kutuyu
            // indirdiğin koridorda geziniyor, ne kulenin içine giriyor ne de
            // çizginin üstüne çıkıyor. Çizginin üstü güvenli alan: oyuncunun
            // kutuyu tuttuğu ve nişan aldığı yer orası, orada vurulmak
            // öğrenilebilir bir tehdit değil, kaza.
            //
            // Önce bant sabit yükseklikteydi ve kule tepesinin belli bir pay
            // üstünden başlıyordu. Öngörülebilirdi ama koridorla ilgisi yoktu:
            // bırakma mesafesi büyük seviyelerde namlu çizginin epey üstüne
            // çıkıyor, küçük olanlarda kuleye fazla yaklaşıyordu.
            float targetBottom = tracker.HighestSettledPointY() + bottomGap;
            float targetTop = Ceiling() - lineClearance;

            if (!initialised)
            {
                smoothedBottom = targetBottom;
                smoothedTop = targetTop;
                initialised = true;
            }

            smoothedBottom = Mathf.SmoothDamp(
                smoothedBottom, targetBottom, ref bottomVelocity, followSmoothTime);

            smoothedTop = Mathf.SmoothDamp(
                smoothedTop, targetTop, ref topVelocity, followSmoothTime);

            float span = Mathf.Max(MinSpan, smoothedTop - smoothedBottom);

            // Evre normalize: hız birim/saniye olarak veriliyor ama bandın
            // yüksekliğine bölünüyor, yani namlu dar koridorda daha sık gidip
            // geliyor ve gezinme hızı her yerde aynı hissediliyor.
            phase += patrolSpeed * Time.deltaTime / span;
            // Geri tepme namlunun disa dogru kacmasi: atisin cikmis oldugunu
            // gosteren en ucuz sey. Yon sideX'in isaretinden turetiliyor ki
            // namluyu karsi kenara tasimak tek bir sayi degistirmek olsun.
            recoil = Mathf.MoveTowards(recoil, 0f, recoilRecovery * Time.deltaTime);

            transform.position = new Vector3(
                sideX + Mathf.Sign(sideX) * recoil,
                smoothedBottom + span * Mathf.PingPong(phase, 1f),
                0f);

            timer += Time.deltaTime;

            if (timer >= interval)
            {
                timer = 0f;
                Fire();
            }
        }

        /// <summary>
        /// Bandın tavanı: eldeki kutunun bırakma çizgisi. Kuyruk bağlı değilse
        /// ya da elde kutu yoksa kule tepesinin sabit bir pay üstü kullanılıyor
        /// — namlunun bir kare için kulenin içine dalmasındansa biraz yukarıda
        /// beklemesi iyi.
        /// </summary>
        float Ceiling()
        {
            var box = queue != null ? queue.Current : null;

            return box != null
                ? box.DropLineY
                : tracker.HighestSettledPointY() + fallbackCeiling;
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
