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
        public static readonly Color PanelColor = new(0.12f, 0.12f, 0.14f, 0.92f);
        public static readonly Color ButtonColor = new(0.26f, 0.26f, 0.30f, 1f);
        public static readonly Color LockedColor = new(0.18f, 0.18f, 0.20f, 1f);
        public static readonly Color TextColor = new(0.92f, 0.92f, 0.94f, 1f);
        public static readonly Color DimTextColor = new(0.55f, 0.55f, 0.58f, 1f);

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
            scaler.matchWidthOrHeight = 1f;

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
        public static UIButton Button(Transform parent, Rect area, string text, int fontSize, float inset = 0.01f)
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
                Label = Label(background, text, fontSize, TextAnchor.MiddleCenter),
            };
        }

        public static Text Label(Transform parent, string text, int fontSize, TextAnchor alignment)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(Text));
            var rect = go.GetComponent<RectTransform>();

            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var label = go.GetComponent<Text>();
            label.text = text;
            label.font = BuiltinFont();
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.color = TextColor;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;

            return label;
        }

        /// <summary>
        /// Yazı tipi olarak motorun içindeki eski çalışma zamanı fontu kullanılıyor.
        /// TextMeshPro daha iyi görünürdü ama projeye ayrıca "TMP Essentials"
        /// içe aktarmayı gerektiriyor — sahneyi tek komutla kurabilme kuralını
        /// bozan bir elle adım. Gri kutu prototipinde yazının güzel olması
        /// gerekmiyor, okunması yetiyor.
        /// </summary>
        static Font BuiltinFont()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
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
        public Text Label;

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
