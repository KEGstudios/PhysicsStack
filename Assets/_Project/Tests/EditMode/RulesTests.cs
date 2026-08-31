using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

namespace PhysicsStack.Tests
{
    /// <summary>
    /// Kural katmanının testleri.
    ///
    /// Bu testlerin yazılabilir olması bir tasarım kararının sonucu: kural
    /// sınıfları MonoBehaviour değil, düz C#; karar verirken sahneye değil
    /// <see cref="StackSnapshot"/>'a bakıyorlar ve o da salt-okunur bir struct.
    /// Yani "kule şu hâldeyken tur ne olur" sorusu, Unity çalıştırmadan
    /// sorulabilir bir soru — sahneyi kurmak, fizik adımı beklemek, kutu
    /// düşürmek gerekmiyor.
    ///
    /// Tersi de doğru: burada test edilemeyen şey <see cref="StackTracker"/>'ın
    /// ölçümü, yani "kule gerçekten 4 birim mi". O fizik işi ve testi cihazda
    /// oynayarak yapılıyor. Testler kuralın ölçüyü doğru yorumladığını
    /// doğruluyor, ölçünün kendisini değil.
    /// </summary>
    public sealed class RulesTests
    {
        // ---------------------------------------------------------------
        // Yardımcılar
        // ---------------------------------------------------------------

        /// <summary>
        /// Anlık görüntü kurucusu. <see cref="StackSnapshot"/>'ın yapıcısı yedi
        /// sırasız parametre alıyor; testte <c>new(4, 4, 4, 1, false, true, 2f)</c>
        /// yazmak, testin neyi anlattığını okunmaz hâle getirirdi.
        ///
        /// Varsayılanlar "sağlıklı kule" durumu: hiçbir şey düşmemiş, yığın
        /// oturmuş. Her test yalnızca kendi konusu olan alanı değiştiriyor, yani
        /// testin gövdesindeki tek satır testin adını da açıklıyor.
        /// </summary>
        sealed class Snap
        {
            public float Height;

            /// <summary>Verilmezse boyla aynı — kule zirvesinde demek.</summary>
            public float PeakHeight = float.NaN;

            public int PlacedCount;

            /// <summary>Bir tanesi kulenin temeli; varsayılan hiç düşmemiş demek.</summary>
            public int GroundedCount = 1;

            public bool AnyFallen;
            public bool Settled = true;
            public float SteadyTime;

            public StackSnapshot Build() => new(
                Height,
                float.IsNaN(PeakHeight) ? Height : PeakHeight,
                PlacedCount,
                GroundedCount,
                AnyFallen,
                Settled,
                SteadyTime);

            public static implicit operator StackSnapshot(Snap snap) => snap.Build();
        }

        readonly List<LevelDefinition> created = new();

        /// <summary>
        /// Test seviyesi. <see cref="LevelDefinition"/> bir ScriptableObject —
        /// gerçek varlık dosyalarını okumak testi seviye verisine bağlardı ve
        /// seviye ayarı değiştiğinde alakasız testler kırılırdı.
        /// </summary>
        LevelDefinition Level(
            int targetBoxes = 4,
            float holdTime = 1.5f,
            float collapseDrop = 0.6f,
            float checkpointEvery = 0f)
        {
            var level = ScriptableObject.CreateInstance<LevelDefinition>();
            level.targetBoxes = targetBoxes;
            level.holdTime = holdTime;
            level.collapseDrop = collapseDrop;
            level.checkpointEvery = checkpointEvery;
            created.Add(level);
            return level;
        }

        [TearDown]
        public void TearDown()
        {
            // ScriptableObject sahneye ait değil, çöp toplayıcı da onu
            // toplamıyor: elle yok edilmezse Editor oturumu boyunca birikirler.
            foreach (var level in created)
            {
                Object.DestroyImmediate(level);
            }

            created.Clear();
        }

        // ---------------------------------------------------------------
        // StackSnapshot — ölçünün yorumu
        // ---------------------------------------------------------------

        [Test]
        public void IlkZemindekiKutuKuleninTemeli_DusenSayilmaz()
        {
            var snapshot = new Snap { GroundedCount = 1 }.Build();

            Assert.AreEqual(0, snapshot.DroppedCount);
        }

