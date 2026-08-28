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

        static Sprite roundedSquare;

        /// <summary>
        /// Köşeleri yuvarlatılmış kare. Simge altlığı olarak kullanılıyor.
        ///
        /// uGUI'nin varsayılan <c>Image</c>'ı keskin köşeli bir dikdörtgen
        /// çiziyor ve paletin geri kalanı yumuşak; keskin köşeli tek bir kutu
        /// arayüzün içinde yabancı duruyor. Dokuz dilimli hazır bir sprite
        /// kullanmak da olurdu ama o yine dosya demek.
        ///
        /// Köşe yarıçapı kenarın %28'i: daha azı fark edilmiyor, daha fazlası
        /// kare değil hap görünüyor.
        /// </summary>
        public static Sprite RoundedSquare
        {
            get
            {
                if (roundedSquare != null)
                {
                    return roundedSquare;
                }

                const int size = 64;
                const int samples = 3;
                const float radius = size * 0.28f;

                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                };

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
                                float px = x + (sx + 0.5f) / samples;
                                float py = y + (sy + 0.5f) / samples;

                                // Köşeye olan uzaklık: nokta iç dikdörtgenin
                                // dışındaysa yalnızca taşan bileşenler sayılıyor.
                                // Kenarlarda bu sıfır kalıyor, yani yalnızca dört
                                // köşe yuvarlanıyor.
                                float dx = Mathf.Max(Mathf.Abs(px - size * 0.5f) - (size * 0.5f - radius), 0f);
                                float dy = Mathf.Max(Mathf.Abs(py - size * 0.5f) - (size * 0.5f - radius), 0f);

                                if (dx * dx + dy * dy <= radius * radius)
                                {
                                    inside++;
                                }
                            }
                        }

                        byte alpha = (byte)(255 * inside / (samples * samples));
                        pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                    }
                }

                texture.SetPixels32(pixels);
                texture.Apply();

                roundedSquare = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
                return roundedSquare;
            }
        }

        static Sprite gear;

        /// <summary>
        /// Dişli simgesi. Yıldız gibi çalışma zamanında çiziliyor.
        ///
        /// Yıldızdan farkı çokgen değil, kutupsal bir fonksiyon olması: her
        /// piksel için merkeze uzaklık ve açı hesaplanıyor, sonra "bu açıda
        /// dişlinin yarıçapı ne" diye soruluyor. Diş profilini çokgen köşesiyle
        /// yazmak sekiz diş için 32 köşe demekti ve yuvarlatılmış diş ucu
        /// çıkmazdı.
        ///
        /// Diş profili kosinüsün yumuşatılmış eşiği: keskin bir kare dalga
        /// dişleri testere gibi gösteriyor, düz kosinüs ise dişli değil çiçek.
        /// Aradaki geçişi <c>SmoothStep</c> veriyor.
        ///
        /// Ortadaki delik simgeyi dişli yapan şey: onsuz görüntü sekiz kollu bir
        /// güneşe benziyor ve ayar simgesi olarak okunmuyor.
        /// </summary>
        public static Sprite Gear
        {
            get
            {
                if (gear != null)
                {
                    return gear;
                }

                const int size = 64;
                const int samples = 3;
                const int teeth = 8;

                // Oranlar dokunun yarısına göre. Diş ucu 0.47'de: kenara daha
                // fazla yaklaşınca kenar yumuşatma için yer kalmıyor ve simge
                // dokunun sınırında kırpılmış görünüyor.
                const float toothRadius = 0.47f;
                const float bodyRadius = 0.36f;
                const float holeRadius = 0.17f;

                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                };

                var pixels = new Color32[size * size];
                float center = size * 0.5f;

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        int inside = 0;

                        for (int sy = 0; sy < samples; sy++)
                        {
                            for (int sx = 0; sx < samples; sx++)
                            {
                                float px = x + (sx + 0.5f) / samples - center;
                                float py = y + (sy + 0.5f) / samples - center;

                                float radius = Mathf.Sqrt(px * px + py * py) / size;

                                if (radius < holeRadius)
                                {
                                    continue;
                                }

                                float angle = Mathf.Atan2(py, px);
                                float wave = Mathf.Cos(angle * teeth);
                                float profile = Mathf.SmoothStep(bodyRadius, toothRadius, Mathf.InverseLerp(-0.4f, 0.4f, wave));

                                if (radius <= profile)
                                {
                                    inside++;
                                }
                            }
                        }

                        byte alpha = (byte)(255 * inside / (samples * samples));
                        pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                    }
                }

                texture.SetPixels32(pixels);
                texture.Apply();

                gear = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
                return gear;
            }
        }

        static Sprite retry;

        /// <summary>
        /// Yeniden başlatma simgesi: ucu oklu bir çember yayı.
        ///
        /// Dişli gibi kutupsal çiziliyor. Yay, halkanın belli bir açı
        /// aralığındaki parçası; ok ucu ise yayın bittiği noktaya oturan bir
        /// üçgen. Üçgenin tabanı yarıçap yönünde, ucu teğet yönünde: böylece ok
        /// yayın devamına bakıyor ve iki parça ek yeri belli olmadan birleşiyor.
        ///
        /// Boşluk sağ tarafta ve yaklaşık 65 derece. Daha darı okun sığmadığı,
        /// daha genişi çemberin çember olarak okunmadığı yer.
        /// </summary>
        public static Sprite Retry
        {
            get
            {
                if (retry != null)
                {
                    return retry;
                }

                const int size = 64;
                const int samples = 3;

                const float ring = 0.30f;
                const float thickness = 0.085f;

                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                };

                float start = 50f * Mathf.Deg2Rad;
                float end = 345f * Mathf.Deg2Rad;

                // Ok ucu yayın başladığı yerde ve saat yönüne bakıyor: yayın
                // hangi yöne döndüğünü söyleyen tek şey bu.
                var head = ArrowHead(start, ring, thickness);

                var pixels = new Color32[size * size];
                float center = size * 0.5f;

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
                                    (x + (sx + 0.5f) / samples - center) / size,
                                    (y + (sy + 0.5f) / samples - center) / size);

                                float radius = point.magnitude;
                                float angle = Mathf.Atan2(point.y, point.x);

                                if (angle < 0f)
                                {
                                    angle += 2f * Mathf.PI;
                                }

                                bool onArc = Mathf.Abs(radius - ring) <= thickness * 0.5f &&
                                             angle >= start &&
                                             angle <= end;

                                if (onArc || Contains(head, point))
                                {
                                    inside++;
                                }
                            }
                        }

                        byte alpha = (byte)(255 * inside / (samples * samples));
                        pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                    }
                }

                texture.SetPixels32(pixels);
                texture.Apply();

                retry = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
                return retry;
            }
        }

        /// <summary>Yayın ucundaki üçgen: taban yarıçap yönünde, uç teğet yönünde.</summary>
        static Vector2[] ArrowHead(float angle, float ring, float thickness)
        {
            var radial = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            var tangent = new Vector2(Mathf.Sin(angle), -Mathf.Cos(angle));
            var origin = radial * ring;

            return new[]
            {
                origin + tangent * (thickness * 3.4f),
                origin + radial * (thickness * 1.9f),
                origin - radial * (thickness * 1.9f),
            };
        }

        /// <summary>
        /// Yazısı olmayan, simgeli düğme. Simge dikdörtgenin içine oranı
        /// korunarak yerleşiyor ama dokunma hedefi dikdörtgenin tamamı: küçük
        /// bir simgeye tam isabet ettirmek zorunda kalmak, parmakla oynanan bir
        /// oyunda gereksiz bir zorluk.
        /// </summary>
        public static UIButton IconButton(Transform parent, Rect area, Sprite icon, Color tint)
        {
            // Altlık, dokunma hedefinin tamamını kaplıyor ve köşeleri yuvarlak:
            // simge tek başına dururken küçük ve kaybolmuş görünüyordu, altlık
            // hem onu bir düğmeye çeviriyor hem de dokunulacak yeri gösteriyor.
            var plateGo = new GameObject("IconButton", typeof(RectTransform), typeof(Image));
            var rect = plateGo.GetComponent<RectTransform>();

            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(area.xMin, area.yMin);
            rect.anchorMax = new Vector2(area.xMax, area.yMax);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var plate = plateGo.GetComponent<Image>();
            plate.sprite = RoundedSquare;
            plate.color = PanelColor;

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            var iconRect = iconGo.GetComponent<RectTransform>();

            // Simge altlığın içinde pay bırakıyor. Kenara dayanan bir simge,
            // altlığı çerçeve değil kırpma gibi gösteriyor.
            iconRect.SetParent(rect, false);
            iconRect.anchorMin = new Vector2(0.22f, 0.22f);
            iconRect.anchorMax = new Vector2(0.78f, 0.78f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;

            var image = iconGo.GetComponent<Image>();
            image.sprite = icon;
            image.preserveAspect = true;
            image.color = tint;

            return new UIButton
            {
                Rect = rect,
                Background = plate,
            };
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
        /// Bu önce menünün seviye kartında duruyordu. Aynı taşma tur sonu
        /// ekranında da çıkınca buraya taşıdım: iki ekranın da aynı kanvas
        /// ölçeğini kullandığı düşünülürse sorun ekranların değil, kutuya
        /// sabit punto yazmanın sorunuymuş.
        ///
        /// Yine de her etikette çağrılmıyor: otomatik boyutlandırma her karede
        /// ölçüm yapıyor, o yüzden yalnızca içeriği ya da kutusu değişken olan
        /// yazılarda var.
        /// </summary>
        public static void Fit(TMP_Text label, float min, float max)
        {
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Truncate;
            label.enableAutoSizing = true;
            label.fontSizeMin = min;
            label.fontSizeMax = max;
        }

        /// <summary>
        /// Sürüklenebilir bir çubuk.
        ///
        /// uGUI'nin <c>Slider</c> bileşenini kullanmıyorum, aynı sebeple:
        /// EventSystem istiyor ve bu projede o altyapı hiç kurulu değil. Elle
        /// yazınca gereken şey iki dikdörtgen ve bir bölme işlemi.
        ///
        /// Dokunma hedefi çubuğun kendisinden kalın: görünen çizgi ince olmalı
        /// ama parmağın ince bir çizgiyi tutturması gerekmemeli. Kök nesne
        /// saydam ve verilen alanın tamamını kaplıyor, ince çubuk onun içinde
        /// duruyor.
        /// </summary>
        public static UISlider Slider(Transform parent, Rect area, float value)
        {
            var root = Panel(
                parent,
                new Vector2(area.xMin, area.yMin),
                new Vector2(area.xMax, area.yMax),
                new Color(0f, 0f, 0f, 0f));

            root.name = "Slider";

            var track = Panel(root, new Vector2(0f, 0.42f), new Vector2(1f, 0.58f), LockedColor);
            var fill = Panel(track, Vector2.zero, new Vector2(1f, 1f), AccentColor);
            var knob = Panel(root, new Vector2(0f, 0.12f), new Vector2(0f, 0.88f), ButtonColor);

            // Topuz sabit genişlikte: çubuğun oranına bağlansaydı dar ekranda
            // tutulamayacak kadar incelirdi.
            knob.offsetMin = new Vector2(-22f, 0f);
            knob.offsetMax = new Vector2(22f, 0f);

            var slider = new UISlider
            {
                Rect = root,
                Fill = fill,
                Knob = knob,
            };

            slider.SetValue(value);
            return slider;
        }

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
    /// Sürüklenebilir çubuğun durumu. <see cref="UIButton"/> gibi düz bir C#
    /// sınıfı: sahnede bir bileşen değil, kurulan nesnelere tutamak.
    /// </summary>
    public sealed class UISlider
    {
        public RectTransform Rect;
        public RectTransform Fill;
        public RectTransform Knob;

        public float Value { get; private set; }

        public bool Contains(Vector2 screenPoint) =>
            RectTransformUtility.RectangleContainsScreenPoint(Rect, screenPoint);

        /// <summary>
        /// Ekrandaki bir noktanın karşılığı olan değer. Parmağın çubuğun
        /// dışına taşması sorun değil: değer kırpılıyor, yani sürüklerken
        /// parmağını yukarı kaydıran biri çubuğu kaybetmiyor.
        /// </summary>
        public float ValueAt(Vector2 screenPoint)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(Rect, screenPoint, null, out var local))
            {
                return Value;
            }

            var rect = Rect.rect;
            return Mathf.Clamp01((local.x - rect.xMin) / Mathf.Max(1f, rect.width));
        }

        public void SetValue(float value)
        {
            Value = Mathf.Clamp01(value);

            Fill.anchorMax = new Vector2(Value, Fill.anchorMax.y);
            Knob.anchorMin = new Vector2(Value, Knob.anchorMin.y);
            Knob.anchorMax = new Vector2(Value, Knob.anchorMax.y);
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

            // Simgeli düğmenin yazısı yok. Etiketi zorunlu tutup boş bir yazı
            // koymak da olurdu ama o, her karede ölçülen ve hiçbir şey çizmeyen
            // bir TMP bileşeni demek.
            if (Label != null)
            {
                Label.color = enabled ? UIKit.TextColor : UIKit.DimTextColor;
            }
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
