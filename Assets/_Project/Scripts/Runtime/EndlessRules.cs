namespace PhysicsStack
{
    /// <summary>
    /// Sonsuz modun kuralları: hedef yok, kazanma yok. Bir parça düşene kadar
    /// yığıyorsun, skor yığdığın kutu sayısı.
    ///
    /// Sınıf neredeyse boş ve bu bilinçli. Kazancı burada değil karşı tarafta:
    /// controller artık hedef yüksekliği diye bir şey bilmediği için "hedefi
    /// olmayan mod"u desteklemek adına tek bir <c>if</c> bile taşımıyor.
    /// Gün 8'de zorluk artışı (kutu boyut varyansı, spawn salınımı) buraya
    /// girecek — sonsuz modu ilginç kılan şey o eğri.
    /// </summary>
    public sealed class EndlessRules : IStackRules
    {
        readonly float collapseDrop;

        /// <param name="collapseDrop">Kule zirvesinin bu kadar altına düşerse çökmüş sayılır.</param>
        public EndlessRules(float collapseDrop = 0.6f)
        {
            this.collapseDrop = collapseDrop;
        }

        public string Title => "Sonsuz";

        /// <summary>Hedef yok. Sıfır dönmek hedef çizgisini de gizliyor.</summary>
        public float TargetHeight => 0f;

        public RunOutcome Evaluate(in StackSnapshot snapshot)
        {
            if (snapshot.AnyFallen)
            {
                return RunOutcome.Lost;
            }

            // Sonsuz modun tek bitiş koşulu bu. Zemine düşmeyi beklemek işe
            // yaramıyordu: devrilen kutu 14 birimlik zemine oturuyor, ölüm
            // yüksekliğinin altına hiç inmiyor, yani tur hiç bitmiyordu.
            return snapshot.Settled && snapshot.Collapsed(collapseDrop)
                ? RunOutcome.Lost
                : RunOutcome.Continue;
        }

        /// <summary>
        /// Skor kule boyu, yığılan kutu sayısı değil. Sayıyı seçerken şunu
        /// düşündüm: kutu sayısı, kuleyi hiç yükseltmeyen kutularla da artar —
        /// yere yan yana kutu dizerek skor toplanabilirdi. Boy ise ancak
        /// gerçekten yükseldiğinde artıyor, yani ölçtüğü şey oyunun kendisi.
        /// </summary>
        public float Score(in StackSnapshot snapshot) => snapshot.PeakHeight;

        public string DescribeScore(float score) => $"{score:0.00} birim";

        public override string ToString() => "Sonsuz";
    }
}
