using System;
using UnityEngine;

namespace PhysicsStack
{
    /// <summary>
    /// Sürüklenebilir kutu. Üç takip yaklaşımı da burada duruyor; hangisinin
    /// neden elendiği ancak sırayla denenince anlaşılıyor, o yüzden elenenleri
    /// silmek yerine Inspector'dan seçilebilir bıraktım.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class DraggableBody : MonoBehaviour
    {
        public enum FollowMode
        {
            /// <summary>1. yaklaşım: rb.position doğrudan set edilir.</summary>
            DirectPosition,

            /// <summary>2. yaklaşım: kinematik hale getirilip MovePosition ile taşınır.</summary>
            KinematicMovePosition,

            /// <summary>3. yaklaşım (seçilen): dinamik kalır, hedefe doğru hız atanır.</summary>
            VelocityFollow,
        }

        [SerializeField] FollowMode mode = FollowMode.VelocityFollow;

        [Tooltip("His ayarları. Boş bırakılırsa kod içindeki varsayılanlarla çalışır ama uyarı basar.")]
        [SerializeField] DragSettings settings;

        Rigidbody rb;
        BoxVisual visual;
        bool isDragged;

        /// <summary>
        /// Kuyruktaki kutu sırasını beklerken havada asılı duruyor: kinematik ve
        /// yerçekimsiz. İlk dokunuşta kendini serbest bırakıyor.
        /// </summary>
        bool isWaiting;

        /// <summary>
        /// Parmağın son pozisyonu. Update'te yazılır, FixedUpdate'te okunur.
        /// İkisini karıştırmamak bu prototipin en kritik kuralı: girdi kare hızında
        /// gelir, fizik sabit adımda çalışır. Girdiyi doğrudan fiziğe bağlarsak aynı
        /// hareket bazı fizik adımlarında iki kez, bazılarında hiç işlenmez ve his
        /// kare hızına bağlı hale gelir.
        /// </summary>
        Vector3 targetPoint;

        /// <summary>
        /// Kutunun indirilebileceği en alçak nokta. Parmak bunun altına inse bile
        /// hedef nokta burada kalıyor, yani kutu çizginin altına sürüklenemiyor.
        ///
        /// Oyunun tek en önemli kuralı bu. Olmadığında kutuyu kulenin üstüne
        /// milimetrik yerine getirip sıfır hızla bırakabiliyordun: ortada risk
        /// yoktu, sadece sabır vardı. Mesafe zorunlu olunca yerleştirme bir
        /// koymadan bir atışa dönüşüyor ve Gün 4'te ayarladığım bırakma hızı
        /// nihayet bir şey ifade etmeye başlıyor.
        ///
        /// Kısıt hedefe uygulanıyor, cismin kendisine değil: kutu fizikle
        /// aşağı itilebilir, sadece oyuncu tarafından indirilemez.
        /// </summary>
        float minDragHeight = float.NegativeInfinity;

        /// <summary>
        /// Oyun alaninin yatay siniri.
        ///
        /// Buna ihtiyac top aticiyla ortaya cikti: surukleme yatayda sinirsiz
        /// oldugu icin oyuncu kutuyu namlunun disina goturup tehdidi tamamen
        /// atlatabiliyordu. Namluyu daha kenara tasimak cozmuyor, cunku "kenar"
        /// ekran genisligine gore degisen bir sey - genis ekranda oyun alani
        /// kendiliginden buyuyor ve oyun ekran boyutuna gore kolaylasiyordu.
        ///
        /// Sinir koyunca oyun alani ekrandan bagimsiz hale geliyor: namlu o
        /// bandin hemen disinda duruyor ve ulasilabilir her noktayi vurabiliyor.
        /// </summary>
        float minDragX = float.NegativeInfinity;
        float maxDragX = float.PositiveInfinity;

        public void SetHorizontalBounds(float min, float max)
        {
            minDragX = min;
            maxDragX = max;
        }

        /// <summary>
        /// Kutu bir kez bırakıldıysa artık yığının parçası; tekrar alınamıyor.
        ///
        /// Oynarken çıktı: yerleştirilmiş kutuyu geri alıp yeniden bırakabilmek
        /// oyunun bütün zorluğunu siliyor. Beğenmediğin her atışı düzeltebiliyorsan
        /// bırakma mesafesi de, kule dengesi de, tutunma şartı da anlamını
        /// kaybediyor — sonunda herkes mükemmel kuleyi kuruyor, sadece daha uzun
        /// sürede. Bir atış bir karardır; geri alınabilen karar karar değildir.
        /// </summary>
        public bool CanGrab => !isPlaced;

        bool isPlaced;

        /// <summary>
        /// Bırakıldıktan sonra ilk temasını yaptı mı?
        ///
        /// Rüzgâr bunu soruyor. Önce "oturdu mu" diye soruyordu ama oturmuş
        /// sayılmak için 0.2 sn kesintisiz durmak gerekiyor: kutu kuleye indikten
        /// sonra da yanlamasına itilmeye devam ediyor ve devriliyordu. Rüzgârın
        /// işi kutu havadayken biter — yere değdikten sonrası artık fizik.
        /// </summary>
        public bool HasLanded { get; private set; }

        public bool IsDragged => isDragged;

        /// <summary>Bırakma çizgisinin yüksekliği; ekrandaki çizgi bunu okuyor.</summary>
        public float DropLineY { get; private set; } = float.NegativeInfinity;

        /// <summary>
        /// Kuyruk kutuyu üretirken seviyenin bırakma çizgisini buraya yazıyor.
        ///
        /// Çizgi kutunun **altının** inebileceği yeri gösteriyor, merkezinin değil.
        /// Merkeze göre tanımlasaydım aynı sayı farklı boydaki kutular için farklı
        /// düşme mesafesi anlamına gelirdi; boyut oynayan seviyelerde bırakma
        /// mesafesi sessizce rastgeleleşirdi.
        ///
        /// Yarım boy bir kez, üretim anında ölçülüyor. Kutu döndükçe yeniden
        /// ölçseydim kısıt oyuncunun elinde değişirdi — kural sabit durmalı.
        /// </summary>
        public void SetDropLine(float lineY)
        {
            DropLineY = lineY;

            float halfHeight = TryGetComponent(out Collider collider) ? collider.bounds.extents.y : 0.5f;
            minDragHeight = lineY + halfHeight;
        }

        /// <summary>Yerleşme tespiti rigidbody'nin kendisine bakıyor; dışarı açıyoruz.</summary>
        public Rigidbody Body => rb;

        public event Action<DraggableBody> Grabbed;
        public event Action<DraggableBody> Released;

        /// <summary>
        /// Birakilmis kutu bir seye carpti. Temas noktasi ve carpma hizi ile
        /// birlikte veriliyor; sahne efektleri (toz, kamera sarsintisi) bunu
        /// dinliyor.
        ///
        /// Carpismayi burada yakalamamin sebebi: temas bilgisi yalnizca burada
        /// var. Disaridan her karede "acaba carpti mi" diye hiz farkina bakmak
        /// hem daha pahali hem de temas noktasini vermiyor.
        /// </summary>
        public event Action<DraggableBody, Vector3, float> Landed;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();

            // Görsel gövde ayrı bir çocuk nesnede; ezilme animasyonu orada
            // çalışıyor ki collider'a dokunmasın.
            visual = GetComponentInChildren<BoxVisual>();

            if (settings == null)
            {
                // Sessizce çalışmasındansa bağırarak çalışsın: varlığı prefab'a
                // vermeyi unutmak, "değeri değiştiriyorum ama hiçbir şey olmuyor"
                // diye yarım saat harcanacak türden bir hata.
                Debug.LogWarning($"[DraggableBody] {name}: DragSettings atanmamış, varsayılan değerlerle çalışıyorum.", this);
                settings = ScriptableObject.CreateInstance<DragSettings>();
            }
        }

