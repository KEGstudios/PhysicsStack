using TMPro;
using UnityEngine;

namespace PhysicsStack
{
    /// <summary>
    /// Oyun icindeki bilgi yazisi: hangi seviye, kule nerede, ne kadar tutunmasi
    /// kaldi.
    ///
    /// Bu bilgiler once <see cref="DebugOverlay"/> uzerinden ekrana basiliyordu
    /// ama orasi bir olcu aleti: <c>OnGUI</c> kullaniyor, dokunmatik olceklemesi
    /// yok ve yazisi her cozunurlukte bulanik. Oyuncunun gormesi gereken uc sayi
    /// ile gelistiricinin gormesi gereken on sayi ayni yerde durmamali - biri
    /// oyunun parcasi, digeri gecici bir arac.
    ///
    /// Kanvasini calisma zamaninda kendisi kuruyor; gerekcesi menu ekranlariyla
    /// ayni.
    /// </summary>
    public sealed class HudUI : MonoBehaviour
    {
        /// <summary>
        /// Panelin kadrajın üstünde kapladığı oran. Kamera bunu okuyor: kutunun
        /// belirdiği yer bu bandın altında kalmalı, yoksa kule yükselip kutu
        /// yukarı çıktıkça beliriş noktası yazının arkasına giriyor.
        ///
        /// Sayı burada duruyor çünkü sahibi bu ekran. Kamerada ayrı bir sayı
        /// olsaydı paneli aşağı kaydırdığım gün ikisi sessizce ayrışırdı.
        /// Aşağıdaki yerleşim 0.83'te başlıyor; üstüne biraz pay bırakıldı.
        /// </summary>
        public const float TopBandFraction = 0.20f;

        [SerializeField] StackGameController controller;
        [SerializeField] Palette palette;

        TMP_Text title;
        TMP_Text readout;

        void Start()
        {
            // Menude tur yok, dolayisiyla gosterilecek bir sey de yok.
            if (controller.Rules == null)
            {
                enabled = false;
                return;
            }

            UIKit.Use(palette);

            var canvas = UIKit.CreateCanvas("HudCanvas", sortOrder: 5);

            title = UIKit.Label(canvas.transform, controller.Rules.Title, 58f, TextAlignmentOptions.Top);
            Place(title.rectTransform, 0.88f, 0.97f);

            readout = UIKit.Label(canvas.transform, string.Empty, 42f, TextAlignmentOptions.Top);
            readout.color = UIKit.DimTextColor;
            Place(readout.rectTransform, 0.83f, 0.89f);
        }

        /// <summary>
        /// Panel sağ kenara kadar gitmiyor: sağ üst köşede duraklatma dişlisi
        /// var. Yazıyı ortalayıp dişliyi üstüne bindirmek, iki şeyi aynı yere
        /// koyup birinin diğerini örtmesini beklemek olurdu.
        /// </summary>
        static void Place(RectTransform rect, float bottom, float top)
        {
            rect.anchorMin = new Vector2(0.06f, bottom);
            rect.anchorMax = new Vector2(0.82f, top);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        void Update()
        {
            readout.text = Describe();
        }

        /// <summary>
        /// Tek satir, duruma gore degisiyor. Tutunma sirasinda geri sayim
        /// gosteriliyor: oyuncunun o an bekledigi sey "durdu mu" degil,
        /// "ne kadar daha".
        /// </summary>
        string Describe()
        {
            var rules = controller.Rules;
            var tracker = controller.Tracker;

            float height = tracker != null ? tracker.HighestSettledPointY() : 0f;

            if (controller.State == GameState.Holding && controller.HoldTime > 0f)
            {
                return $"tutun!  {controller.SteadyTimer:0.0} / {controller.HoldTime:0.0}";
            }

            if (rules.TargetHeight > 0f)
            {
                return $"{height:0.0} / {rules.TargetHeight:0.0}";
            }

            return $"{rules.DescribeScore(controller.Score)}   ·   en iyi {Progress.EndlessBest:0.00}";
        }
    }
}
