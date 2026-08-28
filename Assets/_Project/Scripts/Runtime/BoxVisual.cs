using UnityEngine;

namespace PhysicsStack
{
    /// <summary>
    /// Kutunun gorsel govdesi: carpma aninda ezilip geri aciliyor.
    ///
    /// **Neden ayri bir cocuk nesne:** olcegi rigidbody'nin kendisinde oynatmak
    /// collider'i da oynatir, yani gorsel bir suslemeyle fizigi degistirmis
    /// olurduk - kule kendi kendine sallanmaya baslardi. Mesh ayri bir nesnede
    /// durunca ezilme tamamen goze ait kaliyor: carpismalar, yukseklik olcumu ve
    /// devrilme hesabi hicbir sey hissetmiyor.
    ///
    /// **Neden sonumlu sinus:** tek yonlu bir ezilip acilma "lastik" gibi
    /// duruyor. Genligi zamanla azalan bir salinim, sert bir cismin carpma
    /// aninda kisa sureli titremesini taklit ediyor ve gozun bekledigi sey bu.
    ///
    /// Hacim korunuyor: y'de ezilirken x ve z buyuyor. Sadece y'yi kisaltmak
    /// kutuyu bir an icin kucultuyor ve carpma degil, uzaklasma gibi okunuyor.
    /// </summary>
    public sealed class BoxVisual : MonoBehaviour
    {
        [Tooltip("En sert carpmada ulasilan ezilme orani.")]
        [SerializeField] float maxSquash = 0.34f;

        [Tooltip("Bu hizdaki carpma tam ezilme veriyor (m/s).")]
        [SerializeField] float speedForMaxSquash = 5.5f;

        [Tooltip("Salinimin sonumlenme suresi (sn).")]
        [SerializeField] float duration = 0.32f;

        [Tooltip("Salinimin tur sayisi. 1 civari tek bir ezilip acilma veriyor; yuksek degerler titreme gibi okunuyor.")]
        [SerializeField] float oscillations = 1f;

        [Header("Dususte uzama")]
        [Tooltip("En hizli dususte ulasilan uzama orani.")]
        [SerializeField] float maxStretch = 0.22f;

        [Tooltip("Bu dikey hizda tam uzama olur (m/s).")]
        [SerializeField] float speedForMaxStretch = 8f;

        [Tooltip("Uzamanin hedefe yetisme hizi (oran/sn). Ani degisim goze carpiyor.")]
        [SerializeField] float stretchResponse = 4f;

        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        [Tooltip("Donan kutunun renk carpani. 1 = renk degismez.")]
        [SerializeField] float solidTint = 0.82f;

        Vector3 baseScale;
        Rigidbody body;
        DraggableBody draggable;
        MaterialPropertyBlock block;

        float amplitude;
        float time;
        float stretch;

        void Awake()
        {
            baseScale = transform.localScale;

            // Hiz bilgisi kok nesnede; gorsel govde onun cocugu.
            body = GetComponentInParent<Rigidbody>();
            draggable = GetComponentInParent<DraggableBody>();
        }

        /// <summary>Carpma siddetine gore ezilmeyi baslatir.</summary>
        public void Impact(float speed)
        {
            float strength = Mathf.Clamp01(speed / speedForMaxSquash);

            // Zaten devam eden bir ezilme varsa daha sertini kazaniyor; ust uste
            // binen kucuk carpmalar goruntuyu titretmesin diye.
            amplitude = Mathf.Max(amplitude, strength * maxSquash);
            time = 0f;

            // Uzama aninda sifirlaniyor: dususten carpmaya gecis ne kadar keskin
            // olursa carpma o kadar sert okunuyor.
            stretch = 0f;
        }

