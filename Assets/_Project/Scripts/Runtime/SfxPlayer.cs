using System.Collections.Generic;
using UnityEngine;

namespace PhysicsStack
{
    /// <summary>
    /// Ses çalma katmanı: klipleri açılışta üretir, bir <see cref="AudioSource"/>
    /// havuzu üzerinden çalar.
    ///
    /// Neden statik erişim: ses tetikleyen yerler sahnede sabit değil. Kutular
    /// ve mermiler tur içinde üretiliyor, menü ve tur sonu ekranları da
    /// tamamen çalışma anında kuruluyor. Her birine SerializeField ile referans
    /// geçirmek, sırf ses çalabilmek için üç ayrı sınıfa referans taşıma kodu
    /// yazmak demekti. Tekil erişim burada gerçekten daha az kod.
    ///
    /// Bunun bedelini bilerek ödüyorum: statik durum test edilebilirliği
    /// düşürüyor. Bedeli sınırlı tutan şey <see cref="Play"/>'in null
    /// kontrolü — oyuncu yoksa ses yok, ama hiçbir şey patlamıyor. Ses
    /// olmadığında oyunun oynanabilirliği değişmiyor, dolayısıyla sessizce
    /// çalışmaması kabul edilebilir bir başarısızlık.
    ///
    /// Neden havuz: tek bir AudioSource'ta iki ses üst üste bindiğinde ikincisi
    /// birincisini kesiyor. Kule çökerken onlarca çarpma aynı anda olabiliyor;
    /// tek kaynakla bunların hepsi tek bir "tık"a iniyordu.
    /// </summary>
    public sealed class SfxPlayer : MonoBehaviour
    {
        public static SfxPlayer Instance { get; private set; }

        [Tooltip("Bütün seslerin ortak çarpanı.")]
        [SerializeField, Range(0f, 1f)] float masterVolume = 0.75f;

        [Tooltip("Aynı anda çalabilecek ses sayısı.")]
        [SerializeField] int voiceCount = 10;

        AudioSource[] voices;
        int nextVoice;

        Dictionary<Sfx, AudioClip> clips;
        Dictionary<Sfx, float> levels;

