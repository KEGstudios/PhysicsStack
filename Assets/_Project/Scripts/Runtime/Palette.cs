using UnityEngine;

namespace PhysicsStack
{
    /// <summary>
    /// Sahnenin bütün renkleri tek varlıkta.
    ///
    /// Dağıtılmış renkler bir prototipte hızlı ilerletiyor ama bir bütün olarak
    /// bakmayı imkânsızlaştırıyor: kutunun rengi prefab'da, zemin bootstrap'ta,
    /// çizgiler kendi bileşenlerinde duruyordu ve "bu palet çalışıyor mu"
    /// sorusunu ancak oyunu açıp bakarak cevaplayabiliyordum. Tek varlıkta
    /// toplanınca renk denemesi sahneyi yeniden kurmayı gerektirmiyor.
    ///
    /// Yön pastel ve düz renk. Sebebi estetik tercih kadar teknik: kutuların
    /// birbirinden ve zeminden ayrılması gerekiyor, pastel palette her kutu
    /// farklı ton alabiliyor. Koyu/neon yönde kenarlar kayboluyor, sıcak/gerçekçi
    /// yönde ise sonuç doku kalitesine bağlanıyor — sanatçısız en riskli olan o.
    /// </summary>
    [CreateAssetMenu(menuName = "PhysicsStack/Palette", fileName = "Palette")]
    public sealed class Palette : ScriptableObject
    {
        [Header("Kutular")]
        [Tooltip("Kutulara sırayla dağıtılır. Rastgele değil: rastgelede yan yana aynı renk gelebiliyor ve kaza gibi duruyor.")]
        public Color[] boxColors =
        {
            new Color32(242, 166, 154, 255), // mercan
            new Color32(245, 203, 142, 255), // şeftali
            new Color32(168, 213, 186, 255), // nane
            new Color32(156, 197, 224, 255), // gök
            new Color32(185, 167, 219, 255), // lavanta
            new Color32(239, 168, 196, 255), // gül
        };

        [Header("Sahne")]
        [Tooltip("Zemin. Gökyüzünden belirgin şekilde koyu olmalı, yoksa ikisi birbirine karışıyor.")]
        public Color ground = new Color32(74, 84, 96, 255);

        [Tooltip("Gökyüzü gradyanının üst rengi.")]
        public Color skyTop = new Color32(220, 233, 242, 255);

        [Tooltip("Gökyüzü gradyanının alt rengi.")]
        public Color skyBottom = new Color32(251, 238, 227, 255);

        [Header("Tehditler")]
        [Tooltip("Namlu: tehdit olduğu anlaşılsın diye paletin en koyu tonu.")]
        public Color cannon = new Color32(110, 106, 120, 255);

        [Tooltip("Mermi: pastelin içinde göze çarpması gereken tek şey.")]
        public Color ball = new Color32(217, 99, 74, 255);

        public Color wind = new Color32(224, 164, 88, 255);

        [Header("Arayüz")]
        [Tooltip("Menü ve tur sonu panelinin zemini.")]
        public Color uiPanel = new Color32(250, 246, 240, 245);

        public Color uiButton = new Color32(224, 214, 204, 255);
        public Color uiButtonLocked = new Color32(232, 228, 224, 255);
        public Color uiAccent = new Color32(168, 213, 186, 255);
        public Color uiText = new Color32(58, 58, 66, 255);
        public Color uiTextDim = new Color32(140, 138, 148, 255);

        [Header("Çizgiler")]
        public Color dropLine = new Color32(127, 166, 196, 255);
        public Color targetIdle = new Color32(185, 178, 170, 255);
        public Color targetHolding = new Color32(233, 185, 73, 255);
        public Color targetWon = new Color32(111, 191, 115, 255);
        public Color targetLost = new Color32(217, 83, 79, 255);

        /// <summary>
        /// Sıradaki kutunun rengi. Sıra numarasına göre dönüyor: rastgele seçim
        /// yan yana aynı rengi getirebiliyor ve o an palet bozuk değil, kod
        /// bozukmuş gibi görünüyor.
        /// </summary>
        public Color BoxColor(int index)
        {
            if (boxColors == null || boxColors.Length == 0)
            {
                return Color.white;
            }

            return boxColors[((index % boxColors.Length) + boxColors.Length) % boxColors.Length];
        }
    }
}