        /// <summary>
        /// Kutu bir kontrol noktasinda donduruldu: bir kez ezilip aciliyor ve
        /// rengi hafifce koyuluyor.
        ///
        /// Renk kalici olan kisim ve asil is onda: ses ile ezilme "az once bir
        /// sey oldu" diyor, renk ise "bu kutular artik sabit" demeye devam
        /// ediyor. Yalnizca anlik bir geri bildirim verseydim oyuncu birkac kutu
        /// sonra hangi kismin donmus oldugunu bilemezdi.
        ///
        /// Ton carpimi seciliyor, ayri bir "donmus" rengi degil: paletteki her
        /// kutu kendi renginde kaliyor, yalnizca bir basamak koyuluyor. Tek bir
        /// gri renk verseydim kule renklerini kaybeder ve donmus kisim oyunun
        /// disindan gelmis gibi dururdu.
        /// </summary>
        public void Solidify()
        {
            Impact(speedForMaxSquash * 0.5f);

            if (!TryGetComponent(out Renderer renderer))
            {
                return;
            }

            block ??= new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);

            var color = block.GetColor(BaseColor);

            // Blok'ta renk hic yazilmamissa GetColor saydam siyah donuyor; onu
            // 0.82 ile carpmak kutuyu gorunmez yapardi.
            if (color.a <= 0f)
            {
                return;
            }

            block.SetColor(BaseColor, new Color(
                color.r * solidTint,
                color.g * solidTint,
                color.b * solidTint,
                color.a));

            renderer.SetPropertyBlock(block);
        }

        void Update()
        {
            // Ezilme ve uzama ayni eksende ama ters yonde calisiyor: biri pozitif
            // basiyor, digeri cekiyor. Tek bir sayida toplamak ikisinin ayni anda
            // olmasini da dogal olarak cozuyor.
            float amount = Squash() - Stretch();

            transform.localScale = new Vector3(
                baseScale.x * (1f + amount * 0.5f),
                baseScale.y * (1f - amount),
                baseScale.z * (1f + amount * 0.5f));
        }

        float Squash()
        {
            if (amplitude <= 0f)
            {
                return 0f;
            }

            time += Time.deltaTime;

            if (time >= duration)
            {
                amplitude = 0f;
                return 0f;
            }

            float progress = time / duration;
            return amplitude * (1f - progress) * Mathf.Sin(progress * Mathf.PI * 2f * oscillations);
        }

        /// <summary>
        /// Dususte uzama: hizli inen kutu inceliyor ve uzuyor.
        ///
        /// Ezilmenin eksik yarisi buydu. Ezilme tek basina carpmayi anlatiyor ama
        /// oncesindeki dusus "hicbir sey olmuyor" gibi duruyordu; uzama, kutunun
        /// hizlandigini carpmadan once gosteriyor.
        ///
        /// Surukleme sirasinda uygulanmiyor: orada hiz parmagin hizi ve kutunun
        /// elde incelmesi hareketi degil, kontrolu bulanik gosteriyor.
        /// </summary>
        float Stretch()
        {
            float target = 0f;

            if (body != null && !body.isKinematic && (draggable == null || !draggable.IsDragged))
            {
                float speed = Mathf.Clamp01(Mathf.Abs(body.linearVelocity.y) / speedForMaxStretch);

                // Uzama kutunun kendi y ekseninde yapiliyor, cunku olcek her zaman
                // yerel. Kutu takla atinca o eksen dikey olmaktan cikiyor ve uzama
                // hareketle alakasiz bir yone gidiyor - mermi carpip kutuyu
                // dondurdugunde ortaya cikan garip goruntu buydu.
                //
                // Cozum uzamayi dunya dikeyine cevirmek degil: o, mesh'i de
                // dondurmek ya da ayri bir deformasyon yazmak demek. Bunun yerine
                // eksen dikeyden uzaklastikca uzama soneliyor. Donen kutuda
                // uzamanin zaten anlatacagi bir sey yok.
                float alignment = Mathf.Abs(Vector3.Dot(transform.up, Vector3.up));

                target = speed * alignment * alignment * maxStretch;
            }

            stretch = Mathf.MoveTowards(stretch, target, stretchResponse * Time.deltaTime);
            return stretch;
        }
    }
}
