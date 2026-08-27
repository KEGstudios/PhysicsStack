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

        [Tooltip("Kulenin oturmuş hâlde geçmesi gereken yükseklik.")]
        public float targetHeight = 4f;

        /// <summary>
        /// Üç yıldız için gereken kutu sayısı: hedef yükseklik + 1.
        ///
        /// Türetiliyor, alan olarak tutulmuyor. Kutular sabit 1 birim yüksekliğinde
        /// olduğu için hedefe ulaşmanın teorik alt sınırı zaten hedef yüksekliği
        /// kadar kutu; üstüne bir kutu pay veriyorum çünkü kutular oturduğunda
        /// aralarında küçük bir temas payı kalıyor ve tam sınırda kalan bir kule
        /// hedefi ıskalayabiliyor. Yani üç yıldız "kusursuz istifle" demek değil,
        /// "bir kutuluk israfla" demek.
        ///
        /// Ayrı bir alan olsaydı hedef yüksekliği değiştirdiğimde onu güncellemeyi
        /// unutabilirdim ve yıldız eşiği sessizce yanlış kalırdı.
        /// </summary>
        public int StarBoxes => Mathf.CeilToInt(targetHeight) + 1;

        /// <summary>
        /// Turun kaybedildiği kutu sayısı. Üç yıldızın iki fazlası son şans;
        /// üç fazlasını atmak kayıp.
        ///
        /// Kutu sınırı eskiden seviye başına elle verilen bir sayıydı. Yıldız
        /// sistemi gelince ikisi aynı şeyi ölçmeye başladı — elle verilen sınır
        /// yıldız eşikleriyle çelişebilirdi, mesela sınır üç yıldız sayısının
        /// altında kalsaydı üç yıldız alınamayan bir seviye ortaya çıkardı.
        /// </summary>
        public int BoxLimit => StarBoxes + 2;

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

        [Tooltip("Seviyenin çevresel tehditleri. Hepsi yalnızca havadaki kutuya dokunur.")]
        public HazardSettings hazards;

        /// <summary>
        /// Seviyenin kaç kutuyla geçildiğine göre yıldız: hedefte üç, her fazla
        /// kutuda bir eksik.
        ///
        /// Kutu sayısını yıldıza çeviren yer burası, arayüz değil. Menü, tur
        /// sonu ekranı ve pop-up aynı soruyu soruyor; üçüne ayrı hesap yazmak
        /// birinin diğerinden farklı cevap verdiği bir hatayı davet ederdi.
        /// </summary>
        public int StarsFor(int boxesUsed)
        {
            if (boxesUsed <= 0)
            {
                return 0;
            }

            // Hedeften az kutuyla geçmek mümkün (kutular eğik oturunca kule
            // beklenenden yüksek çıkabiliyor); üç yıldızın üstü yok.
            return Mathf.Clamp(3 - (boxesUsed - StarBoxes), 0, 3);
        }

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
                float height = Mathf.InverseLerp(3f, 6f, targetHeight);

                float raw =
                    gap * 2f +
                    height * 1.5f +
                    (hazards.windSpeed > 0f ? 1.2f : 0f) +
                    (hazards.cannon ? 1.6f : 0f);

                return Mathf.Clamp(1 + Mathf.RoundToInt(raw / 5.3f * 4f), 1, 5);
            }
        }

        /// <summary>Seviyedeki tehdidin adı; yoksa boş.</summary>
        public string HazardLabel =>
            hazards.cannon ? "top atıcı" :
            hazards.windSpeed > 0f ? "rüzgâr" :
            string.Empty;
    }
}
