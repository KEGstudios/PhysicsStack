using UnityEngine;

namespace PhysicsStack
{
    /// <summary>
    /// Kare süresini ölçüp gerekirse grafik kalitesini bir kademe düşürür.
    ///
    /// Neden gerekti: beş cihazlık ölçümde bir telefon (Oppo Reno 2Z) yüksek
    /// ayarda 40-45 fps veriyor, düşük ayarda sabit 60. Yani çözüm zaten
    /// oyunun içinde duruyordu — ama oyuncunun ayarlar ekranına girip sorunu
    /// kendi teşhis etmesi gerekiyordu. Kare hızının düşük olduğunu fark eden
    /// bir oyuncunun "acaba grafik ayarı var mıdır" diye aramasını beklemek,
    /// çözümü olan bir sorunu oyuncuya bırakmak olur.
    ///
    /// Üç kural ölçümü güvenilir kılıyor:
    ///
    /// **Isınma süresi.** İlk kareler her zaman yavaş: shader derleniyor, TMP
    /// atlası doluyor, ses klipleri sentezleniyor. Bu projede aynı tuzağa bir
    /// kez düşüldü — tanıtım animasyonu ilk karenin süresi yüzünden görünmeden
    /// bitiyordu. Isınmayı ölçmek "her cihaz yavaş" derdi.
    ///
    /// **Takılmalar sayılmıyor.** Sekme arka plana alınıp geri gelince tek bir
    /// kare saniyelerce sürmüş görünüyor; çöp toplama da benzer bir sıçrama
    /// yapıyor. Bunlar yükün değil, kesintinin işareti — ortalamaya girselerdi
    /// tek bir sekme değişimi kaliteyi düşürürdü.
    ///
    /// **Ortalama değil, oran.** Ekran 60 Hz ve dikey senkron açık, yani kare
    /// süresi 16.7 ms'nin katlarına yuvarlanıyor: bir kare ya yetişiyor ya da
    /// bir sonrakini bekliyor. Bu yüzden ölçülen şey "ortalama kaç fps" değil,
    /// **kaç karede bir kaçırdık**. Aynı sayıyı daha doğrudan okuyor.
    ///
    /// Yalnızca aşağı iniyor. Yukarı çıkma denemesi kaliteyi bir yükseltip
    /// ölçüp tekrar düşürebilirdi ve oyuncu ortada sebepsiz yanıp sönen bir
    /// görüntü görürdü; kararlı bir alt kademe, doğru ama titreyen bir
    /// kademeden iyi.
    /// </summary>
    public sealed class AutoQuality : MonoBehaviour
    {
        [SerializeField] StackGameController controller;

        /// <summary>Ölçüme başlamadan önce beklenen süre. Açılış sıçramalarını dışarıda bırakıyor.</summary>
        const float WarmUp = 3f;

        /// <summary>Karar için toplanan pencere. Kısa pencere tek bir zor anı genelleştirirdi.</summary>
        const float Window = 4f;

        /// <summary>Bunun üstündeki kare "yavaş" sayılıyor (~47 fps).</summary>
        const float SlowFrame = 0.021f;

        /// <summary>Bunun üstündeki kare hiç sayılmıyor: yük değil, kesinti.</summary>
        const float Hitch = 0.2f;

        /// <summary>Kaçırılan kare oranı bunu geçerse kalite düşüyor.</summary>
        const float SlowShare = 0.25f;

        float elapsed;
        int frames;
        int slowFrames;

        void OnEnable()
        {
            Restart();
        }

        void Update()
        {
            // Oyuncu kaliteyi kendi seçtiyse ölçüm susuyor; en düşük kademedeyken
            // de inecek yer yok. İkisinde de bileşen kendini kapatıyor: her kare
            // aynı iki koşulu tekrar kontrol etmenin bir faydası yok.
            if (GameSettings.QualityChosen || GameSettings.Quality <= GameSettings.LowQuality)
            {
                enabled = false;
                return;
            }

            // Yalnızca tur sırasında ölçülüyor. Menü de tam ekran çiziyor, yani
            // ölçmek teknik olarak mümkündü; iki sebeple ölçmüyorum. Birincisi
            // ölçünün anlamı: karar verilen şey "oyun bu cihazda dönüyor mu" ve
            // menüde dönen bir oyun yok. İkincisi görünen bir yan etki — ayarlar
            // ekranı menüden de açılıyor ve kademe oyuncunun gözünün önünde
            // değişirse ekrandaki vurgu ile ayarın kendisi ayrışırdı.
            //
            // Duraklatma da aynı sebeple dışarıda: sahne donmuş, temsil ettiği
            // bir yük yok. Ayarlar oradan açılınca ölçüm zaten susuyor.
            if (Time.timeScale <= 0f || controller == null || controller.Rules == null)
            {
                return;
            }

            float delta = Time.unscaledDeltaTime;

            if (delta <= 0f || delta > Hitch)
            {
                return;
            }

            elapsed += delta;

            if (elapsed < WarmUp)
            {
                return;
            }

            frames++;

            if (delta > SlowFrame)
            {
                slowFrames++;
            }

            if (elapsed < WarmUp + Window)
            {
                return;
            }

            if (frames > 0 && (float)slowFrames / frames > SlowShare)
            {
                GameSettings.ApplyMeasuredQuality(GameSettings.Quality - 1);
            }

            // Pencere dolduğunda her hâlükârda sıfırlanıyor. Düştüyse yeni
            // kademe de yetmemiş olabilir ve bir aşağısı gerekebilir; düşmediyse
            // de ölçüm devam ediyor, çünkü oyunun yükü tur ilerledikçe artıyor —
            // menüde yetişen bir cihaz on üçüncü seviyede yetişmeyebilir.
            Restart();
        }

        void Restart()
        {
            elapsed = 0f;
            frames = 0;
            slowFrames = 0;
        }
    }
}
