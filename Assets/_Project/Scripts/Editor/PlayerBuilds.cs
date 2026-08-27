using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace PhysicsStack.EditorTools
{
    /// <summary>
    /// Oynanabilir çıktıları üretir: Android APK ve WebGL.
    ///
    /// Neden script? Build ayarları Inspector'dan tıklayarak da yapılabilir, ama
    /// bu proje iki makine arasında Git ile taşınıyor ve tıklamaların bir kısmı
    /// taşınmıyor (aktif build target, çıktı yolu). Ayarlar burada durunca
    /// build'in nasıl alındığı repoda yazılı oluyor ve her makinede aynı çıktı
    /// çıkıyor.
    ///
    /// Komut satırından:
    /// Unity.exe -batchmode -quit -projectPath . -buildTarget Android
    ///           -executeMethod PhysicsStack.EditorTools.PlayerBuilds.BuildAndroid
    /// </summary>
    public static class PlayerBuilds
    {
        const string ScenePath = "Assets/_Project/Scenes/Main.unity";

        [MenuItem("PhysicsStack/Build/Android APK")]
        public static void BuildAndroid()
        {
            // IL2CPP + ARM64: Mono ARM64 desteklemiyor ve modern telefonların bir
            // kısmı 32-bit çalıştırmıyor. Build süresi uzuyor (ilk seferde C++
            // runtime da derleniyor) ama telefonda çalışmayan bir APK'yı hızlı
            // almanın değeri yok.
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            // Sideload için APK; AAB Play Store'a yükleme formatı, burada işe yaramaz.
            EditorUserBuildSettings.buildAppBundle = false;

            // Unity 6 artik en dusuk API 26 (Android 8.0) istiyor; 24 yazmak
            // gelecek surumde derleme hatasi olacakti.
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.bundleVersionCode = 1;
            PlayerSettings.bundleVersion = "1.0";

            Run(BuildTarget.Android, BuildTargetGroup.Android, "Build/Android/PhysicsStack.apk");
        }

        [MenuItem("PhysicsStack/Build/WebGL")]
        public static void BuildWebGL()
        {
            // Asıl hedef hâlâ mobil; WebGL onun yerine geçmiyor, ona ulaşmanın
            // yolu oluyor. Elimdeki telefon iPhone ve iOS build'i macOS istiyor —
            // tarayıcı build'i ise iPhone'da Safari'de açılıp dokunmatik girdiyi
            // gerçekten alıyor. Yani "parmakla nasıl hissettiriyor" sorusunu
            // Windows'tan çıkmadan gerçek cihazda cevaplayabiliyorum. Yan faydası:
            // portföy için indirilip kurulan bir dosya yerine tıklanır bir link.
            //
            // Brotli + decompressionFallback: GitHub Pages sıkıştırılmış dosyaları
            // doğru başlıkla sunmuyor, tarayıcı da açamıyor. Fallback açıkken
            // Unity'nin yükleyicisi dosyayı kendisi çözüyor — statik sunucuda
            // çalışmasının tek dürüst yolu bu.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.decompressionFallback = true;

            // Prototipte istisna yakalamaya ihtiyaç yok ama tamamen kapatmak
            // telefonda çıkan bir hatayı görünmez yapardı; varsayılan seviye kalıyor.
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;

            Run(BuildTarget.WebGL, BuildTargetGroup.WebGL, "Build/WebGL");
        }

        static void Run(BuildTarget target, BuildTargetGroup group, string output)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(output));

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = output,
                target = target,
                targetGroup = group,
                options = BuildOptions.None,
            };

            BuildSummary summary = BuildPipeline.BuildPlayer(options).summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[PlayerBuilds] {target} başarılı: {output} · " +
                          $"{SizeOnDisk(output):0.0} MB · {summary.totalTime.TotalMinutes:0.0} dk");
                return;
            }

            // Batchmode'da BuildPlayer başarısız olsa bile süreç 0 ile çıkabiliyor.
            // Hatanın sessizce geçmesindense build'in patlaması lazım: yeşil görünen
            // kırık bir build'den kötüsü yok.
            Debug.LogError($"[PlayerBuilds] {target} başarısız: {summary.result} · {summary.totalErrors} hata");

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(1);
            }
        }

        /// <summary>
        /// Boyutu diskten ölçüyoruz. <c>BuildSummary.totalSize</c> ara dosyaları da
        /// sayıyor ve 32 MB'lık bir APK için 667 MB yazdırdı — yanlış sayı,
        /// hiç sayı olmamasından kötü.
        /// </summary>
        static double SizeOnDisk(string path)
        {
            long bytes = File.Exists(path)
                ? new FileInfo(path).Length
                : Directory.Exists(path)
                    ? DirectorySize(new DirectoryInfo(path))
                    : 0L;

            return bytes / 1024d / 1024d;
        }

        /// <summary>
        /// Klasörün yayınlanacak boyutu.
        ///
        /// İki şey dışarıda bırakılıyor. Burst'ün <c>_DoNotShip</c> klasörünü
        /// Unity'nin kendisi "gönderme" diye adlandırıyor. <c>.git</c> ise WebGL
        /// çıktısının içinde duruyor, çünkü yayın o klasörden gh-pages dalına
        /// push edilerek yapılıyor — ve eski build'lerin nesneleri orada birikiyor.
        /// İkisini saymak, ölçtüğüm sayının "kullanıcının indireceği şey" olmasını
        /// engelliyordu: 23.6 MB yazıyordu, gerçekte sunulan 12.5 MB'tı.
        /// </summary>
        static long DirectorySize(DirectoryInfo directory)
        {
            long total = 0;

            foreach (var file in directory.GetFiles("*", SearchOption.AllDirectories))
            {
                if (IsExcluded(file.DirectoryName))
                {
                    continue;
                }

                total += file.Length;
            }

            return total;
        }

        static bool IsExcluded(string path) =>
            path != null &&
            (path.Contains("_DoNotShip") ||
             path.Contains($"{Path.DirectorySeparatorChar}.git"));
    }
}
