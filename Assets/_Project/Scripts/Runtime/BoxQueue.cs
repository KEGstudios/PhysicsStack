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

        [Tooltip("Kutunun belireceği nokta. Kamera kadrajının üst kısmı.")]
        [SerializeField] Vector3 spawnPosition = new(0f, 7f, 0f);

        [Tooltip("Her kutu biraz farklı dursun diye küçük bir yatay kaydırma.")]
        [SerializeField] float horizontalJitter = 1.5f;

        public event Action<DraggableBody> BoxSpawned;

        public DraggableBody Current { get; private set; }

        public DraggableBody SpawnNext()
        {
            Vector3 position = spawnPosition + new Vector3(
                UnityEngine.Random.Range(-horizontalJitter, horizontalJitter), 0f, 0f);

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
