using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace PhysicsStack.EditorTools
{
    /// <summary>
    /// Uretilen ses kliplerini olcer ve .wav olarak disa aktarir.
    ///
    /// Neden boyle bir arac var: ses, "derleniyor" ile "dogru" arasindaki
    /// mesafenin en buyuk oldugu yer. Sentez kodu hatasiz derlenir, sifir dolu
    /// bir tampon uretir ve hicbir yerde hata gorunmez - oyun sadece sessiz
    /// calisir. Ayni sekilde NaN uretse hoparlorden ciyaklama gelir ama
    /// konsolda tek satir yazmaz.
    ///
    /// Bu yuzden klipler sayiyla denetleniyor: uzunluk, tepe genlik, RMS ve
    /// bozuk ornek sayisi. RMS sifirsa ses uretilmemis demektir; bozuk sayisi
    /// sifirdan buyukse sentezde bir bolme hatasi var.
    ///
    /// .wav ciktisi ayri bir ise yariyor: sesleri build almadan dinleyebilmek.
    /// Bir ses ayarini denemek icin WebGL build'i beklemek, denemeyi pahali
    /// hale getirip yapilmamasina yol aciyordu.
    /// </summary>
    public static class AudioProbe
    {
        [MenuItem("PhysicsStack/Ses Kliplerini Denetle")]
        public static void Run()
        {
            string directory = System.Environment.GetEnvironmentVariable("PS_SFX_OUT");

            if (string.IsNullOrEmpty(directory))
            {
                directory = Path.Combine(Application.dataPath, "..", "SfxDump");
            }

            Directory.CreateDirectory(directory);

            // Klipler oyunun kullandigi tablodan geliyor, buraya kopyalanmiyor:
            // ayri bir liste tutsaydim bir sesi degistirdigimde denetleyici
            // eskisini olcmeye devam ederdi.
            var library = SfxPlayer.BuildLibrary();
            var report = new StringBuilder();

            foreach (var clip in library.Values)
            {
                var data = new float[clip.samples];
                clip.GetData(data, 0);

                double sum = 0.0;
                float peak = 0f;
                int bad = 0;
                int silentTail = 0;

                foreach (float sample in data)
                {
                    if (float.IsNaN(sample) || float.IsInfinity(sample))
                    {
                        bad++;
                        continue;
                    }

                    sum += sample * (double)sample;
                    peak = Mathf.Max(peak, Mathf.Abs(sample));
                }

                for (int i = data.Length - 1; i >= 0 && Mathf.Abs(data[i]) < 0.001f; i--)
                {
                    silentTail++;
                }

                float rms = Mathf.Sqrt((float)(sum / Mathf.Max(1, data.Length)));

                report.AppendLine(
                    $"{clip.name,-14} sure={clip.length:0.000}s ornek={clip.samples,6} " +
                    $"tepe={peak:0.000} rms={rms:0.0000} bozuk={bad} sondaSessiz={silentTail}");

                WriteWav(Path.Combine(directory, clip.name + ".wav"), data, clip.frequency);
            }

            Debug.Log($"[AudioProbe] Klip raporu:\n{report}\nDosyalar: {Path.GetFullPath(directory)}");
        }

        /// <summary>16-bit PCM mono WAV.</summary>
        static void WriteWav(string path, float[] data, int sampleRate)
        {
            using var stream = new FileStream(path, FileMode.Create);
            using var writer = new BinaryWriter(stream);

            int dataBytes = data.Length * 2;

            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + dataBytes);
            writer.Write(Encoding.ASCII.GetBytes("WAVE"));
            writer.Write(Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)1);
            writer.Write(sampleRate);
            writer.Write(sampleRate * 2);
            writer.Write((short)2);
            writer.Write((short)16);
            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(dataBytes);

            foreach (float sample in data)
            {
                writer.Write((short)(Mathf.Clamp(sample, -1f, 1f) * short.MaxValue));
            }
        }
    }
}
