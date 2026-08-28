using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

namespace PhysicsStack
{
    /// <summary>
    /// Tur sonu ekranı: sonuç, skor ve üç seçenek — tekrar, sonraki seviye, menü.
    ///
    /// Gün 5'te bunun yerine "ekrana dokun, sahne yeniden yüklensin" vardı. O
    /// çözüm tek seviyeli bir prototip için doğruydu; seviyeler ve iki mod
    /// gelince yetmiyor, çünkü oyuncunun bitişte verebileceği karar birden fazla.
    ///
    /// Ekran, kanvası ancak tur bittiğinde kuruyor: oyun boyunca kurulu bekleyen
    /// gizli bir kanvas her karede düzen hesabına giriyor ve hiçbir işe yaramıyor.
    /// </summary>
    public sealed class ResultUI : MonoBehaviour
    {
        [SerializeField] StackGameController controller;
        [SerializeField] LevelLibrary levels;
        [SerializeField] Palette palette;

        Canvas canvas;
        UIButton retryButton;
        UIButton nextButton;
        UIButton menuButton;

        GameState shown = GameState.Menu;

        void Update()
        {
            var state = controller.State;

            if (state is GameState.Won or GameState.Lost && shown != state)
            {
                shown = state;
                Build(state);
            }

            if (canvas == null)
            {
                return;
            }

            var pointer = Pointer.current;
            if (pointer == null || !pointer.press.wasPressedThisFrame)
            {
                return;
            }

            Vector2 position = pointer.position.ReadValue();

            if (retryButton.Contains(position))
            {
                Launch(controller.Mode, controller.LevelIndex);
            }
            else if (nextButton != null && nextButton.Contains(position))
            {
                Launch(StackMode.Level, controller.LevelIndex + 1);
            }
            else if (menuButton.Contains(position))
            {
                SfxPlayer.Play(Sfx.UiTap);
                RunRequest.Clear();
                SceneManager.LoadScene(gameObject.scene.buildIndex);
            }
        }

        void Build(GameState state)
        {
            // Menü hiç açılmadan doğrudan tura girilmiş olabilir; paleti burada da
            // bildiriyoruz, yoksa tur sonu ekranı varsayılan renklerle çizilir.
            UIKit.Use(palette);

            canvas = UIKit.CreateCanvas("ResultCanvas", sortOrder: 20);

            var level = state == GameState.Won && controller.Mode == StackMode.Level && levels != null
                ? levels.Get(controller.LevelIndex)
                : null;

            bool hasNext = state == GameState.Won &&
                           controller.Mode == StackMode.Level &&
                           levels != null &&
                           controller.LevelIndex + 1 < levels.Count;

            // Panel yatay ekranda daha uzun, daha dar. Kanvas hem genişliğe hem
            // yüksekliğe eşlendiği için yatayda kanvasın yüksekliği 1080 birime
            // iniyor: sabit %40'lık bir panel orada 432 birim ediyor ve başlık,
            // yıldızlar, skor ile üç düğme o bandın içine sığmıyor. Dikeyde aynı
            // oran 768 birim, yani sorun ekranın değil oranın sabit olmasıydı.
            //
            // Panel kadrajın tamamını da kaplamıyor: altındaki kule görünsün
            // istiyorum, çünkü oyuncunun ilk sorusu "ne oldu" değil "nasıl
            // devrildi".
            bool wide = Screen.width > Screen.height;
            float halfHeight = wide ? 0.33f : 0.21f;
            float sideMargin = wide ? 0.16f : 0.06f;

            var panel = UIKit.Panel(
                canvas.transform,
                new Vector2(sideMargin, 0.5f - halfHeight),
                new Vector2(1f - sideMargin, 0.5f + halfHeight),
                UIKit.PanelColor);

            // Bantlar tek yerde ve üst üste binmiyor. Önceki hâlde skor bandı
            // 0.34-0.48, "Sonraki seviye" düğmesi 0.24-0.42 idi: yazı düğmenin
            // üstüne taşıyordu. Puntoyu esnetmek bunu düzeltmiyor, çünkü sorun
            // yazının kutusuna sığmaması değil, kutuların birbirine girmesiydi.
            float titleTop = 0.97f;
            float titleBottom;
            float scoreTop, scoreBottom;
            Rect stars;

            if (level != null)
            {
                titleBottom = 0.79f;
                stars = hasNext
                    ? new Rect(0.36f, 0.59f, 0.28f, 0.17f)
                    : new Rect(0.36f, 0.55f, 0.28f, 0.20f);
                scoreTop = hasNext ? 0.57f : 0.53f;
                scoreBottom = hasNext ? 0.46f : 0.40f;
            }
            else
            {
                titleBottom = 0.72f;
                stars = Rect.zero;
                scoreTop = 0.68f;
                scoreBottom = 0.48f;
            }

            string headline = state == GameState.Won ? "KAZANDIN" : "KAYBETTIN";

            var title = UIKit.Label(panel, headline, 84, TextAlignmentOptions.Center);
            Place(title.rectTransform, 0.06f, titleBottom, 0.94f, titleTop);

            // Punto sabit değil aralık: aynı bant dar telefonda uzun, geniş
            // ekranda basık oluyor ve sabit punto ikisinden birinde mutlaka
            // taşıyor. Yardımcı UIKit'te, çünkü aynı taşma menüdeki seviye
            // kartında da çıkmıştı.
            UIKit.Fit(title, 34f, 84f);

            if (level != null)
            {
                UIKit.StarRow(panel, stars, level.StarsFor(Mathf.RoundToInt(controller.Score)));
            }

            var score = UIKit.Label(panel, Describe(state), 44, TextAlignmentOptions.Center);
            score.color = UIKit.DimTextColor;
            Place(score.rectTransform, 0.06f, scoreBottom, 0.94f, scoreTop);
            UIKit.Fit(score, 18f, 44f);

            if (hasNext)
            {
                nextButton = UIKit.Button(panel, new Rect(0.06f, 0.25f, 0.88f, 0.18f), "Sonraki seviye", 46, 0.015f);
                retryButton = UIKit.Button(panel, new Rect(0.06f, 0.05f, 0.42f, 0.17f), "Tekrar", 42, 0.015f);
                menuButton = UIKit.Button(panel, new Rect(0.52f, 0.05f, 0.42f, 0.17f), "Menü", 42, 0.015f);
            }
            else
            {
                // UIButton bir MonoBehaviour degil, yani Unity'nin "yok edilmis"
                // null'i burada islemiyor: alan onceki kurulumdan kalmis bir
                // nesneyi tutuyor olabilir ve Update onu hala tiklanabilir sanar.
                nextButton = null;

                retryButton = UIKit.Button(panel, new Rect(0.06f, 0.10f, 0.42f, 0.22f), "Tekrar", 46, 0.015f);
                menuButton = UIKit.Button(panel, new Rect(0.52f, 0.10f, 0.42f, 0.22f), "Menü", 46, 0.015f);
            }

            // Düğme yazıları da esnek: yazı düğmenin dikdörtgenini birebir
            // dolduruyor, yani "Sonraki seviye" dar ekranda kenarlara değiyordu.
            if (nextButton != null)
            {
                UIKit.Fit(nextButton.Label, 22f, 46f);
                Inset(nextButton.Label.rectTransform, 0.06f, 0.14f);
            }

            UIKit.Fit(retryButton.Label, 22f, 46f);
            UIKit.Fit(menuButton.Label, 22f, 46f);
            Inset(retryButton.Label.rectTransform, 0.08f, 0.14f);
            Inset(menuButton.Label.rectTransform, 0.08f, 0.14f);
        }

