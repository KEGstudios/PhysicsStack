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
        const string MutedKey = "physicsstack.muted";

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
        /// İlerlemeyle aynı yerde tutuyorum çünkü ikisi de aynı soruya cevap
        /// veriyor: "oyuncu bu oyunu daha önce açtığında ne yapmıştı".
        /// Ayrı bir ayar sınıfı açmak tek bir bayrak için fazla olurdu.
        ///
        /// PlayerPrefs bool tutmuyor, int tutuyor; dönüşüm burada kapalı
        /// kalsın ki çağıran taraf 0/1 ile uğraşmasın.
        /// </summary>
        public static bool Muted
        {
            get => PlayerPrefs.GetInt(MutedKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(MutedKey, value ? 1 : 0);
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
