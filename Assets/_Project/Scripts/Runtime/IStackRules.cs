namespace PhysicsStack
{
    /// <summary>Bir anlık görüntüye bakınca turun durumu.</summary>
    public enum RunOutcome
    {
        /// <summary>Tur devam ediyor.</summary>
        Continue,

        /// <summary>
        /// Karar askıda: sonuç belli değil ama sıradaki kutu da verilmemeli.
        /// Kule hedefi geçtiğinde tutunmasını beklerken bu durumdayız.
        /// </summary>
        Pending,

        /// <summary>Turun hedefi tamamlandı.</summary>
        Won,

        /// <summary>Tur bitti, hedefe ulaşılamadı.</summary>
        Lost,
    }

    /// <summary>
    /// Bir modun kuralları. <see cref="StackGameController"/> artık "kim kazandı"yı
    /// bilmiyor, sadece soruyor.
    ///
    /// Neden arayüz: iki modu tek sınıfa <c>if (endless)</c> ile sığdırmak bugün
    /// çalışır, üçüncü bir şey eklendiğinde okunmaz hâle gelir. Asıl kazanç şu:
    /// controller'ın işi (girdi dinlemek, sıradaki kutuyu istemek, durumu
    /// yayınlamak) iki modda da birebir aynı. Değişen tek şey "bu anlık görüntü
    /// ne anlama geliyor" sorusunun cevabı — ayrılması gereken yer tam orası.
    ///
    /// Kural sınıfları MonoBehaviour değil, düz C#: sahneye bağlı olmadıkları
    /// için mod değiştirmek bir nesne değiştirmek kadar ucuz.
    /// </summary>
    public interface IStackRules
    {
        /// <summary>Ekranda görünecek mod adı.</summary>
        string Title { get; }

        /// <summary>Hedef yükseklik. Sıfır ya da altı "hedef yok" demek.</summary>
        float TargetHeight { get; }

        /// <summary>
        /// Hedefi geçtikten sonra tutunması gereken süre (sn). Sıfır ise böyle
        /// bir şart yok. Panel ilerlemeyi bunun üstünden gösteriyor.
        /// </summary>
        float HoldTime { get; }

        /// <summary>Bu anlık görüntüde tur devam ediyor mu, bitti mi?</summary>
        RunOutcome Evaluate(in StackSnapshot snapshot);

        /// <summary>
        /// Turun skoru. Neyin skor sayıldığına mod karar veriyor; ondalıklı,
        /// çünkü bir modda kutu sayısı diğerinde kule boyu.
        /// </summary>
        float Score(in StackSnapshot snapshot);

        /// <summary>Skoru ekranda okunacak hâle getirir. Birim de moda ait.</summary>
        string DescribeScore(float score);

        /// <summary>
        /// Sıradaki kutu nasıl gelsin? Seviye modunda sabit — seviyenin verisi;
        /// sonsuz modda yığdıkça büyüyen bir eğri.
        /// </summary>
        BoxDifficulty NextBox(in StackSnapshot snapshot);

        /// <summary>
        /// Turun çevresel tehditleri. Kutu başına değil tur başına: rüzgâr ve top
        /// sahnede duran şeyler, her kutuda yeniden pazarlık edilmiyorlar.
        /// </summary>
        HazardSettings Hazards { get; }
    }
}
