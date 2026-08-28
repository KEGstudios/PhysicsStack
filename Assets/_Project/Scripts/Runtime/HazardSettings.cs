using System;
using UnityEngine;

namespace PhysicsStack
{
    /// <summary>
    /// Seviyenin çevresel tehditleri. Kural veriyor, sahnedeki bileşenler uyguluyor —
    /// <see cref="BoxDifficulty"/> ile aynı ilişki.
    ///
    /// Hepsinin ortak kuralı var: **tehdit yalnızca havadaki kutuya dokunur.**
    /// Duran kuleyi bozan bir tehlike, oyuncu hata yapmadan kaybettirir; bu ceza
    /// değil haksızlıktır. Rüzgâr sadece bırakılmış kutuya kuvvet uyguluyor, top
    /// da kulenin tepesinin altına inmeyen bir koridorda geziniyor.
    /// </summary>
    [Serializable]
    public struct HazardSettings : IEquatable<HazardSettings>
    {
        [Tooltip("Rüzgârın kendi yatay hızı (m/s). Kutu bu hıza doğru itilir, geçemez. 0 = rüzgâr yok.")]
        public float windSpeed;

        [Tooltip("Kutunun rüzgâr hızına yetişme sertliği (1/sn). Yükseldikçe daha çabuk yetişir.")]
        public float windResponse;

        [Tooltip("Rüzgârın yön değiştirme periyodu (sn). 0 = sabit yön.")]
        public float windPeriod;

        [Tooltip("Kaç namlu var? 0 = yok, 1 = sol kenar, 2 = iki kenar.")]
        public int cannonCount;

        [Tooltip("İki atış arası süre (sn).")]
        public float cannonInterval;

        [Tooltip("Topun yatay hızı (m/s).")]
        public float cannonBallSpeed;

        [Tooltip("Namlunun aşağı yukarı gezinme hızı (m/s).")]
        public float cannonPatrolSpeed;

        [Tooltip("Bandın alt kenarı kule tepesinin bu kadar üstünde. Küçük bir pay: namlu kulenin tepesine kadar iniyor.")]
        public float cannonBottomGap;

        /// <summary>Hiçbir tehdit yok.</summary>
        public static HazardSettings None => default;

        /// <summary>Bu ayarlarda dokunulacak bir tehdit var mı?</summary>
        public bool Any => windSpeed > 0f || cannonCount > 0;

        /// <summary>
        /// Alan alan karşılaştırma. Sonsuz modda tehditler tur içinde değiştiği
        /// için sahnedeki bileşenler "elimdeki ayar hâlâ geçerli mi" diye
        /// soruyor; karşılaştırılan şey iki fiziksel rüzgârın aynı olup olmadığı
        /// değil, kuralın bana farklı bir ayar verip vermediği. O yüzden
        /// ondalık sayılarda tolerans yok: iki taraf da aynı hesabın çıktısı,
        /// değişmediyse bit bit aynı.
        /// </summary>
        public bool Equals(HazardSettings other) =>
            windSpeed == other.windSpeed &&
            windResponse == other.windResponse &&
            windPeriod == other.windPeriod &&
            cannonCount == other.cannonCount &&
            cannonInterval == other.cannonInterval &&
            cannonBallSpeed == other.cannonBallSpeed &&
            cannonPatrolSpeed == other.cannonPatrolSpeed &&
            cannonBottomGap == other.cannonBottomGap;

        public override bool Equals(object obj) => obj is HazardSettings other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(
            windSpeed,
            windResponse,
            windPeriod,
            cannonCount,
            cannonInterval,
            cannonBallSpeed,
            cannonPatrolSpeed,
            cannonBottomGap);

        public static bool operator ==(HazardSettings left, HazardSettings right) => left.Equals(right);

        public static bool operator !=(HazardSettings left, HazardSettings right) => !left.Equals(right);
    }
}
