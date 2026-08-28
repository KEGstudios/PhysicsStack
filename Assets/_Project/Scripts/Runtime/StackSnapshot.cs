using UnityEngine;

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
        /// Kuleye girmeyip zemine düşen kutu sayısı: zemindeki kutulardan
        /// birincisi kulenin temeli, gerisi düşmüş demektir.
        ///
        /// İki ayrı olayı tek sayıda topluyor ve bu bilinçli: kuleyi ıskalayıp
        /// yere düşen kutu ile kuleden devrilip yere inen kutu, oyuncu açısından
        /// aynı şey — bir kutu kaybettin. Ayrı ayrı saymak iki farklı ceza
        /// üretirdi ve oyuncunun ikisini birbirinden ayırması için hiçbir sebep
        /// yok.
        /// </summary>
        public int DroppedCount => Mathf.Max(0, GroundedCount - 1);

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

        /// <summary>
        /// Ölçülen boyun kaçta kaçı temas gömülmesine gidebilir. Eşikler bu pay
        /// kadar aşağı çekiliyor, üst ve alt sınırlarla birlikte.
        /// </summary>
        const float HeightSlack = 0.02f;
        const float MinSlack = 0.03f;
        const float MaxSlack = 0.4f;

        /// <summary>
        /// Kule verilen yüksekliğe ulaştı mı?
        ///
        /// Doğrudan karşılaştırma yapmıyor ve sebebi fizik: PhysX üst üste duran
        /// cisimlerin birbirine milimetrik gömülmesine izin veriyor. Tek kutuda
        /// görünmüyor ama gömülmeler toplanıyor — on kutuluk kule 10.00 değil
        /// 9.99 ölçülüyor. Sonuç sistematik bir kutu kayması: hedefi 8 olan
        /// seviyeyi 8 kutuyla geçemiyorsun, sonsuz modda "6 birimde rüzgâr"
        /// 7. kutuda başlıyor.
        ///
        /// Payın oransal olmasının sebebi hatanın da oransal olması: her temas
        /// bir miktar gömülme ekliyor, yani hata kule uzadıkça büyüyor. Üst
        /// sınır bir kutunun epey altında — pay bir kutuyu geçseydi eşikler bu
        /// kez erken tetiklenirdi ve o, düzeltmeye çalıştığımız hatanın aynısı
        /// olurdu.
        ///
        /// Alternatif, fizikteki temas payını küçültmekti. Onu yapmadım: o sayı
        /// kulenin kararlılığını da belirliyor ve bir görüntü sorunu için
        /// simülasyonu bozmak, sorunu ölçüden çözmekten pahalı.
        /// </summary>
        public bool Reached(float threshold) =>
            Height >= threshold - Mathf.Clamp(threshold * HeightSlack, MinSlack, MaxSlack);
    }
}
