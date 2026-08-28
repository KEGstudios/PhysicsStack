using UnityEngine;

namespace PhysicsStack
{
    /// <summary>
    /// Tek bir seviyenin verisi. Kod değişmeden yeni seviye eklenebilsin diye
    /// varlık dosyası.
    ///
    /// Seviyeyi tanımlayan şey "kaç kutu" değil "hangi soru": bırakma mesafesi,
    /// kutu sınırı ve boyut oynaması birlikte her seviyeye farklı bir karakter
    /// veriyor. Hedef yükseklik 8 seviye boyunca 3'ten 6'ya çıkıp orada kalıyor —
    /// büyüyen şeyin miktar olması, 100. seviyede 100 kutu demek olurdu.
    /// </summary>
    [CreateAssetMenu(menuName = "PhysicsStack/Level", fileName = "Level")]
    public sealed class LevelDefinition : ScriptableObject
    {
        [Tooltip("Ekranda görünecek ad.")]
        public string title = "Seviye";

        [Tooltip("Kulede olması gereken kutu sayısı. Kule oturmuş hâlde bu sayıya ulaşınca seviye geçiliyor.")]
        public int targetBoxes = 4;

        /// <summary>
        /// Kaç kutu düşürmeye izin var. Üçüncüsü turu bitiriyor.
        ///
        /// Sabit ve bütün seviyelerde aynı: bu bir zorluk kolu değil, oyunun
        /// kuralı. Seviyeye göre değişseydi oyuncunun her seviyede yeniden
        /// öğrenmesi gerekirdi — tutunma süresinde de aynı karar verilmişti.
        /// </summary>
        public const int MaxDrops = 3;

        /// <summary>
        /// Hedefin yükseklik karşılığı. Kutular sabit 1 birim olduğu için sayı
        /// aynı; hedef çizgisi ve kamera bunu okuyor.
        /// </summary>
        public float TargetHeight => targetBoxes;

        [Tooltip("Kutu, kule tepesinin en az bu kadar üstünden bırakılmak zorunda.")]
        public float dropGap = 1f;

        [Tooltip("Kutu, bırakma çizgisinin bu kadar üstünde belirir. Düşme mesafesine eklenmez; tehdit koridorunu uzatır.")]
        public float spawnLift;

        [Tooltip("Kutu genişliğinin oynama payı. Boy hep 1 birim. 0 = bütün kutular aynı.")]
        public float widthVariance;

        [Tooltip("Hedefi geçtikten sonra kulenin kıpırdamadan durması gereken süre (sn).")]
        public float holdTime = 1.5f;

        [Tooltip("Kule zirvesinin bu kadar altına düşerse çökmüş sayılır.")]
        public float collapseDrop = 0.6f;

        [Tooltip("Kaç birim yükseklikte bir kulenin altı dondurulsun? 0 = kontrol noktası yok. Yüksek kuleli seviyeler için.")]
        public float checkpointEvery;

        [Tooltip("Seviyenin çevresel tehditleri. Hepsi yalnızca havadaki kutuya dokunur.")]
        public HazardSettings hazards;

        /// <summary>
        /// Düşürülen kutu sayısına göre yıldız: hiç düşürmeden üç, her düşen
        /// kutuda bir eksik, üçüncüsünde tur biter.
        ///
        /// Yıldızın ölçtüğü şey değişti. Önce "kaç kutu harcadın" idi ve hedef
        /// yükseklik olduğu sürece anlamlıydı: eğri oturan kule aynı yüksekliğe
        /// çıkmak için fazladan kutu istiyordu. Hedef kutu sayısına dönünce o
        /// ölçü öldü — her tur tam hedef kadar kutuyla biterdi ve herkes hep üç
        /// yıldız alırdı. Yeni ölçü doğrudan hatayı sayıyor.
        /// </summary>
        public int StarsFor(int droppedBoxes) => Mathf.Clamp(3 - droppedBoxes, 0, 3);

        /// <summary>
        /// Kayıttaki kutu sayısından yıldız. Kayıt "kaç kutu kullandın" olarak
        /// tutuluyor (menüde gösterilen sayı o), düşen kutu sayısı da aradaki
        /// fark: hedefin üstünde harcanan her kutu bir düşmüş kutudur.
        /// </summary>
        public int StarsForBoxes(int boxesUsed) =>
            boxesUsed <= 0 ? 0 : StarsFor(boxesUsed - targetBoxes);

        /// <summary>
        /// Menüde gösterilen zorluk (1-5). Seviye verisinden türetiliyor.
        ///
        /// Elle yazılmış bir zorluk sayısı da olabilirdi ama o, seviye ayarını
        /// her değiştirdiğimde güncellenmesi gereken ikinci bir yer demekti —
        /// ve güncellenmediğinde kimse fark etmez, çünkü yanlış olduğunu ancak
        /// oynayan anlar. Buradaki ağırlıklar elle seçildi, ama en azından
        /// verinin kendisini okuyorlar.
        ///
        /// Aralıklar oyunun tasarım aralığı: bırakma mesafesi 2-4, hedef 3-6.
        /// Dışına çıkan bir seviye kırpılıyor, yani ölçek bozulmuyor.
        /// </summary>
        public int Difficulty
        {
            get
            {
                float gap = Mathf.InverseLerp(2f, 4f, dropGap);
                // Yükseklik aralığı 3-6'ydı; son üç seviye 7 ve 8'e çıkınca üst
            // sınır 8 oldu. Aksi hâlde 6'nın üstündeki her hedef aynı görünüyor
            // ve zorluk göstergesi tam da yeni açılan kolu ölçemiyordu.
            float height = Mathf.InverseLerp(3f, 8f, targetBoxes);

                float raw =
                    gap * 2f +
                    height * 1.5f +
                    (hazards.windSpeed > 0f ? 1.2f : 0f) +
                    hazards.cannonCount * 1.6f;

                return Mathf.Clamp(1 + Mathf.RoundToInt(raw / 5.3f * 4f), 1, 5);
            }
        }

        /// <summary>
        /// Seviyedeki tehditlerin adı; yoksa boş. İkisi birden varsa ikisi de
        /// yazılıyor: kartta yalnızca birini göstermek, seviyeyi olduğundan
        /// sade tanıtmak olurdu.
        /// </summary>
        public string HazardLabel
        {
            get
            {
                string cannon =
                    hazards.cannonCount >= 2 ? "iki top atıcı" :
                    hazards.cannonCount == 1 ? "top atıcı" :
                    string.Empty;

                string wind = hazards.windSpeed > 0f ? "rüzgâr" : string.Empty;

                if (cannon.Length > 0 && wind.Length > 0)
                {
                    return $"{wind} ve {cannon}";
                }

                return cannon.Length > 0 ? cannon : wind;
            }
        }
    }
}
