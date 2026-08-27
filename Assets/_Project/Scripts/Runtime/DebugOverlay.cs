using UnityEngine;

namespace PhysicsStack
{
    /// <summary>
    /// Ekranın köşesine oyunun iç durumunu basar: hangi durumdayız, kule ne kadar
    /// yüksek, yığın ne kadar süredir duruyor.
    ///
    /// Neden OnGUI? Bu bir arayüz değil, bir ölçü aleti. Canvas kurmak sahneye
    /// dört beş nesne daha ekler ve prototipi "menüsü olan oyun"a benzetmeye
    /// başlar; oysa buradaki her satırın işi build alındıktan sonra bitiyor.
    /// OnGUI tek dosyada duruyor, componenti kapatınca hiç iz bırakmıyor.
    /// Oyunun kendi arayüzü olsaydı OnGUI yanlış seçim olurdu — her karede
    /// çöp üretir ve dokunmatik ölçeklemesi yoktur.
    ///
    /// Asıl kazancı telefonda: konsol logu göremediğim yerde "kutu neden
    /// yerleşmiş sayılmıyor" sorusunu ancak oturma sayacını gözümle görerek
    /// cevaplayabiliyorum.
    /// </summary>
    public sealed class DebugOverlay : MonoBehaviour
    {
        [SerializeField] StackGameController controller;
        [SerializeField] DragSettings settings;
        [Tooltip("Bırakma çizgisini panele basmak için; kısıtı fiilen taşıyan nesne kutunun kendisi.")]
        [SerializeField] BoxQueue queue;

        [Tooltip("Rüzgâr açıksa o anki değeri panele basmak için.")]
        [SerializeField] Wind wind;

        [Tooltip("Varsayılan olarak kapalı: oyuncunun göreceği bilgiler artık HudUI'de. Burası geliştirici aracı.")]
        [SerializeField] bool visible;

        GUIStyle style;
        GUIStyle boxStyle;

        void Awake()
        {
            if (controller == null)
            {
                controller = FindAnyObjectByType<StackGameController>();
            }
        }

        void OnGUI()
        {
            // Menüde ölçü aletine gerek yok; panel orada sadece arayüzün üstünü
            // kirletiyor.
            if (!visible || controller == null || controller.State == GameState.Menu)
            {
                return;
            }

            EnsureStyles();

            var tracker = controller.Tracker;
            var rules = controller.Rules;

            // Panelde yerleştirilmiş kule gösteriliyor, eldeki kutu dahil değil:
            // oyuncu kutuyu havada tutarken sayının bir zıplayıp geri düşmesi
            // "kule ne kadar yüksek" sorusunu cevaplamaz, bulandırır.
            float height = tracker != null ? tracker.HighestSettledPointY() : 0f;
            float target = controller.TargetHeight;

            float pad = Screen.height * 0.012f;

            // Kutunun genişliği içeriğe göre belirlensin: sabit oran verdiğimde
            // portre ekranda yazı kutunun dışına taşıyordu. ExpandWidth(false)
            // ile GUILayout kutuyu en uzun satıra göre ölçüyor.
            GUILayout.BeginArea(new Rect(pad, pad, Screen.width - pad * 2f, Screen.height * 0.5f));
            GUILayout.BeginVertical(boxStyle, GUILayout.ExpandWidth(false));

            // İlk satır modu da yazıyor: iki kural seti aynı sahnede çalıştığı
            // için "hangi modu test ediyorum" sorusu telefonda tek bakışta
            // cevaplanabilmeli.
            string title = rules != null ? rules.Title : "-";

            // Tutunurken sayaç geri sayım gibi okunsun: oyuncunun beklediği şey
            // "durdu mu" değil, "ne kadar daha".
            string timer = controller.State == GameState.Holding && controller.HoldTime > 0f
                ? $"{controller.SteadyTimer:0.0}/{controller.HoldTime:0.0} sn"
                : $"{controller.RestTimer:0.0} sn";

            GUILayout.Label($"{title} · {Describe(controller.State)} · {timer}", style);

            GUILayout.Label(
                target > 0f
                    ? $"kule {height:0.00} / {target:0.00} · {controller.ScoreText}"
                    : $"kule {height:0.00} · {controller.ScoreText}",
                style);

            // Kadraj satırı buradan kalktı: portre çerçevesi Gün 6'da oturdu, artık
            // her karede bakılacak bir sayı değil. Yerini günün asıl kolu aldı.
            var current = queue != null ? queue.Current : null;

            if (current != null)
            {
                // Rüzgâr ancak açıkken satıra giriyor: kapalı bir tehdidi her
                // karede "0.0" diye yazmak paneli uzatmaktan başka bir işe yaramaz.
                string gust = wind != null && wind.Active ? $" · rüzgâr {wind.CurrentForce:+0.0;-0.0}" : "";
                GUILayout.Label($"çizgi {current.DropLineY:0.00} · mesafe {current.DropLineY - height:0.00}{gust}", style);
            }

            if (settings != null)
            {
                GUILayout.Label($"takip {settings.followStrength:0.00} · hız {settings.maxSpeed:0} · ivme {settings.maxAcceleration:0}", style);
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        /// <summary>
        /// Stiller ilk çizimde kuruluyor: OnGUI'de her karede new GUIStyle()
        /// çağırmak çöp toplayıcıyı boş yere meşgul ediyor.
        /// </summary>
        void EnsureStyles()
        {
            if (style != null)
            {
                return;
            }

            // Telefonun piksel yoğunluğu masaüstünün üç katı olabiliyor; sabit
            // punto orada okunmaz hale geliyor. Ekran yüksekliğine oranlamak
            // bunu tek satırla çözüyor. Oran bilerek küçük: bu bir ölçü aleti,
            // oyunun önüne geçmemeli — ilk denemede panel kadrajın altıda birini
            // kaplıyor ve sıradaki kutuyu örtüyordu.
            style = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(Screen.height * 0.017f),
                normal = { textColor = new Color(0.88f, 0.88f, 0.9f) },
                padding = new RectOffset(0, 0, 1, 1),
            };

            boxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(8, 8, 6, 6),
            };
        }

        /// <summary>Durum adını enum yazımıyla değil oynarken anladığım dille yazıyorum.</summary>
        static string Describe(GameState state) => state switch
        {
            GameState.WaitingForDrag => "bekliyor",
            GameState.Dragging => "sürükleniyor",
            GameState.Settling => "yerleşiyor",
            GameState.Menu => "menü",
            GameState.Holding => "tutunuyor",
            GameState.Won => "KAZANDIN",
            GameState.Lost => "KAYBETTIN",
            _ => state.ToString(),
        };
    }
}
