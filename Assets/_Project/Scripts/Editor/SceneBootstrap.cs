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
        const string BoxMaterialPath = "Assets/_Project/Art/Materials/M_Box.mat";
        const string GroundMaterialPath = "Assets/_Project/Art/Materials/M_Ground.mat";

        [MenuItem("PhysicsStack/Sahneyi Kur (Gun 1)")]
        public static void Build()
        {
            var boxMaterial = CreateLitMaterial(BoxMaterialPath, new Color(0.62f, 0.62f, 0.62f));
            var groundMaterial = CreateLitMaterial(GroundMaterialPath, new Color(0.30f, 0.30f, 0.32f));

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateCamera();
            CreateLight();
            CreateGround(groundMaterial);
            CreateBoxPrefab(boxMaterial);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[SceneBootstrap] Sahne kuruldu: {ScenePath}");
        }

        static void CreateCamera()
        {
            var go = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            go.tag = "MainCamera";

            // Kule yukarı doğru büyüyor. Kamera hafif yukarıdan ve hafif aşağı bakıyor:
            // hem zemin hem de hedef yükseklik aynı anda kadrajda kalsın diye.
            go.transform.SetPositionAndRotation(new Vector3(0f, 4f, -12f), Quaternion.Euler(8f, 0f, 0f));

            var camera = go.GetComponent<Camera>();
            camera.fieldOfView = 55f;
            camera.backgroundColor = new Color(0.16f, 0.17f, 0.19f);
            camera.clearFlags = CameraClearFlags.SolidColor;
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

        static void CreateBoxPrefab(Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Box";
            go.GetComponent<MeshRenderer>().sharedMaterial = material;

            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 1f;

            // Fizik sabit adımda çalışıyor, çizim kare hızında. Interpolate olmadan
            // hızlı sürüklenen kutu titriyor.
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            // Hız tabanlı sürüklemede kutu tek FixedUpdate'te kendi boyu kadar yol
            // alabiliyor. Discrete çarpışma bu adımı ıskalayıp kutunun zeminden
            // veya yığından geçmesine yol açıyor.
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            PrefabUtility.SaveAsPrefabAsset(go, BoxPrefabPath);
            Object.DestroyImmediate(go);
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
