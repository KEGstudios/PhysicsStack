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

        // Pop-up ayri bir kanvas degil, menu kanvasinin en son eklenen cocugu.
        // uGUI hiyerarsi sirasina gore ciziyor, yani en son eklenen en ustte
        // kaliyor - ayri kanvas kurup siralama numarasi yonetmeye gerek yok.
        GameObject popup;
        UIButton playButton;
        UIButton closeButton;
        int selectedLevel = -1;

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
                var button = UIKit.Button(canvas.transform, area, level != null ? level.title : $"Seviye {i + 1}", 44);

                bool unlocked = Progress.IsLevelUnlocked(i);
                button.SetEnabled(unlocked);

                // Ad yukari kayiyor, altina yildiz siralaniyor. Yaziyi ortada
                // birakip yildizi ustune koysaydim ikisi cakisirdi; TMP etiketi
                // dugmenin tamamini kapliyor.
                button.Label.rectTransform.anchorMin = new Vector2(0f, 0.38f);
                button.Label.rectTransform.anchorMax = new Vector2(1f, 1f);

                // Kilitli seviyede yildiz gostermiyorum: kazanilmamis uc solgun
                // yildiz, kilitli olan ile sifir yildizla gecilen seviyeyi ayni
                // gosterirdi.
                if (unlocked)
                {
                    int stars = level != null ? level.StarsFor(Progress.LevelBest(i)) : 0;
                    UIKit.StarRow(button.Rect, new Rect(0.30f, 0.10f, 0.40f, 0.28f), stars);
                }

                levelButtons.Add(button);
            }
        }

        /// <summary>
        /// Seviye kartı: ad, kazanılan yıldızlar, zorluk ve tur başlatma.
        ///
        /// Seviyeye dokunmak eskiden turu doğrudan başlatıyordu. Araya bu ekranı
        /// koymamın sebebi yıldızlar: oyuncunun "bu seviyeden kaç yıldız aldım
        /// ve üç yıldız için ne gerekiyor" sorusunu soracağı bir yer gerekiyordu.
        /// Bu bilgiyi ızgaradaki düğmeye sığdırmayı denedim; sekiz düğmenin
        /// sekizi de dört satır bilgi bağırınca ızgara okunmaz oluyor.
        ///
        /// Kart her açılışta yeniden kuruluyor, gizlenip gösterilmiyor: içeriği
        /// zaten seviyeye göre baştan sona değişiyor ve tek bir kart nesnesini
        /// güncel tutmak, kurmaktan daha çok kod olurdu.
        /// </summary>
        void OpenPopup(int index)
        {
            selectedLevel = index;

            var level = levels != null ? levels.Get(index) : null;

            // Karartma perdesi kartın da altında duruyor ve ekranın tamamını
            // kaplıyor: arkadaki ızgara görünmeye devam ediyor ama geri planda
            // olduğu belli oluyor.
            popup = UIKit.Panel(
                canvas.transform,
                Vector2.zero,
                Vector2.one,
                new Color(0f, 0f, 0f, 0.45f)).gameObject;

            popup.name = "LevelPopup";

            // Kart yükseklikte daha cömert. İlk ölçüler dikey telefonda
            // sığıyordu ama yatay ekranda kanvas 1920x1080 referans birime
            // oturuyor ve kart kısalıyor: aynı dört satır aynı puntoda artık
            // sığmıyordu. Ekrandan bağımsız düzen, oranı değişen bir kutuya
            // sabit sayıda satır sığdırmak demek değilmiş.
            var card = UIKit.Panel(
                popup.transform,
                new Vector2(0.08f, 0.20f),
                new Vector2(0.92f, 0.82f),
                UIKit.PanelColor);

            var title = UIKit.Label(card, level != null ? level.title : $"Seviye {index + 1}", 72, TextAlignmentOptions.Top);
            title.rectTransform.anchorMin = new Vector2(0.04f, 0.78f);
            title.rectTransform.anchorMax = new Vector2(0.96f, 0.97f);
            title.rectTransform.offsetMin = Vector2.zero;
            title.rectTransform.offsetMax = Vector2.zero;
            Fit(title, 34f, 72f);

            int best = Progress.LevelBest(index);
            int stars = level != null ? level.StarsFor(best) : 0;

            UIKit.StarRow(card, new Rect(0.32f, 0.55f, 0.36f, 0.21f), stars);

            var info = UIKit.Label(card, Describe(level, best), 34, TextAlignmentOptions.Top);
            info.color = UIKit.DimTextColor;
            info.rectTransform.anchorMin = new Vector2(0.05f, 0.24f);
            info.rectTransform.anchorMax = new Vector2(0.95f, 0.53f);
            info.rectTransform.offsetMin = Vector2.zero;
            info.rectTransform.offsetMax = Vector2.zero;
            Fit(info, 16f, 34f);

            playButton = UIKit.Button(card, new Rect(0.05f, 0.03f, 0.55f, 0.18f), "Oyna", 46, 0.02f);
            closeButton = UIKit.Button(card, new Rect(0.62f, 0.03f, 0.33f, 0.18f), "Kapat", 42, 0.02f);

            Fit(playButton.Label, 26f, 46f);
            Fit(closeButton.Label, 24f, 42f);
        }

        /// <summary>
        /// Kartın bilgi metni. Üç yıldızın kaç kutu istediği burada açıkça
        /// yazıyor: yıldız sistemi ancak eşiği bilinirse bir hedef olur, yoksa
        /// tur sonunda öğrenilen bir sürprizdir.
        /// </summary>
        static string Describe(LevelDefinition level, int best)
        {
            if (level == null)
            {
                return string.Empty;
            }

            string hazard = level.HazardLabel;
            string threat = string.IsNullOrEmpty(hazard) ? "" : $"  ·  {hazard}";

            // Oynanmamış seviyede hiçbir şey yazmıyor. "Henüz geçmedin" bilgi
            // taşımıyordu: yıldızlar zaten boş, oyuncu bunu görüyor. Boş
            // olduğunu ayrıca yazmak, ekranı bilgi değil metinle dolduruyor.
            string record = best > 0 ? $"\nen düşük: {best} kutu" : string.Empty;

            return
                $"zorluk {level.Difficulty}/5{threat}\n" +
                $"hedef yükseklik {level.targetHeight:0.0}\n" +
                $"3 yıldız: {level.StarBoxes} kutu  ·  sınır: {level.BoxLimit}" +
                record;
        }

        /// <summary>
        /// Yazıyı kutusuna sığdırır: sarma açık, punto verilen aralıkta
        /// küçülebiliyor.
        ///
        /// Sabit punto vermek yerine aralık vermenin sebebi ekran oranı. Kanvas
        /// hem genişliğe hem yüksekliğe eşlendiği için aynı kutu dikey telefonda
        /// uzun, yatay ekranda basık oluyor; sabit punto ikisinden birinde
        /// mutlaka taşıyor. Alt sınır okunabilirliğin sınırı — daha küçüğüne
        /// izin vermektense yazının kırpılması daha dürüst olurdu, ama bu
        /// aralıkta o noktaya gelinmiyor.
        ///
        /// Bunu baştan her etikete koymadım çünkü otomatik boyutlandırma her
        /// karede ölçüm yapıyor; yalnızca içeriği değişken olan yerlerde var.
        /// </summary>
        static void Fit(TMP_Text label, float min, float max)
        {
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Truncate;
            label.enableAutoSizing = true;
            label.fontSizeMin = min;
            label.fontSizeMax = max;
        }

        void ClosePopup()
        {
            if (popup != null)
            {
                Destroy(popup);
            }

            popup = null;
            playButton = null;
            closeButton = null;
            selectedLevel = -1;
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

            // Pop-up acikken arkadaki dugmeler tamamen yok sayiliyor. Yalnizca
            // gorsel olarak ustunu ortmek yetmez: parmak pop-up'in yanindaki
            // bosluga dokundugunda arkadaki seviye baslardi.
            if (popup != null)
            {
                if (playButton.Contains(position))
                {
                    Launch(StackMode.Level, selectedLevel);
                }
                else if (closeButton.Contains(position))
                {
                    SfxPlayer.Play(Sfx.UiTap);
                    ClosePopup();
                }

                return;
            }

            for (int i = 0; i < levelButtons.Count; i++)
            {
                if (levelButtons[i].Contains(position))
                {
                    SfxPlayer.Play(Sfx.UiTap);
                    OpenPopup(i);
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
