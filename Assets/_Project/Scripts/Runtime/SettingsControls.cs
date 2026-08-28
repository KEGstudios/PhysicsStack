using TMPro;
using UnityEngine;

namespace PhysicsStack
{
    /// <summary>
    /// Ayar denetimleri: ses çubuğu, kalite kademeleri, sarsıntı anahtarı.
    ///
    /// Kendi ekranı yok; verilen bir dikdörtgenin içine kuruluyor ve dokunuşu
    /// dışarıdan alıyor. Sebebi iki yerde kullanılması: menüdeki ayarlar ekranı
    /// ve oyunun içindeki duraklatma paneli. İkisine ayrı ayrı yazsaydım aynı
    /// üç denetim iki kopya olurdu ve dördüncü bir ayar eklendiğinde birini
    /// güncellemeyi unutmak an meselesiydi.
    ///
    /// MonoBehaviour değil, düz C#: sahnede bir bileşen olmasına gerek yok,
    /// kuran taraf zaten bir MonoBehaviour.
    /// </summary>
    public sealed class SettingsControls
    {
        UISlider volume;
        TMP_Text volumeLabel;
        UIButton[] quality;
        UIButton shake;
        bool dragging;

        /// <summary>
        /// Denetimleri verilen kutunun içine kurar. Konumlar kutunun kendi
        /// normalize koordinatlarında, yani çağıran taraf yalnızca "nereye"
        /// diyor; iç düzen burada duruyor ve iki kullanıcıda da aynı.
        /// </summary>
        public void Build(RectTransform parent)
        {
            volumeLabel = Label(parent, VolumeText(), 0.86f, 1f);
            volume = UIKit.Slider(parent, new Rect(0.06f, 0.70f, 0.88f, 0.14f), GameSettings.Volume);

            Label(parent, "Grafik kalitesi", 0.52f, 0.66f);

            quality = new UIButton[3];

            for (int i = 0; i < quality.Length; i++)
            {
                var area = new Rect(0.03f + i * 0.325f, 0.34f, 0.31f, 0.16f);

                quality[i] = UIKit.Button(parent, area, GameSettings.QualityName(i), 36, 0.012f);
                UIKit.Fit(quality[i].Label, 18f, 36f);
            }

            shake = UIKit.Button(parent, new Rect(0.03f, 0.14f, 0.94f, 0.16f), ShakeText(), 36, 0.012f);
            UIKit.Fit(shake.Label, 18f, 36f);

            Refresh();
        }

        static TMP_Text Label(RectTransform parent, string text, float bottom, float top)
        {
            var label = UIKit.Label(parent, text, 40, TextAlignmentOptions.Center);

            label.rectTransform.anchorMin = new Vector2(0.02f, bottom);
            label.rectTransform.anchorMax = new Vector2(0.98f, top);
            label.rectTransform.offsetMin = Vector2.zero;
            label.rectTransform.offsetMax = Vector2.zero;

            UIKit.Fit(label, 18f, 40f);
            return label;
        }

        static string VolumeText() => $"Ses  ·  %{Mathf.RoundToInt(GameSettings.Volume * 100f)}";

        static string ShakeText() => GameSettings.Shake
            ? "Kamera sarsıntısı: açık"
            : "Kamera sarsıntısı: kapalı";

        /// <summary>
        /// Ekrandaki yazıları ve seçili kademeyi ayarların son hâline göre
        /// günceller. Tek yerden yazılıyor: her değişiklikte ilgili etiketi elle
        /// güncellemek, üçüncü ayarda mutlaka unutulacak bir şey.
        /// </summary>
        public void Refresh()
        {
            volumeLabel.text = VolumeText();
            shake.Label.text = ShakeText();
            volume.SetValue(GameSettings.Volume);

            for (int i = 0; i < quality.Length; i++)
            {
                // Seçili kademe vurgu renginde. Düğmeyi devre dışı bırakmak
                // yanlış olurdu: "seçili" ile "dokunulamaz" farklı şeyler ve
                // solgun bir düğme ikincisini söylüyor.
                quality[i].Background.color = i == GameSettings.Quality
                    ? UIKit.AccentColor
                    : UIKit.ButtonColor;
            }
        }

        /// <summary>
        /// Basışı işler. Denetimlerden biri kullandıysa <c>true</c> dönüyor ki
        /// çağıran taraf aynı basışı ikinci bir işe yormasın.
        /// </summary>
        public bool Press(Vector2 position)
        {
            if (volume.Contains(position))
            {
                dragging = true;
                Drag(position);
                return true;
            }

            for (int i = 0; i < quality.Length; i++)
            {
                if (quality[i].Contains(position))
                {
                    GameSettings.Quality = i;
                    SfxPlayer.Play(Sfx.UiTap);
                    Refresh();
                    return true;
                }
            }

            if (shake.Contains(position))
            {
                GameSettings.Shake = !GameSettings.Shake;
                SfxPlayer.Play(Sfx.UiTap);
                Refresh();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Sürükleme devam ediyor mu? Ses çubuğunda karar basışta veriliyor ama
        /// sürükleme bırakışa kadar sürüyor: parmağını çubuğun üstünde kaydıran
        /// biri sesi ayarlarken duymak istiyor, bıraktığında değil.
        ///
        /// <c>true</c> dönerken çağıran taraf başka hiçbir dokunuşu işlememeli,
        /// yoksa çubuğu sürüklerken parmağın altına giren düğmeler tetiklenir.
        /// </summary>
        public bool Dragging(bool pressed, Vector2 position)
        {
            if (!dragging)
            {
                return false;
            }

            Drag(position);

            if (!pressed)
            {
                dragging = false;
            }

            return true;
        }

        void Drag(Vector2 position)
        {
            float value = volume.ValueAt(position);

            if (Mathf.Approximately(value, volume.Value))
            {
                return;
            }

            volume.SetValue(value);
            GameSettings.Volume = value;
            volumeLabel.text = VolumeText();
        }
    }
}
