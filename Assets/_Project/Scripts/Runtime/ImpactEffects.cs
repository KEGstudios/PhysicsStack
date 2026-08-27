using System.Collections;
using UnityEngine;

namespace PhysicsStack
{
    /// <summary>
    /// Carpma ve cokus geri bildirimleri: toz, kamera sarsintisi, kisa zaman
    /// yavaslamasi.
    ///
    /// Hepsi tamamen gorsel. Fizige tek dokunusu cokus anindaki zaman
    /// yavaslamasi ve o da simulasyonun hizini degistiriyor, sonucunu degil.
    /// Bu ayrimi korumak onemli: "his" katmani oyunu daha iyi hissettirmeli,
    /// baska bir oyun haline getirmemeli.
    ///
    /// Efektler tek yerde toplandi cunku hepsi ayni olaylari dinliyor. Her
    /// bilesenin kendi carpisma dinleyicisi olsaydi ayni carpma uc kez
    /// islenirdi.
    /// </summary>
    public sealed class ImpactEffects : MonoBehaviour
    {
        [SerializeField] StackGameController controller;
        [SerializeField] BoxQueue queue;
        [SerializeField] StackCamera stackCamera;
        [SerializeField] ParticleSystem dust;
        [SerializeField] ParticleSystem speedLines;

        [Tooltip("Kutu bu dikey hizi asinca hiz cizgileri basliyor (m/s).")]
        [SerializeField] float speedLineThreshold = 2.5f;

        [Tooltip("Saniyede uretilen en fazla hiz cizgisi.")]
        [SerializeField] float speedLineRate = 70f;

        float speedLineDebt;

        [Tooltip("Bu hizin altindaki carpmalar toz ve sarsinti uretmez (m/s).")]
        [SerializeField] float minImpactSpeed = 4.5f;

        [Tooltip("Bu hizin altindaki carpmalar hic ses cikarmaz (m/s).")]
        [SerializeField] float quietImpactSpeed = 1.2f;

        [Tooltip("Carpma hizi basina kamera sarsintisi.")]
        [SerializeField] float shakePerSpeed = 0.008f;

        [Tooltip("Kule coktugunde uygulanan sarsinti.")]
        [SerializeField] float collapseShake = 0.22f;

        [Tooltip("Cokuste zamanin yavasladigi sure (gercek sn).")]
        [SerializeField] float hitStopDuration = 0.14f;

        [Tooltip("Cokuste zaman olcegi.")]
        [SerializeField] float hitStopScale = 0.35f;

        GameState lastSeen = GameState.Menu;

        /// <summary>
        /// Hiz cizgisi olcumleri. Efekt iki tur boyunca gorunmedi ve iki turda
        /// da sebebi tahmin ettim; ucuncusunde tahmin etmemek icin sayilari
        /// disari aciyorum.
        ///
        /// Uc sayi uc ayri soruyu ayiriyor: kutu yeterince hizli mi (dususHizi),
        /// parcacik uretiliyor mu (uretilen), ve uretilen parcacik yasiyor mu
        /// (canli). Hangisinin sifir oldugu, hatanin hangi katmanda oldugunu
        /// dogrudan soyluyor - "gorunmuyor" tek basina bunu soylemiyor.
        /// </summary>
        public float LastFallSpeed { get; private set; }

        public int SpeedLinesEmitted { get; private set; }

        public int SpeedLinesAlive => speedLines != null ? speedLines.particleCount : -1;

        /// <summary>
        /// Parcacik sistemi Unity tarafindan gorunur sayiliyor mu.
        ///
        /// "Yasiyor" ile "ciziliyor" ayni sey degil ve aradaki farki bu bayrak
        /// veriyor. Bir renderer'in sinirlari (bounds) kameranin goru
        /// piramidinin disinda kalirsa Unity onu tamamen atliyor - icinde
        /// parcacik olsa bile tek piksel cizilmiyor. Dunya uzayinda simule
        /// edilen ve her karede elle tasinan bir sistemde bu sinirlarin geride
        /// kalmasi mumkun.
        ///
        /// Bu bayrak false ise sorun renk, kontrast ya da malzeme degil:
        /// sistem hic cizilmiyor ve arka plani degistirmenin bir anlami yok.
        /// </summary>
        public bool SpeedLinesVisible =>
            speedLines != null &&
            speedLines.TryGetComponent(out Renderer renderer) &&
            renderer.isVisible;

        void Start()
        {
            if (queue != null)
            {
                queue.BoxSpawned += OnSpawned;
            }
        }

        void OnDestroy()
        {
            if (queue != null)
            {
                queue.BoxSpawned -= OnSpawned;
            }

            // Sahne yeniden yuklenirken zaman yavaslamasi devam ediyor olabilir.
            // Coroutine nesneyle birlikte olur ama Time.timeScale global: geri
            // yazilmazsa oyun kalici olarak agir cekimde acilir.
            Time.timeScale = 1f;
        }

        void OnSpawned(DraggableBody body)
        {
            body.Landed += OnLanded;
            body.Grabbed += OnGrabbed;
            body.Released += OnReleased;

            SfxPlayer.Play(Sfx.Spawn);
        }

        void OnGrabbed(DraggableBody body)
        {
            SfxPlayer.Play(Sfx.Grab);
        }

        void OnReleased(DraggableBody body)
        {
            SfxPlayer.Play(Sfx.Release);
        }

        void OnLanded(DraggableBody body, Vector3 point, float speed)
        {
            PlayLandSound(speed);

            if (speed < minImpactSpeed)
            {
                return;
            }

            if (stackCamera != null)
            {
                stackCamera.Shake(speed * shakePerSpeed);
            }

            Burst(point, speed);
        }

