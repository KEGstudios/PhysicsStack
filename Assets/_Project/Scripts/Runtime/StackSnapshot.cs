namespace PhysicsStack
{
    /// <summary>
    /// Kuralların karar verirken ihtiyaç duyduğu her şey, tek bir okuma anında
    /// dondurulmuş hâlde.
    ///
    /// Kural sınıflarına <see cref="StackTracker"/>'ı doğrudan vermek daha kısa
    /// olurdu ama iki şeyi kaybederdim: kural sınıfı sahnedeki bir bileşene
    /// bağlanır (tek başına test edilemez) ve aynı karar içinde ölçümü iki kez
    /// okuyup iki farklı cevap alma ihtimali doğar. Struct olması da bilinçli —
    /// karede bir kez üretilip geçiliyor, çöp toplayıcıya iş çıkarmıyor.
    /// </summary>
    public readonly struct StackSnapshot
    {
        /// <summary>Yerleştirilmiş kutuların en tepe noktası. Eldeki kutu sayılmaz.</summary>
        public readonly float Height;

        /// <summary>Yığına girmiş ve şu an elde olmayan kutu sayısı.</summary>
        public readonly int PlacedCount;

        /// <summary>Zemine oturmuş kutu sayısı. Bir tanesi kulenin temeli, fazlası ıskadır.</summary>
        public readonly int GroundedCount;

        /// <summary>Bir parça ölüm yüksekliğinin altına düştü mü?</summary>
        public readonly bool AnyFallen;

        /// <summary>Tur boyunca ulaşılmış en yüksek oturmuş kule boyu.</summary>
        public readonly float PeakHeight;

        /// <summary>Yığın bırakıldıktan sonra kesintisiz duruyor mu? Sıradaki kutunun geleceği an budur.</summary>
        public readonly bool Settled;

        /// <summary>
        /// Kulenin **gerçekten** kıpırdamadan geçirdiği kesintisiz süre (sn).
        ///
        /// <see cref="Settled"/>'dan ayrı bir ölçü, çünkü ikisi farklı soruya
        /// cevap veriyor. "Sıradaki kutu gelebilir mi" sorusu gevşek bir eşikle
        /// cevaplanabilir; "bu kule ayakta kaldı mı" sorusu cevaplanamaz. Yavaşça
        /// devrilen bir kule gevşek eşiğin altında kalıp duruyormuş gibi görünüyor.
        /// </summary>
        public readonly float SteadyTime;

        public StackSnapshot(
            float height,
            float peakHeight,
            int placedCount,
            int groundedCount,
            bool anyFallen,
            bool settled,
            float steadyTime)
        {
            Height = height;
            PeakHeight = peakHeight;
            PlacedCount = placedCount;
            GroundedCount = groundedCount;
            AnyFallen = anyFallen;
            Settled = settled;
            SteadyTime = steadyTime;
        }

        /// <summary>
        /// Kutu ıskalandı mı: zemine oturmuş ikinci bir kutu var demek, kulenin
        /// üstüne konmamış bir kutu var demek.
        ///
        /// İlk kutu her zaman zemine oturuyor, o kulenin temeli. İkincisi
        /// zemine oturduysa oyuncu ya kuleyi ıskalamış ya da yığmak yerine
        /// yan yana dizmeye başlamış. İkincisi de bilerek kapatıldı: geniş bir
        /// taban kuleyi sağlamlaştırıyor ama oyun "yığ" diyor.
        ///
        /// Bunu ölçmenin başka yolu yoktu. "Kutu kuleye değdi mi" sorusu
        /// çarpışmayla cevaplanabilirdi ama kuleden sekip yere düşen kutu da
        /// değmiş sayılırdı; bakılması gereken şey temas değil, kutunun nerede
        /// durduğu.
        /// </summary>
        public bool Missed => GroundedCount > 1;

        /// <summary>
        /// Kule çöktü mü: oturmuş boy, tur boyunca ulaşılan zirvenin bu kadar
        /// altına düştüyse tepeden en az bir kutu gitmiş demektir.
        ///
        /// "Bir kutu zeminin altına düştü mü" sorusu bu oyunda işe yaramıyor:
        /// zemin 14 birim geniş, kuleden devrilen kutu yere oturuyor ve hiçbir
        /// zaman ölüm yüksekliğinin altına inmiyor. Ölçülmesi gereken şey
        /// kutunun nereye gittiği değil, kulenin kısalması.
        /// </summary>
        public bool Collapsed(float drop) => PeakHeight - Height > drop;
    }
}
