using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

namespace PhysicsStack
{
    /// <summary>
    /// Açılış ekranı: seviye listesi ve sonsuz mod.
    ///
    /// Sahne, bekleyen bir tur isteği yoksa bu ekranla açılıyor. Seçim yapılınca
    /// istek <see cref="RunRequest"/>'e yazılıp sahne yeniden yükleniyor; fizik
    /// dünyası da böylece sıfırdan kuruluyor.
    /// </summary>
    public sealed class MenuUI : MonoBehaviour
    {
        [SerializeField] LevelLibrary levels;
        [SerializeField] Palette palette;

        [Tooltip("Açıkken bütün seviyeler ve sonsuz mod kilitsiz. Kayda yazılmıyor, her açılışta kapalı başlıyor.")]
        [SerializeField] bool unlockEverything;

        readonly List<UIButton> levelButtons = new();

        UIButton endlessButton;
        UIButton muteButton;
        Canvas canvas;

        void Awake()
        {
            // Geliştirici bayrağı Awake'te uygulanıyor: menü çizilmeden önce
            // kilitlerin son hâli belli olmalı.
            Progress.UnlockEverything = unlockEverything;
            UIKit.Use(palette);

            if (RunRequest.HasRequest)
            {
                // Tur oynanıyor: menü hiç kurulmuyor. Gizlenmiş bir kanvas bile
                // her karede düzen hesabı yapar; kurmamak en ucuzu.
                enabled = false;
                return;
            }

            Build();
        }

        void Build()
        {
            canvas = UIKit.CreateCanvas("MenuCanvas", sortOrder: 10);

            UIKit.Panel(canvas.transform, Vector2.zero, Vector2.one, UIKit.PanelColor);

            var title = UIKit.Label(canvas.transform, "PhysicsStack", 92, TextAlignmentOptions.Top);
            title.rectTransform.anchorMin = new Vector2(0f, 0.86f);
            title.rectTransform.anchorMax = new Vector2(1f, 0.96f);
            title.rectTransform.offsetMin = Vector2.zero;
            title.rectTransform.offsetMax = Vector2.zero;

            BuildLevelGrid();
            BuildEndless();
            BuildMute();
        }

        /// <summary>
        /// Ses açma/kapama. Yalnızca menüde: oyunun içinde bir ayar düğmesi
        /// olması, parmağın sürekli ekranda olduğu bir oyunda yanlışlıkla
        /// basılacak bir hedef eklemek demekti.
        ///
        /// Ayarı <see cref="Progress"/> tutuyor, yani sekmeyi kapatıp açınca
        /// tercih duruyor. Sessiz oynamak isteyen birinin bunu her açılışta
        /// tekrar söylemesi gerekmiyor.
        /// </summary>
        void BuildMute()
        {
            muteButton = UIKit.Button(canvas.transform, new Rect(0.28f, 0.05f, 0.44f, 0.08f), MuteLabel(), 34);
        }

        static string MuteLabel() => Progress.Muted ? "ses: kapalı" : "ses: açık";

        /// <summary>
        /// Seviyeler iki sütunlu bir ızgarada. Konumlar normalize koordinatlarla
        /// hesaplanıyor, yani seviye sayısı değişince düzen kendini ayarlıyor —
        /// elle yerleştirilmiş sekiz düğme olsaydı dokuzuncuyu eklemek düzeni
        /// baştan kurmak demekti.
        /// </summary>
        void BuildLevelGrid()
        {
            const int columns = 2;

            int count = levels != null ? levels.Count : 0;
            int rows = Mathf.Max(1, Mathf.CeilToInt(count / (float)columns));

            const float top = 0.82f;
            const float bottom = 0.34f;
            float rowHeight = (top - bottom) / rows;

            for (int i = 0; i < count; i++)
            {
                int row = i / columns;
                int column = i % columns;

                var area = new Rect(
                    column / (float)columns,
                    top - (row + 1) * rowHeight,
                    1f / columns,
                    rowHeight);

                var level = levels.Get(i);
                var button = UIKit.Button(canvas.transform, area, level != null ? level.title : $"Seviye {i + 1}", 48);

                button.SetEnabled(Progress.IsLevelUnlocked(i));
                levelButtons.Add(button);
            }
        }

        void BuildEndless()
        {
            int unlockIndex = levels != null ? levels.EndlessUnlockIndex : int.MaxValue;
            bool unlocked = Progress.IsEndlessUnlocked(unlockIndex);

            string label = unlocked
                ? $"Sonsuz  ·  en iyi {Progress.EndlessBest:0.00}"
                : $"Sonsuz  ·  {unlockIndex + 1}. seviyeyi bitir";

            endlessButton = UIKit.Button(canvas.transform, new Rect(0.1f, 0.16f, 0.8f, 0.14f), label, 46);
            endlessButton.SetEnabled(unlocked);
        }

        void Update()
        {
            var pointer = Pointer.current;
            if (pointer == null || !pointer.press.wasPressedThisFrame)
            {
                return;
            }

            Vector2 position = pointer.position.ReadValue();

            for (int i = 0; i < levelButtons.Count; i++)
            {
                if (levelButtons[i].Contains(position))
                {
                    Launch(StackMode.Level, i);
                    return;
                }
            }

            if (endlessButton != null && endlessButton.Contains(position))
            {
                Launch(StackMode.Endless, 0);
                return;
            }

            if (muteButton != null && muteButton.Contains(position))
            {
                ToggleMute();
            }
        }

        void ToggleMute()
        {
            Progress.Muted = !Progress.Muted;
            muteButton.Label.text = MuteLabel();

            // Ses tıkı kapatırken değil açarken çalıyor. Kapatma dokunuşunun
            // sesi çıksaydı, "sesi kapattım ama ses geldi" diye okunurdu.
            SfxPlayer.Play(Sfx.UiTap);
        }

        void Launch(StackMode mode, int levelIndex)
        {
            SfxPlayer.Play(Sfx.UiTap);

            RunRequest.Set(mode, levelIndex);
            SceneManager.LoadScene(gameObject.scene.buildIndex);
        }
    }
}
