using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
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
                RunRequest.Clear();
                SceneManager.LoadScene(gameObject.scene.buildIndex);
            }
        }

        void Build(GameState state)
        {
            canvas = UIKit.CreateCanvas("ResultCanvas", sortOrder: 20);

            // Panel kadrajın tamamını kaplamıyor: altındaki kule görünsün istiyorum,
            // çünkü oyuncunun ilk sorusu "ne oldu" değil "nasıl devrildi".
            var panel = UIKit.Panel(
                canvas.transform,
                new Vector2(0.06f, 0.30f),
                new Vector2(0.94f, 0.70f),
                UIKit.PanelColor);

            string headline = state == GameState.Won ? "KAZANDIN" : "KAYBETTIN";

            var title = UIKit.Label(panel, headline, 84, TextAnchor.UpperCenter);
            title.rectTransform.anchorMin = new Vector2(0f, 0.62f);
            title.rectTransform.anchorMax = new Vector2(1f, 0.95f);
            title.rectTransform.offsetMin = Vector2.zero;
            title.rectTransform.offsetMax = Vector2.zero;

            var score = UIKit.Label(panel, Describe(state), 44, TextAnchor.UpperCenter);
            score.color = UIKit.DimTextColor;
            score.rectTransform.anchorMin = new Vector2(0f, 0.44f);
            score.rectTransform.anchorMax = new Vector2(1f, 0.62f);
            score.rectTransform.offsetMin = Vector2.zero;
            score.rectTransform.offsetMax = Vector2.zero;

            bool hasNext = state == GameState.Won &&
                           controller.Mode == StackMode.Level &&
                           levels != null &&
                           controller.LevelIndex + 1 < levels.Count;

            if (hasNext)
            {
                nextButton = UIKit.Button(panel, new Rect(0.05f, 0.24f, 0.9f, 0.18f), "Sonraki seviye", 46, 0.02f);
                retryButton = UIKit.Button(panel, new Rect(0.05f, 0.04f, 0.45f, 0.18f), "Tekrar", 42, 0.02f);
                menuButton = UIKit.Button(panel, new Rect(0.50f, 0.04f, 0.45f, 0.18f), "Menü", 42, 0.02f);
            }
            else
            {
                retryButton = UIKit.Button(panel, new Rect(0.05f, 0.14f, 0.45f, 0.22f), "Tekrar", 46, 0.02f);
                menuButton = UIKit.Button(panel, new Rect(0.50f, 0.14f, 0.45f, 0.22f), "Menü", 46, 0.02f);
            }
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

            return state == GameState.Won
                ? $"{score} ile geçtin"
                : $"kule {controller.FinalHeight:0.00} / hedef {rules.TargetHeight:0.00}";
        }

        void Launch(StackMode mode, int levelIndex)
        {
            RunRequest.Set(mode, levelIndex);
            SceneManager.LoadScene(gameObject.scene.buildIndex);
        }
    }
}
