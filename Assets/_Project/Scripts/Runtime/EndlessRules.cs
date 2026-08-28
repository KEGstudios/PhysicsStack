using UnityEngine;

namespace PhysicsStack
{
    /// <summary>
    /// Sonsuz modun kuralları: hedef yok, kazanma yok. Kule çökene kadar
    /// yığıyorsun, skor ulaştığın yükseklik.
    ///
    /// Seviye modunda zorluk seviyeler arasında artıyor; burada turun **içinde**
    /// artıyor. Sonsuz modu ilginç kılan tek şey bu eğri: sabit zorlukta sonsuz
    /// bir mod, bir süre sonra oyun değil test ortamı olur.
    ///
    /// Eğrinin basamakları oynanışta ölçülen tur uzunluğuna göre yerleşti:
    /// rüzgâr 6 birimde başlayıp 14'te doluyor, 14'te salınmaya başlıyor, namlu
    /// 18'de giriyor. Bütün eşikler **kule boyu**, atılan kutu sayısı değil:
    /// kutu sayısıyla çalışırken kutuları kulenin yanına atarak merdiveni
    /// tırmandırmak mümkündü. İlk hâlinde sırasıyla 8, 18, 16 ve 22'ydi ve turlar 8 kutu
    /// civarında bitiyordu — yani merdivenin tamamı hiç görülmeyen bir yere
    /// kurulmuştu.
    ///
    /// Cevabı merdiveni indirmek değil, tavanı yükseltmek oldu: kutuları
    /// yavaşlatan hava sürtünmesi ve daha güçlü fizik çözücü ayarları (bkz.
    /// DragSettings.fallDrag). 8 kutuluk bir tavan sonsuz modu kısaltmakla
    /// kalmaz, üstüne seviye tasarlanacak alanı da bitirirdi.
    ///
    /// Bu yüzden ilk yazdığım kural — "her tehdit bir öncekinin tepeye
    /// ulaşmasını bekler" — burada tam uygulanamıyor: rüzgâr, bırakma mesafesi
    /// hâlâ artarken giriyor. O kural bir amaç değil, okunurluğun aracıydı;
    /// amaç aynı anda **değişen** tek bir şey olması. Mesafenin tavanı da bu
    /// yüzden 4.0'dan 3.6'ya indi — yeni bir tehdit eklerken eskisinden bir şey
    /// geri vermezsen mod zorlaşmıyor, sadece kısalıyor.
    /// </summary>
    public sealed class EndlessRules : IStackRules
    {
        readonly float collapseDrop;

        /// <summary>
        /// Zorluğun tepeye ulaşması için gereken kule boyu (birim).
        ///
        /// Bütün eşikler kutu sayısından yüksekliğe çevrildi. Sebebi oynanışta
        /// çıktı: kutuları kulenin yanına atarak sayacı ilerletebiliyordun,
        /// rüzgâr ve top geliyordu ama kule 7 birimde duruyordu. Zorluk,
        /// zorluğu hak eden şeyden bağımsız artıyordu.
        ///
        /// Sayılar aynı kaldı çünkü kutu boyu 1 birim: "15 kutu" ile "15 birim"
        /// düzgün yığılmış bir kulede zaten aynı şey. Değişen tek şey, artık
        /// yalnızca kuleye konan kutunun sayılması.
        /// </summary>
        const float RampHeight = 15f;

        const float StartDropGap = 2.5f;

        /// <summary>
        /// Mesafenin tavanı. Bir ara 4.0'dı, yani seviye 8 ile aynı. Rüzgâr
        /// sonsuz moda girince düşürdüm: yeni bir tehdit eklerken eskisinden
        /// bir şey geri vermezsen mod zorlaşmıyor, sadece kısalıyor.
        ///
        /// Fizik sağlamlaştıktan sonra 4.0'a geri çıkarılabilir. Şimdilik
        /// bilerek burada duruyor: aynı turda hem çarpmayı yumuşatıp hem mesafeyi
        /// büyütseydim, ortaya çıkan tur uzunluğunun hangisinden geldiğini
        /// ölçemezdim.
        /// </summary>
        const float EndDropGap = 3.6f;

        const float EndWidthVariance = 0.25f;

        /// <summary>Rüzgârın ilk esintisi ve tam şiddete ulaştığı kule boyu (birim).</summary>
        const float WindFirst = 6f;
        const float WindFull = 14f;

        /// <summary>Tepe rüzgâr hızı. Seviye 7'dekiyle aynı.</summary>
        const float TopWindSpeed = 1f;

