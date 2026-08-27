namespace PhysicsStack
{
    /// <summary>
    /// Menüden seçilen tur, sahne yeniden yüklenirken taşınıyor.
    ///
    /// Neden sahneyi yeniden yüklüyorum da menüyü ve turu aynı sahnede yan yana
    /// çalıştırmıyorum: tur bittiğinde ortada onlarca rigidbody, bir kule ve
    /// yarım kalmış fizik durumu oluyor. Onları tek tek temizleyen bir kod
    /// yazmak, sahneyi yeniden yüklemekten hem uzun hem de her yeni nesne
    /// eklendiğinde güncellenmesi gereken bir borç. Sahne yeniden yüklenince
    /// fizik dünyası da sıfırdan kuruluyor.
    ///
    /// Statik alan sahne yüklemesinden sağ çıkıyor; taşınacak şey iki sayı
    /// olduğu için bundan fazlası gerekmiyor.
    /// </summary>
    public static class RunRequest
    {
        /// <summary>Bekleyen bir tur var mı? Yoksa sahne menüyle açılıyor.</summary>
        public static bool HasRequest { get; private set; }

        public static StackMode Mode { get; private set; }

        public static int LevelIndex { get; private set; }

        public static void Set(StackMode mode, int levelIndex)
        {
            HasRequest = true;
            Mode = mode;
            LevelIndex = levelIndex;
        }

        /// <summary>Menüye dönüldüğünde çağrılıyor: sahne bir sonraki açılışta menü gösterir.</summary>
        public static void Clear()
        {
            HasRequest = false;
        }
    }
}
