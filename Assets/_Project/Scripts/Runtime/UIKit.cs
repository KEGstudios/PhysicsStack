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
        static Sprite star;

        /// <summary>
        /// Yıldız simgesi. Dosyadan gelmiyor, çalışma zamanında çiziliyor.
        ///
        /// İki alternatifi de eledim. **Yazı karakteri (★)** en ucuzu görünüyordu
        /// ama TMP yalnızca font atlasındaki karakterleri çizebiliyor; kullandığım
        /// fontta bu karakter yoksa ekranda boş kare çıkar ve bunu ancak build'i
        /// alıp bakınca görürüm. **Hazır sprite** ise projenin "hiçbir hazır varlık
        /// yok" kuralını bozardı.
        ///
        /// Yıldız on köşeli bir çokgen: beş dış, beş iç köşe. İç yarıçapın dış
        /// yarıçapa oranı 0.42 — daha büyüğü şişman bir çiçek, daha küçüğü ince
        /// bir yıldız patlaması gibi duruyor.
        ///
        /// Kenar yumuşatma elle yapılıyor (her pikselde 3x3 örnek): tek örnekle
        /// yıldızın eğik kenarları merdiven gibi çıkıyor ve küçük boyutta bu
        /// doğrudan görünüyor.
        /// </summary>
        public static Sprite Star
        {
            get
            {
                if (star != null)
                {
                    return star;
                }

                const int size = 64;
                const int samples = 3;

                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                };

                var polygon = StarPolygon(size * 0.5f, size * 0.5f, size * 0.46f, 0.42f);
                var pixels = new Color32[size * size];

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        int inside = 0;

                        for (int sy = 0; sy < samples; sy++)
                        {
                            for (int sx = 0; sx < samples; sx++)
                            {
                                var point = new Vector2(
                                    x + (sx + 0.5f) / samples,
                                    y + (sy + 0.5f) / samples);

                                if (Contains(polygon, point))
                                {
                                    inside++;
                                }
                            }
                        }

                        byte alpha = (byte)(255 * inside / (samples * samples));

                        // Renk beyaz, bilgi yalnızca alfada: böylece aynı doku
                        // hem dolu hem boş yıldız için kullanılabiliyor, rengi
                        // Image bileşeni veriyor.
                        pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                    }
                }

                texture.SetPixels32(pixels);
                texture.Apply();

                star = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
                return star;
            }
        }

        static Vector2[] StarPolygon(float centerX, float centerY, float outerRadius, float innerRatio)
        {
            var points = new Vector2[10];

            for (int i = 0; i < 10; i++)
            {
                // Tek indeksler iç köşe. -90 derece kaydırma yıldızın bir ucunu
                // yukarı bakacak şekilde çeviriyor.
                float radius = i % 2 == 0 ? outerRadius : outerRadius * innerRatio;
                float angle = Mathf.Deg2Rad * (-90f + i * 36f);

                points[i] = new Vector2(
                    centerX + Mathf.Cos(angle) * radius,
                    centerY + Mathf.Sin(angle) * radius);
            }

            return points;
        }

        /// <summary>Işın atma yöntemiyle nokta-çokgen testi.</summary>
        static bool Contains(Vector2[] polygon, Vector2 point)
        {
            bool inside = false;

            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                if (polygon[i].y > point.y != polygon[j].y > point.y &&
                    point.x < (polygon[j].x - polygon[i].x) * (point.y - polygon[i].y) /
                              (polygon[j].y - polygon[i].y) + polygon[i].x)
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        /// <summary>
        /// Üç yıldızlık bir sıra çizer. Kazanılanlar vurgu renginde, kalanlar
        /// solgun — boş bırakmak yerine solgun çizmek "kaç yıldız var" bilgisini
        /// de veriyor, yoksa iki yıldızlı bir seviye iki yıldızlık bir oyun gibi
        /// görünüyor.
        /// </summary>
        public static void StarRow(Transform parent, Rect area, int earned, int total = 3)
        {
            for (int i = 0; i < total; i++)
            {
                float width = area.width / total;

                var go = new GameObject($"Star{i}", typeof(RectTransform), typeof(Image));
                var rect = go.GetComponent<RectTransform>();

                rect.SetParent(parent, false);
                rect.anchorMin = new Vector2(area.xMin + width * i, area.yMin);
                rect.anchorMax = new Vector2(area.xMin + width * (i + 1), area.yMax);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                var image = go.GetComponent<Image>();
                image.sprite = Star;
                image.preserveAspect = true;
                image.color = i < earned ? StarColor : StarEmptyColor;
            }
        }

        public static Color StarColor => palette != null ? palette.star : new Color(0.95f, 0.76f, 0.29f);
        public static Color StarEmptyColor => palette != null ? palette.starEmpty : new Color(0.85f, 0.83f, 0.80f);

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
