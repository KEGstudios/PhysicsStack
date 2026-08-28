namespace PhysicsStack
{
    /// <summary>
    /// Oyunun ses sözlüğü.
    ///
    /// Klip referansı yerine enum geçirmemin sebebi: sesi tetikleyen kodun
    /// (kutu, top, arayüz) hangi klibin çalacağını bilmesi gerekmiyor, sadece
    /// ne olduğunu bildirmesi gerekiyor. Böylece ses üretimi tek yerde kalıyor
    /// ve sentetik sesleri dosyayla değiştirmek istersem tetikleme
    /// noktalarının hiçbiri değişmiyor.
    /// </summary>
    public enum Sfx
    {
        /// <summary>Sıradaki kutu sahneye geldi.</summary>
        Spawn,

        /// <summary>Oyuncu kutuyu tuttu.</summary>
        Grab,

        /// <summary>Oyuncu kutuyu bıraktı; düşüş başlıyor.</summary>
        Release,

        /// <summary>Kutu bir şeye çarptı.</summary>
        Land,

        /// <summary>Kule çöktü.</summary>
        Collapse,

        /// <summary>Seviye geçildi.</summary>
        Win,

        /// <summary>Tur kaybedildi.</summary>
        Lose,

        /// <summary>Top atıldı.</summary>
        CannonFire,

        /// <summary>Mermi kutuya çarptı.</summary>
        BallHit,

        /// <summary>Kontrol noktası: kulenin altı donduruldu.</summary>
        Checkpoint,

        /// <summary>Arayüz dokunuşu.</summary>
        UiTap,
    }
}
