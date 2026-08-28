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
        /// Turun o anki çevresel tehditleri.
        ///
        /// Önce tur başına sabit bir özellikti ("rüzgâr ve top sahnede duran
        /// şeyler, her kutuda yeniden pazarlık edilmiyorlar"). Sonsuz modda
        /// tehdidin tur içinde büyümesi gerekince bu doğru olmaktan çıktı.
        /// Seviye modu için hiçbir şey değişmiyor: orada cevap anlık görüntüden
        /// bağımsız, hep aynı.
        ///
        /// Kutu başına sorulan bir soru, kare başına sorulan bir soru değil:
        /// tehdit yalnızca yeni kutu istendiğinde yeniden hesaplanıyor, yani
        /// zorluk basamak basamak artıyor, sürekli kayan bir zemin gibi değil.
        /// </summary>
        HazardSettings HazardsFor(in StackSnapshot snapshot);

        /// <summary>
        /// Bu kutu sayısına ulaşıldığında kulenin altı dondurulsun mu?
        ///
        /// Kontrol noktası, o ana kadar oturmuş bütün kutuları kinematik yapıyor:
        /// artık itilemiyorlar, kule için yeni bir zemin oluyorlar. Amaç kolaylık
        /// değil, tavan: sallanma en alttan başlıyor ve her kutu onu biraz daha
        /// büyütüyor, yani belli bir yükseklikten sonra kule oyuncunun becerisiyle
        /// değil birikmiş salınımla devriliyor. Altı sabitlenince yüksekliğin
        /// üst sınırı kalkıyor — "seviye 100'de hâlâ 15 kutu" demek zorunda
        /// kalmamanın yolu bu.
        ///
        /// Kutu sayısına bakıyor, yüksekliğe değil: oyuncunun yaptığı iş kutu
        /// koymak ve "10 kutuda bir" sayılabilir bir söz, "6 birimde bir" ise
        /// kutu genişliklerine göre değişen bir söz olurdu.
        /// </summary>
        bool IsCheckpoint(int placedCount);
    }
}
