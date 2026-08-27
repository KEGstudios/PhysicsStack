using UnityEngine;

namespace PhysicsStack
{
    /// <summary>
    /// Seviyelerin sırası ve sonsuz modun hangi seviyede açıldığı.
    ///
    /// Sıra bir dizide duruyor, seviyelerin içinde numara olarak değil: seviyeyi
    /// araya sokmak ya da yerini değiştirmek diziyi düzenlemek kadar kolay olsun
    /// istiyorum. Seviyenin kendisi kaçıncı olduğunu bilmiyor.
    /// </summary>
    [CreateAssetMenu(menuName = "PhysicsStack/Level Library", fileName = "LevelLibrary")]
    public sealed class LevelLibrary : ScriptableObject
    {
        [SerializeField] LevelDefinition[] levels;

        [Tooltip("Bu seviye bitince sonsuz mod açılır. 0 tabanlı; 7 = 8. seviye.")]
        [SerializeField] int endlessUnlockIndex = 7;

        public int Count => levels != null ? levels.Length : 0;

        /// <summary>Sonsuz modu açan seviyenin sırası (0 tabanlı).</summary>
        public int EndlessUnlockIndex => endlessUnlockIndex;

        /// <summary>
        /// Sıradaki seviye. İndeks taşarsa kırpılıyor: eksik veriyle çöken bir
        /// oyun yerine son seviyeyi tekrar oynatan bir oyun daha az kötü.
        /// </summary>
        public LevelDefinition Get(int index) =>
            Count == 0 ? null : levels[Mathf.Clamp(index, 0, Count - 1)];
    }
}
