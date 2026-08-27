using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PhysicsStack
{
    /// <summary>
    /// Gri kutu arayüzü için minik bir kurulum yardımcısı: kanvas, panel, yazı,
    /// düğme. Hepsi çalışma zamanında koddan kuruluyor.
    ///
    /// **Neden sahnede değil de kodda:** arayüzde elle ayarlanacak hiçbir şey yok
    /// — ne sanat, ne düzen, ne yazı tipi. Sahneye kurunca ortaya onlarca
    /// RectTransform'luk, diff'i okunmayan bir YAML yığını çıkıyor ve her küçük
    /// değişiklik için Editor açmak gerekiyor. Kodda duran arayüz okunuyor,
    /// gözden geçirilebiliyor ve sahne kurulumunu şişirmiyor.
    ///
    /// **Neden EventSystem yok:** uGUI'nin düğme altyapısı bir EventSystem, bir
    /// input modülü ve GraphicRaycaster istiyor; proje de yalnızca yeni Input
    /// System kullandığı için modülün doğru kurulması ayrı bir bakım borcu.
    /// Buradaki düğmeler dikdörtgen, ve bir dokunuşun dikdörtgenin içinde olup
    /// olmadığı tek satır: <c>RectTransformUtility.RectangleContainsScreenPoint</c>.
    /// Sürükleme, odak, klavye gezinme gibi şeyler gerekseydi bu karar yanlış
    /// olurdu — burada gereken tek şey "parmağım buna değdi mi".
    /// </summary>
    public static class UIKit
    {
        /// <summary>
        /// Arayüz renkleri de sahnenin geri kalanıyla aynı paletten geliyor.
        /// Ekranlar kendi kanvaslarını çalışma zamanında kurduğu için renkleri
        /// sahneye işlemek mümkün değil; bunun yerine ilk kurulan ekran paleti
        /// buraya bırakıyor.
        /// </summary>
        static Palette palette;

        public static void Use(Palette value)
        {
            palette = value;
        }

        public static Color PanelColor => palette != null ? palette.uiPanel : new Color(0.98f, 0.96f, 0.94f, 0.96f);
        public static Color ButtonColor => palette != null ? palette.uiButton : new Color(0.88f, 0.84f, 0.80f);
        public static Color LockedColor => palette != null ? palette.uiButtonLocked : new Color(0.91f, 0.89f, 0.88f);
        public static Color AccentColor => palette != null ? palette.uiAccent : new Color(0.66f, 0.84f, 0.73f);
        public static Color TextColor => palette != null ? palette.uiText : new Color(0.23f, 0.23f, 0.26f);
        public static Color DimTextColor => palette != null ? palette.uiTextDim : new Color(0.55f, 0.54f, 0.58f);

        /// <summary>
        /// Referans çözünürlük portre telefon. <c>ScaleWithScreenSize</c> ve
        /// yükseklik eşlemesi, aynı düzenin hem dar hem geniş telefonlarda aynı
        /// büyüklükte görünmesini sağlıyor — sabit piksel verilseydi yazı, yüksek
        /// yoğunluklu ekranda okunmaz olurdu.
        /// </summary>
        public static Canvas CreateCanvas(string name, int sortOrder)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler));

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortOrder;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            // Genişlik ve yükseklik yarı yarıya eşleniyor. Yalnızca yüksekliğe
            // eşlemek portrede doğruydu ama geniş ekranda her şeyi küçültüyordu:
            // 810 piksel yüksekliğinde bir tarayıcı penceresinde ölçek 0.42'ye
            // düşüyor ve 48 puntoluk yazı 20 piksele iniyor.
            scaler.matchWidthOrHeight = 0.5f;

            // Dinamik yazı tipi varsayılanda ekran ölçeğiyle değil bu değerle
            // rasterleniyor; 1'de büyütülmüş yazı bulanık çıkıyor.
            scaler.dynamicPixelsPerUnit = 3f;

            return canvas;
        }

        public static RectTransform Panel(Transform parent, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            var go = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            var rect = go.GetComponent<RectTransform>();

            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            go.GetComponent<Image>().color = color;
            return rect;
        }

        /// <summary>
        /// Dikdörtgen bir düğme. Konum normalize koordinatlarla veriliyor
        /// (0-1 aralığı), böylece düzen çözünürlükten bağımsız kalıyor.
        /// </summary>
        public static UIButton Button(Transform parent, Rect area, string text, float fontSize, float inset = 0.01f)
        {
            var background = Panel(
                parent,
                new Vector2(area.xMin + inset, area.yMin + inset),
                new Vector2(area.xMax - inset, area.yMax - inset),
                ButtonColor);

            background.name = $"Button ({text})";

            return new UIButton
            {
                Rect = background,
                Background = background.GetComponent<Image>(),
                Label = Label(background, text, fontSize, TextAlignmentOptions.Center),
            };
        }

        /// <summary>
        /// Yazı TextMeshPro ile çiziliyor, eski <c>Text</c> ile değil.
        ///
        /// Sebep ölçeklenebilirlik: eski bileşen yazıyı piksel haritası olarak
        /// rasterliyor, yani kanvas büyüdüğünde yazı bulanıklaşıyor. Telefon ve
        /// masaüstü arasında üç kat ölçek farkı olan bir oyunda bu doğrudan
        /// görünüyordu. TMP mesafe alanı (SDF) kullanıyor: aynı varlık her
        /// boyutta keskin.
        /// </summary>
        public static TMP_Text Label(Transform parent, string text, float fontSize, TextAlignmentOptions alignment)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            var rect = go.GetComponent<RectTransform>();

            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var label = go.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.color = TextColor;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;

            return label;
        }

    }

    /// <summary>
    /// Dokunulabilir bir dikdörtgen. uGUI'nin <c>Button</c> bileşeni değil:
    /// o bileşen EventSystem ister, biz de dokunuşu zaten kendimiz okuyoruz.
    /// </summary>
    public sealed class UIButton
    {
        public RectTransform Rect;
        public Image Background;
        public TMP_Text Label;

        public bool Enabled { get; private set; } = true;

        public void SetEnabled(bool enabled)
        {
            Enabled = enabled;
            Background.color = enabled ? UIKit.ButtonColor : UIKit.LockedColor;
            Label.color = enabled ? UIKit.TextColor : UIKit.DimTextColor;
        }

        public void SetVisible(bool visible)
        {
            Rect.gameObject.SetActive(visible);
        }

        /// <summary>
        /// Kanvas ekran uzayında olduğu için kamera parametresi null geçiliyor;
        /// dünya uzayı kanvası olsaydı buraya kamera vermek gerekirdi.
        /// </summary>
        public bool Contains(Vector2 screenPosition) =>
            Enabled &&
            Rect.gameObject.activeInHierarchy &&
            RectTransformUtility.RectangleContainsScreenPoint(Rect, screenPosition, null);
    }
}