        /// <summary>
        /// Kuyrukta sırasını bekleyen kutuyu havada tutar. Dinamik bırakırsak
        /// oyuncu dokunmadan düşer; kinematik yapmak "sıradaki kutu" fikrini
        /// ayrı bir bekleme nesnesi icat etmeden veriyor.
        /// </summary>
        public void HoldInPlace()
        {
            isWaiting = true;
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        public void BeginDrag(Vector3 point)
        {
            if (isWaiting)
            {
                isWaiting = false;
                rb.isKinematic = false;
                rb.useGravity = true;
            }

            targetPoint = Clamp(point);
            isDragged = true;

            Grabbed?.Invoke(this);

            if (mode == FollowMode.KinematicMovePosition)
            {
                rb.isKinematic = true;
            }
        }

        /// <summary>Update'ten çağrılır; sadece hedefi günceller, fiziğe dokunmaz.</summary>
        public void MoveTarget(Vector3 point)
        {
            targetPoint = Clamp(point);
        }

        Vector3 Clamp(Vector3 point)
        {
            point.x = Mathf.Clamp(point.x, minDragX, maxDragX);
            point.y = Mathf.Max(point.y, minDragHeight);
            return point;
        }

        public void EndDrag()
        {
            isDragged = false;
            isPlaced = true;

            switch (mode)
            {
                case FollowMode.DirectPosition:
                    // Kutu her kare ışınlandığı için rigidbody'nin üstünde anlamlı bir
                    // hız birikmiyor; sıfırlamasak da bırakınca olduğu yerde düşüyor.
                    rb.linearVelocity = Vector3.zero;
                    break;

                case FollowMode.KinematicMovePosition:
                    // Kinematikten dinamiğe dönerken hız sıfır: fırlatma yok, taş gibi düşüş.
                    rb.isKinematic = false;
                    rb.linearVelocity = Vector3.zero;
                    break;

                case FollowMode.VelocityFollow:
                    // Bırakma anında rigidbody'nin üstünde zaten doğru hız var,
                    // fırlatma ayrı bir kod yazmadan geliyor — bu yaklaşımın en güzel
                    // tarafı buydu. Ama tamamen serbest bırakınca sorun çıkıyordu:
                    // kuleye yaklaşırken parmağı hızlı oynatıp bırakınca kutu maksimum
                    // hızla kulenin içine giriyor ve altındaki her şeyi süpürüyordu.
                    // Kırpma fırlatmayı öldürmüyor, sadece üst sınırını "kuleyi
                    // yıkmayacak" seviyeye çekiyor.
                    rb.linearVelocity = Vector3.ClampMagnitude(rb.linearVelocity, settings.releaseSpeedClamp);
                    break;
            }

            Released?.Invoke(this);
        }

        void OnCollisionEnter(Collision collision)
        {
            // Sürüklenirken kuleye çarpmak iniş değil; oyuncu hâlâ kontrol ediyor.
            if (!isPlaced || isDragged)
            {
                return;
            }

            HasLanded = true;

            if (visual != null)
            {
                visual.Impact(collision.relativeVelocity.magnitude);
            }

            Landed?.Invoke(this, collision.GetContact(0).point, collision.relativeVelocity.magnitude);
        }

        void FixedUpdate()
        {
            if (!isDragged)
            {
                return;
            }

            switch (mode)
            {
                case FollowMode.DirectPosition:
                    // Kutu kareler arasında ışınlanıyor. Çarpışma çözücüsü araya
                    // giremediği için zeminden ve yığından geçiyor.
                    rb.position = targetPoint;
                    break;

                case FollowMode.KinematicMovePosition:
                    // Çarpışmalara saygılı ama kinematik cisim itilemez: sürüklenen
                    // kutu yığını ezip geçiyor, kendisi hiç zorlanmıyor.
                    rb.MovePosition(targetPoint);
                    break;

                case FollowMode.VelocityFollow:
                    ApplyVelocityFollow();
                    break;
            }
        }

        void ApplyVelocityFollow()
        {
            Vector3 delta = targetPoint - rb.position;

            // delta / fixedDeltaTime = "tek fizik adımında oraya varmak için gereken hız".
            // followStrength bunun ne kadarını uygulayacağımızı söylüyor; 1'e yaklaştıkça
            // kutu parmağa yapışıyor, düşük değerlerde arkadan sürüklenen bir ağırlık gibi.
            Vector3 desired = delta / Time.fixedDeltaTime * settings.followStrength;

            // Parmağı hızlı çekince kutu yetişemiyor ve geride kalıyor; bu gecikme
            // "ağır cisim" olarak okunuyor. Aynı zamanda kutunun çılgın hızlara
            // çıkıp sahneden fırlamasını da engelliyor.
            desired = Vector3.ClampMagnitude(desired, settings.maxSpeed);

            // Hızı doğrudan atamak yerine ona doğru yürüyoruz. Doğrudan atama fiziğin
            // karşı koyma hakkını elinden alıyordu: kutu kulenin üstündeki kutuya
            // değdiği anda çözücü hızı sıfırlıyor, biz bir sonraki adımda aynı
            // hızı geri yazıyoruz — yani her fizik adımında kuleye yeni bir darbe.
            // Adım başına değişimi sınırlayınca kutu engele dayandığında hız
            // birikemiyor: itiş sertliğinin üst sınırı artık maxAcceleration.
            //
            // Bu, AddForce(ForceMode.Acceleration) ile aynı hesabın açık yazılmış hali.
            // AddForce'u tercih etmememin sebebi şu: kuvvet uygulayıp sonucu fiziğe
            // bırakınca kutunun ne kadar hızlanacağını kütle, sürtünme ve temas
            // belirliyor, dolayısıyla "parmağa ne kadar yetişecek" sorusunun cevabı
            // elimden çıkıyor. Burada hedef hız benim, ona ulaşma sertliği fiziğin.
            rb.linearVelocity = Vector3.MoveTowards(
                rb.linearVelocity, desired, settings.maxAcceleration * Time.fixedDeltaTime);
        }
    }
}
