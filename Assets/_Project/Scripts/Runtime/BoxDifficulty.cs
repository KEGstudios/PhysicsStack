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

        /// <summary>
        /// Kutunun bırakma çizgisinin ne kadar üstünde belireceği — düşme
        /// mesafesine eklenmiyor, sadece oyuncunun kutuyu indireceği yolu uzatıyor.
        ///
        /// İkisini ayırmak zorunda kaldım. Koridoru uzatmanın doğal yolu bırakma
        /// mesafesini büyütmekti ama serbest düşüşte hız yükseklikle karekök olarak
        /// artıyor: 4 birimden düşen kutu yere ~9 m/s ile çarpıyor, 6 birimden
        /// ~11 m/s. O hızda kutu yerleşmiyor, kuleyi süpürüyor. Yani düşme mesafesi
        /// oynanabilirlik tavanına dayanmış durumda.
        ///
        /// Oysa tehdit koridorunun uzun olması gereken kısmı düşüş değil, oyuncunun
        /// kutuyu aşağı indirdiği kısım. Onu ayrı bir sayı yapınca koridor
        /// istediğim kadar uzayabiliyor, düşüş güvenli mesafede kalıyor.
        /// </summary>
        public readonly float SpawnLift;

        public BoxDifficulty(float dropGap, float widthVariance, float spawnLift = 0f)
        {
            DropGap = dropGap;
            WidthVariance = widthVariance;
            SpawnLift = spawnLift;
        }
    }
}