        [Test]
        public void ZemindekiFazlaKutularDusmusSayilir()
        {
            var snapshot = new Snap { GroundedCount = 3 }.Build();

            Assert.AreEqual(2, snapshot.DroppedCount);
        }

        [Test]
        public void HicKutuOturmamissaDusenSayisiNegatifOlmaz()
        {
            // Turun ilk anı: daha hiçbir kutu zemine değmemiş.
            var snapshot = new Snap { GroundedCount = 0 }.Build();

            Assert.AreEqual(0, snapshot.DroppedCount);
        }

        [Test]
        public void TemasGomulmesiHedefiKacirtmaz()
        {
            // On kutuluk kule PhysX'in temas payı yüzünden 9.99 ölçülüyor.
            // Doğrudan karşılaştırma bu seviyeyi geçilemez yapardı.
            var snapshot = new Snap { Height = 9.99f }.Build();

            Assert.IsTrue(snapshot.Reached(10f));
        }

        [Test]
        public void PayBirKutuyuGecmez()
        {
            // Pay oransal ama tavanı var. Tavansız olsaydı 100 birimlik eşikte
            // pay 2 birim olur, yani eşik iki kutu erken tetiklenirdi — düzeltmeye
            // çalıştığımız hatanın aynısı, ters yönde.
            var snapshot = new Snap { Height = 99.5f }.Build();

            Assert.IsFalse(snapshot.Reached(100f));
        }

        [Test]
        public void GercektenKisaKuleHedefeUlasmisSayilmaz()
        {
            var snapshot = new Snap { Height = 9.5f }.Build();

            Assert.IsFalse(snapshot.Reached(10f));
        }

        [Test]
        public void ZirvedenDusmekCokusDemek()
        {
            var snapshot = new Snap { Height = 9.3f, PeakHeight = 10f }.Build();

            Assert.IsTrue(snapshot.Collapsed(0.6f));
        }

        [Test]
        public void SallanmaCokusSayilmaz()
        {
            var snapshot = new Snap { Height = 9.8f, PeakHeight = 10f }.Build();

            Assert.IsFalse(snapshot.Collapsed(0.6f));
        }

        // ---------------------------------------------------------------
        // LevelRules — seviye modunun kararları
        // ---------------------------------------------------------------

        [Test]
        public void HedefeUlasipTutunanKuleKazanir()
        {
            var rules = new LevelRules(Level(targetBoxes: 4, holdTime: 1.5f));
            var snapshot = new Snap { Height = 4f, PlacedCount = 4, SteadyTime = 2f };

            Assert.AreEqual(RunOutcome.Won, rules.Evaluate(snapshot));
        }

        [Test]
        public void HedefeUlasipHenuzTutunmayanKuleAskida()
        {
            // Pending, Continue'dan farklı: sıradaki kutu verilmiyor ama tur da
            // bitmiş değil.
            var rules = new LevelRules(Level(targetBoxes: 4, holdTime: 1.5f));
            var snapshot = new Snap { Height = 4f, PlacedCount = 4, SteadyTime = 0.5f };

            Assert.AreEqual(RunOutcome.Pending, rules.Evaluate(snapshot));
        }

        [Test]
        public void OturmamisYiginKazanamaz()
        {
            // Sallanan kule bir kare için hedefi geçip sonra devrilebilir.
            var rules = new LevelRules(Level(targetBoxes: 4, holdTime: 1.5f));
            var snapshot = new Snap
            {
                Height = 4f, PlacedCount = 4, SteadyTime = 5f, Settled = false,
            };

            Assert.AreEqual(RunOutcome.Continue, rules.Evaluate(snapshot));
        }

        [Test]
        public void YereDusenKutularHedefeSaymaz()
        {
            // Altı kutu atıldı, ikisi yere düştü: kulede dört kutu var, hedef beş.
            var rules = new LevelRules(Level(targetBoxes: 5, holdTime: 1.5f));
            var snapshot = new Snap
            {
                Height = 4f, PlacedCount = 6, GroundedCount = 3, SteadyTime = 5f,
            };

            Assert.AreEqual(RunOutcome.Continue, rules.Evaluate(snapshot));
        }

