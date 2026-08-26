using System;
using UnityEngine;

namespace PhysicsStack
{
    /// <summary>
    /// Sıradaki kutuyu ekranın üstünde hazır bekletir. Tek kutu üretiyor:
    /// oyuncu onu alıp yerleştirene kadar yenisi gelmiyor.
    ///
    /// Havuz (pooling) yok, çünkü bir turda üretilen kutu sayısı iki haneli;
    /// erken optimizasyon burada okunabilirlikten çalar. Sayı büyürse yeri belli.
    /// </summary>
    public sealed class BoxQueue : MonoBehaviour
    {
        [SerializeField] GameObject boxPrefab;

        [Tooltip("Kamera bağlıysa kutu kadrajın üstünde belirir; boşsa aşağıdaki sabit noktada.")]
        [SerializeField] StackCamera stackCamera;

        [Tooltip("Kutunun yığının tepesinden ne kadar yukarıda belireceği.")]
        [SerializeField] float spawnAboveTower = 4f;

        [Tooltip("Yığın boşken bile kutunun ineceği en alçak yükseklik. Hedef çizgisinin üstünde kalması için.")]
        [SerializeField] float minSpawnHeight = 5.2f;

        [Tooltip("Kadrajın üst kenarına bu kadar yaklaşabilir; kule hızlı büyürse ekran dışına taşmasın diye.")]
        [SerializeField] float spawnMarginFromTop = 1f;

        [Tooltip("Kamera yoksa kullanılan sabit nokta.")]
        [SerializeField] Vector3 spawnPosition = new(0f, 7f, 0f);

        [Tooltip("Her kutu biraz farklı dursun diye küçük bir yatay kaydırma.")]
        [SerializeField] float horizontalJitter = 1.5f;

        public event Action<DraggableBody> BoxSpawned;

        public DraggableBody Current { get; private set; }

        public DraggableBody SpawnNext()
        {
            // Kutu yığının biraz üstünde belirmeli: hem kameranın gördüğü yerde
            // kalıyor hem de düşme mesafesi kule yükseldikçe değişmiyor.
            float height = stackCamera != null
                ? stackCamera.SpawnHeight(spawnAboveTower, minSpawnHeight, spawnMarginFromTop)
                : spawnPosition.y;

            Vector3 position = new(
                spawnPosition.x + UnityEngine.Random.Range(-horizontalJitter, horizontalJitter),
                height,
                spawnPosition.z);

            var instance = Instantiate(boxPrefab, position, Quaternion.identity, transform);
            instance.name = $"Box_{transform.childCount - 1}";

            var body = instance.GetComponent<DraggableBody>();

            // Oyuncu dokunana kadar havada assın. Dinamik bıraksaydım kutu
            // daha oyuncu bakmadan zemine düşerdi.
            body.HoldInPlace();

            Current = body;
            BoxSpawned?.Invoke(body);
            return body;
        }
    }
}
