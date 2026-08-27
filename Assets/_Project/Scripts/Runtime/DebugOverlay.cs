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
        [SerializeField] StackCamera stackCamera;

        [Tooltip("Kapatınca hiçbir şey çizilmiyor. Build'e bu kapalı gitmeli mi diye Gün 5'te karar vereceğim.")]
        [SerializeField] bool visible = true;

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
            if (!visible || controller == null)
            {
                return;
            }

            EnsureStyles();

            var tracker = controller.Tracker;
            var rules = controller.Rules;

            // Panelde yerleştirilmiş kule gösteriliyor, eldeki kutu dahil değil:
            // oyuncu kutuyu havada tutarken sayının bir zıplayıp geri düşmesi
            // "kule ne kadar yüksek" sorusunu cevaplamaz, bulandırır.
            float height = tracker != null ? tracker.HighestRestingPointY() : 0f;
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

            GUILayout.Label($"{title} · {Describe(controller.State)} · {controller.RestTimer:0.0} sn", style);

            GUILayout.Label(
                target > 0f
                    ? $"kule {height:0.00} / {target:0.00} · {controller.ScoreText}"
                    : $"kule {height:0.00} · {controller.ScoreText}",
                style);

            if (stackCamera != null)
            {
                GUILayout.Label($"kadraj {stackCamera.FrameBottomY:0.0} → {stackCamera.FrameTopY:0.0} · {(float)Screen.width / Screen.height:0.00}", style);
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
            GameState.Won => "KAZANDIN",
            GameState.Lost => "KAYBETTIN",
            _ => state.ToString(),
        };
    }
}