        [Test]
        public void UcuncuDusenKutuTuruBitirir()
        {
            var rules = new LevelRules(Level(targetBoxes: 4));
            var snapshot = new Snap { Height = 2f, PlacedCount = 5, GroundedCount = 4 };

            Assert.AreEqual(3, snapshot.Build().DroppedCount);
            Assert.AreEqual(RunOutcome.Lost, rules.Evaluate(snapshot));
        }

        [Test]
        public void IkiKutuDusurmekTuruBitirmez()
        {
            var rules = new LevelRules(Level(targetBoxes: 8));
            var snapshot = new Snap { Height = 2f, PlacedCount = 4, GroundedCount = 3 };

            Assert.AreEqual(RunOutcome.Continue, rules.Evaluate(snapshot));
        }

        [Test]
        public void ZeminAltinaDusenParcaSuruklenirkenDeTuruBitirir()
        {
            // Bu kontrol oturmayı beklemiyor: yığın hâlâ hareket hâlindeyken de
            // kaybediyorsun.
            var rules = new LevelRules(Level(targetBoxes: 4));
            var snapshot = new Snap { Height = 3f, PlacedCount = 3, AnyFallen = true, Settled = false };

            Assert.AreEqual(RunOutcome.Lost, rules.Evaluate(snapshot));
        }

        [Test]
        public void CokenKuleHedefinUstundeOlsaBileKaybeder()
        {
            // Tepeden kutu gitti ama kalan kule hâlâ hedefin üstünde. Çöküş
            // kontrolü hedef kontrolünden önce geldiği için tur kayıp.
            var rules = new LevelRules(Level(targetBoxes: 4, collapseDrop: 0.6f));
            var snapshot = new Snap
            {
                Height = 8f, PeakHeight = 10f, PlacedCount = 10, SteadyTime = 5f,
            };

            Assert.AreEqual(RunOutcome.Lost, rules.Evaluate(snapshot));
        }

        [Test]
        public void SeviyeSkoruHarcananKutuSayisi()
        {
            var rules = new LevelRules(Level(targetBoxes: 4));
            var snapshot = new Snap { Height = 4f, PlacedCount = 6, GroundedCount = 3 };

            Assert.AreEqual(6f, rules.Score(snapshot));
        }

        [Test]
        public void KontrolNoktasiKapaliysaSonsuzDoner()
        {
            var rules = new LevelRules(Level(checkpointEvery: 0f));

            Assert.AreEqual(float.PositiveInfinity, rules.CheckpointAfter(5f));
        }

        [Test]
        public void YildizDusenKutuyuOlcuyor()
        {
            var level = Level(targetBoxes: 4);

            Assert.AreEqual(3, level.StarsFor(0));
            Assert.AreEqual(2, level.StarsFor(1));
            Assert.AreEqual(0, level.StarsFor(5));
        }

        [Test]
        public void YanSutunlarKuleyeYazilmiyor()
        {
            // Oynarken çıkan hata. Zemine üç kutu atılıp üstlerine 4-2-2 diye
            // yığıldığında sekiz kutu atılmış, ikisi düşmüş sayılıyordu ve
            // "atılan eksi düşen" altı veriyordu — hedefi altı olan seviye, en
            // yüksek sütunu dört kutuyken geçiliyordu.
            //
            // Ölçü boya bağlanınca sömürü kapandı: yan yana dizilen sütunlar
            // boyu artırmıyor.
            var rules = new LevelRules(Level(targetBoxes: 6, holdTime: 1.5f));
            var snapshot = new Snap
            {
                Height = 4f, PlacedCount = 8, GroundedCount = 3, SteadyTime = 5f,
            };

            Assert.AreEqual(4, snapshot.Build().TowerBoxes);
            Assert.AreEqual(RunOutcome.Continue, rules.Evaluate(snapshot));
        }

        [Test]
        public void KuleKutuSayisiTemasGomulmesineTakilmiyor()
        {
            // Altı kutuluk kule biriken gömülme yüzünden 5.97 ölçülüyor.
            // Yarım birimlik yuvarlama payı bu hatadan kat kat büyük.
            var snapshot = new Snap { Height = 5.97f }.Build();

            Assert.AreEqual(6, snapshot.TowerBoxes);
        }

        // ---------------------------------------------------------------
        // EndlessRules — sonsuz modun kararları
        // ---------------------------------------------------------------

