using UnityEditor;
using UnityEngine;

namespace PhysicsStack.EditorTools
{
    /// <summary>
    /// Seviye varlıklarını ve kütüphaneyi üretir.
    ///
    /// Seviyeleri elle oluşturmak yerine koddan üretiyorum çünkü eğri bir bütün:
    /// sekiz seviyeyi tek bir tabloda yan yana görmek, sekiz ayrı Inspector
    /// penceresinde görmekten çok daha iyi. Tablo üretimden sonra kaybolmuyor,
    /// aşağıda duruyor — "5. seviyede ne değişiyordu" sorusunun cevabı burada.
    ///
    /// Üretim varolan varlığın üstüne yazmıyor: oynayarak ayarladığım sayıları
    /// sahneyi tazelemek çöpe atmamalı. Eğriyi baştan kurmak istersem ayrı bir
    /// menü var, o da soruyor.
    /// </summary>
    public static class LevelBootstrap
    {
        const string DataFolder = "Assets/_Project/Data";
        const string LevelFolder = DataFolder + "/Levels";
        const string LibraryPath = DataFolder + "/LevelLibrary.asset";

        /// <summary>Sonsuz modu açan seviye (0 tabanlı): sekizinci.</summary>
        const int EndlessUnlockIndex = 7;

        /// <summary>
        /// Hedefi geçtikten sonra tutunması gereken süre. Bütün seviyelerde aynı:
        /// bu bir zorluk kolu değil, oyunun kuralı. Seviyeye göre değişseydi
        /// oyuncunun her seviyede yeniden öğrenmesi gerekirdi.
        /// </summary>
        const float HoldTime = 1.5f;

        /// <summary>
        /// Zorluk eğrisi. Büyüyen şey bilerek kutu sayısı değil: hedef yükseklik
        /// 3'ten 6'ya çıkıp orada duruyor, asıl artan şey bırakma mesafesi —
        /// yani kutunun kendi başına kat ettiği yol.
        ///
        /// Sıra da bilinçli: önce sadece mesafe (1-3), sonra kutu sınırı (4),
        /// sonra genişlik oynaması (5). Her yeni kısıt tek başına bir seviye
        /// tanıtılıyor, sonrakiler birleştiriyor.
        ///
        /// Mesafeler bir kez yükseltildi: ilk hâlde 1.0'dan başlıyordu ve oynayınca
        /// "çok yakın, zorlanmıyorum" çıktı. Bir birim, bir kutu boyu kadar düşüş
        /// demek — kutu daha hızlanmadan yerine oturuyordu.
        /// </summary>
        static readonly (string Title, float Target, int Limit, float Gap, float WidthVariance)[] Curve =
        {
            ("Seviye 1", 3f, 0, 2.00f, 0f),
            ("Seviye 2", 4f, 0, 2.50f, 0f),
            ("Seviye 3", 4f, 0, 3.00f, 0f),
            ("Seviye 4", 5f, 6, 3.00f, 0f),
            ("Seviye 5", 5f, 0, 3.50f, 0.15f),
            ("Seviye 6", 5f, 7, 3.50f, 0.15f),
            ("Seviye 7", 6f, 0, 4.00f, 0.25f),
            ("Seviye 8", 6f, 8, 4.00f, 0.30f),
        };

        [MenuItem("PhysicsStack/Seviyeleri Yeniden Kur")]
        public static void Rebuild()
        {
            if (!Application.isBatchMode &&
                !EditorUtility.DisplayDialog(
                    "Seviyeleri yeniden kur",
                    "Sekiz seviyenin bütün değerleri koddaki eğriye döner. Oynayarak yaptığın ayarlar kaybolur.",
                    "Kur",
                    "Vazgeç"))
            {
                return;
            }

            Create(overwrite: true);
            Debug.Log("[LevelBootstrap] Seviyeler koddaki eğriye döndürüldü.");
        }

        /// <summary>Kütüphaneyi verir; yoksa seviyelerle birlikte üretir.</summary>
        public static LevelLibrary LoadOrCreate() => Create(overwrite: false);

        static LevelLibrary Create(bool overwrite)
        {
            EnsureFolder(DataFolder);
            EnsureFolder(LevelFolder);

            var levels = new LevelDefinition[Curve.Length];

            for (int i = 0; i < Curve.Length; i++)
            {
                string path = $"{LevelFolder}/Level{i + 1:00}.asset";
                var level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(path);
                bool isNew = level == null;

                if (isNew)
                {
                    level = ScriptableObject.CreateInstance<LevelDefinition>();
                    AssetDatabase.CreateAsset(level, path);
                }

                if (isNew || overwrite)
                {
                    Apply(level, Curve[i]);
                    EditorUtility.SetDirty(level);
                }

                levels[i] = level;
            }

            var library = AssetDatabase.LoadAssetAtPath<LevelLibrary>(LibraryPath);

            if (library == null)
            {
                library = ScriptableObject.CreateInstance<LevelLibrary>();
                AssetDatabase.CreateAsset(library, LibraryPath);
            }

            // Sıra ve kilit her zaman yazılıyor: bunlar ayarlanacak değil
            // yapısal değerler, ve eksik bir dizi sessizce oynanamaz bir oyun
            // demek. Seviyelerin içindeki sayılar ise korunuyor.
            WriteLibrary(library, levels);

            AssetDatabase.SaveAssets();
            return library;
        }

        static void Apply(LevelDefinition level, (string Title, float Target, int Limit, float Gap, float WidthVariance) row)
        {
            level.title = row.Title;
            level.targetHeight = row.Target;
            level.boxLimit = row.Limit;
            level.dropGap = row.Gap;
            level.widthVariance = row.WidthVariance;
            level.holdTime = HoldTime;
        }

        static void WriteLibrary(LevelLibrary library, LevelDefinition[] levels)
        {
            var serialized = new SerializedObject(library);

            var array = serialized.FindProperty("levels");
            array.arraySize = levels.Length;

            for (int i = 0; i < levels.Length; i++)
            {
                array.GetArrayElementAtIndex(i).objectReferenceValue = levels[i];
            }

            serialized.FindProperty("endlessUnlockIndex").intValue = EndlessUnlockIndex;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            int split = path.LastIndexOf('/');
            AssetDatabase.CreateFolder(path[..split], path[(split + 1)..]);
        }
    }
}
