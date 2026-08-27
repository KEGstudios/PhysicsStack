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

        /// <summary>Bir parça ölüm yüksekliğinin altına düştü mü?</summary>
        public readonly bool AnyFallen;

        /// <summary>Tur boyunca ulaşılmış en yüksek oturmuş kule boyu.</summary>
        public readonly float PeakHeight;

        /// <summary>Yığın bırakıldıktan sonra kesintisiz duruyor mu? Karar anı budur.</summary>
        public readonly bool Settled;

        public StackSnapshot(float height, float peakHeight, int placedCount, bool anyFallen, bool settled)
        {
            Height = height;
            PeakHeight = peakHeight;
            PlacedCount = placedCount;
            AnyFallen = anyFallen;
            Settled = settled;
        }

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
