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
        float FrameCenterY => transform.position.y - PitchDrop;

        /// <summary>Kadrajın z = 0 düzlemindeki üst kenarı. Debug paneli okuyor.</summary>
        public float FrameTopY => FrameCenterY + HalfHeight;

        /// <summary>Kadrajın z = 0 düzlemindeki alt kenarı.</summary>
        public float FrameBottomY => FrameCenterY - HalfHeight;

        void Awake()
        {
            cam = GetComponent<Camera>();
            ApplyFraming();

            // İlk karede kameranın süzülerek yerine gitmesini istemiyoruz.
            var position = transform.position;
            transform.position = new Vector3(position.x, DesiredHeight(), position.z);
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
            float current = transform.position.y;

            float smoothTime = target > current ? riseSmoothTime : fallSmoothTime;
            float next = Mathf.SmoothDamp(current, target, ref followVelocity, smoothTime);

            transform.position = new Vector3(transform.position.x, next, transform.position.z);
        }

        void ApplyFraming()
        {
            // tan(yatayYarıAçı) = ekranOranı * tan(dikeyYarıAçı)
            // Yatay yarı açıyı istediğim genişlikten buluyorum, dikeyi ondan çözüyorum.
            float halfHorizontal = Mathf.Atan(visibleWidth * 0.5f / PlaneDistance);
            float halfVertical = Mathf.Atan(Mathf.Tan(halfHorizontal) / cam.aspect);

            cam.fieldOfView = halfVertical * 2f * Mathf.Rad2Deg;
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