        /// <summary>
        /// Inis sesi. Toz ve sarsintidan daha dusuk bir esikle caliyor: hafifce
        /// oturan bir kutu toz kaldirmiyor ama sessiz de olmamali, yoksa kutunun
        /// yere degdigi an belli olmuyor.
        ///
        /// Hiz iki seyi birden ayarliyor. Ses seviyesi acik: sert carpma
        /// yuksek. Perde ters yonde: sert carpma daha kalin. Ikisi birlikte
        /// tek bir klibi farkli agirliklarda cisimler gibi duyuruyor - ayri
        /// klipler uretmeden.
        ///
        /// Ustune kucuk bir rastgelelik biniyor: ayni klip ayni perdeyle arka
        /// arkaya calinca makineli tufek gibi duyuluyor. Bir kule cokerken
        /// onlarca carpma oldugu icin bu fark ediliyor.
        /// </summary>
        void PlayLandSound(float speed)
        {
            if (speed < quietImpactSpeed)
            {
                return;
            }

            float t = Mathf.InverseLerp(quietImpactSpeed, 10f, speed);

            SfxPlayer.Play(
                Sfx.Land,
                Mathf.Lerp(0.35f, 1f, t),
                Mathf.Lerp(1.25f, 0.85f, t) + Random.Range(-0.05f, 0.05f));
        }

        /// <summary>
        /// Toz tek bir sistemden cikiyor, her carpmada yeni bir nesne
        /// uretilmiyor: bir turda onlarca carpma oluyor ve her biri icin
        /// Instantiate/Destroy yapmak coplugu bosuna mesgul eder.
        /// </summary>
        void Burst(Vector3 point, float speed)
        {
            if (dust == null)
            {
                return;
            }

            dust.transform.position = point;

            int count = Mathf.RoundToInt(Mathf.Lerp(4f, 14f, Mathf.InverseLerp(minImpactSpeed, 10f, speed)));
            dust.Emit(count);
        }

        void Update()
        {
            UpdateSpeedLines();

            if (controller.State == lastSeen)
            {
                return;
            }

            lastSeen = controller.State;

            if (lastSeen == GameState.Won)
            {
                SfxPlayer.Play(Sfx.Win);
            }

            if (lastSeen == GameState.Lost)
            {
                if (stackCamera != null)
                {
                    stackCamera.Shake(collapseShake);
                }

                SfxPlayer.Play(Sfx.Collapse);
                StartCoroutine(HitStop());
                StartCoroutine(LoseSting());
            }
        }

        /// <summary>
        /// Kaybetme melodisi gumburtunun uzerine degil, ardina biniyor.
        /// Ayni anda calindiginda ikisi de duyulmuyordu: gumburtu genis bantli
        /// ve yuksek, melodinin notalarini ortuyor. Yarim saniyelik bekleme
        /// "cokus oldu" ile "tur bitti" arasina bir virgul koyuyor.
        /// </summary>
        IEnumerator LoseSting()
        {
            yield return new WaitForSecondsRealtime(0.45f);
            SfxPlayer.Play(Sfx.Lose);
        }

        /// <summary>
        /// Hiz cizgileri: dusen kutu belirli bir hizi asinca arkasinda ince izler
        /// birakiyor.
        ///
        /// Kare basina sabit sayida degil, saniyede sabit sayida uretiliyor.
        /// Aradaki fark kare hizi degistiginde ortaya cikiyor: kare basina uretim,
        /// 30 fps'te yarisi kadar cizgi demek olurdu ve efektin siddeti donanima
        /// bagli hale gelirdi. Ondalikli borc tutmamin sebebi de bu - saniyede 70
        /// cizgi, karede tam sayi etmiyor.
        /// </summary>
        void UpdateSpeedLines()
        {
            if (speedLines == null || queue == null)
            {
                return;
            }

            var box = queue.Current;

            // Elde tutulan ya da henuz birakilmamis kutu icin cizgi yok: orada
            // hiz parmagin hizi, dusus degil.
            if (box == null || box.CanGrab || box.IsDragged)
            {
                speedLineDebt = 0f;
                LastFallSpeed = 0f;
                return;
            }

            float fallSpeed = -box.Body.linearVelocity.y;

            // Olcum esigin ONUNDE yaziliyor: esik yuzunden hic girilmeyen bir
            // kod yolunda hiz sifir gorunurdu ve "kutu yavas" ile "kod buraya
            // hic gelmiyor" birbirinden ayirt edilemezdi.
            LastFallSpeed = fallSpeed;

            if (fallSpeed < speedLineThreshold)
            {
                speedLineDebt = 0f;
                return;
            }

            speedLines.transform.position = box.transform.position;
            speedLineDebt += speedLineRate * Time.deltaTime;

            int count = Mathf.FloorToInt(speedLineDebt);
            speedLineDebt -= count;

            if (count > 0)
            {
                speedLines.Emit(count);
                SpeedLinesEmitted += count;
            }
        }

        /// <summary>Merminin kutuya carpmasi: kucuk toz ve hafif sarsinti.</summary>
        public void BallHit(Vector3 point)
        {
            if (dust != null)
            {
                dust.transform.position = point;
                dust.Emit(6);
            }

            if (stackCamera != null)
            {
                stackCamera.Shake(0.05f);
            }

            SfxPlayer.Play(Sfx.BallHit);
        }

        IEnumerator HitStop()
        {
            Time.timeScale = hitStopScale;
            yield return new WaitForSecondsRealtime(hitStopDuration);
            Time.timeScale = 1f;
        }
    }
}
