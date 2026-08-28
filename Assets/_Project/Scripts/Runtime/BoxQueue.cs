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

        [Tooltip("Kutulara sırayla dağıtılan renkler.")]
        [SerializeField] Palette palette;

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

        [Tooltip("Oyun alanının yarı genişliği. Kutu bu bandın dışına sürüklenemiyor.")]
        [SerializeField] float playHalfWidth = 1.7f;

        [Tooltip("Kutunun beliriş noktası. Yüksekliği kule ve kadraj belirliyor, x/z buradan.")]
        [SerializeField] Vector3 spawnPosition = new(0f, 7f, 0f);

        [Tooltip("Kutunun beliriş noktasının yanal oynama payı (birim). 0 = her kutu aynı yerde belirir.")]
        [SerializeField] float spawnSpread = 1.2f;

        [Tooltip("Arka arkaya iki kutunun beliriş noktası arasındaki en küçük fark (birim).")]
        [SerializeField] float spawnMinStep = 0.6f;

        public event Action<DraggableBody> BoxSpawned;

        public DraggableBody Current { get; private set; }

        /// <summary>
        /// Renk <see cref="MaterialPropertyBlock"/> ile veriliyor, kutuya kendi
        /// malzemesi verilerek değil. İkincisi her kutu için malzemenin çalışma
        /// zamanı kopyasını çıkarır: on kutuluk bir kulede on ayrı malzeme, on
        /// ayrı draw call. PropertyBlock aynı malzemeyi paylaşan nesnelerin
        /// tek tek rengini değiştiriyor ve gruplamayı bozmuyor.
        /// </summary>
        MaterialPropertyBlock colorBlock;

        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        int spawnCount;

        /// <summary>Bir önceki kutunun beliriş x'i; yeni kutu ondan yeterince uzağa düşsün diye.</summary>
        float lastSpawnX;

        /// <summary>Son kutunun belirdiği yükseklik. Top atıcı gezinme koridorunun tavanı bu.</summary>
        public float LastSpawnHeight { get; private set; }

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
                difficulty.DropGap + halfHeight + spawnAboveDropLine + difficulty.SpawnLift);

            // Kamera bu kadar tepe boşluğu bırakmazsa spawn kadrajın dışında kalır
            // ve aşağıdaki kırpma kuralı sessizce gevşetir. Gereken yeri sayıyı
            // hesaplayan taraf söylüyor: kamera tahmin etmek zorunda kalmıyor.
            if (stackCamera != null)
            {
                // Ayrılan boşluk arayüz payını da içeriyor. İçermeseydi kamera
                // kadrajı kutunun tam üstünde bitirir, aşağıdaki kırpma da
                // kutuyu panelin altına indirmek için düşüş mesafesini sessizce
                // kısaltırdı — yani oyunun tek risk kolu, kule yükseldikçe
                // kendiliğinden gevşerdi.
                stackCamera.ReserveHeadroom(aboveTower + Mathf.Max(spawnMarginFromTop, stackCamera.UiMargin));
            }

            // Kutu yığının biraz üstünde belirmeli: hem kameranın gördüğü yerde
            // kalıyor hem de düşme mesafesi kule yükseldikçe değişmiyor.
            float height = stackCamera != null
                ? stackCamera.SpawnHeight(aboveTower, minSpawnHeight, spawnMarginFromTop)
                : towerTop + aboveTower;

            // Yatay rastgelelik bir ara kaldırılmıştı ve gerekçesi şuydu: zorluk
            // atışın kendisinden gelmeli, kutunun nereye düştüğünü şansın
            // belirlemesinden değil. Oynayınca o gerekçenin açığı görüldü —
            // sabit beliriş noktası dejenere bir strateji üretiyor: parmağı
            // hiç kıpırdatmadan aynı yere arka arkaya dokunmak kusursuz bir
            // kule veriyor, yani oyun oynanmadan çözülüyor.
            //
            // Geri getirilen şey eskisi değil. Rastgele olan tek şey kutunun
            // **belirdiği** yer; nereye ineceğine hâlâ tamamen oyuncu karar
            // veriyor, çünkü kutu zaten sürüklenerek indiriliyor. Yani şans
            // sonucu değil, sadece başlangıç noktasını belirliyor: aynı seviye
            // iki kez oynandığında problem aynı, tek fark her kutu için
            // gerçekten bir hamle yapmak zorunda olmak.
            float spawnX = PickSpawnX(scale.x * 0.5f);
            Vector3 position = new(spawnX, height, spawnPosition.z);

            LastSpawnHeight = height;

            var instance = Instantiate(boxPrefab, position, Quaternion.identity, transform);
            instance.name = $"Box_{transform.childCount - 1}";

            ApplyScale(instance, scale);
            ApplyColor(instance, spawnCount++);

            var body = instance.GetComponent<DraggableBody>();

            // Kadraj kırpması spawn'ı aşağı çekmiş olabilir; çizgi kutunun
            // üstünde kalmasın diye alt sınırı da kırpıyoruz.
            body.SetDropLine(Mathf.Min(dropLineY, height - halfHeight));

            // Oyun alanı ekrandan bağımsız: geniş ekranda daha çok dünya görünüyor
            // ama oynanan bant aynı kalıyor.
            body.SetHorizontalBounds(-playHalfWidth, playHalfWidth);

            // Oyuncu dokunana kadar havada assın. Dinamik bıraksaydım kutu
            // daha oyuncu bakmadan zemine düşerdi.
            body.HoldInPlace();

            Current = body;
            BoxSpawned?.Invoke(body);
            return body;
        }

        /// <summary>
        /// Beliriş x'i. İki kural var: oyun alanının içinde kalmak ve bir
        /// öncekinden yeterince uzağa düşmek.
        ///
        /// İkincisi olmadan rastgelelik işini yapmıyor — art arda gelen iki
        /// kutu tesadüfen aynı yere düştüğünde, sabit beliriş noktasının
        /// açığı o iki kutu boyunca geri geliyor. Rastgelelik burada bir çeşni
        /// değil, dejenere stratejiyi kapatan bir kural; kapattığından emin
        /// olmak gerekiyor.
        /// </summary>
        float PickSpawnX(float halfWidth)
        {
            float limit = Mathf.Max(0f, playHalfWidth - halfWidth);
            float spread = Mathf.Min(spawnSpread, limit);

            if (spread <= 0f)
            {
                return spawnPosition.x;
            }

            float x = UnityEngine.Random.Range(-spread, spread);

            if (Mathf.Abs(x - lastSpawnX) < spawnMinStep)
            {
                x = lastSpawnX + (x >= lastSpawnX ? spawnMinStep : -spawnMinStep);
            }

            x = Mathf.Clamp(x, -spread, spread);
            lastSpawnX = x;

            return spawnPosition.x + x;
        }

        void ApplyColor(GameObject instance, int index)
        {
            // Renderer artik kok nesnede degil, gorsel cocuk nesnede.
            var renderer = instance.GetComponentInChildren<Renderer>();

            if (palette == null || renderer == null)
            {
                return;
            }

            colorBlock ??= new MaterialPropertyBlock();

            renderer.GetPropertyBlock(colorBlock);
            colorBlock.SetColor(BaseColor, palette.BoxColor(index));
            renderer.SetPropertyBlock(colorBlock);
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
