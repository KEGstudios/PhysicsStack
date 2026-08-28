namespace PhysicsStack
{
    /// <summary>
    /// Seviye modunun kuralları. Bütün sayılar <see cref="LevelDefinition"/>'dan
    /// geliyor: yeni seviye eklemek kod değil veri işi.
    ///
    /// Kural sınıfı varlığı sadece okuyor, kendi kopyasını almıyor. Böylece
    /// Inspector'da bir sayıyı değiştirdiğimde Play Mode'u durdurup başlatmak
    /// yetiyor, sahneyi yeniden kurmak gerekmiyor.
    /// </summary>
    public sealed class LevelRules : IStackRules
    {
        readonly LevelDefinition level;

        public LevelRules(LevelDefinition level)
        {
            this.level = level;
        }

        public string Title => level.title;

        public float TargetHeight => level.targetHeight;

        public float HoldTime => level.holdTime;

        /// <summary>Seviyede tehdit sabit: cevap anlık görüntüye bakmıyor.</summary>
        public HazardSettings HazardsFor(in StackSnapshot snapshot) => level.hazards;

        /// <summary>
        /// Seviyede kontrol noktası isteğe bağlı ve varsayılanı kapalı: sekiz
        /// seviyenin hiçbiri buna ihtiyaç duymuyor, kuleleri 3-6 birim.
        ///
        /// Alanı yine de şimdi ekledim, çünkü asıl sebebi ileride yüksek kule
        /// isteyen bir seviye tasarlayabilmek. Verinin hazır olması, o seviyeyi
        /// tasarlarken mekaniği baştan yazmak zorunda kalmamak demek.
        /// </summary>
        public bool IsCheckpoint(int placedCount) =>
            level.checkpointEvery > 0 &&
            placedCount > 0 &&
            placedCount % level.checkpointEvery == 0;

        public RunOutcome Evaluate(in StackSnapshot snapshot)
        {
            // Düşme kontrolü oturmayı beklemiyor: sürükleme sırasında devrilen
            // eski bir kutu da turu bitirir.
            if (snapshot.AnyFallen)
            {
                return RunOutcome.Lost;
            }

            // Kazanma kararı yalnızca yığın oturduktan sonra veriliyor. Sallanan
            // kule bir kare için hedefi geçip sonra devrilebilir; o kazanma değil.
            if (!snapshot.Settled)
            {
                return RunOutcome.Continue;
            }

            // Çöküş kontrolü hedeften önce: tepeden kutu gittiyse kule hâlâ
            // hedefin üstünde olsa bile tutunamamış demektir.
            if (snapshot.Collapsed(level.collapseDrop))
            {
                return RunOutcome.Lost;
            }

            // Hedefi geçmek yetmiyor, orada durmak da gerekiyor. Geçmek bir an,
            // tutunmak bir süre — ve bu oyunda ayakta kalan kule ile devrilmek
            // üzere olan kule arasındaki fark tam olarak o süre.
            if (snapshot.Height >= level.targetHeight)
            {
                return snapshot.SteadyTime >= level.holdTime
                    ? RunOutcome.Won
                    : RunOutcome.Pending;
            }

            // Kutu sınırı olan seviyede hedefe ulaşmadan kutular bitince tur kaybedilir.
            // Sınır kaybettirmenin değil, seviyeye kimlik vermenin yolu: "altı kutuyla
            // şu yüksekliğe çık" ile "istediğin kadar kutuyla çık" iki farklı problem.
            // Kutu sinirini seviye kendisi soyluyor: yildiz esiginin iki
            // fazlasi. Ayri bir sayi olsaydi sinir ile yildiz esigi celisebilir,
            // ornegin uc yildizin alinamadigi bir seviye ortaya cikabilirdi.
            if (snapshot.PlacedCount >= level.BoxLimit)
            {
                return RunOutcome.Lost;
            }

            return RunOutcome.Continue;
        }

        /// <summary>
        /// Seviyede skor "kaç kutu harcadın". Az olan iyi — sonsuz moddaki skorun
        /// tam tersi yönde, bu yüzden en iyi skor karşılaştırması Gün 9'da mod
        /// başına yapılacak.
        /// </summary>
        public float Score(in StackSnapshot snapshot) => snapshot.PlacedCount;

        public string DescribeScore(float score) => $"{score:0} kutu";

        /// <summary>Seviye boyunca sabit: zorluk turun içinde değil seviyeler arasında artıyor.</summary>
        public BoxDifficulty NextBox(in StackSnapshot snapshot) =>
            new(level.dropGap, level.widthVariance, level.spawnLift);

        public override string ToString() =>
            $"{level.title} · hedef {level.targetHeight:0.0} · " +
            $"{level.StarBoxes} kutu (sinir {level.BoxLimit}) · mesafe {level.dropGap:0.0}";
    }
}
