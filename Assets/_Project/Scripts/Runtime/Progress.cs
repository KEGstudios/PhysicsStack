using UnityEngine;

namespace PhysicsStack
{
    /// <summary>
    /// Oyuncunun ilerlemesi. Üç sayı: kaçıncı seviyeye kadar açtı, sonsuz modun
    /// en iyi skoru, ve bir de geliştirici kilidi.
    ///
    /// <c>PlayerPrefs</c> kullanmamın sebebi saklanacak şeyin üç sayı olması.
    /// Kendi dosya formatımı tasarlamak, JSON yazmak ya da kayıt sürümlemesi
    /// düşünmek burada tamamen israf olurdu; PlayerPrefs her platformda çalışıyor
    /// ve WebGL'de tarayıcı deposuna yazıyor.
    ///
    /// Statik sınıf, MonoBehaviour değil: kaydın sahnede bir nesnesi yok, sahne
    /// yeniden yüklendiğinde de kaybolmaması gerekiyor.
    /// </summary>
    public static class Progress
    {
        const string UnlockedKey = "physicsstack.unlocked";
        const string EndlessBestKey = "physicsstack.endlessbest";

        /// <summary>
        /// Açılmış en yüksek seviye indeksi (0 tabanlı). Sıfır: sadece ilk seviye açık.
        /// </summary>
        public static int UnlockedLevel
        {
            get => PlayerPrefs.GetInt(UnlockedKey, 0);
            private set
            {
                PlayerPrefs.SetInt(UnlockedKey, value);
                PlayerPrefs.Save();
            }
        }

        /// <summary>Sonsuz modda ulaşılan en yüksek kule.</summary>
        public static float EndlessBest
        {
            get => PlayerPrefs.GetFloat(EndlessBestKey, 0f);
            private set
            {
                PlayerPrefs.SetFloat(EndlessBestKey, value);
                PlayerPrefs.Save();
            }
        }

        /// <summary>
        /// Kilitleri yok sayan geliştirici bayrağı. Kayda yazılmıyor, oyun her
        /// açıldığında kapalı başlıyor: yanlışlıkla açık kalan bir hile bayrağı,
        /// test ettiğim şeyin gerçek oyun olmadığı anlamına gelir.
        /// </summary>
        public static bool UnlockEverything { get; set; }

        public static bool IsLevelUnlocked(int index) =>
            UnlockEverything || index <= UnlockedLevel;

        public static bool IsEndlessUnlocked(int unlockIndex) =>
            UnlockEverything || UnlockedLevel > unlockIndex;

        /// <summary>Seviye geçildi: bir sonraki seviyeyi açar. Geri almaz.</summary>
        public static void CompleteLevel(int index)
        {
            if (index + 1 > UnlockedLevel)
            {
                UnlockedLevel = index + 1;
            }
        }

        public static void ReportEndless(float height)
        {
            if (height > EndlessBest)
            {
                EndlessBest = height;
            }
        }

        /// <summary>Test için: kaydı siler.</summary>
        public static void Clear()
        {
            PlayerPrefs.DeleteKey(UnlockedKey);
            PlayerPrefs.DeleteKey(EndlessBestKey);
            PlayerPrefs.Save();
        }
    }
}