        [Test]
        public void SonsuzModdaHedefYok()
        {
            var rules = new EndlessRules();

            Assert.AreEqual(0f, rules.TargetHeight);
            Assert.AreEqual(0f, rules.HoldTime);
        }

        [Test]
        public void SonsuzModdaCokusTuruBitirir()
        {
            var rules = new EndlessRules(collapseDrop: 0.6f);
            var snapshot = new Snap { Height = 12f, PeakHeight = 14f, PlacedCount = 14 };

            Assert.AreEqual(RunOutcome.Lost, rules.Evaluate(snapshot));
        }

        [Test]
        public void OturmamisYigininCokusuHenuzKararDegil()
        {
            // Kule hâlâ hareket hâlinde; devrilen kutu geri oturabilir.
            var rules = new EndlessRules(collapseDrop: 0.6f);
            var snapshot = new Snap
            {
                Height = 12f, PeakHeight = 14f, PlacedCount = 14, Settled = false,
            };

            Assert.AreEqual(RunOutcome.Continue, rules.Evaluate(snapshot));
        }

        [Test]
        public void DusurmeHakkiIkiModdaAyni()
        {
            // Kuralın kendisi: aynı el hareketi iki modda aynı sonucu vermeli.
            // Bu test sayının paylaşılan sabitten geldiğini değil, iki modun
            // gerçekten aynı davrandığını doğruluyor.
            var level = new LevelRules(Level(targetBoxes: 20));
            var endless = new EndlessRules();

            var ucuncuDusen = new Snap { Height = 5f, PlacedCount = 8, GroundedCount = 4 }.Build();
            var ikinciDusen = new Snap { Height = 5f, PlacedCount = 7, GroundedCount = 3 }.Build();

            Assert.AreEqual(RunOutcome.Lost, level.Evaluate(ucuncuDusen));
            Assert.AreEqual(RunOutcome.Lost, endless.Evaluate(ucuncuDusen));

            Assert.AreEqual(RunOutcome.Continue, level.Evaluate(ikinciDusen));
            Assert.AreEqual(RunOutcome.Continue, endless.Evaluate(ikinciDusen));
        }

        [Test]
        public void SonsuzSkorKuleBoyu_KutuSayisiDegil()
        {
            // Yere yan yana dizilen kutular skoru artırmamalı.
            var rules = new EndlessRules();
            var snapshot = new Snap { Height = 8f, PeakHeight = 12f, PlacedCount = 30 };

            Assert.AreEqual(12f, rules.Score(snapshot));
        }

        [Test]
        public void KontrolNoktalariAralikBuyuterekIlerliyor()
        {
            var rules = new EndlessRules();

            Assert.AreEqual(10f, rules.CheckpointAfter(0f));
            Assert.AreEqual(25f, rules.CheckpointAfter(10f));
            Assert.AreEqual(45f, rules.CheckpointAfter(25f));
            Assert.AreEqual(70f, rules.CheckpointAfter(45f));
        }

        [Test]
        public void ZorlukTepeyeVarincaSabitleniyor()
        {
            var rules = new EndlessRules();

            float basta = rules.NextBox(new Snap { Height = 0f }).DropGap;
            float tepede = rules.NextBox(new Snap { Height = 15f }).DropGap;
            float cokSonra = rules.NextBox(new Snap { Height = 40f }).DropGap;

            Assert.Less(basta, tepede);
            Assert.AreEqual(tepede, cokSonra, 0.0001f);
        }

        [Test]
        public void TehditlerSirayaDizilmis()
        {
            var rules = new EndlessRules();

            var alcak = rules.HazardsFor(new Snap { Height = 3f });
            var orta = rules.HazardsFor(new Snap { Height = 10f });
            var yuksek = rules.HazardsFor(new Snap { Height = 20f });

            // Rüzgâr 6 birimden önce yok, sonra artıyor.
            Assert.AreEqual(0f, alcak.windSpeed, 0.0001f);
            Assert.Greater(orta.windSpeed, 0f);

            // Namlu en son geliyor: rüzgârla aynı anda gelseydi oyuncu neyi
            // yanlış yaptığını göremezdi.
            Assert.AreEqual(0, orta.cannonCount);
            Assert.AreEqual(1, yuksek.cannonCount);
        }
    }
}
