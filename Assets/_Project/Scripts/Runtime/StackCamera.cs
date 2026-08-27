using UnityEngine;

namespace PhysicsStack
{
    /// <summary>
    /// Kamerayı kulenin tepesine göre konumlandırır ve kadrajın genişliğini
    /// cihazdan bağımsız tutar.
    ///
    /// İki ayrı sorunu çözüyor:
    ///
    /// 1. <b>Sabit kamera kuleyi kaybediyordu.</b> Beş kutuda sorun çıkmıyordu
    ///    ama oyun "nereye kadar" sorusuna dönüşünce kule kadrajdan çıkıyor.
    ///
    /// 2. <b>Dikey FOV sabit kalırsa görünen genişlik ekrana göre değişiyor.</b>
    ///    Dar bir telefonda oyun alanı daralıyor, tablette genişliyor — yani
    ///    oyunun zorluğu cihaza bağlı hale geliyor. Oysa bu oyunun ihtiyacı olan
    ///    şey genişlik: kutuyu sağa sola taşıyacak alan. O yüzden yatay görüşü
    ///    sabitleyip dikey FOV'u ekran oranından hesaplıyorum. Portre telefonda
    ///    da yatay tarayıcıda da oyun alanı aynı genişlikte kalıyor.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class StackCamera : MonoBehaviour
    {
        [SerializeField] StackTracker tracker;

        [Tooltip("Her cihazda görünmesi garanti edilen yatay alan (dünya birimi).")]
        [SerializeField] float visibleWidth = 5f;

        [Tooltip("Kadrajda en az bu kadar dünya yüksekliği görünür. Kule buna sığmalı.")]
        [SerializeField] float minVisibleHeight = 11f;

        [Tooltip("Kulenin tepesiyle kadrajın üst kenarı arasında bırakılan boşluk.")]
        [SerializeField] float topMargin = 6f;

        /// <summary>
        /// Kuyruğun istediği tepe boşluğu. Sabit bir sayı yerine seviyeye göre
        /// değişmesinin sebebi: uzun tehdit koridoru olan bir seviyede kutu çok
        /// yukarıda beliriyor, kamera onu göremezse kural sessizce gevşiyor. Ama
        /// aynı boşluğu bütün seviyelere vermek de kuleyi her seviyede lüzumsuz
        /// yere kadrajın dibine iterdi.
        ///
        /// Kamera bunu kendi hesaplamıyor: gereken yeri, sayıyı zaten hesaplayan
        /// <see cref="BoxQueue"/> söylüyor.
        /// </summary>
        float reservedHeadroom;

        /// <summary>Yürürlükteki tepe boşluğu: tabanla istenenin büyüğü.</summary>
        float TopMargin => Mathf.Max(topMargin, reservedHeadroom);

        /// <summary>Kuyruk sıradaki kutuyu üretmeden önce gereken tepe boşluğunu bildiriyor.</summary>
        public void ReserveHeadroom(float headroom)
        {
            reservedHeadroom = headroom;
        }

        [Tooltip("Zeminin kadrajın alt kenarının altında kalma payı.")]
        [SerializeField] float groundMargin = 0.6f;

        [Tooltip("Yükselirken yumuşatma süresi (sn).")]
        [SerializeField] float riseSmoothTime = 0.35f;

        [Tooltip("Alçalırken yumuşatma. Bilerek daha uzun: kule devrilince kamera hemen dalmasın, ne olduğu görülsün.")]
        [SerializeField] float fallSmoothTime = 1.1f;

        Camera cam;
        float followVelocity;

        /// <summary>Kutular z = 0 düzleminde; kameranın o düzleme uzaklığı bu.</summary>
        /// <summary>
        /// Kameranin sarsintisiz mantiksal yuksekligi.
        ///
        /// Ayri tutulmasi sart: sarsinti dogrudan transform'a yazilsaydi bir
        /// sonraki karede o sarsinti "hedefe olan mesafe" olarak okunur ve
        /// yumusatmaya geri beslenirdi - kamera kendi titremesini kovalamaya
        /// baslardi. Kadraj hesaplari da bu degeri kullaniyor, yoksa sarsinti
        /// sirasinda birakma cizgisi ve spawn yuksekligi titrerdi.
        /// </summary>
        float height;

        float shake;
        float shakeSeed;

        /// <summary>
        /// Kameranin sarsintisiz x/z konumu.
        ///
        /// Yukseklikle ayni sebep, ama bunu ilk yazista kacirdim: yatay sarsintiyi
        /// <c>transform.position.x</c>'e ekliyordum ve o deger zaten onceki
        /// karenin sarsintisini iceriyordu. Offset birikince kamera her carpmada
        /// biraz daha yana kayiyor ve bir daha geri donmuyordu.
        /// </summary>
        Vector2 basePlane;

        [Header("Sarsinti")]
        [Tooltip("Sarsintinin sonme hizi (birim/sn).")]
        [SerializeField] float shakeDecay = 0.9f;

        [Tooltip("Sarsintinin titresim sikligi.")]
        [SerializeField] float shakeFrequency = 26f;

        [Tooltip("Tek seferde izin verilen en buyuk sarsinti (dunya birimi).")]
        [SerializeField] float maxShake = 0.16f;

        /// <summary>Carpma ya da cokus aninda cagriliyor; sertlik dunya birimi.</summary>
        public void Shake(float amount)
        {
            shake = Mathf.Min(Mathf.Max(shake, amount), maxShake);
        }

        float PlaneDistance => Mathf.Abs(transform.position.z);

        /// <summary>Kadrajın z = 0 düzlemindeki yarı yüksekliği.</summary>
        float HalfHeight => PlaneDistance * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);

