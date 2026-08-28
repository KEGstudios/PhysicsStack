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
        /// Ses kapalı mı.
        ///
        /// Buradaki tek satırlık "ses açık mı" bayrağı <see cref="GameSettings"/>'e
        /// taşındı ve seviye çubuğuna dönüştü. Gerekçesi de değişti: ses açık
        /// olup olmaması ilerlemenin değil tercihin parçası, ve "ilerlemeyi
        /// sıfırla" düğmesi tercihleri silmemeli.
        /// </summary>

        const string DevKey = "physicsstack.dev";

        /// <summary>Inspector'dan gelen, oturumluk geliştirici bayrağı.</summary>
        static bool sessionUnlock;

        /// <summary>
        /// Kilitleri yok sayan geliştirici bayrağı. İki kaynağı var: Inspector'daki
        /// kutu (oturumluk) ve menüdeki gizli jest (kalıcı).
        /// </summary>
        public static bool UnlockEverything
        {
            get => sessionUnlock || DevUnlock;
            set => sessionUnlock = value;
        }

        /// <summary>
        /// Menüdeki gizli jestle açılan kalıcı geliştirici modu.
        ///
        /// Kalıcı olması ilk yazdığım gerekçeye aykırı görünüyor: "yanlışlıkla
        /// açık kalan bir hile bayrağı, test ettiğim şeyin gerçek oyun olmadığı
        /// anlamına gelir." O itiraz hâlâ doğru ama sorun kalıcılık değil,
        /// **görünmezlik**miş. Tarayıcıda Inspector yok ve telefonda 13. seviyeyi
        /// test etmenin başka yolu da yok; bayrak açıkken menüde bunu söyleyen
        /// bir satır duruyor, yani hangi oyunu test ettiğim her zaman ekranda
        /// yazıyor.
        /// </summary>
        public static bool DevUnlock
        {
            get => PlayerPrefs.GetInt(DevKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(DevKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static bool IsLevelUnlocked(int index) =>
            UnlockEverything || index <= UnlockedLevel;

        public static bool IsEndlessUnlocked(int unlockIndex) =>
            UnlockEverything || UnlockedLevel > unlockIndex;

        /// <summary>
        /// Seviyenin en iyi derecesi: o seviyeyi geçmek için kullanılan en az
        /// kutu sayısı. Sıfır, seviyenin hiç geçilmediği anlamına geliyor.
        ///
        /// Neden "en az kutu": seviyenin hedefi sabit bir yükseklik, yani
        /// "daha yükseğe çık" diye bir yarış yok. Geriye kalan tek anlamlı
        /// ölçü, hedefe kaç kutuyla ulaştığın — bu da doğrudan yerleştirme
        /// isabetini ölçüyor. Süre tutmayı düşündüm ama eledim: bu oyunda
        /// acele etmek her zaman kötü oynamak demek, dolayısıyla süreyi
        /// ödüllendirmek mekaniğin tersine çalışırdı.
        ///
        /// Sonsuz modun tersi yönde çalışması (orada yüksek olan iyi, burada
        /// düşük olan) kafa karıştırıcı değil çünkü ikisi aynı ekranda hiç
        /// yan yana gelmiyor ve ikisi de kendi biriminde yazılıyor.
        /// </summary>
        public static int LevelBest(int index) =>
            PlayerPrefs.GetInt(LevelBestKey(index), 0);

        static string LevelBestKey(int index) => $"physicsstack.best.{index}";

        /// <summary>Seviye geçildi: bir sonraki seviyeyi açar. Geri almaz.</summary>
        public static void CompleteLevel(int index, int boxesUsed)
        {
            if (index + 1 > UnlockedLevel)
            {
                UnlockedLevel = index + 1;
            }

            int previous = LevelBest(index);

            // Sıfır "derece yok" demek olduğu için ilk geçiş her zaman yazılıyor;
            // sonrasında yalnızca daha azı. Karşılaştırmayı ters yazmak, ilk
            // dereceyi hiç kaydetmemek gibi sessiz bir hataya yol açardı.
            if (boxesUsed > 0 && (previous == 0 || boxesUsed < previous))
            {
                PlayerPrefs.SetInt(LevelBestKey(index), boxesUsed);
                PlayerPrefs.Save();
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

            // Geliştirici modu da siliniyor: "ilerlemeyi sıfırla" dedikten sonra
            // bütün seviyelerin açık kalması, sıfırlamanın işe yaramadığını
            // düşündürür.
            PlayerPrefs.DeleteKey(DevKey);

            // Seviye dereceleri ayrı anahtarlarda; silme döngüsünün üst sınırı
            // seviye sayısından bağımsız olarak geniş tutuldu. "Kaç seviye var"
            // bilgisini buraya taşımak, seviye eklendiğinde güncellenmesi
            // unutulacak ikinci bir yer açardı.
            for (int i = 0; i < 64; i++)
            {
                PlayerPrefs.DeleteKey(LevelBestKey(i));
            }

            PlayerPrefs.Save();
        }
    }
}
