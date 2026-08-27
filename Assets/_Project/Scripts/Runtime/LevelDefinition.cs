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

        [Tooltip("İzin verilen kutu sayısı. 0 = sınırsız.")]
        public int boxLimit;

        [Tooltip("Kutu, kule tepesinin en az bu kadar üstünden bırakılmak zorunda.")]
        public float dropGap = 1f;

        [Tooltip("Kutu genişliğinin oynama payı. Boy hep 1 birim. 0 = bütün kutular aynı.")]
        public float widthVariance;

        [Tooltip("Hedefi geçtikten sonra kulenin kıpırdamadan durması gereken süre (sn).")]
        public float holdTime = 1.5f;

        [Tooltip("Kule zirvesinin bu kadar altına düşerse çökmüş sayılır.")]
        public float collapseDrop = 0.6f;
    }
}