        /// <summary>
        /// Rüzgârın yön değiştirmeye başladığı boy ve periyodu (sn). Şiddetin
        /// dolduğu boyla aynı: rüzgâr önce güçleniyor, tavana vurunca artmayı
        /// bırakıp huysuzlaşıyor. Aynı anda hem güçlenip hem salınsaydı ikisini
        /// birbirinden ayırmak mümkün olmazdı.
        /// </summary>
        const float WindSwingFirst = WindFull;
        const float WindSwingPeriod = 3.2f;

        /// <summary>Namlunun devreye girdiği kule boyu (birim).</summary>
        const float CannonFirst = 18f;

        /// <summary>İlk kontrol noktası ve sonraki aralıkların başlangıcı (birim).</summary>
        const float FirstCheckpoint = 10f;
        const float CheckpointGap = 15f;

        /// <summary>Her kontrol noktasından sonra aralığın büyüme miktarı (birim).</summary>
        const float CheckpointGapGrowth = 5f;

        /// <param name="collapseDrop">Kule zirvesinin bu kadar altına düşerse çökmüş sayılır.</param>
        public EndlessRules(float collapseDrop = 0.6f)
        {
            this.collapseDrop = collapseDrop;
        }

        public string Title => "Sonsuz";

        /// <summary>Hedef yok. Sıfır dönmek hedef çizgisini de gizliyor.</summary>
        public float TargetHeight => 0f;

        /// <summary>Hedef yoksa tutunma şartı da yok: sonsuz modda tur kule çökene kadar sürüyor.</summary>
        public float HoldTime => 0f;

        /// <summary>
        /// Tehditler yığdıkça devreye giriyor.
        ///
        /// Uzun süre burada hiç tehdit yoktu ve gerekçesi şuydu: tek bir şeyin
        /// (bırakma mesafesinin) sürekli artması, üst üste binen üç şeyden daha
        /// okunur bir tırmanış verir. Gerekçe hâlâ doğru ama eksikmiş — mesafe
        /// 15 kutuda tavana vuruyor ve ondan sonrası sabit zorlukta bir tur
        /// oluyor. Yani "tırmanış okunur olsun" derken tırmanışın kendisini
        /// 15 kutuyla sınırlamışım.
        ///
        /// Düzeltme tehditleri üst üste yığmak değil, sıraya dizmek: rüzgâr
        /// sabit yönle başlıyor (bir kez öğrenilip telafi edilen bir şey),
        /// sonra salınıma geçiyor (telafi zamanlamaya bağlanıyor), en son namlu
        /// geliyor (ritim). Üçü aynı anda gelseydi oyuncu neyi yanlış yaptığını
        /// göremezdi.
        ///
        /// Sayılar oynanıştan geldi, tasarım masasından değil: ilk denemede
        /// eşikler 8/16/22'ydi ve turlar 8 kutu civarında bitiyordu, yani
        /// merdivenin tamamı ulaşılamayan bir yerdeydi.
        ///
        /// Tavan var: 18 kutudan sonra hiçbir şey artmıyor. Sonsuza kadar artan
        /// bir eğri, oyuncunun becerisinin değil eğrinin kazandığı bir yer
        /// yaratır; buradaki bütün sayılar aynı düşüncenin devamı.
        /// </summary>
        public HazardSettings HazardsFor(in StackSnapshot snapshot)
        {
            float height = snapshot.Height;

            var hazards = HazardSettings.None;

            // InverseLerp kırpıyor: 6'dan önce sıfır, 14'ten sonra bir. Eşiği
            // ayrıca kontrol etmeye gerek yok ve rüzgâr sıfırken gösterge de
            // kendiliğinden gizli kalıyor.
            hazards.windSpeed = TopWindSpeed * Mathf.InverseLerp(WindFirst, WindFull, height);
            hazards.windResponse = 3f;
            hazards.windPeriod = snapshot.Reached(WindSwingFirst) ? WindSwingPeriod : 0f;

            if (snapshot.Reached(CannonFirst))
            {
                // Sonsuz modda tek namlu. İkincisi tavanı yükseltirdi ve
                // "18 birimden sonra hiçbir şey artmıyor" kuralı bilerek
                // konmuştu; ikinci namlu seviye tarafının işi.
                hazards.cannonCount = 1;

                // Seviye 8'dekinden yumuşak: orada namlu bilinen bir kule
                // yüksekliğine karşı ayarlanmıştı, burada oyuncu ona 18 birim
                // yorulmuş ve rüzgârla boğuşurken geliyor.
                hazards.cannonInterval = 2.4f;
                hazards.cannonBallSpeed = 6.5f;
                hazards.cannonPatrolSpeed = 1.5f;
                hazards.cannonBottomGap = 0.4f;
            }

            return hazards;
        }

