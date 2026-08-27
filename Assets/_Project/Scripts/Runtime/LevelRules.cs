namespace PhysicsStack
{
    /// <summary>
    /// Seviye modunun kuralları: bir hedef yüksekliğe ulaş, istersen sınırlı
    /// sayıda kutuyla.
    ///
    /// Gün 8'de bu sınıfın alanları seviye varlığından (ScriptableObject)
    /// doldurulacak; kural mantığı orada değişmeyecek. Bu ayrımı şimdiden
    /// yapmamın sebebi, seviye verisi geldiğinde dokunacağım yerin sadece
    /// kurucu metot olması.
    /// </summary>
    public sealed class LevelRules : IStackRules
    {
        readonly float targetHeight;
        readonly int boxLimit;
        readonly float collapseDrop;

        /// <param name="targetHeight">Kulenin oturmuş hâlde geçmesi gereken yükseklik.</param>
        /// <param name="boxLimit">İzin verilen kutu sayısı. Sıfır ya da altı sınırsız demek.</param>
        /// <param name="collapseDrop">Kule zirvesinin bu kadar altına düşerse çökmüş sayılır.</param>
        public LevelRules(float targetHeight, int boxLimit = 0, float collapseDrop = 0.6f)
        {
            this.targetHeight = targetHeight;
            this.boxLimit = boxLimit;
            this.collapseDrop = collapseDrop;
        }

        public string Title => "Seviye";

        public float TargetHeight => targetHeight;

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

            if (snapshot.Height >= targetHeight)
            {
                return RunOutcome.Won;
            }

            // Kule devrildiyse tur biter. Buna kadar seviye modunun kaybetme
            // koşulu pratikte hiç tetiklenmiyordu: devrilen kutu geniş zemine
            // oturuyor, oyuncu da enkazın üstüne yığmaya devam edebiliyordu.
            if (snapshot.Collapsed(collapseDrop))
            {
                return RunOutcome.Lost;
            }

            // Kutu sınırı olan seviyede hedefe ulaşmadan kutular bitince tur kaybedilir.
            // Sınır kaybettirmenin değil, seviyeye kimlik vermenin yolu: "beş kutuyla
            // şu yüksekliğe çık" ile "istediğin kadar kutuyla çık" iki farklı problem.
            if (boxLimit > 0 && snapshot.PlacedCount >= boxLimit)
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

        public override string ToString() =>
            boxLimit > 0
                ? $"Seviye · hedef {targetHeight:0.0} · {boxLimit} kutu"
                : $"Seviye · hedef {targetHeight:0.0}";
    }
}
