using UnityEngine;

namespace PhysicsStack
{
    /// <summary>
    /// Sürükleme hissini belirleyen sayılar. Prefab'ın içinde değil ayrı bir
    /// varlıkta duruyorlar, çünkü:
    ///
    /// 1. Play mode'da yapılan değişiklikler kayboluyor — ama ScriptableObject
    ///    varlığına yapılanlar kalıyor. His ayarı ancak oynarken yapılabilen bir
    ///    şey; her denemede oyunu durdurup değeri yeniden girmek zorunda kalmak
    ///    ayarın kendisini engelliyordu.
    /// 2. Değerler tek yerde: sahnedeki her kutu aynı varlığa bakıyor, "hangi
    ///    kutuda hangi değer kalmış" sorusu ortadan kalkıyor.
    /// 3. İleride "ağır kutu / hafif kutu" gibi bir varyant gerekirse ikinci bir
    ///    varlık oluşturup prefab'a vermek yetiyor, kod değişmiyor.
    /// </summary>
    [CreateAssetMenu(menuName = "PhysicsStack/Drag Settings", fileName = "DragSettings")]
    public sealed class DragSettings : ScriptableObject
    {
        [Header("Takip")]
        [Tooltip("1 = parmağa tam yetişmeye çalışır, 0.2 = ağır ve gecikmeli hisseder.")]
        [Range(0.05f, 1f)] public float followStrength = 0.35f;

        [Tooltip("Kutunun ulaşabileceği en yüksek hız (m/s). Ağırlık hissinin çoğu buradan geliyor.")]
        public float maxSpeed = 10f;

        [Tooltip("Hızın bir saniyede değişebileceği miktar (m/s²). Kutunun kuleye vurduğunda ne kadar sert ittiğini bu belirliyor.")]
        public float maxAcceleration = 90f;

        [Header("Bırakma")]
        [Tooltip("Bırakma anında hız bu değerin üstündeyse buraya kırpılır. Fırlatma kalsın ama kuleyi süpürmesin diye.")]
        public float releaseSpeedClamp = 5f;

        [Tooltip("Bırakılan kutunun hava sürtünmesi (1/sn). Düşüş hızına tavan koyuyor: 0 = serbest düşüş.")]
        public float fallDrag = 1.2f;
    }
}
