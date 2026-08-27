using System;
using UnityEngine;

namespace PhysicsStack
{
    /// <summary>
    /// Sıradaki kutuyu kulenin üstünde hazır bekletir. Tek kutu üretiyor:
    /// oyuncu onu alıp yerleştirene kadar yenisi gelmiyor.
    ///
    /// Kutunun nasıl olacağına burası karar vermiyor; kural veriyor
    /// (<see cref="BoxDifficulty"/>), burası uyguluyor. Kuyruk bir fabrika,
    /// zorluk ayarı değil.
    ///
    /// Havuz (pooling) yok, çünkü bir turda üretilen kutu sayısı iki haneli;
    /// erken optimizasyon burada okunabilirlikten çalar. Sayı büyürse yeri belli.
    /// </summary>
    public sealed class BoxQueue : MonoBehaviour
    {
        [SerializeField] GameObject boxPrefab;

        [Tooltip("Kule tepesini okumak için. Hem spawn yüksekliği hem bırakma çizgisi buna bağlı.")]
        [SerializeField] StackTracker tracker;

        [Tooltip("Kamera bağlıysa kutu kadrajın içinde kalacak şekilde belirir.")]
        [SerializeField] StackCamera stackCamera;

        [Tooltip("Kutunun yığının tepesinden ne kadar yukarıda belireceği.")]
        [SerializeField] float spawnAboveTower = 4f;

        [Tooltip("Kutu, kendi bırakma çizgisinin en az bu kadar üstünde belirmeli.")]
        [SerializeField] float spawnAboveDropLine = 0.8f;

        [Tooltip("Yığın boşken bile kutunun ineceği en alçak yükseklik. Hedef çizgisinin üstünde kalması için.")]
        [SerializeField] float minSpawnHeight = 5.2f;

        [Tooltip("Kadrajın üst kenarına bu kadar yaklaşabilir; kule hızlı büyürse ekran dışına taşmasın diye.")]
        [SerializeField] float spawnMarginFromTop = 1f;

        [Tooltip("Kutunun beliriş noktası. Yüksekliği kule ve kadraj belirliyor, x/z buradan.")]
        [SerializeField] Vector3 spawnPosition = new(0f, 7f, 0f);

        public event Action<DraggableBody> BoxSpawned;

        public DraggableBody Current { get; private set; }

        public DraggableBody SpawnNext(in BoxDifficulty difficulty)
        {
            float towerTop = tracker != null ? tracker.HighestSettledPointY() : 0f;

            // Boyutu önce seçiyoruz: bırakma çizgisi de spawn yüksekliği de
            // kutunun yarım boyuna bağlı.
            Vector3 scale = PickScale(difficulty.WidthVariance);
            float halfHeight = scale.y * 0.5f;

            float dropLineY = towerTop + difficulty.DropGap;

            // Kutu kendi bırakma çizgisinin üstünde belirmeli, yoksa oyun daha
            // ilk karede kendi kuralını çiğnemiş olur.
            float aboveTower = Mathf.Max(
                spawnAboveTower,
                difficulty.DropGap + halfHeight + spawnAboveDropLine);

            // Kutu yığının biraz üstünde belirmeli: hem kameranın gördüğü yerde
            // kalıyor hem de düşme mesafesi kule yükseldikçe değişmiyor.
            float height = stackCamera != null
                ? stackCamera.SpawnHeight(aboveTower, minSpawnHeight, spawnMarginFromTop)
                : towerTop + aboveTower;

            // Yatay rastgelelik kaldırıldı. Kutu her seferinde aynı yerde beliriyor:
            // zorluk atışın kendisinden gelmeli, kutunun nereye düştüğünü şansın
            // belirlemesinden değil. Rastgelelik varken aynı seviyeyi iki kez
            // oynamak iki farklı problem çözmek demekti.
            Vector3 position = new(spawnPosition.x, height, spawnPosition.z);

            var instance = Instantiate(boxPrefab, position, Quaternion.identity, transform);
            instance.name = $"Box_{transform.childCount - 1}";

            ApplyScale(instance, scale);

            var body = instance.GetComponent<DraggableBody>();

            // Kadraj kırpması spawn'ı aşağı çekmiş olabilir; çizgi kutunun
            // üstünde kalmasın diye alt sınırı da kırpıyoruz.
            body.SetDropLine(Mathf.Min(dropLineY, height - halfHeight));

            // Oyuncu dokunana kadar havada assın. Dinamik bıraksaydım kutu
            // daha oyuncu bakmadan zemine düşerdi.
            body.HoldInPlace();

            Current = body;
            BoxSpawned?.Invoke(body);
            return body;
        }

        /// <summary>
        /// Kutunun boyutunu seçer. Prefab 1 birimlik küp, yani ölçek doğrudan
        /// boyut demek.
        ///
        /// Yalnızca genişlik oynuyor, boy sabit 1 birim. Önce ikisini birden
        /// oynatmıştım — daha çeşitli görünüyordu ama seviyenin sorusunu bozuyordu:
        /// kutu boyu değişince aynı hedef yüksekliğe bazen beş, bazen altı kutuyla
        /// çıkılıyor. Yani "bu hedefe çıkabilir miyim" sorusunun cevabını kısmen
        /// zar belirliyor.
        ///
        /// Genişlik ise tam tersi: kaç kutu gerektiğini değiştirmiyor, sadece
        /// üst üste koymayı zorlaştırıyor. Zorluk orada olmalı.
        /// </summary>
        static Vector3 PickScale(float variance)
        {
            if (variance <= 0f)
            {
                return Vector3.one;
            }

            return new Vector3(1f + UnityEngine.Random.Range(-variance, variance), 1f, 1f);
        }

        /// <summary>
        /// Boyutu uygular. Kütle hacimle birlikte ölçekleniyor: sabit kütle
        /// bırakılsaydı küçük kutu taş gibi ağır, büyük kutu köpük gibi hafif
        /// davranırdı ve fizik yalan söylerdi.
        ///
        /// Sürükleme hissi bundan etkilenmiyor, çünkü hızı doğrudan atıyoruz;
        /// kütle yalnızca çarpışmalarda konuşuyor — yani tam da istediğimiz yerde.
        /// </summary>
        static void ApplyScale(GameObject instance, Vector3 scale)
        {
            if (scale == Vector3.one)
            {
                return;
            }

            instance.transform.localScale = scale;
            instance.GetComponent<Rigidbody>().mass *= scale.x * scale.y * scale.z;
        }
    }
}
