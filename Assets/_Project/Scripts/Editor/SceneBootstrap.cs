using PhysicsStack;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PhysicsStack.EditorTools
{
    /// <summary>
    /// Gün 1'in gri kutu sahnesini sıfırdan kurar: zemin, kamera, ışık ve kutu prefab'ı.
    ///
    /// Neden elle değil de script? Sahne ve prefab dosyaları YAML; ikisi de GUID
    /// referanslarıyla birbirine bağlı. Kurulumu kodla yapınca sahne her an
    /// tek tuşla temiz haline dönebiliyor — his ayarlarken sahneyi bozup
    /// "acaba neyi kaydırdım" diye aramak istemiyorum.
    ///
    /// Bu bir kurulum aracı, oyun kodu değil; bu yüzden Editor assembly'sinde
    /// duruyor ve build'e girmiyor.
    /// </summary>
    public static class SceneBootstrap
    {
        const string ScenePath = "Assets/_Project/Scenes/Main.unity";
        const string BoxPrefabPath = "Assets/_Project/Prefabs/Box.prefab";
        const string BallPrefabPath = "Assets/_Project/Prefabs/Ball.prefab";
        const string BoxMaterialPath = "Assets/_Project/Art/Materials/M_Box.mat";
        const string GroundMaterialPath = "Assets/_Project/Art/Materials/M_Ground.mat";
        const string DragSettingsPath = "Assets/_Project/Data/DragSettings.asset";
        // Uzantı bilerek ".asset": Unity, PhysicsMaterial'ı CreateAsset ile
        // ".physicsMaterial" olarak yazmaya "bu ileride hata olacak" uyarısı
        // veriyor ve kendi çözümü olarak ".asset"i öneriyor. Dosya türü değişse
        // de içerik aynı PhysicsMaterial; Inspector da aynı arayüzü açıyor.
        const string BoxPhysicsMaterialPath = "Assets/_Project/Settings/PM_Box.asset";

        /// <summary>Kulenin geçmesi gereken yükseklik. Controller'ın varsayılanıyla aynı tutuluyor.</summary>
        const float TargetHeight = 4f;

        [MenuItem("PhysicsStack/Sahneyi Sifirdan Kur")]
        public static void Build()
        {
            // Bu komut sahneyi sıfırdan kuruyor: elle yapılmış her düzenleme gider.
            // Batchmode'da soru sorulamaz, orada doğrudan çalışıyor.
            if (!Application.isBatchMode &&
                !EditorUtility.DisplayDialog(
                    "Sahneyi sıfırdan kur",
                    "Main.unity yeniden oluşturulacak. Sahnede elle yaptığın değişiklikler kaybolur.",
                    "Kur",
                    "Vazgeç"))
            {
                return;
            }

            var boxMaterial = CreateLitMaterial(BoxMaterialPath, new Color(0.62f, 0.62f, 0.62f));
            var groundMaterial = CreateLitMaterial(GroundMaterialPath, new Color(0.30f, 0.30f, 0.32f));

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camera = CreateCamera();
            CreateLight();
            CreateGround(groundMaterial);

            var dragSettings = LoadOrCreateDragSettings();
            var boxPhysics = LoadOrCreateBoxPhysicsMaterial();
            var boxPrefab = CreateBoxPrefab(boxMaterial, boxPhysics, dragSettings);
            var targetLine = CreateTargetLine(groundMaterial, TargetHeight);
            var dropLine = CreateDropLine(groundMaterial);
            var ballPrefab = CreateBallPrefab(groundMaterial);
            var levels = LevelBootstrap.LoadOrCreate();
            CreateSystems(camera, boxPrefab, ballPrefab, dragSettings, targetLine, dropLine, levels);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[SceneBootstrap] Sahne kuruldu: {ScenePath}");
        }

        static Camera CreateCamera()
        {
            var go = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            go.tag = "MainCamera";

            // Kule yukarı doğru büyüyor. Kamera hafif yukarıdan ve hafif aşağı bakıyor:
            // hem zemin hem de hedef yükseklik aynı anda kadrajda kalsın diye.
            go.transform.SetPositionAndRotation(new Vector3(0f, 4f, -12f), Quaternion.Euler(8f, 0f, 0f));

            var camera = go.GetComponent<Camera>();

            // FOV burada verilmiyor: StackCamera onu her karede ekran oranından
            // hesaplıyor. Sabit bir değer yazsaydım dar telefonda oyun alanı
            // daralır, tablette genişlerdi.
            camera.backgroundColor = new Color(0.16f, 0.17f, 0.19f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            return camera;
        }

        static void CreateLight()
        {
            var go = new GameObject("Directional Light", typeof(Light));
            go.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var light = go.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            // Gölge tek başına derinlik bilgisi taşıyor: gri kutuların birbirine göre
            // yüksekliği başka türlü okunmuyor.
            light.shadows = LightShadows.Soft;
        }

        static void CreateGround(Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Ground";

            // Üst yüzey tam y = 0'da olsun: yığın yüksekliğini ölçerken referans
            // noktasının ondalıklı olmaması ileride her hesabı sadeleştiriyor.
            go.transform.position = new Vector3(0f, -0.5f, 0f);
            go.transform.localScale = new Vector3(14f, 1f, 14f);

            go.isStatic = true;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        static GameObject CreateBoxPrefab(Material material, PhysicsMaterial physics, DragSettings settings)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Box";
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            go.GetComponent<BoxCollider>().sharedMaterial = physics;

            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 1f;

            // Varsayılan 0.05 neredeyse sıfır: kutu bir kere dönmeye başlayınca
            // durmuyor ve üst üste konan her kutu yığını biraz daha sallıyor.
            // Sürtünmenin dönme karşılığı bu; yükseltince kule oturuyor ama
            // kutular hâlâ devrilebiliyor.
            rb.angularDamping = 0.35f;

            // Fizik sabit adımda çalışıyor, çizim kare hızında. Interpolate olmadan
            // hızlı sürüklenen kutu titriyor.
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            // Hız tabanlı sürüklemede kutu tek FixedUpdate'te kendi boyu kadar yol
            // alabiliyor. Discrete çarpışma bu adımı ıskalayıp kutunun zeminden
            // veya yığından geçmesine yol açıyor.
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            // Görüntü 3D, simülasyon 2D. Oyuncu kutuyu yalnızca XY düzleminde
            // hareket ettirebiliyor; derinlikte serbest bırakılan fizik, oyuncunun
            // hiç erişemediği bir eksende kuleyi deviriyordu. Zorluk değil,
            // adaletsizlik olduğu için kilitlendi: z'de konum, x/y'de dönüş kapalı;
            // kutular hâlâ devrilir ve yuvarlanır, ama görünen düzlemde.
            rb.constraints = RigidbodyConstraints.FreezePositionZ |
                             RigidbodyConstraints.FreezeRotationX |
                             RigidbodyConstraints.FreezeRotationY;

            var draggable = go.AddComponent<DraggableBody>();
            SetReference(draggable, "settings", settings);

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, BoxPrefabPath);
            Object.DestroyImmediate(go);
            return prefab;
        }

        static void CreateSystems(
            Camera camera,
            GameObject boxPrefab,
            GameObject ballPrefab,
            DragSettings settings,
            Renderer targetLine,
            Renderer dropLine,
            LevelLibrary levels)
        {
            var go = new GameObject("Systems");

            var input = go.AddComponent<PointerDragInput>();
            var queue = go.AddComponent<BoxQueue>();
            var tracker = go.AddComponent<StackTracker>();
            var controller = go.AddComponent<StackGameController>();

            // Camera.main'e Awake'te de düşüyor ama referansı sahnede görünür tutmak
            // "hangi kamerayı kullanıyor" sorusunu Inspector'da cevaplıyor.
            SetReference(input, "targetCamera", camera);
            SetReference(queue, "boxPrefab", boxPrefab);
            SetReference(controller, "queue", queue);
            SetReference(controller, "tracker", tracker);
            SetReference(controller, "levelLibrary", levels);

            // Kuyruk kule tepesini kendi okuyor: hem kutunun beliriş yüksekliği
            // hem de bırakma çizgisi aynı sayıya dayanıyor.
            SetReference(queue, "tracker", tracker);

            var overlay = go.AddComponent<DebugOverlay>();
            SetReference(overlay, "controller", controller);
            SetReference(overlay, "settings", settings);
            SetReference(overlay, "queue", queue);

            // Çizginin yüksekliği artık burada değil, kural setinden belirleniyor;
            // aşağıdaki başlangıç yüksekliği sadece Editor'da sahneyi boş
            // bakarken anlamlı dursun diye.
            var indicator = go.AddComponent<TargetLine>();
            SetReference(indicator, "controller", controller);
            SetReference(indicator, "targetLine", targetLine);

            var dropLineView = go.AddComponent<DropLineView>();
            SetReference(dropLineView, "controller", controller);
            SetReference(dropLineView, "queue", queue);
            SetReference(dropLineView, "line", dropLine);

            // Arayüz: menü ve tur sonu. İkisi de kanvasını çalışma zamanında
            // kendisi kuruyor, o yüzden sahnede tek bir bileşenden ibaretler.
            var menu = go.AddComponent<MenuUI>();
            SetReference(menu, "levels", levels);

            var result = go.AddComponent<ResultUI>();
            SetReference(result, "controller", controller);
            SetReference(result, "levels", levels);

            // Tehditler: ikisi de seviyenin verisinden kendi ayarlarını okuyor,
            // kapalıysa hiç görünmüyorlar.
            var wind = go.AddComponent<Wind>();
            SetReference(wind, "controller", controller);
            SetReference(wind, "queue", queue);

            SetReference(overlay, "wind", wind);

            var windIndicator = go.AddComponent<WindIndicator>();
            SetReference(windIndicator, "wind", wind);
            SetReference(windIndicator, "bar", CreateWindPart(boxPrefab, "WindBar", 0f));
            SetReference(windIndicator, "head", CreateWindPart(boxPrefab, "WindHead", 45f));

            CreateCannon(boxPrefab, ballPrefab, controller, tracker);

            // Kamera bileşeni kameranın kendi nesnesinde duruyor ama ölçümü
            // buradaki tracker'dan alıyor; kuyruk da kutuyu kadrajın üstünde
            // üretebilmek için kamerayı tanıyor.
            var stackCamera = camera.gameObject.AddComponent<StackCamera>();
            SetReference(stackCamera, "tracker", tracker);
            SetReference(queue, "stackCamera", stackCamera);
            SetReference(windIndicator, "stackCamera", stackCamera);
        }

        /// <summary>
        /// Varlık zaten varsa dokunmuyoruz. Bu bilerek: sahneyi sıfırdan kurmak
        /// his ayarlarını da silseydi, "sahneyi tazele" hareketi her seferinde
        /// bir günlük ayar çalışmasını çöpe atardı.
        /// </summary>
        static DragSettings LoadOrCreateDragSettings()
        {
            var existing = AssetDatabase.LoadAssetAtPath<DragSettings>(DragSettingsPath);
            if (existing != null)
            {
                return existing;
            }

            var settings = ScriptableObject.CreateInstance<DragSettings>();
            AssetDatabase.CreateAsset(settings, DragSettingsPath);
            return settings;
        }

        static PhysicsMaterial LoadOrCreateBoxPhysicsMaterial()
        {
            var existing = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(BoxPhysicsMaterialPath);
            if (existing != null)
            {
                return existing;
            }

            // PhysX'in varsayılanı 0.6/0.6 ve bu kutular için az: üst üste konan
            // kutular en ufak temasta yatay kayıyordu. Statik sürtünmeyi dinamikten
            // yüksek tutmak "duran kutuyu kaydırmak, kayan kutuyu durdurmaktan
            // zordur" demek — kule bir kere oturunca yerinde kalıyor.
            var material = new PhysicsMaterial("PM_Box")
            {
                staticFriction = 0.85f,
                dynamicFriction = 0.6f,

                // Sıfır zıplama: gri kutu prototipinde en ufak sekme bile kuleyi
                // yıkıyor ve oyuncuya "ben mi yanlış yaptım" dedirtiyor.
                bounciness = 0f,

                // İki cismin sürtünmesi farklıysa büyüğü kazansın: kutunun zemine
                // ve birbirine tutunması, ortalamayla zayıflatılmasın.
                frictionCombine = PhysicsMaterialCombine.Maximum,
                bounceCombine = PhysicsMaterialCombine.Minimum,
            };

            AssetDatabase.CreateAsset(material, BoxPhysicsMaterialPath);
            return material;
        }

        static void SetReference(Object target, string propertyName, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static Renderer CreateTargetLine(Material material, float height)
        {
            // Hedef yüksekliği görünmeden oynamak, kaç kutu kaldığını sayarak
            // oynamak demek. İnce bir çizgi bunu tek bakışta çözüyor.
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "TargetLine";
            go.transform.position = new Vector3(0f, height, 0f);
            go.transform.localScale = new Vector3(16f, 0.04f, 0.04f);

            // Collider'ı yok: kulenin ona çarpması saçma olurdu.
            Object.DestroyImmediate(go.GetComponent<Collider>());

            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;

            // Oyun bittiğinde rengi değişecek olan nesne bu; referansı dışarı veriyoruz.
            return renderer;
        }

        /// <summary>
        /// Rüzgâr göstergesinin parçaları: kadrajın üstünde esme yönüne uzayan bir
        /// çubuk ve ucunda 45° döndürülmüş bir küp — eşkenar dörtgen olarak
        /// okunuyor ve yönü tek bakışta veriyor. Görünmeyen bir kuvvet zorluk
        /// değil kafa karışıklığı üretir; oyuncu kendi hatasını arar.
        /// </summary>
        static Renderer CreateWindPart(GameObject boxPrefab, string name, float roll)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.rotation = Quaternion.Euler(0f, 0f, roll);
            go.transform.localScale = Vector3.one * 0.2f;

            Object.DestroyImmediate(go.GetComponent<Collider>());

            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = boxPrefab.GetComponent<MeshRenderer>().sharedMaterial;
            renderer.enabled = false;
            return renderer;
        }

        static GameObject CreateCannon(
            GameObject boxPrefab,
            GameObject ballPrefab,
            StackGameController controller,
            StackTracker tracker)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Cannon";
            go.transform.localScale = new Vector3(0.55f, 0.45f, 0.45f);

            // Collider yok: mermi namludan çıkarken kendi gövdesine çarpıp
            // anında yok olurdu, ve namlunun kuleye fiziksel olarak dokunması
            // zaten istenmeyen bir şey.
            Object.DestroyImmediate(go.GetComponent<Collider>());

            go.GetComponent<MeshRenderer>().sharedMaterial =
                boxPrefab.GetComponent<MeshRenderer>().sharedMaterial;

            var cannon = go.AddComponent<Cannon>();
            SetReference(cannon, "controller", controller);
            SetReference(cannon, "tracker", tracker);
            SetReference(cannon, "ballPrefab", ballPrefab);
            SetReference(cannon, "body", go.GetComponent<MeshRenderer>());

            return go;
        }

        static GameObject CreateBallPrefab(Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Ball";
            go.transform.localScale = Vector3.one * 0.45f;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;

            var rb = go.AddComponent<Rigidbody>();

            // Yerçekimsiz: mermi düz gidiyor. Parabol çizen bir mermi, oyuncudan
            // tehdidi okumak için ayrı bir sezgi isterdi.
            rb.useGravity = false;

            // Kutuyu saptıracak kadar var, fırlatacak kadar değil. Kutunun kütlesi
            // 1 civarında; bu oran itiyor ama süpürmüyor.
            rb.mass = 0.35f;

            rb.interpolation = RigidbodyInterpolation.Interpolate;

            // Küçük ve hızlı: Discrete çarpışmayla kutunun içinden geçerdi.
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.constraints = RigidbodyConstraints.FreezePositionZ;

            go.AddComponent<CannonBall>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, BallPrefabPath);
            Object.DestroyImmediate(go);
            return prefab;
        }

        static Renderer CreateDropLine(Material material)
        {
            // Kutunun altına inemeyeceği yükseklik. Hedef çizgisinden ince ve
            // farklı renkte: biri "geçmen gereken yer", diğeri "burada bırak".
            // İki çizgi aynı görünseydi oyuncu hangisinin ne olduğunu ancak
            // deneyerek öğrenirdi.
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "DropLine";
            go.transform.position = new Vector3(0f, 2f, 0f);
            go.transform.localScale = new Vector3(16f, 0.025f, 0.025f);

            Object.DestroyImmediate(go.GetComponent<Collider>());

            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            return renderer;
        }

        static Material CreateLitMaterial(string path, Color color)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                return existing;
            }

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            var material = new Material(shader) { color = color };
            material.SetFloat("_Smoothness", 0.15f); // Parlak gri kutu, kenarları okunmuyor.

            AssetDatabase.CreateAsset(material, path);
            return material;
        }
    }
}