        /// <summary>
        /// Etiketi panelin normalize koordinatlarına oturtur. Dört satırlık aynı
        /// kalıbı her etiket için tekrar yazmak, bir bandı kaydırırken diğerini
        /// unutmayı kolaylaştırıyordu.
        /// </summary>
        static void Place(RectTransform rect, float xMin, float yMin, float xMax, float yMax)
        {
            rect.anchorMin = new Vector2(xMin, yMin);
            rect.anchorMax = new Vector2(xMax, yMax);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// Düğme yazısına iç boşluk verir. UIKit yazıyı düğmenin dikdörtgenine
        /// birebir oturtuyor; otomatik boyutlandırma da "sığıyor" derken tam
        /// kenara değmeyi sığmak sayıyor. Boşluk, punto aralığının alt sınırına
        /// inmeden önce yazının nefes almasını sağlıyor.
        /// </summary>
        static void Inset(RectTransform rect, float horizontal, float vertical)
        {
            rect.anchorMin = new Vector2(horizontal, vertical);
            rect.anchorMax = new Vector2(1f - horizontal, 1f - vertical);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        string Describe(GameState state)
        {
            var rules = controller.Rules;
            if (rules == null)
            {
                return string.Empty;
            }

            string score = rules.DescribeScore(controller.Score);

            if (controller.Mode == StackMode.Endless)
            {
                return $"kule {controller.FinalHeight:0.00}  ·  en iyi {Progress.EndlessBest:0.00}";
            }

            if (state != GameState.Won)
            {
                return $"kule {controller.FinalHeight:0.00} / hedef {rules.TargetHeight:0.00}";
            }

            // Derece bu noktada zaten kaydedilmiş durumda, yani "en iyi" bu turu
            // da içeriyor. Rekor kırıldığında ikisini yan yana yazmak "5 kutu ·
            // en iyi 5 kutu" gibi kendini tekrar eden bir satır üretiyordu;
            // onun yerine rekoru ayrıca duyuruyorum.
            int best = Progress.LevelBest(controller.LevelIndex);
            bool record = best > 0 && Mathf.RoundToInt(controller.Score) <= best;

            // "En iyi" yerine "en düşük": seviyede iyi olan az kutu kullanmak ve
            // sayının hangi yönde iyi olduğunu etiketin kendisi söylemeli.
            // Sonsuz modda tersi geçerli, orada "en iyi" doğru kalıyor.
            if (record)
            {
                return $"{score} ile geçtin  ·  rekor";
            }

            return $"{score} ile geçtin  ·  en düşük {best} kutu";
        }

        void Launch(StackMode mode, int levelIndex)
        {
            SfxPlayer.Play(Sfx.UiTap);

            RunRequest.Set(mode, levelIndex);
            SceneManager.LoadScene(gameObject.scene.buildIndex);
        }
    }
}
