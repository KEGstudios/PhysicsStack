using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PhysicsStack
{
    /// <summary>
    /// Grafik kalitesi ayarını sahneye uygular: çözünürlük ölçeği,
    /// post-process yığını ve gölgeler.
    ///
    /// Üç kolun üçü de bilerek seçildi ve üçü de aynı şeyi hedefliyor: doldurma
    /// oranı. Telefon ölçümünde 1. seviyede — sahnede iki üç kutu varken — kare
    /// hızı tavanın altına düşüyordu, yani maliyet fizikte değil ekranda.
    ///
    /// **Çözünürlük ölçeği** en güçlü kol. Tarayıcı oyunu cihazın piksel
    /// yoğunluğunda çiziyor ve telefonda bu 3 kata kadar çıkıyor, yani dokuz
    /// katı piksel. Ölçeği düşürmek maliyeti doğrudan kare alanıyla azaltıyor.
    /// **Post-process** ikinci: tonemapping, bloom ve vignette tam ekran
    /// geçişler ve bloom birden fazla kez örnekliyor. **Gölge** üçüncü, çünkü
    /// gölge haritası ayrı bir çizim geçişi.
    ///
    /// Kalitenin sahnede uygulanması gerekiyor çünkü kamera ve ışık sahneye ait.
    /// Menü ekranı ikisini de tanımıyor; yalnızca ayarın değiştiğini söylüyor
    /// ve burası dinliyor.
    /// </summary>
    public sealed class QualityRuntime : MonoBehaviour
    {
        [SerializeField] Camera targetCamera;
        [SerializeField] Light sun;

        [Tooltip("Kademe başına çözünürlük ölçeği: düşük, orta, yüksek.")]
        [SerializeField] float[] renderScales = { 0.65f, 0.85f, 1f };

        void OnEnable()
        {
            GameSettings.Changed += Apply;
            Apply();
        }

        void OnDisable()
        {
            GameSettings.Changed -= Apply;
        }

        void Apply()
        {
            int level = GameSettings.Quality;

            ApplyRenderScale(level);

            if (targetCamera != null)
            {
                // Post-process yalnızca en yüksek kademede tam açık. Orta
                // kademede de açık çünkü oyunun görünüşünün büyük kısmı ondan
                // geliyor; kapatınca sahne "ayarları düşürülmüş" değil "bozuk"
                // görünüyor.
                targetCamera.GetUniversalAdditionalCameraData().renderPostProcessing =
                    level > GameSettings.LowQuality;
            }

            if (sun != null)
            {
                // Gölge kapanınca kutuların birbirine göre yüksekliği okunması
                // zorlaşıyor, o yüzden en son feda edilen şey bu.
                sun.shadows = level == GameSettings.LowQuality
                    ? LightShadows.None
                    : LightShadows.Soft;
            }
        }

        /// <summary>
        /// Çözünürlük ölçeği render pipeline varlığında duruyor.
        ///
        /// Editor'de bu varlığı çalışma zamanında değiştirmek dosyayı kirletir
        /// ve yanlışlıkla kaydedilirse depoya girer. Bunu kabul edilebilir
        /// kılan şey değerin her açılışta ayardan yeniden yazılması: kaydedilmiş
        /// yanlış bir değer bir sonraki oyunda üzerine yazılıyor.
        /// </summary>
        void ApplyRenderScale(int level)
        {
            if (renderScales == null || renderScales.Length == 0)
            {
                return;
            }

            var pipeline = (QualitySettings.renderPipeline ?? GraphicsSettings.defaultRenderPipeline)
                as UniversalRenderPipelineAsset;

            if (pipeline == null)
            {
                return;
            }

            pipeline.renderScale = renderScales[Mathf.Clamp(level, 0, renderScales.Length - 1)];
        }
    }
}
