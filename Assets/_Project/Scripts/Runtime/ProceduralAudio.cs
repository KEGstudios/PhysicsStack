using UnityEngine;

namespace PhysicsStack
{
    /// <summary>
    /// Sesleri dosyadan yüklemek yerine örnek örnek üreten katman.
    ///
    /// Neden böyle yaptım: bu projede hiçbir hazır varlık yok, ses de hazır
    /// varlıkların en kolay sızdığı yer. İndirilmiş bir .wav daha iyi ses
    /// verirdi ama repoda gösterecek bir şey bırakmazdı. Burada gösterilecek
    /// şey şu: oyunun ihtiyacı olan seslerin tamamı darbe sesi — tok bir vuruş,
    /// bir tık, bir uğultu — ve bunların hepsi zarf + gürültü + birkaç sinüsle
    /// ikna edici biçimde üretilebiliyor.
    ///
    /// Elemiş olduğum alternatifler:
    /// - İndirilmiş CC0 ses paketi: daha kaliteli ama lisans takibi ve
    ///   "elle yazıldı" iddiasının delinmesi pahasına.
    /// - Editörde bir kez üretip .wav olarak kaydetmek: build'i küçültmez
    ///   (aksine büyütür) ve değeri her değiştirdiğimde yeniden üretme adımı
    ///   ekler. Klipler zaten açılışta birkaç milisaniyede üretiliyor.
    ///
    /// Ses üretimini çalma katmanından ayırdım: <see cref="SfxPlayer"/> klibin
    /// nereden geldiğini bilmiyor. Sentetik sesler kulağa ucuz gelirse tek
    /// yapılacak şey bu sınıfı dosya yüklemesiyle değiştirmek olur, tetikleme
    /// noktalarının hiçbirine dokunmadan.
    /// </summary>
    public static class ProceduralAudio
    {
        /// <summary>
        /// 44.1 kHz. Daha düşüğü (22 kHz) yarı bellek demek olurdu ama tık
        /// seslerindeki yüksek frekanslar aliasing yapıyor; toplam bellek
        /// zaten birkaç yüz kilobayt olduğu için tasarrufun karşılığı yok.
        /// </summary>
        public const int SampleRate = 44100;

        /// <summary>
        /// Tek kutuplu alçak geçiren filtrenin katsayısı. Kesim frekansını
        /// örnekleme hızından bağımsız hale getiriyor: katsayıyı sabit
        /// yazsaydım örnekleme hızını değiştirdiğimde bütün sesler kayardı.
        /// </summary>
        static float Coefficient(float cutoffHz) =>
            1f - Mathf.Exp(-2f * Mathf.PI * cutoffHz / SampleRate);

        /// <summary>
        /// Tok vuruş: kutunun inişi, merminin çarpması.
        ///
        /// İki bileşen var. Alçalan bir sinüs gövdeyi veriyor (perde düşüşü
        /// olmadan "biip" gibi duyuluyor), alçak geçirilmiş gürültü de
        /// malzemeyi. Oranı <paramref name="noiseMix"/> ile veriyorum: tahta
        /// kutu için gürültü ağırlıklı, sert çarpma için sinüs ağırlıklı.
        /// </summary>
        public static AudioClip Thud(string name, float baseFrequency, float duration, float noiseMix, float noiseCutoff, int seed)
        {
            int count = Mathf.CeilToInt(SampleRate * duration);
            var data = new float[count];
            var random = new System.Random(seed);

            float lowpass = 0f;
            float k = Coefficient(noiseCutoff);
            float phase = 0f;

            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float progress = t / duration;

                // Perde düşüşü karekökle: başta hızlı, sonra yavaş. Doğrusal
                // düşüş siren gibi duyuluyor, karekök vuruş gibi.
                float frequency = baseFrequency * Mathf.Lerp(1.7f, 0.65f, Mathf.Sqrt(progress));
                phase += frequency / SampleRate;
                float body = Mathf.Sin(phase * Mathf.PI * 2f);

                float noise = (float)(random.NextDouble() * 2.0 - 1.0);
                lowpass += (noise - lowpass) * k;

                // Üstel sönüm: doğrusal sönüm sesin sonunu bıçakla kesiyormuş
                // gibi bırakıyor ve kulak bunu ayrı bir tıklama olarak duyuyor.
                float envelope = Mathf.Exp(-t * (5.5f / duration));

                data[i] = (body * (1f - noiseMix) + lowpass * noiseMix) * envelope;
            }

