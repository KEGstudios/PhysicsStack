using TMPro;
using UnityEditor;
using UnityEngine;

namespace PhysicsStack.EditorTools
{
    /// <summary>
    /// TextMeshPro'nun temel kaynaklarini projeye alir.
    ///
    /// Normalde bu Unity menusunden elle yapilan bir adim. Koddan cagirmamin
    /// sebebi projenin kurallarindan biri: sahne ve varliklar tek komutla
    /// kurulabilmeli. Elle bir adim eklemek, projeyi baska bir makineye tasiyan
    /// birinin "neden yazilar bozuk" diye arayacagi bir tuzak birakmak demek.
    ///
    /// Batchmode'da bir incelik var: <c>AssetDatabase.ImportPackage</c> asenkron
    /// calisiyor. Unity'yi <c>-quit</c> ile calistirinca import bitmeden cikiyor
    /// ve hicbir sey olmuyor - hata da vermiyor, ki en kotusu bu. O yuzden burada
    /// <c>-quit</c> kullanilmiyor; cikis, import tamamlandi callback'inde
    /// yapiliyor.
    /// </summary>
    public static class TextResources
    {
        [MenuItem("PhysicsStack/Yazi Kaynaklarini Iceri Aktar")]
        public static void Import()
        {
            AssetDatabase.importPackageCompleted += OnCompleted;
            AssetDatabase.importPackageFailed += OnFailed;
            AssetDatabase.importPackageCancelled += OnCancelled;

            TMP_PackageResourceImporter.ImportResources(true, false, false);
        }

        static void OnCompleted(string packageName)
        {
            Debug.Log($"[TextResources] Iceri aktarildi: {packageName}");
            Finish(0);
        }

        static void OnFailed(string packageName, string error)
        {
            Debug.LogError($"[TextResources] Basarisiz: {packageName} - {error}");
            Finish(1);
        }

        static void OnCancelled(string packageName)
        {
            Debug.LogError($"[TextResources] Iptal edildi: {packageName}");
            Finish(1);
        }

        static void Finish(int exitCode)
        {
            AssetDatabase.importPackageCompleted -= OnCompleted;
            AssetDatabase.importPackageFailed -= OnFailed;
            AssetDatabase.importPackageCancelled -= OnCancelled;

            AssetDatabase.Refresh();

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(exitCode);
            }
        }
    }
}
