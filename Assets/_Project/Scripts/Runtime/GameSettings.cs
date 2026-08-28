using System;
using UnityEngine;

namespace PhysicsStack
{
    /// <summary>
    /// Oyuncunun değiştirebildiği ayarlar: ses, grafik kalitesi, kamera
    /// sarsıntısı.
    ///
    /// <see cref="Progress"/>'ten ayrı bir sınıf, çünkü ikisi farklı şeyler.
    /// İlerleme oyunun oyuncu hakkında bildikleri (nereye kadar geldi, kaç
    /// yıldız aldı); ayarlar ise oyuncunun oyun hakkındaki tercihleri. Aynı
    /// dosyada dursalardı "ilerlemeyi sıfırla" düğmesi tercihleri de silerdi.
    ///
    /// Değerler <c>PlayerPrefs</c>'te: sekmeyi kapatıp açan biri sesi yeniden
    /// kısmak zorunda kalmasın. Tarayıcıda bu IndexedDB'ye yazılıyor, yani
    /// gizli sekmede kaybolur — kabul edilebilir, çünkü kaybolan şey bir tercih.
    ///
    /// <see cref="Changed"/> olayı, ayarı değiştiren yer ile uygulayan yeri
    /// birbirinden ayırıyor. Menü ekranı kamerayı ya da ışığı tanımıyor;
    /// yalnızca "değişti" diyor.
    /// </summary>
    public static class GameSettings
    {
        const string VolumeKey = "physicsstack.volume";
        const string QualityKey = "physicsstack.quality";
        const string ShakeKey = "physicsstack.shake";

        /// <summary>Ayarlardan biri değişti. Uygulayan taraf bunu dinliyor.</summary>
        public static event Action Changed;

        /// <summary>Kalite kademeleri. Sıra önemli: sayı büyüdükçe kalite artıyor.</summary>
        public const int LowQuality = 0;
        public const int MediumQuality = 1;
        public const int HighQuality = 2;

        static readonly string[] QualityNames = { "düşük", "orta", "yüksek" };

        public static string QualityName(int level) =>
            QualityNames[Mathf.Clamp(level, LowQuality, HighQuality)];

        /// <summary>
        /// Ses seviyesi (0-1). Varsayılan tam değil: kliplerin tepe genliği
        /// zaten 0.9'a normalize ediliyor ve tam sesle üst üste binen iki efekt
        /// kırpılmaya çok yaklaşıyor.
        /// </summary>
        public static float Volume
        {
            get => Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, 0.75f));
            set
            {
                PlayerPrefs.SetFloat(VolumeKey, Mathf.Clamp01(value));
                PlayerPrefs.Save();
                Changed?.Invoke();
            }
        }

        /// <summary>Sesin tamamen kapalı olması ayrı bir ayar değil, sıfır seviye.</summary>
        public static bool Muted => Volume <= 0.001f;

        /// <summary>
        /// Grafik kalitesi. Varsayılan en yüksek: oyunu ilk açan kişinin gördüğü
        /// şey oyunun en iyi hâli olmalı. Düşürmek isteyen zaten arıyor.
        /// </summary>
        public static int Quality
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt(QualityKey, HighQuality), LowQuality, HighQuality);
            set
            {
                PlayerPrefs.SetInt(QualityKey, Mathf.Clamp(value, LowQuality, HighQuality));
                PlayerPrefs.Save();
                Changed?.Invoke();
            }
        }

        /// <summary>
        /// Kamera sarsıntısı. Kapatılabilir olmasının sebebi tercihten çok
        /// erişilebilirlik: sarsılan bir kadraj bazı insanlarda mide bulantısı
        /// yapıyor ve bu, oyunu oynanamaz kılan türden bir şey.
        /// </summary>
        public static bool Shake
        {
            get => PlayerPrefs.GetInt(ShakeKey, 1) == 1;
            set
            {
                PlayerPrefs.SetInt(ShakeKey, value ? 1 : 0);
                PlayerPrefs.Save();
                Changed?.Invoke();
            }
        }
    }
}
