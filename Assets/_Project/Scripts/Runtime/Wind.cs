using UnityEngine;

namespace PhysicsStack
{
    /// <summary>
    /// Havadaki kutuyu yatay olarak iter.
    ///
    /// Yalnızca **bırakılmış ve henüz oturmamış** kutuya dokunuyor. Duran kuleye
    /// de esseydi oyuncu hiçbir hata yapmadan kaybedebilirdi; bu ceza değil
    /// haksızlık olurdu. Sürükleme sırasında da uygulamıyorum: takip zaten hızı
    /// doğrudan atıyor, rüzgâr orada sadece görünmez bir sürtünme gibi hissedilirdi.
    ///
    /// Kuvvet <c>ForceMode.Acceleration</c> ile veriliyor, yani kütleden bağımsız.
    /// Fiziksel olarak rüzgâr yüzey alanıyla iter — geniş kutu daha çok sürüklenmeli.
    /// Ama bizim geniş kutumuz aynı zamanda ağır, ve iki etki birbirini götürünce
    /// ortaya "bazı kutular neden daha çok savruluyor" diye açıklanamayan bir
    /// düzensizlik çıkıyordu. Kütleden bağımsız olması öngörülebilir, öngörülebilir
    /// olan da öğrenilebilir.
    /// </summary>
    public sealed class Wind : MonoBehaviour
    {
        [SerializeField] StackGameController controller;
        [SerializeField] BoxQueue queue;

        /// <summary>
        /// O anki tehdit ayarı. Eskiden <c>Start</c>'ta bir kez okunup
        /// saklanıyordu; sonsuz modda rüzgâr tur içinde başlayıp şiddetlendiği
        /// için bu yanlış oldu. Her karede sormak birkaç float kopyalamak
        /// demek — "değişti mi" diye bir haber mekanizması kurmaktan hem ucuz
        /// hem de unutulacak bir adımı az.
        /// </summary>
        HazardSettings Hazards => controller != null && controller.Rules != null
            ? controller.Hazards
            : HazardSettings.None;

        /// <summary>
        /// O anki rüzgâr. İşareti yönü, büyüklüğü şiddeti veriyor.
        ///
        /// **Ortam değeri**: kutu havada olmasa da hesaplanıyor. İlk hâlinde
        /// yalnızca kuvvet uygulanırken doluyordu, yani gösterge tam da bakılması
        /// gereken anda — kutu bırakılmadan önce — sıfır gösteriyordu. Rüzgârı
        /// atıştan sonra görmenin hiçbir değeri yok; atışı ona göre ayarlamak için
        /// önce görmek gerekiyor.
        /// </summary>
        public float CurrentForce { get; private set; }

        public bool Active => Hazards.windSpeed > 0f;

        void Update()
        {
            CurrentForce = Ambient(Time.time);
        }

        void FixedUpdate()
        {
            if (!Active)
            {
                return;
            }

            var box = queue.Current;

            // CanGrab hâlâ true ise kutu bırakılmamış demek; yere değdiyse de iş bitti.
            // Rüzgâr esmeye devam ediyor, sadece dokunacak bir şey bulamıyor.
            if (box == null || box.CanGrab || box.IsDragged || box.HasLanded)
            {
                return;
            }

            // Rüzgâr kutuyu kendi hızına doğru itiyor, sonsuza kadar hızlandırmıyor.
            // İlk hâlinde sabit ivme uyguluyordum ve kutunun yatay hızı düşüş
            // boyunca büyümeye devam ediyordu; iniş anında o hız kutuyu devirmeye
            // yetiyordu. Bağıl hıza orantılı kuvvet hem fiziksel olarak doğrusu
            // (rüzgâr hareket eden hava, sabit bir itiş değil) hem de iniş hızına
            // bir tavan koyuyor: kutu rüzgârın hızını geçemiyor.
            float response = Hazards.windResponse > 0f ? Hazards.windResponse : 3f;

            float relative = Ambient(Time.fixedTime) - box.Body.linearVelocity.x;
            box.Body.AddForce(Vector3.right * relative * response, ForceMode.Acceleration);
        }

        /// <summary>
        /// Rüzgârın o anki hızı. Periyot sıfırsa sabit yönlü: oyuncu bir kez
        /// öğrenip telafi ediyor. Periyot varsa yön salınıyor ve telafi
        /// zamanlamaya bağlanıyor — oyuncu doğru anı beklemek zorunda.
        /// </summary>
        float Ambient(float time)
        {
            var hazards = Hazards;

            if (hazards.windSpeed <= 0f)
            {
                return 0f;
            }

            // Faz mutlak zamandan geliyor, biriktirilmiyor. Sonsuz modda periyot
            // tur ortasında açıldığında rüzgâr bu yüzden sinüsün ortasından
            // devam ediyor, sıfırdan değil — yani bir anlık sıçrama oluyor. O an
            // havada kutu olmadığı için (tehditler ancak yeni kutu istenirken
            // tazeleniyor) bunu kabul ettim; fazı biriktirmek iki yerde ayrı
            // ayrı ilerletmek demekti ve gösterge ile kuvvetin ayrışması,
            // sıçramadan daha kötü bir hata olurdu.
            return hazards.windPeriod > 0f
                ? hazards.windSpeed * Mathf.Sin(time * 2f * Mathf.PI / hazards.windPeriod)
                : hazards.windSpeed;
        }
    }
}