        /// <summary>
        /// Kamera hafif aşağı bakıyor (kutuların üst yüzü görünsün diye), bu yüzden
        /// kadrajın z = 0 düzlemindeki merkezi kameranın y'sinin altında kalıyor.
        /// Eğim küçük olduğu için düzlem mesafesini z ile alıyorum; 8 derecede
        /// gerçek mesafeyle arasındaki fark binde beş, ölçülecek bir şey değil.
        /// </summary>
        float PitchDrop => PlaneDistance * Mathf.Tan(transform.eulerAngles.x * Mathf.Deg2Rad);

        /// <summary>Kadrajın z = 0 düzlemindeki dikey merkezi.</summary>
        float FrameCenterY => height - PitchDrop;

        /// <summary>Kadrajın z = 0 düzlemindeki üst kenarı. Debug paneli okuyor.</summary>
        public float FrameTopY => FrameCenterY + HalfHeight;

        /// <summary>Kadrajın z = 0 düzlemindeki alt kenarı.</summary>
        public float FrameBottomY => FrameCenterY - HalfHeight;

        void Awake()
        {
            cam = GetComponent<Camera>();

            // Mantıksal yükseklik kadraj hesaplarının girdisi; ApplyFraming'den
            // önce doldurulması gerekiyor, yoksa ilk kare sıfırdan hesaplanır.
            height = transform.position.y;
            basePlane = new Vector2(transform.position.x, transform.position.z);
            shakeSeed = Random.value * 100f;

            ApplyFraming();

            // İlk karede kameranın süzülerek yerine gitmesini istemiyoruz.
            height = DesiredHeight();

            var position = transform.position;
            transform.position = new Vector3(position.x, height, position.z);
        }

        /// <summary>
        /// LateUpdate: kutular fizikte hareket ediyor, kamera onların o karedeki
        /// son hâline bakmalı. Update'te takip edersek kamera bir kare geriden
        /// gelir ve hızlı düşüşlerde titrer.
        /// </summary>
        void LateUpdate()
        {
            // Ekran oranı çalışırken değişebiliyor (tarayıcıda telefon döndürülünce),
            // o yüzden her karede kontrol ediliyor; hesap birkaç trigonometriden ibaret.
            ApplyFraming();

            float target = DesiredHeight();
            float smoothTime = target > height ? riseSmoothTime : fallSmoothTime;
            height = Mathf.SmoothDamp(height, target, ref followVelocity, smoothTime);

            transform.position = new Vector3(
                basePlane.x + ShakeOffset(out float verticalShake),
                height + verticalShake,
                basePlane.y);
        }

        /// <summary>
        /// Sarsinti offseti. Her karede rastgele bir nokta secmek ucuz ama
        /// "bozuk goruntu" gibi okunuyor; Perlin gurultusu surekli oldugu icin
        /// hareket sarsinti gibi hissediliyor.
        /// </summary>
        float ShakeOffset(out float vertical)
        {
            shake = Mathf.MoveTowards(shake, 0f, shakeDecay * Time.deltaTime);

            if (shake <= 0f)
            {
                vertical = 0f;
                return 0f;
            }

            float t = Time.time * shakeFrequency;
            vertical = (Mathf.PerlinNoise(shakeSeed, t) - 0.5f) * 2f * shake;
            return (Mathf.PerlinNoise(t, shakeSeed) - 0.5f) * 2f * shake;
        }

