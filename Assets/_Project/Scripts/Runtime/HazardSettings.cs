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
    public struct HazardSettings
    {
        [Tooltip("Rüzgârın kendi yatay hızı (m/s). Kutu bu hıza doğru itilir, geçemez. 0 = rüzgâr yok.")]
        public float windSpeed;

        [Tooltip("Kutunun rüzgâr hızına yetişme sertliği (1/sn). Yükseldikçe daha çabuk yetişir.")]
        public float windResponse;

        [Tooltip("Rüzgârın yön değiştirme periyodu (sn). 0 = sabit yön.")]
        public float windPeriod;

        [Tooltip("Kenarda gezinen top atıcı açık mı?")]
        public bool cannon;

        [Tooltip("İki atış arası süre (sn).")]
        public float cannonInterval;

        [Tooltip("Topun yatay hızı (m/s).")]
        public float cannonBallSpeed;

        [Tooltip("Namlunun aşağı yukarı gezinme hızı (m/s).")]
        public float cannonPatrolSpeed;

        [Tooltip("Bandın alt kenarı kule tepesinin bu kadar üstünde.")]
        public float cannonBottomGap;

        [Tooltip("Namlunun gezindiği bandın yüksekliği (birim). Bant kule tepesinden başlar.")]
        public float cannonPatrolSpan;

        /// <summary>Hiçbir tehdit yok. Sonsuz mod şimdilik bunu kullanıyor.</summary>
        public static HazardSettings None => default;
    }
}
