using UnityEngine;

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

        /// <summary>
        /// Hedefin yükseklik karşılığı: hedef çizgisi ve kamera bunu okuyor.
        /// Kutular sabit 1 birim olduğu için sayı hedef kutu sayısıyla aynı,
        /// yani çizgi hâlâ "kulenin çıkması gereken yer"i gösteriyor.
        /// </summary>
        public float TargetHeight => level.TargetHeight;

        public float HoldTime => level.holdTime;

        /// <summary>Seviyede tehdit sabit: cevap anlık görüntüye bakmıyor.</summary>
        public HazardSettings HazardsFor(in StackSnapshot snapshot) => level.hazards;

        /// <summary>
        /// Seviyede kontrol noktası isteğe bağlı ve varsayılanı kapalı: on bir
        /// seviyenin hiçbiri buna ihtiyaç duymuyor, kuleleri 3-8 birim.
        ///
        /// Alanı yine de ekledim, çünkü asıl sebebi ileride yüksek kule isteyen
        /// bir seviye tasarlayabilmek. Verinin hazır olması, o seviyeyi
        /// tasarlarken mekaniği baştan yazmak zorunda kalmamak demek.
        /// </summary>
        public float CheckpointAfter(float height)
        {
            float every = level.checkpointEvery;

            if (every <= 0f)
            {
                return float.PositiveInfinity;
            }

            return (Mathf.Floor(height / every) + 1f) * every;
        }

        public RunOutcome Evaluate(in StackSnapshot snapshot)
        {
            // Düşme kontrolü oturmayı beklemiyor: sürükleme sırasında devrilen
            // eski bir kutu da turu bitirir.
            if (snapshot.AnyFallen)
            {
                return RunOutcome.Lost;
            }

            // Seviyede üç kutu düşürmek turu bitiriyor. Sonsuz modda tek kutu
            // yetiyor ve fark bilerek: orada tur zaten "nereye kadar
            // dayanabilirsin" sorusu, burada ise oyuncunun öğrenmesi gereken bir
            // düzen var ve öğrenmek hata yapmayı gerektiriyor. İki kutuluk pay,
            // yıldızın da ölçüsü — yani hata cezasız değil, kademeli.
            if (snapshot.DroppedCount >= LevelDefinition.MaxDrops)
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

            // Hedef kutu sayısına ulaşmak yetmiyor, kulenin orada durması da
            // gerekiyor. Ulaşmak bir an, tutunmak bir süre — ve bu oyunda ayakta
            // kalan kule ile devrilmek üzere olan kule arasındaki fark tam
            // olarak o süre.
            //
            // Sayılan şey kuledeki kutu, atılan kutu değil: yere düşenler
            // hedefe saymıyor. Önce hedef yükseklikti ve ölçüm kulenin boyuydu;
            // ikisi kutu 1 birim olduğu için hemen hemen aynı sayı ama aynı şey
            // değil. Yükseklik ölçümü temas gömülmesine takılıyordu (on kutuluk
            // kule 9.99 okunuyor), kutu saymak ise tam sayı: "dört kutu koy"
            // diyen bir seviye dört kutuda geçiliyor.
            if (TowerBoxes(snapshot) >= level.targetBoxes)
            {
                return snapshot.SteadyTime >= level.holdTime
                    ? RunOutcome.Won
                    : RunOutcome.Pending;
            }

            return RunOutcome.Continue;
        }

        /// <summary>Kulede duran kutu sayısı: atılanlardan yere düşenler çıkarılıyor.</summary>
        static int TowerBoxes(in StackSnapshot snapshot) =>
            snapshot.PlacedCount - snapshot.DroppedCount;

        /// <summary>
        /// Seviyede skor "kaç kutu harcadın". Az olan iyi — sonsuz moddaki skorun
        /// tam tersi yönde, bu yüzden en iyi skor karşılaştırması mod başına
        /// yapılıyor.
        ///
        /// Hedef kutu sayısına dönünce bu sayı doğrudan hatayı gösterir oldu:
        /// kusursuz bir tur tam hedef kadar kutu harcıyor, fazlası düşen kutu
        /// demek. Yıldız da zaten aradaki farkı okuyor.
        /// </summary>
        public float Score(in StackSnapshot snapshot) => snapshot.PlacedCount;

        public string DescribeScore(float score) => $"{score:0} kutu";

        /// <summary>Seviye boyunca sabit: zorluk turun içinde değil seviyeler arasında artıyor.</summary>
        public BoxDifficulty NextBox(in StackSnapshot snapshot) =>
            new(level.dropGap, level.widthVariance, level.spawnLift);

        public override string ToString() =>
            $"{level.title} · hedef {level.targetBoxes} kutu · " +
            $"{LevelDefinition.MaxDrops - 1} dusurme hakki · mesafe {level.dropGap:0.0}";
    }
}
