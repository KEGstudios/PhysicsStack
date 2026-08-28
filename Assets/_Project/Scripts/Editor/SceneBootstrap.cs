using System.IO;
using PhysicsStack;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
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
        const string CannonMaterialPath = "Assets/_Project/Art/Materials/M_Cannon.mat";
        const string BallMaterialPath = "Assets/_Project/Art/Materials/M_Ball.mat";
        const string PalettePath = "Assets/_Project/Data/Palette.asset";
        const string SkyMaterialPath = "Assets/_Project/Art/Materials/M_Sky.mat";
        const string LineMaterialPath = "Assets/_Project/Art/Materials/M_Line.mat";
        const string DustMaterialPath = "Assets/_Project/Art/Materials/M_Dust.mat";
        const string StreakMaterialPath = "Assets/_Project/Art/Materials/M_Streak.mat";
        const string StreakTexturePath = "Assets/_Project/Art/Textures/T_Streak.png";
        const string VolumeProfilePath = "Assets/_Project/Settings/PostProcess.asset";
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

            ApplyRenderSettings();

            var palette = LoadOrCreatePalette();

            // Malzemelerin rengi palet varlığından geliyor: sahne kurulumu paletin
            // çıktısı, kaynağı değil. Renk denemek isteyince paleti değiştirip
            // sahneyi yeniden kurmak yetiyor, altı ayrı yerde renk aramak değil.
            var boxMaterial = CreateLitMaterial(BoxMaterialPath, palette.BoxColor(0));
            var groundMaterial = CreateLitMaterial(GroundMaterialPath, palette.ground);
            var cannonMaterial = CreateLitMaterial(CannonMaterialPath, palette.cannon);
            var ballMaterial = CreateLitMaterial(BallMaterialPath, palette.ball);
            var lineMaterial = CreateUnlitMaterial(LineMaterialPath, palette.targetIdle);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camera = CreateCamera();
            var sun = CreateLight(palette);
            CreateSky(palette);
            CreatePostProcessing(camera);
            CreateGround(groundMaterial);

            var dragSettings = LoadOrCreateDragSettings();
            var boxPhysics = LoadOrCreateBoxPhysicsMaterial();
            var boxPrefab = CreateBoxPrefab(boxMaterial, boxPhysics, dragSettings);
            var targetLine = CreateTargetLine(lineMaterial, TargetHeight);
            var dropLine = CreateDropLine(lineMaterial);
            var ballPrefab = CreateBallPrefab(ballMaterial);
            var dust = CreateDust(palette);
            var speedLines = CreateSpeedLines(palette);
            var levels = LevelBootstrap.LoadOrCreate();

            // Palet referansi bilerek yeniden okunuyor. Yukarida yeni varlik
            // uretilmis olabilir (ornegin toz malzemesi) ve varlik uretmek
            // AssetDatabase'i tazeliyor: elimizdeki palet nesnesi gecersizlesiyor,
            // atandiginda da sessizce bos yaziliyor. Bu hata iki kez isirdi ve
            // ikisinde de tek belirtisi renklerin yanlis olmasiydi.
            palette = LoadOrCreatePalette();
            CreateSystems(camera, sun, boxPrefab, ballPrefab, dragSettings, targetLine, dropLine, levels, palette, cannonMaterial, lineMaterial, dust, speedLines);

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
            // Arka planı gradyan gökyüzü dolduruyor; düz renk yerine geçiş,
            // kadrajın üst yarısı boş kaldığında sahneyi ölü göstermiyor.
            camera.clearFlags = CameraClearFlags.Skybox;
            return camera;
        }

        static Light CreateLight(Palette palette)
        {
            var go = new GameObject("Directional Light", typeof(Light));
            go.transform.rotation = Quaternion.Euler(45f, -35f, 0f);

            var light = go.GetComponent<Light>();
            light.type = LightType.Directional;

            // Hafif sıcak anahtar ışık. Beyaz ışık pastel renkleri soluklaştırıyor;
            // azıcık sarıya kaçan bir ışık onları "boyanmış" değil "aydınlatılmış"
            // gösteriyor.
            light.color = new Color(1f, 0.97f, 0.92f);
            light.intensity = 1.15f;

            // Gölge tek başına derinlik bilgisi taşıyor: kutuların birbirine göre
            // yüksekliği başka türlü okunmuyor.
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.55f;

            // Ortam ışığı gökyüzünün gradyanını takip ediyor: üstten açık, alttan
            // zemin rengi. Tek renk ortam ışığı kutuların alt yüzlerini ölü gri
            // yapıyor, bu ise onları sahnenin içine oturtuyor.
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            // Gökyüzü ortam ışığı kısılıyor: tam şiddette verince zemin yukarıdan
            // hem yönlü ışık hem ortam ışığı alıp beyaza yaklaşıyor ve arka planla
            // ayrımı kayboluyor. Zeminin koyu kalması gereken bir yüzey olması
            // renk seçiminden çok aydınlatma meselesiymiş.
            RenderSettings.ambientSkyColor = palette.skyTop * 0.8f;
            RenderSettings.ambientEquatorColor = palette.skyBottom;
            RenderSettings.ambientGroundColor = palette.ground * 0.7f;

            return light;
        }

        /// <summary>
        /// Post-process yigini: tonemapping, bloom, vignette, renk ayari.
        ///
        /// Bunlar bir prototipte "sus" gibi gorunuyor ama sanatcisi olmayan bir
        /// projede en cok isi bunlar yapiyor: duz renkli kutulardan olusan bir
        /// sahne, tonemapping ve hafif bloom olmadan bilgisayar ciktisi gibi
        /// duruyor. Hicbiri varlik gerektirmiyor, hepsi birkac sayi.
        ///
        /// Profil varsa dokunulmuyor - bu degerler gozle ayarlanan seyler ve
        /// sahneyi tazelemek onlari cope atmamali.
        /// </summary>
        static void CreatePostProcessing(Camera camera)
        {
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;

            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);

            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, VolumeProfilePath);

                // Neutral secildi, ACES degil: ACES koyu ve doygun goruntude iyi
                // ama pastel tonlari eziyor, acik renkleri birbirine yaklastiriyor.
                var tonemapping = AddOverride<Tonemapping>(profile);
                tonemapping.mode.Override(TonemappingMode.Neutral);

                // Esik yuksek, siddet dusuk: parlak yuzeylerin kenarina hafif bir
                // yumusaklik katiyor, sahneyi sisletmiyor.
                var bloom = AddOverride<Bloom>(profile);
                bloom.threshold.Override(0.95f);
                bloom.intensity.Override(0.35f);
                bloom.scatter.Override(0.6f);

                // Kadrajin koselerini hafifce karartmak, dikkatin ortadaki kuleye
                // toplanmasini sagliyor.
                var vignette = AddOverride<Vignette>(profile);
                vignette.intensity.Override(0.24f);
                vignette.smoothness.Override(0.45f);
                vignette.color.Override(new Color(0.25f, 0.22f, 0.24f));

                var color = AddOverride<ColorAdjustments>(profile);
                color.postExposure.Override(0.1f);
                color.contrast.Override(8f);
                color.saturation.Override(6f);

                AssetDatabase.SaveAssets();
            }

            var go = new GameObject("Post Process", typeof(Volume));
            var volume = go.GetComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1f;
            volume.sharedProfile = profile;
        }

        /// <summary>
        /// Etki profile eklenirken alt varlik olarak da kaydedilmesi gerekiyor;
        /// yoksa profil dosyasi referansi kaybediyor ve ayarlar sessizce sifirlaniyor.
        /// </summary>
        static T AddOverride<T>(VolumeProfile profile) where T : VolumeComponent
        {
            var component = profile.Add<T>(true);
            component.name = typeof(T).Name;
            AssetDatabase.AddObjectToAsset(component, profile);
            return component;
        }

        /// <summary>
        /// Gradyan gökyüzü. Elle yazılmış shader kullanılıyor: ihtiyaç duyulan tek
        /// şey iki renk arasında dikey geçiş, ve doku kullanmak hem birkaç
        /// megabaytlık varlık hem de palet değişince yeniden üretilmesi gereken
        /// bir şey demekti.
        /// </summary>
        static void CreateSky(Palette palette)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(SkyMaterialPath);

            if (material == null)
            {
                material = new Material(Shader.Find("PhysicsStack/GradientSky"));
                AssetDatabase.CreateAsset(material, SkyMaterialPath);
            }

            material.SetColor("_TopColor", palette.skyTop);
            material.SetColor("_BottomColor", palette.skyBottom);
            material.SetFloat("_Exponent", 1.1f);
            EditorUtility.SetDirty(material);

            RenderSettings.skybox = material;
        }

        static void CreateGround(Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Ground";

            // Üst yüzey tam y = 0'da olsun: yığın yüksekliğini ölçerken referans
            // noktasının ondalıklı olmaması ileride her hesabı sadeleştiriyor.
            go.transform.position = new Vector3(0f, -0.5f, 0f);

            // Geniş ekranda kadraj yanlara doğru açılıyor; zemin oyun alanından
            // belirgin şekilde geniş olmazsa kenarlarda boşluk görünüyor.
            go.transform.localScale = new Vector3(30f, 1f, 14f);

            go.isStatic = true;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        static GameObject CreateBoxPrefab(Material material, PhysicsMaterial physics, DragSettings settings)
        {
            // Gorsel govde ayri bir cocuk nesnede duruyor. Sebebi ezilme
            // animasyonu: olcegi rigidbody'nin kendisinde oynatmak collider'i da
            // oynatirdi, yani gorsel bir susleme fizigi degistirmis olurdu.
            var go = new GameObject("Box");

            var collider = go.AddComponent<BoxCollider>();
            collider.sharedMaterial = physics;

            var mesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mesh.name = "Mesh";
            Object.DestroyImmediate(mesh.GetComponent<Collider>());
            mesh.GetComponent<MeshRenderer>().sharedMaterial = material;
            mesh.AddComponent<BoxVisual>();
            mesh.transform.SetParent(go.transform, false);

            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 1f;

            // Varsayılan 0.05 neredeyse sıfır: kutu bir kere dönmeye başlayınca
            // durmuyor ve üst üste konan her kutu yığını biraz daha sallıyor.
            // Sürtünmenin dönme karşılığı bu; yükseltince kule oturuyor ama
            // kutular hâlâ devrilebiliyor.
            //
            // 0.35'ten 0.6'ya çıktı. Sebep sonsuz modda ölçülen tavan: tur 8
            // kutu civarında bitiyordu ve kuleyi deviren şey tek bir kötü atış
            // değil, her inişte biraz büyüyen sallanmaydı. Sallanma dönme
            // demek, dönmenin frenlenecek yeri de burası.
            rb.angularDamping = 0.6f;

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
            Light sun,
            GameObject boxPrefab,
            GameObject ballPrefab,
            DragSettings settings,
            Renderer targetLine,
            Renderer dropLine,
            LevelLibrary levels,
            Palette palette,
            Material cannonMaterial,
            Material lineMaterial,
            ParticleSystem dust,
            ParticleSystem speedLines)
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
            SetReference(queue, "palette", palette);
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

            // Görsel bileşenlerin renkleri paletten sahneye işleniyor. Her birine
            // paletin referansını vermek yerine değeri yazmayı seçtim: renk
            // çalışma zamanında değişen bir şey değil, sahnenin bir özelliği.
            SetColor(indicator, "idleColor", palette.targetIdle);
            SetColor(indicator, "holdingColor", palette.targetHolding);
            SetColor(indicator, "wonColor", palette.targetWon);
            SetColor(indicator, "lostColor", palette.targetLost);

            var dropLineView = go.AddComponent<DropLineView>();
            SetReference(dropLineView, "controller", controller);
            SetReference(dropLineView, "queue", queue);
            SetReference(dropLineView, "line", dropLine);
            SetColor(dropLineView, "color", palette.dropLine);

            var effects = go.AddComponent<ImpactEffects>();
            SetReference(effects, "controller", controller);
            SetReference(effects, "queue", queue);
            SetReference(effects, "dust", dust);
            SetReference(effects, "speedLines", speedLines);

            // Arayüz: menü ve tur sonu. İkisi de kanvasını çalışma zamanında
            // kendisi kuruyor, o yüzden sahnede tek bir bileşenden ibaretler.
            var menu = go.AddComponent<MenuUI>();
            SetReference(menu, "levels", levels);
            SetReference(menu, "palette", palette);

            var hud = go.AddComponent<HudUI>();
            SetReference(hud, "controller", controller);
            SetReference(hud, "palette", palette);

            var result = go.AddComponent<ResultUI>();
            SetReference(result, "controller", controller);
            SetReference(result, "levels", levels);
            SetReference(result, "palette", palette);

            // Tehditler: ikisi de seviyenin verisinden kendi ayarlarını okuyor,
            // kapalıysa hiç görünmüyorlar.
            var wind = go.AddComponent<Wind>();
            SetReference(wind, "controller", controller);
            SetReference(wind, "queue", queue);

            SetReference(overlay, "wind", wind);
            SetReference(overlay, "effects", effects);

            var windIndicator = go.AddComponent<WindIndicator>();
            SetReference(windIndicator, "wind", wind);
            SetReference(windIndicator, "palette", palette);

            // Grafik kalitesini uygulayan bileşen. Kamerayı ve ışığı tanıması
            // gerekiyor, o yüzden burada kuruluyor: ayar sınıfı statik ve
            // sahneyi tanımıyor, menü de kamerayı tanımıyor.
            var quality = go.AddComponent<QualityRuntime>();
            SetReference(quality, "targetCamera", camera);
            SetReference(quality, "sun", sun);

            var pause = go.AddComponent<PauseUI>();
            SetReference(pause, "controller", controller);
            SetReference(pause, "palette", palette);

            // İki namlu kuruluyor, ikisi de sahnede duruyor ama yalnızca
            // tehdidin istediği kadarı açılıyor: ikincisi kapalıyken gövdesi
            // gizli ve Update'i ilk satırda dönüyor. Alternatifi namluyu
            // çalışma zamanında üretmekti — prefab, referans bağlama ve
            // "üretilen nesne sahneye ait değil" sorunları için, kazancı
            // kapalıyken hiçbir şey yapmayan bir nesne olan bir şey.
            CreateCannon(cannonMaterial, ballPrefab, controller, tracker, effects, queue, index: 0, sideX: -2.25f);
            CreateCannon(cannonMaterial, ballPrefab, controller, tracker, effects, queue, index: 1, sideX: 2.25f);

            // Kamera bileşeni kameranın kendi nesnesinde duruyor ama ölçümü
            // buradaki tracker'dan alıyor; kuyruk da kutuyu kadrajın üstünde
            // üretebilmek için kamerayı tanıyor.
            var stackCamera = camera.gameObject.AddComponent<StackCamera>();
            SetReference(stackCamera, "tracker", tracker);
            SetReference(queue, "stackCamera", stackCamera);
            SetReference(effects, "stackCamera", stackCamera);

            CreateAudio();
        }

        /// <summary>
        /// Ses oyuncusu kendi nesnesinde duruyor, "Systems"in uzerinde degil.
        ///
        /// Sebep: <see cref="SfxPlayer"/> sahneler arasi yasiyor
        /// (<c>DontDestroyOnLoad</c>). Systems nesnesinin uzerinde olsaydi butun
        /// oyun sistemleri onunla birlikte kalici hale gelirdi ve sahne yeniden
        /// yuklendiginde iki kuyruk, iki kontrolcu ve iki girdi okuyucu olurdu.
        ///
        /// Sesin kalici olmasi gereken tek bilesen olmasi tesadufi degil: tek
        /// durumsuz sistem o. Digerlerinin hepsi turun durumunu tutuyor ve
        /// tur bitince silinmeleri gerekiyor.
        /// </summary>
        static void CreateAudio()
        {
            var go = new GameObject("Audio");
            go.AddComponent<SfxPlayer>();
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

        /// <summary>
        /// Palet varlığı: varsa dokunulmuyor. Renkler oynayarak ayarlanan şeyler
        /// ve sahneyi tazelemek bir günlük renk çalışmasını çöpe atmamalı.
        /// </summary>
        /// <summary>
        /// URP ayarlarini sahne kurulumunun parcasi yapiyorum.
        ///
        /// Bunlar Inspector'dan tiklanabilir seyler ama proje iki makine arasinda
        /// tasiniyor ve tiklamalarin bir kismi tasinmiyor. Daha kotusu: kenar
        /// yumusatma kapaliyken oyun "biraz kaba" gorunuyor ve sebebini aramak
        /// icin once akla kod geliyor, ayar dosyasi degil.
        ///
        /// Projedeki butun URP varliklarina uygulaniyor: hangi kalite seviyesinin
        /// hangi platformda secildigi ayri bir ayar ve birini atlamak "PC'de
        /// duzgun, telefonda tirtikli" gibi acikcasi bulmasi zor bir fark yaratir.
        /// </summary>
        static void ApplyRenderSettings()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);

                if (asset == null)
                {
                    continue;
                }

                var serialized = new SerializedObject(asset);

                // Sahne birkac yuz ucgenden ibaret; MSAA'nin bedeli burada
                // neredeyse yok, kazanci ise dogrudan gorunuyor: kutu kenarlari
                // duz renk oldugu icin merdivenlenme en cok orada okunuyor.
                serialized.FindProperty("m_MSAA").intValue = 4;

                // Mobil profilinde yumusak golge kapali geliyordu; sert golge
                // pastel yonde yamalik gibi duruyor.
                serialized.FindProperty("m_SoftShadowsSupported").boolValue = true;

                // Golge mesafesi kucultuldu: ayni golge haritasi daha dar bir
                // alana dagildigi icin golgeler keskinlesiyor. Oyun alani zaten
                // otuz birim genisliginde.
                serialized.FindProperty("m_ShadowDistance").floatValue = 28f;

                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(asset);
            }

            AssetDatabase.SaveAssets();
        }

        [MenuItem("PhysicsStack/Paleti Yeniden Kur")]
        public static void RebuildPalette()
        {
            if (!Application.isBatchMode &&
                !EditorUtility.DisplayDialog(
                    "Paleti yeniden kur",
                    "Bütün renkler koddaki varsayılanlara döner. Elle yaptığın renk ayarları kaybolur.",
                    "Kur",
                    "Vazgeç"))
            {
                return;
            }

            var palette = LoadOrCreatePalette();

            // Taze bir örnek koddaki varsayılanları taşıyor; onu varlığın üstüne
            // kopyalamak, varsayılanları ikinci bir yerde tekrar yazmaktan iyi.
            var defaults = ScriptableObject.CreateInstance<Palette>();
            EditorUtility.CopySerialized(defaults, palette);
            Object.DestroyImmediate(defaults);

            // CopySerialized varlığın **adını da** kopyalıyor ve taze örneğin adı
            // boş. Adsız kalan bir ana varlığı AssetDatabase artık bulamıyor:
            // sahnedeki bütün palet referansları sessizce boşa düşüyor, oyun da
            // varsayılan renklerle çalışıyor. Hiçbir yerde hata görünmüyor,
            // sadece renkler yanlış — bulması en pahalı hata türü.
            palette.name = System.IO.Path.GetFileNameWithoutExtension(PalettePath);

            EditorUtility.SetDirty(palette);
            AssetDatabase.SaveAssets();
            Debug.Log("[SceneBootstrap] Palet koddaki varsayılanlara döndürüldü.");
        }

        static Palette LoadOrCreatePalette()
        {
            var palette = AssetDatabase.LoadAssetAtPath<Palette>(PalettePath);

            if (palette == null)
            {
                palette = ScriptableObject.CreateInstance<Palette>();
                AssetDatabase.CreateAsset(palette, PalettePath);
            }
            else if (string.IsNullOrEmpty(palette.name))
            {
                // Eski bir hatadan kalma adsız varlığı onar.
                palette.name = System.IO.Path.GetFileNameWithoutExtension(PalettePath);
                EditorUtility.SetDirty(palette);
            }

            return palette;
        }

        static void SetColor(Object target, string propertyName, Color value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).colorValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Referansi yazar ve yazildigini dogrular.
        ///
        /// Dogrulama sonradan eklendi: gecersizlesmis bir varlik nesnesi
        /// atandiginda Unity hata vermiyor, alani sessizce bos birakiyor. Sahne
        /// kuruluyor, derleme temiz, log sessiz - ve oyun yanlis calisiyor.
        /// Bulmasi en pahali hata turu bu, o yuzden artik bagiriyor.
        /// </summary>
        /// <summary>
        /// Serileştirilmiş bir sayı alanını yazar. Referans yazan kardeşiyle
        /// aynı sebeple var: alan <c>private</c> ve öyle kalmalı — sırf kurulum
        /// betiği yazabilsin diye <c>public</c> yapmak, çalışma zamanı kodunun
        /// arayüzünü kurulum aracına göre şekillendirmek olurdu.
        /// </summary>
        static void SetValue(Object target, string propertyName, float value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(propertyName);

            if (property == null)
            {
                Debug.LogError($"[SceneBootstrap] {target.GetType().Name} uzerinde '{propertyName}' alani yok.");
                return;
            }

            if (property.propertyType == SerializedPropertyType.Integer)
            {
                property.intValue = Mathf.RoundToInt(value);
            }
            else
            {
                property.floatValue = value;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetReference(Object target, string propertyName, Object value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(propertyName);

            if (property == null)
            {
                Debug.LogError($"[SceneBootstrap] {target.GetType().Name} uzerinde '{propertyName}' alani yok.");
                return;
            }

            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            if (value != null && new SerializedObject(target).FindProperty(propertyName).objectReferenceValue == null)
            {
                Debug.LogError($"[SceneBootstrap] {target.GetType().Name}.{propertyName} referansi yazilamadi: kaynak nesne gecersiz.");
            }
        }

        static Renderer CreateTargetLine(Material material, float height)
        {
            // Hedef yüksekliği görünmeden oynamak, kaç kutu kaldığını sayarak
            // oynamak demek. İnce bir çizgi bunu tek bakışta çözüyor.
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "TargetLine";
            go.transform.position = new Vector3(0f, height, 0f);
            go.transform.localScale = new Vector3(30f, 0.09f, 0.09f);

            // Collider'ı yok: kulenin ona çarpması saçma olurdu.
            Object.DestroyImmediate(go.GetComponent<Collider>());

            // Oyun bittiğinde rengi değişecek olan nesne bu; referansı dışarı veriyoruz.
            return MakeIndicator(go, material);
        }

        static GameObject CreateCannon(
            Material cannonMaterial,
            GameObject ballPrefab,
            StackGameController controller,
            StackTracker tracker,
            ImpactEffects effects,
            BoxQueue queue,
            int index,
            float sideX)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = $"Cannon{index}";
            go.transform.localScale = new Vector3(0.55f, 0.45f, 0.45f);

            // Collider yok: mermi namludan çıkarken kendi gövdesine çarpıp
            // anında yok olurdu, ve namlunun kuleye fiziksel olarak dokunması
            // zaten istenmeyen bir şey.
            Object.DestroyImmediate(go.GetComponent<Collider>());

            // Namlu havada duruyor; gölgesi kulenin üstüne düşünce oyuncu onu
            // yanlışlıkla bir nesne sanıyor. Gölge almaya devam ediyor, düşürmüyor.
            var cannonRenderer = go.GetComponent<MeshRenderer>();
            cannonRenderer.sharedMaterial = cannonMaterial;
            cannonRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var cannon = go.AddComponent<Cannon>();
            SetReference(cannon, "controller", controller);
            SetReference(cannon, "tracker", tracker);
            SetReference(cannon, "ballPrefab", ballPrefab);
            SetReference(cannon, "effects", effects);
            SetReference(cannon, "queue", queue);
            SetReference(cannon, "body", go.GetComponent<MeshRenderer>());
            SetValue(cannon, "index", index);
            SetValue(cannon, "sideX", sideX);

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
            go.transform.localScale = new Vector3(30f, 0.06f, 0.06f);

            Object.DestroyImmediate(go.GetComponent<Collider>());

            return MakeIndicator(go, material);
        }

        /// <summary>
        /// Malzeme yoksa üretiliyor, varsa rengi güncelleniyor.
        ///
        /// Diğer varlıklarda (his ayarları, seviyeler) kural "varsa dokunma"ydı,
        /// çünkü onlar oynayarak ayarlanan şeyler. Malzeme öyle değil: rengi
        /// paletten türetiliyor, yani elle ayarlanmış bir değeri yok. Dokunmasaydım
        /// palet değişince malzeme eski rengiyle kalırdı.
        /// </summary>
        /// <summary>
        /// Göstergelerin malzemesi: ışıksız.
        ///
        /// Çizgiler ve rüzgâr oku dünyanın nesneleri değil, oyuncuya bilgi veren
        /// işaretler. Işık alan bir gösterge sahnenin aydınlatmasına göre renk
        /// değiştiriyor ve "şu an yeşil mi sarı mı" sorusunu belirsizleştiriyor.
        /// Işıksız malzeme rengi olduğu gibi gösteriyor, üstelik gölge de almıyor.
        /// </summary>
        /// <summary>
        /// Carpma tozu. Doku yok: parcaciklar kucuk kareler olarak ciziliyor ve
        /// bu, oyunun geometrik diline zaten uyuyor. Bir toz dokusu eklemek hem
        /// varlik hem de "hangi yumusaklikta olmali" diye ayarlanacak bir sey
        /// daha demekti.
        ///
        /// Tek sistem uretiliyor ve her carpmada yerine tasinip Emit ediliyor.
        /// Carpma basina ayri sistem uretmek bir turda onlarca Instantiate/Destroy
        /// demek olurdu.
        /// </summary>
        /// <summary>
        /// Hiz cizgileri: kutu hizlandiginda arkasinda birakilan ince izler.
        ///
        /// Parcacik olarak degil, "gerilmis" olarak ciziliyorlar
        /// (<c>ParticleSystemRenderMode.Stretch</c>): parcacik kendi hizi yonunde
        /// uzatiliyor, yani cizgiyi hesaplamama gerek kalmiyor, hareket zaten
        /// uretiyor.
        ///
        /// Saydam malzeme gerekiyor - opak cizgiler hava degil enkaz gibi
        /// duruyor. URP'de saydamliga gecmek tek bir bayrak degil: yuzey tipi,
        /// harmanlama modu, derinlik yazimi ve render sirasi birlikte ayarlanmali.
        ///
        /// Cizgiler once beyazdi ve oynarken hic gorunmuyorlardi. Sebep esikleri
        /// ya da uretim hizi degildi: gokyuzu gradyani (220,233,242) ile
        /// (251,238,227) arasinda, yani neredeyse beyaz. Beyazin ustune beyaz
        /// ciziyordum. Rengi paletten koyu bir maviye almak, tek basina bir
        /// gorunurluk ayarindan daha cok fark etti - bu isin dersi su: bir efekt
        /// gorunmuyorsa once "yeterince mi uretiliyor" degil, "arkasindaki seyden
        /// ayirt ediliyor mu" diye sormak gerekiyor.
        /// </summary>
        static ParticleSystem CreateSpeedLines(Palette palette)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(StreakMaterialPath);

            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                AssetDatabase.CreateAsset(material, StreakMaterialPath);
            }

            // Malzeme duz beyaz ve OPAK. Ikisi de bilincli birer geri adim.
            //
            // Renk beyaz: URP'nin parcacik shader'i malzeme rengini parcacik
            // rengiyle CARPIYOR. Ikisine de paletin saydam rengini verdigimde
            // alfa iki kez uygulaniyordu (0.43 x 0.43 = 0.19) ve cizgiler %19
            // opaklikta ciziliyordu. Renk ve saydamlik artik tek yerden geliyor.
            //
            // Malzemenin ALFASI 1 ve bu bilincli. Saydamlik yalnizca parcacigin
            // renginden geliyor; ikisine birden saydam deger vermek alfayi iki
            // kez uygulamak demek (0.43 x 0.43 = 0.19) ve cizgiler bir kez bu
            // yuzden neredeyse gorunmez olmustu.
            material.color = Color.white;

            // Saydam yuzey. Bir ara buradan vazgecip malzemeyi opak yapmistim:
            // cizgiler gorunmuyordu ve dogrulamadigim bu bes ayardan suphelendim.
            // Sebep bu degilmis (gerilmis parcacigin hizsiz birakilmasiymis) ve
            // opak cizgiler siyahimsi, sert duruyordu - hava degil cubuk gibi.
            // Saydamlik geri geldi; artik efektin calistigi bilindigi icin bu
            // ayarlar da ilk kez gercekten dogrulanabilir durumda.
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

            // Arka yuz kirpma kapali. Kamera'ya bakan sade bir billboard her
            // zaman on yuzunu gosteriyor, ama gerilmis parcacik hiz yonune gore
            // donuyor ve bazi acilarda arkasi kameraya gelebiliyor; kirpma
            // aciksa o parcacik hic cizilmiyor. Tozun bu sorunu yok, cunku o
            // gerilmiyor.
            material.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);

            // Dokusuz parcacik duz bir dikdortgen olarak ciziliyor. Gerilmis
            // haldeyken bu, cizgi degil ince bir cubuk gibi duruyordu -
            // kenarlari keskin, iki ucu duz kesik. Doku yalnizca alfa tasiyor:
            // ortada parlak, iki uca ve iki kenara dogru sonuyor. Efektin
            // "cizgi" hissi buradan geliyor, geometriden degil.
            var texture = CreateStreakTexture();
            material.mainTexture = texture;
            material.SetTexture("_BaseMap", texture);
            EditorUtility.SetDirty(material);

            var go = new GameObject("SpeedLines", typeof(ParticleSystem));
            var system = go.GetComponent<ParticleSystem>();

            var main = system.main;
            main.loop = true;
            main.playOnAwake = true;
            // Omur uzatildi: 0.16 sn'lik bir iz, 60 fps'te on kare demek ve o
            // sure icinde goz onu bir cizgi olarak degil bir titreme olarak
            // aliyor.
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.26f, 0.42f);

            // Parcaciklar asagi dogru, kutudan yavas gidiyor. Kutu 5-8 m/s ile
            // duserken izler 2-4 m/s ile indigi icin geride kaliyorlar - iz
            // birakma etkisi bundan geliyor, ayri bir "kuyruk" koduna gerek yok.
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 4f);

            // Baslangic boyutu cizginin KALINLIGI (uzunluk asagida lengthScale
            // ile veriliyor). Once 0.08-0.14'tu; doku gelince kalinlik gercekten
            // gorunur oldu ve o degerler cizgiyi degil seridi cizdiriyordu.
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.09f);
            main.startColor = palette.speedLine;
            main.gravityModifier = 0f;
            main.maxParticles = 200;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = system.emission;
            emission.rateOverTime = 0f;

            // Kutunun etrafinda genis bir bant: cizgiler kutunun icinden degil
            // yanindan geciyormus gibi gorunsun.
            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Box;

            // Sekil 90 derece dondurulmus: parcacik sistemi her zaman seklin
            // +Z yonune firlatiyor ve dondurulmemis halde o yon kameraya
            // dogruydu ("bana geliyor" goruntusunun sebebi buydu). X ekseninde
            // 90 derece cevirince +Z dunya -Y'ye, yani asagi bakiyor.
            //
            // Yonu duzeltmenin yolu hizi sifirlamak DEGIL. Bir kez oyle yaptim
            // ve cizgiler tamamen kayboldu: gerilmis parcacik kendi hiz vektoru
            // boyunca uzatilarak ciziliyor, hiz sifir olunca uzatilacak yon
            // kalmiyor. Toz sisteminde bu sorun yok cunku o gerilmiyor.
            shape.rotation = new Vector3(90f, 0f, 0f);

            // Olcekler dondurulmus eksende veriliyor: yerel X dunya X'te kaliyor,
            // yerel Y dunya Z'ye, yerel Z dunya Y'ye gidiyor. Yani bu deger
            // dunyada 2.1 genis, 0.2 yuksek, 0.3 derin bir bant demek.
            shape.scale = new Vector3(2.1f, 0.3f, 0.2f);

            // Yayilim kutunun ONUNE aliniyor (kamera -Z tarafinda). Kutunun
            // merkezinde dogan parcacigin yarisi, 1 birim derinligindeki opak
            // kutunun ICINDE kaliyordu ve hicbir zaman cizilmiyordu. Efekt
            // caliyordu, sadece gorunmuyordu - bir parcacik sistemini ayarlarken
            // once "uretiliyor mu" degil, "cizildigi yer gorunur mu" diye
            // bakmak gerekiyormus.
            // Ayni eksen takasi konum icin de gecerli: dunyada kameraya dogru
            // (-Z) kaydirmak istiyorum, o da yerel -Y oluyor.
            shape.position = new Vector3(0f, -0.75f, 0f);

            // Omur boyu hiz modulu kapali. Yonu bir ara buradan vermistim ve
            // ise yaramadi: gerilmis parcacigin uzatildigi yon parcacigin
            // baslangic hizindan geliyor, bu modulden degil. Tek bir mekanizma
            // (baslangic hizi + dondurulmus sekil) hem yonu hem uzunlugu
            // veriyor; ikinci bir modul sadece nereye bakacagimi karistirdi.
            var velocity = system.velocityOverLifetime;
            velocity.enabled = false;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 0.25f;

            // Uzunluk carpani buyudu (4.5 -> 7): hem kalinlik yariya indigi icin
            // ayni oran ancak boyle korunuyor, hem de dokunun iki ucu sondugu
            // icin gorunen uzunluk cizilen uzunluktan kisa.
            renderer.lengthScale = 7f;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            return system;
        }

        /// <summary>
        /// Hiz cizgisinin dokusunu uretir ve PNG olarak diske yazar.
        ///
        /// Neden dosya degil de kod: projede sanatci yok ve elle cizilmis 8 KB'lik
        /// bir PNG'nin deposunda ne isi oldugunu birkac ay sonra kimse
        /// hatirlamaz. Formul burada durunca "cizgi neden boyle goruunuyor"
        /// sorusunun cevabi da kodda oluyor. Dosya, formulun ciktisi.
        ///
        /// Neden yine de dosyaya yaziliyor: malzeme bir varlik ve varliklar
        /// birbirine GUID ile baglaniyor. Calisma zamaninda uretilen bir
        /// Texture2D'yi malzemeye atayamam, cunku sahne kaydedildiginde
        /// referans bosa duser.
        ///
        /// Alfa iki carpandan olusuyor: uzunluk boyunca sinus (iki uc da
        /// sonuyor, boylece cizginin bas ve son noktasi duz kesilmis
        /// gorunmuyor) ve genislik boyunca kenara dogru dusen bir egri
        /// (kenarlar yumusak). Ustel degerler goze gore secildi: 0.75 uzun
        /// bir parlak bant birakiyor, 1.6 kenari cabuk soluyor.
        /// </summary>
        static Texture2D CreateStreakTexture()
        {
            // Uzun ve alcak: doku zaten tek yone geriliyor, dikeyde 16 piksel
            // yumusak bir kenar icin yeterli. Ikisi de ikinin kuvveti.
            const int width = 128;
            const int height = 16;

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color32[width * height];

            for (int y = 0; y < height; y++)
            {
                float v = (y + 0.5f) / height;
                float across = Mathf.Pow(1f - Mathf.Abs(v * 2f - 1f), 1.6f);

                for (int x = 0; x < width; x++)
                {
                    float u = (x + 0.5f) / width;
                    float along = Mathf.Pow(Mathf.Sin(u * Mathf.PI), 0.75f);

                    byte alpha = (byte)Mathf.RoundToInt(Mathf.Clamp01(along * across) * 255f);
                    pixels[y * width + x] = new Color32(255, 255, 255, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            string absolute = Path.Combine(Directory.GetCurrentDirectory(), StreakTexturePath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolute));
            File.WriteAllBytes(absolute, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);

            // Once Refresh, sonra ImportAsset: klasor az once diskte acildi ve
            // varlik veritabani onu henuz bilmiyor. Dogrudan ImportAsset
            // cagirinca "boyle bir yol yok" deyip sessizce gecistiriyor.
            AssetDatabase.Refresh();
            AssetDatabase.ImportAsset(StreakTexturePath, ImportAssetOptions.ForceUpdate);

            // Ice aktarma ayarlari elle veriliyor. Varsayilan sikistirma bu
            // dokunun tek tasidigi bilgiyi - yumusak alfa gecisini - bozuyor
            // ve cizginin kenarinda blok blok lekeler birakiyor. 8 KB'lik bir
            // doku icin sikistirmanin kazandiracagi bir sey de yok.
            if (AssetImporter.GetAtPath(StreakTexturePath) is TextureImporter importer)
            {
                importer.textureType = TextureImporterType.Default;
                importer.alphaIsTransparency = true;
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.mipmapEnabled = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(StreakTexturePath);
        }

        static ParticleSystem CreateDust(Palette palette)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(DustMaterialPath);

            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                AssetDatabase.CreateAsset(material, DustMaterialPath);
            }

            material.color = Color.white;
            EditorUtility.SetDirty(material);

            var go = new GameObject("Dust", typeof(ParticleSystem));
            var system = go.GetComponent<ParticleSystem>();

            var main = system.main;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.30f, 0.55f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.1f, 2.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.16f);
            main.startColor = palette.ground * 1.6f;
            main.gravityModifier = 0.8f;
            main.maxParticles = 300;

            // Dunya uzayinda: sistem carpma noktasina tasindiginda havadaki eski
            // parcaciklar da onunla birlikte gitmemeli.
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            // Kendiliginden hic parcacik uretmiyor; sistem sadece calisiyor ki
            // Emit ile atilanlar simule edilsin.
            var emission = system.emission;
            emission.rateOverTime = 0f;

            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.12f;

            // Kuculerek kayboluyor: aniden yok olan parcacik goze carpiyor.
            var sizeOverLifetime = system.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            return system;
        }

        static Material CreateUnlitMaterial(string path, Color color)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }

        /// <summary>
        /// Göstergeyi sahnenin ışık hesabından çıkarır: gölge düşürmüyor, gölge
        /// almıyor. Havada duran ince bir çizginin kulenin üstüne gölge düşürmesi
        /// bilgi değil gürültü.
        /// </summary>
        static Renderer MakeIndicator(GameObject go, Material material)
        {
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return renderer;
        }

        static Material CreateLitMaterial(string path, Color color)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;

            // Pastel yön mat yüzey istiyor: parlaklık yükseldikçe düz renk
            // kayboluyor ve her kutu ışığın rengine dönüyor.
            material.SetFloat("_Smoothness", 0.12f);
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
