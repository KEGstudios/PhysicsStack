namespace PhysicsStack
{
    /// <summary>
    /// Sıradaki kutunun nasıl geleceği. Kural üretiyor, <see cref="BoxQueue"/> uyguluyor.
    ///
    /// Zorluğu kuyruğa değil kurala ait yapmamın sebebi: seviye modunda bu değerler
    /// sabit (seviyenin verisi), sonsuz modda yığdıkça büyüyor. İkisi de "sıradaki
    /// kutu nasıl olsun" sorusunun cevabı, sadece cevabı veren farklı.
    /// </summary>
    public readonly struct BoxDifficulty
    {
        /// <summary>
        /// Kutunun kule tepesinden en az bu kadar yukarıdan bırakılması gerekiyor.
        ///
        /// Oyunun asıl zorluk kolu bu. Bu kural olmadan kutuyu milimetrik yerine
        /// getirip sıfır hızla bırakabiliyordun: risk yoktu, sadece sabır vardı,
        /// dolayısıyla zorluk ancak kutu sayısıyla artabiliyordu. Mesafe konunca
        /// yerleştirme bir koymadan bir atışa dönüşüyor.
        /// </summary>
        public readonly float DropGap;

        /// <summary>
        /// Kutu **genişliğinin** oynama payı. 0 = bütün kutular birebir aynı.
        /// Boy hep 1 birim: değişseydi hedefe kaç kutuyla çıkılacağı zara kalırdı.
        /// </summary>
        public readonly float WidthVariance;

        public BoxDifficulty(float dropGap, float widthVariance)
        {
            DropGap = dropGap;
            WidthVariance = widthVariance;
        }
    }
}