            return ToClip(name, data);
        }

        /// <summary>
        /// Kısa tık: kutuyu tutma, arayüz dokunuşu.
        ///
        /// Gürültüden alçak frekansları çıkarıyorum (yüksek geçiren). Ham
        /// gürültü "şşş" diye duyuluyor; alt tarafı alınınca aynı ses "tık"
        /// oluyor. Aradaki tek fark bu filtre.
        /// </summary>
        public static AudioClip Click(string name, float duration, float cutoffHz, float toneFrequency, int seed)
        {
            int count = Mathf.CeilToInt(SampleRate * duration);
            var data = new float[count];
            var random = new System.Random(seed);

            float lowpass = 0f;
            float k = Coefficient(cutoffHz);
            float phase = 0f;

            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;

                float noise = (float)(random.NextDouble() * 2.0 - 1.0);
                lowpass += (noise - lowpass) * k;
                float highpass = noise - lowpass;

                phase += toneFrequency / SampleRate;
                float tone = Mathf.Sin(phase * Mathf.PI * 2f);

                float envelope = Mathf.Exp(-t * (9f / duration));

                data[i] = (highpass * 0.75f + tone * 0.25f) * envelope;
            }

            return ToClip(name, data);
        }

        /// <summary>
        /// Uğultu: kutunun hızlanarak düşmesi. Genlik önce yükselip sonra
        /// sönüyor, kesim frekansı da onunla birlikte yukarı süpürüyor —
        /// yaklaşan bir şeyin sesi bu iki hareketin birleşimi.
        /// </summary>
        public static AudioClip Whoosh(string name, float duration, int seed)
        {
            int count = Mathf.CeilToInt(SampleRate * duration);
            var data = new float[count];
            var random = new System.Random(seed);

            float a = 0f;
            float b = 0f;

            for (int i = 0; i < count; i++)
            {
                float progress = i / (float)count;

                float noise = (float)(random.NextDouble() * 2.0 - 1.0);

                // İki kademeli alçak geçiren: tek kademe yeterince dik değil,
                // gürültünün tepesi hâlâ "şşş" diye sızıyor.
                float k = Coefficient(Mathf.Lerp(450f, 2200f, progress));
                a += (noise - a) * k;
                b += (a - b) * k;

                // Yükselip sönen zarf. Sinüsün yarım periyodu tam da bu şekli
                // veriyor; üssünü alarak tepeyi sona kaydırıyorum.
                float envelope = Mathf.Pow(Mathf.Sin(progress * Mathf.PI), 1.6f);

                data[i] = b * envelope;
            }

            return ToClip(name, data);
        }

        /// <summary>
        /// Gümbürtü: kule çöktüğünde.
        ///
        /// Tek bir vuruştan farkı süresi ve düzensizliği: üstüne yavaş bir
        /// genlik dalgalanması bindiriyorum ki "tek bir şey düştü" değil
        /// "birkaç şey arka arkaya düştü" gibi duyulsun.
        /// </summary>
        public static AudioClip Rumble(string name, float duration, int seed)
        {
            int count = Mathf.CeilToInt(SampleRate * duration);
            var data = new float[count];
            var random = new System.Random(seed);

            float a = 0f;
            float b = 0f;
            float k = Coefficient(260f);
            float phase = 0f;

            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float progress = t / duration;

                float noise = (float)(random.NextDouble() * 2.0 - 1.0);
                a += (noise - a) * k;
                b += (a - b) * k;

                // Aşağı süpüren sinüs gövdeyi taşıyor.
                float frequency = Mathf.Lerp(150f, 55f, progress);
                phase += frequency / SampleRate;
                float body = Mathf.Sin(phase * Mathf.PI * 2f) * 0.6f;

                // Düzensizlik: iki uyumsuz frekansta yavaş dalgalanma.
                float flutter = 0.72f + 0.28f * Mathf.Sin(t * 41f) * Mathf.Sin(t * 17f);

                float envelope = Mathf.Exp(-t * (3.4f / duration));

                data[i] = (b * 1.4f + body) * envelope * flutter;
            }

            return ToClip(name, data);
        }

        /// <summary>
        /// Notalar: kazanma ve kaybetme jingle'ı.
        ///
        /// Her nota saf sinüs değil, üstüne iki harmonik biniyor. Saf sinüs
        /// "test tonu" gibi duyuluyor; ikinci ve üçüncü harmonik onu bir
        /// çalgıya benzetmeye yetiyor. Notalar üst üste biniyor (sonraki nota
        /// öncekinin kuyruğunda başlıyor), yoksa arpej değil mors alfabesi
        /// oluyor.
        /// </summary>
        public static AudioClip Notes(string name, float[] frequencies, float spacing, float noteDuration)
        {
            float total = spacing * (frequencies.Length - 1) + noteDuration;
            int count = Mathf.CeilToInt(SampleRate * total);
            var data = new float[count];

            for (int n = 0; n < frequencies.Length; n++)
            {
                int start = Mathf.RoundToInt(spacing * n * SampleRate);
                int length = Mathf.CeilToInt(noteDuration * SampleRate);
                float frequency = frequencies[n];

                for (int i = 0; i < length && start + i < count; i++)
                {
                    float t = i / (float)SampleRate;
                    float w = t * frequency * Mathf.PI * 2f;

                    float sample =
                        Mathf.Sin(w) +
                        Mathf.Sin(w * 2f) * 0.35f +
                        Mathf.Sin(w * 3f) * 0.12f;

                    // Kısa açılış rampası: sıfırdan tam genliğe anında geçmek
                    // her notanın başına bir tıklama koyuyor.
                    float attack = Mathf.Clamp01(t / 0.008f);
                    float envelope = attack * Mathf.Exp(-t * (4.5f / noteDuration));

                    data[start + i] += sample * envelope * 0.5f;
                }
            }

            return ToClip(name, data);
        }

        /// <summary>
        /// Patlama: top atışı. Kısa bir gürültü darbesi ve hızla aşağı inen
        /// bir gövde.
        /// </summary>
        public static AudioClip Pop(string name, float duration, float startFrequency, float endFrequency, int seed)
        {
            int count = Mathf.CeilToInt(SampleRate * duration);
            var data = new float[count];
            var random = new System.Random(seed);

            float lowpass = 0f;
            float k = Coefficient(1400f);
            float phase = 0f;

            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float progress = t / duration;

                float frequency = Mathf.Lerp(startFrequency, endFrequency, progress * progress);
                phase += frequency / SampleRate;
                float body = Mathf.Sin(phase * Mathf.PI * 2f);

                float noise = (float)(random.NextDouble() * 2.0 - 1.0);
                lowpass += (noise - lowpass) * k;

                // Gürültü yalnızca ilk 25 ms'de: barut sesi darbenin kendisi,
                // devamı değil.
                float transient = Mathf.Exp(-t * 90f);

                float envelope = Mathf.Exp(-t * (6f / duration));

                data[i] = (body * 0.7f + lowpass * transient) * envelope;
            }

            return ToClip(name, data);
        }

        /// <summary>
        /// Örnek dizisini klibe çevirir.
        ///
        /// İki şey burada yapılıyor çünkü ikisini de her üretim fonksiyonunda
        /// tekrar yazmak gerekirdi:
        /// - Normalizasyon: farklı sentez fonksiyonları farklı tepe genlikleri
        ///   üretiyor. Her birine elle ses seviyesi ayarlamak yerine hepsini
        ///   aynı tepeye çekiyorum; denge tek yerde, SfxPlayer'da kalıyor.
        /// - Sonda sönüm: dizinin son örneği sıfır değilse hoparlör oradan
        ///   sıfıra sıçrıyor ve bu duyulabilir bir tıklama üretiyor.
        /// </summary>
        static AudioClip ToClip(string name, float[] data)
        {
            float peak = 0f;

            for (int i = 0; i < data.Length; i++)
            {
                peak = Mathf.Max(peak, Mathf.Abs(data[i]));
            }

            if (peak > 0.0001f)
            {
                float gain = 0.9f / peak;

                for (int i = 0; i < data.Length; i++)
                {
                    data[i] *= gain;
                }
            }

            int fade = Mathf.Min(data.Length, SampleRate / 200);

            for (int i = 0; i < fade; i++)
            {
                data[data.Length - 1 - i] *= i / (float)fade;
            }

            var clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