        void Awake()
        {
            // Sahne yeniden yüklendiğinde yeni bir oyuncu daha kuruluyor ve
            // eskisi hâlâ ayakta. İkincisi kendini siliyor: aksi halde her ses
            // iki kez çalar ve bu, ses seviyesinin iki katına çıkması olarak
            // duyulur.
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            // Sahneler arasında yaşıyor. İki sebep var:
            //
            // Birincisi, menüden seviyeye geçiş sahneyi yeniden yüklemek demek.
            // Düğmeye basma sesi tam o anda çalıyor ve sahneyle birlikte
            // silinseydi hiç duyulmazdı — dokunuşun sesi, dokunuşun sonucundan
            // önce kesilirdi.
            //
            // İkincisi, klipler açılışta üretiliyor. Her sahne yüklemesinde
            // yeniden üretmek, oyuncunun her seviye başlangıcında bekleyeceği
            // gereksiz bir iş olurdu.
            DontDestroyOnLoad(gameObject);

            Build();
            BuildVoices();
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// Klipleri üretir. Toplam süre bir saniyenin altında olduğu için
        /// hepsini açılışta üretmek birkaç milisaniye sürüyor; tembel üretim
        /// (ilk çalındığında üret) sesin ilk kez tetiklendiği karede takılma
        /// yaratırdı ve o kare tam da çarpma karesi olurdu.
        /// </summary>
        void Build()
        {
            clips = BuildLibrary();

            // Klipler normalize edilmiş halde geliyor, yani hepsinin tepe
            // genliği aynı. Dengeyi burada kuruyorum: normalizasyon "hepsi eşit
            // yüksek" demek, oysa çöküş sesinin kutu inişinden daha yüksek
            // olması gerekiyor.
            levels = new Dictionary<Sfx, float>
            {
                [Sfx.Spawn] = 0.22f,
                [Sfx.Grab] = 0.30f,
                [Sfx.Release] = 0.30f,
                [Sfx.Land] = 0.55f,
                [Sfx.Collapse] = 0.85f,
                [Sfx.Win] = 0.45f,
                [Sfx.Lose] = 0.45f,
                [Sfx.CannonFire] = 0.45f,
                [Sfx.BallHit] = 0.50f,
                [Sfx.UiTap] = 0.40f,
            };
        }

        /// <summary>
        /// Ses sözlüğü: hangi olay hangi sentez ayarlarıyla üretiliyor.
        ///
        /// Statik ve dışarı açık, çünkü Editor tarafındaki denetim aracı da
        /// aynı tabloyu kullanıyor. Denetleyici kendi kopyasını tutsaydı bir
        /// değeri burada değiştirdiğimde o başka bir sesi ölçmeye devam eder
        /// ve "denetlendi" damgası yalan olurdu.
        /// </summary>
        public static Dictionary<Sfx, AudioClip> BuildLibrary()
        {
            return new Dictionary<Sfx, AudioClip>
            {
                // Sıradaki kutu: neredeyse duyulmayacak kadar hafif bir tık.
                // Bu ses her kutuda çaldığı için en çok duyulan ses; biraz
                // belirgin olsa kısa sürede sinir bozucu oluyor.
                [Sfx.Spawn] = ProceduralAudio.Click("sfx_spawn", 0.06f, 2600f, 880f, 11),

                // Tutma ve bırakma birbirinden perdeyle ayrılıyor: tutuş
                // yukarı, bırakış aşağı. Aynı sesi kullansaydım hangi olayın
                // gerçekleştiği duyulmazdı.
                [Sfx.Grab] = ProceduralAudio.Click("sfx_grab", 0.07f, 1800f, 620f, 23),
                [Sfx.Release] = ProceduralAudio.Whoosh("sfx_release", 0.22f, 37),

                // İniş sesi gürültü ağırlıklı: kutular tahta gibi duyulsun.
                // Sinüs ağırlıklı olduğunda davul sesi çıkıyordu.
                [Sfx.Land] = ProceduralAudio.Thud("sfx_land", 130f, 0.20f, 0.7f, 900f, 41),

                [Sfx.Collapse] = ProceduralAudio.Rumble("sfx_collapse", 0.85f, 53),

                // Kazanma: yükselen üçlü (do–mi–sol). Kaybetme: aynı fikrin
                // tersi, inen ve minör. Melodiyi kısa tutuyorum çünkü tur sonu
                // ekranı hemen açılıyor ve uzun jingle onun önüne geçiyor.
                [Sfx.Win] = ProceduralAudio.Notes("sfx_win", new[] { 523.25f, 659.25f, 783.99f, 1046.5f }, 0.09f, 0.36f),
                [Sfx.Lose] = ProceduralAudio.Notes("sfx_lose", new[] { 392f, 329.63f, 261.63f }, 0.11f, 0.40f),

                [Sfx.CannonFire] = ProceduralAudio.Pop("sfx_cannon", 0.16f, 420f, 90f, 67),

                // Merminin sesi kutununkinden daha tiz ve daha az gürültülü:
                // metal-tahta ayrımı kulakta böyle oturuyor.
                [Sfx.BallHit] = ProceduralAudio.Thud("sfx_ballhit", 260f, 0.14f, 0.35f, 2200f, 79),

                [Sfx.UiTap] = ProceduralAudio.Click("sfx_uitap", 0.08f, 2000f, 740f, 97),
            };
        }

        void BuildVoices()
        {
            voices = new AudioSource[Mathf.Max(1, voiceCount)];

            for (int i = 0; i < voices.Length; i++)
            {
                var source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;

                // Sesler 2B: dinleyici kamerada ve kamera kule büyüdükçe
                // yukarı kayıyor. Konumlu ses olsaydı aynı çarpma, kule
                // yükseldikçe farklı seviyede duyulurdu.
                source.spatialBlend = 0f;

                // Zaman yavaşlamasından etkilenmesinler: çöküş anındaki
                // hit-stop sesleri de ağırlaştırıyordu ve efekt "yavaşlama"
                // değil "bozulma" gibi duyuluyordu.
                source.ignoreListenerPause = true;

                voices[i] = source;
            }
        }

        /// <summary>
        /// Ses çalar. Oyuncu yoksa (sahnede kurulmadıysa) sessizce hiçbir şey
        /// yapmaz — ses eksikliği oyunu bozmadığı için bunu hata saymıyorum.
        /// </summary>
        public static void Play(Sfx id, float volume = 1f, float pitch = 1f)
        {
            if (Instance != null)
            {
                Instance.PlayInternal(id, volume, pitch);
            }
        }

        void PlayInternal(Sfx id, float volume, float pitch)
        {
            if (Progress.Muted || clips == null || !clips.TryGetValue(id, out var clip))
            {
                return;
            }

            var source = voices[nextVoice];
            nextVoice = (nextVoice + 1) % voices.Length;

            // Perde tek tek çağrılarda değil kaynakta ayarlanıyor; bu yüzden
            // sıradaki sesi çalmadan önce her seferinde yazmak gerekiyor.
            // PlayOneShot perdeyi parametre olarak almıyor.
            source.pitch = pitch;
            source.PlayOneShot(clip, levels[id] * volume * masterVolume);
        }
    }
}
