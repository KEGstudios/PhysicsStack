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

        float speed;
        float response;
        float period;

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

        public bool Active => speed > 0f;

        void Start()
        {
            var hazards = controller.Rules.Hazards;
            speed = hazards.windSpeed;
            period = hazards.windPeriod;
            response = hazards.windResponse > 0f ? hazards.windResponse : 3f;
        }

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
            if (!Active)
            {
                return 0f;
            }

            return period > 0f
                ? speed * Mathf.Sin(time * 2f * Mathf.PI / period)
                : speed;
        }
    }
}