        /// <summary>
        /// Kontrol noktaları: 10, 25, 45, 70, 100... Aralık her seferinde 5 kutu
        /// büyüyor.
        ///
        /// Sabit aralık (her 10 kutuda bir) daha basit olurdu ama yanlış şeyi
        /// ölçer: ilk 10 kutu ile 90'dan 100'e giden 10 kutu aynı iş değil, çünkü
        /// ikincisinde zorluk çoktan tavana vurmuş durumda. Aralığın büyümesi,
        /// kontrol noktasının bir ödül olarak değerini koruyor.
        ///
        /// Döngüyle hesaplanıyor, dizi tutulmuyor: sonsuz modun sonu yok ve
        /// tabloyu bir yere kadar yazmak, o yerden sonrasını sessizce farklı
        /// davranan bir oyun demek.
        /// </summary>
        public float CheckpointAfter(float height)
        {
            float next = FirstCheckpoint;
            float gap = CheckpointGap;

            while (next <= height)
            {
                next += gap;
                gap += CheckpointGapGrowth;
            }

            return next;
        }

        public RunOutcome Evaluate(in StackSnapshot snapshot)
        {
            if (snapshot.AnyFallen)
            {
                return RunOutcome.Lost;
            }

            // Düşen kutunun bir bedeli var, ama üçüncüsüne kadar tur bitmiyor.
            //
            // Önce yere düşen kutu hiçbir şey yapmıyordu: tur devam ediyor,
            // sayaçlar ilerliyordu. En güvenli oynayış "kutuyu kenara at"
            // oluyordu ve bir oyunun en güvenli hamlesi hiçbir şey yapmamaksa
            // orada oyun yoktur. Onu düzeltirken ceza fazla sert kaçtı: tek
            // kutu turu bitiriyordu ve kontrol noktalarıyla birlikte bu, yirmi
            // kutuluk bir tırmanışın tek bir sakar bırakışta bitmesi demekti.
            //
            // Şimdiki hak seviyelerdekiyle aynı sayı ve bu bilerek: aynı el
            // hareketi iki modda farklı cezalandırılınca oyuncunun öğrendiği şey
            // kural değil, "hangi moddayım" oluyor.
            if (snapshot.DroppedCount >= StackRules.MaxDrops)
            {
                return RunOutcome.Lost;
            }

            // Sonsuz modun tek bitiş koşulu bu. Zemine düşmeyi beklemek işe
            // yaramıyordu: devrilen kutu 14 birimlik zemine oturuyor, ölüm
            // yüksekliğinin altına hiç inmiyor, yani tur hiç bitmiyordu.
            return snapshot.Settled && snapshot.Collapsed(collapseDrop)
                ? RunOutcome.Lost
                : RunOutcome.Continue;
        }

        /// <summary>
        /// Skor kule boyu, yığılan kutu sayısı değil. Sayıyı seçerken şunu
        /// düşündüm: kutu sayısı, kuleyi hiç yükseltmeyen kutularla da artar —
        /// yere yan yana kutu dizerek skor toplanabilirdi. Boy ise ancak
        /// gerçekten yükseldiğinde artıyor, yani ölçtüğü şey oyunun kendisi.
        /// </summary>
        public float Score(in StackSnapshot snapshot) => snapshot.PeakHeight;

        /// <summary>
        /// Tek ondalık. İki ondalık, ölçünün taşıyamayacağı bir kesinlik
        /// gösteriyordu: temas gömülmesi yüzünden on kutuluk kule 9.99 okunuyor
        /// ve oyuncuya "neden 10 değil" dedirtiyor. Yüzde birlik fark oyunda
        /// hiçbir karara girmiyor.
        /// </summary>
        public string DescribeScore(float score) => $"{score:0.0} birim";

        /// <summary>
        /// Zorluk ilk 15 kutuda tepeye çıkıyor, sonra sabit kalıyor. Sonsuza kadar
        /// artan bir eğri, oyuncunun becerisinin değil eğrinin kazandığı bir yer
        /// yaratırdı; tavanı olan zorluk "nereye kadar dayanabilirim" sorusunu
        /// oyuncuya bırakıyor.
        /// </summary>
        public BoxDifficulty NextBox(in StackSnapshot snapshot)
        {
            float t = Mathf.Clamp01(snapshot.Height / RampHeight);
            return new BoxDifficulty(
                Mathf.Lerp(StartDropGap, EndDropGap, t),
                Mathf.Lerp(0f, EndWidthVariance, t));
        }

        public override string ToString() => "Sonsuz";
    }
}
