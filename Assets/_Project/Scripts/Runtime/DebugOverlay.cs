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

        [Tooltip("Kapatınca hiçbir şey çizilmiyor. Build'e bu kapalı gitmeli mi diye Gün 5'te karar vereceğim.")]
        [SerializeField] bool visible = true;

        GUIStyle style;

        void Awake()
        {
            if (controller == null)
            {
                controller = FindFirstObjectByType<StackGameController>();
            }
        }

        void OnGUI()
        {
            if (!visible || controller == null)
            {
                return;
            }

            // Stil ilk çizimde kuruluyor: OnGUI'de new GUIStyle() her karede
            // çağrılırsa çöp toplayıcıyı boş yere meşgul ediyor.
            if (style == null)
            {
                style = new GUIStyle(GUI.skin.label)
                {
                    // Telefonun piksel yoğunluğu masaüstünün üç katı olabiliyor;
                    // sabit punto orada okunmaz hale geliyor. Ekran yüksekliğine
                    // oranlamak tek satırla bunu çözüyor.
                    fontSize = Mathf.RoundToInt(Screen.height * 0.028f),
                    normal = { textColor = Color.white },
                };
            }

            var tracker = controller.Tracker;
            float height = tracker != null ? tracker.HighestPointY() : 0f;
            int count = tracker != null ? tracker.Count : 0;

            float pad = Screen.height * 0.02f;
            var rect = new Rect(pad, pad, Screen.width - pad * 2f, Screen.height * 0.4f);

            // Arkasına koyu bir zemin: gri kutunun üstünde beyaz yazı okunmuyor.
            GUI.Box(new Rect(pad * 0.5f, pad * 0.5f, Screen.width * 0.52f, style.fontSize * 6.4f), GUIContent.none);

            GUILayout.BeginArea(rect);
            GUILayout.Label($"durum   : {Describe(controller.State)}", style);
            GUILayout.Label($"kule    : {height:0.00} / {controller.TargetHeight:0.00}", style);
            GUILayout.Label($"kutu    : {count}", style);
            GUILayout.Label($"oturma  : {controller.RestTimer:0.00} sn", style);

            if (settings != null)
            {
                GUILayout.Label($"takip   : {settings.followStrength:0.00} · hız {settings.maxSpeed:0} · ivme {settings.maxAcceleration:0}", style);
            }

            GUILayout.EndArea();
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
