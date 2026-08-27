using UnityEngine;

namespace PhysicsStack
{
    /// <summary>
    /// Rüzgârı ekranda gösterir: kadrajın üstünde, merkezden esme yönüne doğru
    /// uzayan bir çubuk. Uzunluğu şiddeti, yönü de yönü veriyor.
    ///
    /// Buna ihtiyaç oynarken çıktı — rüzgârlı seviyede kutu savruluyordu ama
    /// ortada rüzgâr olduğunu söyleyen hiçbir şey yoktu. Görünmeyen bir kuvvet
    /// zorluk değil, kafa karışıklığı üretiyor: oyuncu kendi hatasını arıyor.
    /// Tehdidin adil olması görülebilir olmasından geçiyor; top atıcıda bu
    /// kendiliğinden var, rüzgârda ayrıca yapmak gerekiyor.
    ///
    /// Neden dünya nesnesi, neden arayüz değil: sahnedeki diğer iki çizgi de
    /// (hedef ve bırakma) böyle çalışıyor. Aynı dili konuşan üç gösterge,
    /// yarısı Canvas'ta duran bir arayüzden daha okunur.
    /// </summary>
    public sealed class WindIndicator : MonoBehaviour
    {
        [SerializeField] Wind wind;
        [SerializeField] StackCamera stackCamera;
        [SerializeField] Renderer bar;

        [Tooltip("Çubuğun ucundaki eşkenar dörtgen: yönü tek bakışta okutuyor.")]
        [SerializeField] Renderer head;

        [Tooltip("Birim kuvvet başına çubuk uzunluğu.")]
        [SerializeField] float unitsPerForce = 1.2f;

        [Tooltip("Çubuk kadrajın üst kenarının bu kadar altında durur.")]
        [SerializeField] float marginFromTop = 0.8f;

        [SerializeField] float thickness = 0.16f;

        [SerializeField] Color color = new(0.80f, 0.60f, 0.30f);

        MaterialPropertyBlock block;

        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        void Awake()
        {
            block = new MaterialPropertyBlock();

            Tint(bar);
            Tint(head);
        }

        void Tint(Renderer renderer)
        {
            if (renderer == null)
            {
                return;
            }

            renderer.GetPropertyBlock(block);
            block.SetColor(BaseColor, color);
            renderer.SetPropertyBlock(block);
        }

        void LateUpdate()
        {
            bool visible = wind != null && wind.Active;

            bar.enabled = visible;

            if (head != null)
            {
                head.enabled = visible;
            }

            if (!visible)
            {
                return;
            }

            float force = wind.CurrentForce;

            // Salınan rüzgârda kuvvet sıfırdan geçiyor; çubuk o anda görünmez
            // olacak kadar kısalıyor ve bu doğru bilgi: yön değiştiriyor.
            // Ölçeği tam sıfıra indirmemek için küçük bir taban bırakıyorum.
            float length = Mathf.Max(Mathf.Abs(force) * unitsPerForce, 0.02f);
            float y = stackCamera != null ? stackCamera.FrameTopY - marginFromTop : 0f;

            bar.transform.localScale = new Vector3(length, thickness, thickness);

            // Çubuk merkezden dışarı doğru büyüyor: konumu uzunluğun yarısı kadar
            // kaydırınca sol ucu daima x = 0'da kalıyor ve yön tek bakışta okunuyor.
            float direction = Mathf.Sign(force);
            bar.transform.position = new Vector3(direction * length * 0.5f, y, 0f);

            if (head != null)
            {
                head.transform.position = new Vector3(direction * length, y, 0f);
            }
        }
    }
}