        /// <summary>
        /// Kadrajı iki kısıt belirliyor: en az <see cref="visibleWidth"/> kadar
        /// genişlik **ve** en az <see cref="minVisibleHeight"/> kadar yükseklik.
        /// Hangisi daha geniş açı istiyorsa o kazanıyor.
        ///
        /// Başta yalnızca genişlik kısıtı vardı ve portrede doğru çalışıyordu.
        /// Geniş ekranda ise aynı kural tersine dönüyor: 5 birimlik genişliği
        /// 16:10 bir ekrana sabitlemek görünür yüksekliği 3.1 birime düşürüyor —
        /// hedef yüksekliği 4.0 olan bir oyunda kule hedefe varmadan kadrajın
        /// dışına çıkıyor. Yani "oyunun çoğu görünmüyor" değil, oyun oraya
        /// sığmıyordu.
        ///
        /// İki kısıtla artık dar ekranda genişlik, geniş ekranda yükseklik
        /// belirleyici oluyor; ikisinde de oynanan alan kadrajda kalıyor.
        /// Geniş ekranda yanlarda fazladan dünya görünüyor, bu yüzden zemin ve
        /// çizgiler oyun alanından belirgin şekilde geniş tutuluyor.
        /// </summary>
        void ApplyFraming()
        {
            // tan(yatayYarıAçı) = ekranOranı * tan(dikeyYarıAçı)
            // Yatay yarı açıyı istediğim genişlikten buluyorum, dikeyi ondan çözüyorum.
            float halfHorizontal = Mathf.Atan(visibleWidth * 0.5f / PlaneDistance);
            float halfFromWidth = Mathf.Atan(Mathf.Tan(halfHorizontal) / cam.aspect);

            float halfFromHeight = Mathf.Atan(minVisibleHeight * 0.5f / PlaneDistance);

            cam.fieldOfView = Mathf.Max(halfFromWidth, halfFromHeight) * 2f * Mathf.Rad2Deg;
        }

        float DesiredHeight()
        {
            float half = HalfHeight;

            // Alt sınır: zemin kadrajın alt kenarının hemen altında kalsın.
            float restingCenter = half - groundMargin;

            // Kule büyüdükçe: tepesi üst kenarın altında kalsın.
            float towerTop = tracker != null ? tracker.HighestSettledPointY() : 0f;
            float followCenter = towerTop + TopMargin - half;

            // İstenen merkez bulundu; kameranın y'si eğim payı kadar yukarıda duruyor.
            return Mathf.Max(restingCenter, followCenter) + PitchDrop;
        }

        /// <summary>
        /// Kuyruk sıradaki kutuyu buraya bırakıyor.
        ///
        /// İlk hâlinde kutu kadrajın en üstünde beliriyordu ve iki sorun çıktı:
        /// kutunun üst kenarı ekranın dışında kalıyordu, ve yerleştiremeden
        /// bırakınca dokuz birim aşağı düşüyordu. Yeni kural kuleye göre:
        /// kutu daima yığının biraz üstünde beliriyor. Böylece düşme mesafesi
        /// kule ne kadar yükselirse yükselsin sabit kalıyor.
        ///
        /// Yine de kadrajın üst kenarına takılıyor: kule kameranın yetişemeyeceği
        /// kadar hızlı büyürse kutu ekran dışına çıkmasın diye.
        /// </summary>
        public float SpawnHeight(float aboveTower, float minHeight, float marginFromTop)
        {
            float towerTop = tracker != null ? tracker.HighestSettledPointY() : 0f;

            // Alt sınır: yığın boşken "kuleTepesi + 4" tam 4 ediyor, yani hedef
            // çizgisinin dibi — kutu çizgiye biniyor ve "zaten hedefteyim" gibi
            // okunuyordu. Kule yükseldikçe bu sınır kendiliğinden devre dışı
            // kalıyor, o yüzden ayrı bir durum kontrolü gerekmiyor.
            float desired = Mathf.Max(towerTop + aboveTower, minHeight);

            return Mathf.Min(desired, FrameTopY - marginFromTop);
        }
    }
}
