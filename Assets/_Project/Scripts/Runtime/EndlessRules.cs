using UnityEngine;

namespace PhysicsStack
{
    /// <summary>
    /// Sonsuz modun kuralları: hedef yok, kazanma yok. Kule çökene kadar
    /// yığıyorsun, skor ulaştığın yükseklik.
    ///
    /// Seviye modunda zorluk seviyeler arasında artıyor; burada turun **içinde**
    /// artıyor. Sonsuz modu ilginç kılan tek şey bu eğri: sabit zorlukta sonsuz
    /// bir mod, bir süre sonra oyun değil test ortamı olur.
    /// </summary>
    public sealed class EndlessRules : IStackRules
    {
        readonly float collapseDrop;

        /// <summary>Zorluğun tepeye ulaşması için gereken kutu sayısı.</summary>
        const int RampBoxes = 15;

        const float StartDropGap = 2.5f;
        const float EndDropGap = 4f;
        const float EndWidthVariance = 0.3f;

        /// <param name="collapseDrop">Kule zirvesinin bu kadar altına düşerse çökmüş sayılır.</param>
        public EndlessRules(float collapseDrop = 0.6f)
        {
            this.collapseDrop = collapseDrop;
        }

        public string Title => "Sonsuz";

        /// <summary>Hedef yok. Sıfır dönmek hedef çizgisini de gizliyor.</summary>
        public float TargetHeight => 0f;

        /// <summary>Hedef yoksa tutunma şartı da yok: sonsuz modda tur kule çökene kadar sürüyor.</summary>
        public float HoldTime => 0f;

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

        /// <summary>
        /// Zorluk ilk 15 kutuda tepeye çıkıyor, sonra sabit kalıyor. Sonsuza kadar
        /// artan bir eğri, oyuncunun becerisinin değil eğrinin kazandığı bir yer
        /// yaratırdı; tavanı olan zorluk "nereye kadar dayanabilirim" sorusunu
        /// oyuncuya bırakıyor.
        /// </summary>
        public BoxDifficulty NextBox(in StackSnapshot snapshot)
        {
            float t = Mathf.Clamp01((float)snapshot.PlacedCount / RampBoxes);
            return new BoxDifficulty(
                Mathf.Lerp(StartDropGap, EndDropGap, t),
                Mathf.Lerp(0f, EndWidthVariance, t));
        }

        public override string ToString() => "Sonsuz";
    }
}
