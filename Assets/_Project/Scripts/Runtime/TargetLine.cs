using UnityEngine;

namespace PhysicsStack
{
    /// <summary>
    /// Hedef çizgisini yönetir: kuralın hedefine göre yerleştirir, hedefi olmayan
    /// modda gizler, tur bitince rengiyle sonucu söyler.
    ///
    /// Rengin sebebi Gün 5: beş kutuyu üst üste koyup kazandığımda oyunun
    /// bittiğini anlamamıştım — köşedeki debug panelinde tek kelime yazıyordu ama
    /// oynarken oraya bakılmıyor. Bitişin oyuncunun zaten baktığı yerde, kulenin
    /// tepesinde olması gerekiyordu.
    ///
    /// Yerleştirmenin sebebi Gün 7: hedef artık sabit değil, kural setinden
    /// geliyor. Çizginin sahnede sabit bir y'de durması, seviye hedefi
    /// değiştiğinde yalan söylemesi demekti. Sonsuz modda ise gösterilecek bir
    /// hedef yok, çizgi tamamen kapanıyor.
    ///
    /// Bileşen çizginin kendisinde değil Systems'te duruyor: sahnedeki görsel
    /// nesneler bootstrap'ın ürettiği gri kutular, üzerlerinde script taşımıyorlar.
    /// </summary>
    public sealed class TargetLine : MonoBehaviour
    {
        [SerializeField] StackGameController controller;
        [SerializeField] Renderer targetLine;

        [SerializeField] Color idleColor = new(0.30f, 0.30f, 0.32f);
        [Tooltip("Hedef geçildi ama tutunma bekleniyor: henüz kazanılmadı.")]
        [SerializeField] Color holdingColor = new(0.85f, 0.68f, 0.20f);

        [SerializeField] Color wonColor = new(0.25f, 0.75f, 0.35f);
        [SerializeField] Color lostColor = new(0.80f, 0.25f, 0.25f);

        /// <summary>
        /// Renk MaterialPropertyBlock ile veriliyor, <c>renderer.material</c> ile değil.
        /// İkincisi materyalin çalışma zamanı kopyasını çıkarır: hem sahnedeki
        /// varlığa dokunmuş oluruz hem de kopya ayrı bir draw call'a düşer.
        /// PropertyBlock materyali hiç kopyalamadan tek nesnenin rengini değiştiriyor.
        /// </summary>
        MaterialPropertyBlock block;

        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        GameState lastSeen = (GameState)(-1);

        [Tooltip("Durum degisiminde cizginin kalinlasma orani.")]
        [SerializeField] float pulseAmount = 2.2f;

        [Tooltip("Kalinlasmanin sonme suresi (sn).")]
        [SerializeField] float pulseDuration = 0.35f;

        /// <summary>
        /// Durum degistiginde cizgi kisa sureli kalinlasiyor.
        ///
        /// Renk degisimi tek basina yetmiyordu: hedefi gecip tutunma basladiginda
        /// cizgi sariya donuyor ama oyuncunun gozu o an kulede oluyor ve degisimi
        /// kaciriyor. Hareket, rengin aksine cevresel gorusle de fark ediliyor.
        /// </summary>
        float pulse;

        Vector3 baseScale;

        void Awake()
        {
            block = new MaterialPropertyBlock();
            baseScale = targetLine.transform.localScale;
        }

        /// <summary>
        /// Yerleştirme Start'ta: kural nesnesi controller'ın Awake'inde kuruluyor,
        /// Unity de bütün Awake'leri bütün Start'lardan önce çalıştırıyor. Sıralamayı
        /// script execution order ayarıyla değil bu doğal garantiyle çözmek,
        /// projeye görünmez bir ayar borcu bırakmıyor.
        /// </summary>
        void Start()
        {
            float target = controller != null ? controller.TargetHeight : 0f;
            bool hasTarget = target > 0f;

            targetLine.enabled = hasTarget;

            if (hasTarget)
            {
                var position = targetLine.transform.position;
                targetLine.transform.position = new Vector3(position.x, target, position.z);
            }
        }

        void Update()
        {
            // Her karede renk yazmanın anlamı yok; sadece durum değişince.
            if (controller.State == lastSeen)
            {
                return;
            }

            lastSeen = controller.State;

            // Vurgu her durum degisiminde degil, yalnizca sonucu ilgilendiren
            // gecislerde. Ilk yazisinda her kutu uretiminde, her iniste ve her
            // tutusta parliyordu - surekli parlayan bir gosterge hicbir sey
            // anlatmiyor.
            if (lastSeen is GameState.Holding or GameState.Won or GameState.Lost)
            {
                pulse = 1f;
            }

            // Hedefsiz modda çizgi tur boyunca kapalı duruyor: gösterecek bir
            // hedef yok. Tur bitince kulenin ulaştığı yüksekliğe taşınıp açılıyor,
            // yani çizgi "geçmen gereken yer"den "geldiğin yer"e dönüşüyor.
            // Sonsuz modda bitişi ekranda gösteren tek şey bu.
            if (!targetLine.enabled && lastSeen is GameState.Won or GameState.Lost)
            {
                var position = targetLine.transform.position;
                targetLine.transform.position = new Vector3(position.x, controller.FinalHeight, position.z);
                targetLine.enabled = true;
            }

            targetLine.GetPropertyBlock(block);
            block.SetColor(BaseColor, ColorFor(lastSeen));
            targetLine.SetPropertyBlock(block);
        }

        void LateUpdate()
        {
            if (pulse <= 0f)
            {
                return;
            }

            pulse = Mathf.MoveTowards(pulse, 0f, Time.deltaTime / pulseDuration);

            // Yalnizca kalinlik buyuyor; uzunluk sabit kaliyor cunku cizginin
            // uzunlugu oyun alanini anlatiyor, degismemeli.
            float scale = 1f + pulse * pulseAmount;

            targetLine.transform.localScale = new Vector3(
                baseScale.x,
                baseScale.y * scale,
                baseScale.z * scale);
        }

        Color ColorFor(GameState state) => state switch
        {
            // Sarı, "geçtin ama henüz kazanmadın" demenin yazısız yolu. Oyuncunun
            // zaten baktığı yer burası; tutunma şartını başka nasıl anlatacağımı
            // bulamadım, menü kurmadan.
            GameState.Holding => holdingColor,
            GameState.Won => wonColor,
            GameState.Lost => lostColor,
            _ => idleColor,
        };
    }
}
