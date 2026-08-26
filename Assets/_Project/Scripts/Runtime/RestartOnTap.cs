using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace PhysicsStack
{
    /// <summary>
    /// Oyun bittikten sonra ekrana dokununca sahneyi baştan yükler.
    ///
    /// Bu bir menü değil; menü kapsam dışı. Ama telefonda kazandıktan sonra
    /// yapılabilecek tek şey uygulamayı kapatmak olurdu — ve Gün 5'in çıktısı
    /// 30 saniyelik bir kayıt. Kayıt tek turdan ibaret kalmasın diye gerekli.
    ///
    /// Sahneyi yeniden yüklemek "durumu sıfırla" fonksiyonu yazmaktan daha
    /// güvenilir: unutulan bir alan, kaydı silinmeyen bir olay ya da sahnede
    /// kalan bir kutu ihtimali kalmıyor. Prototip tek sahne ve yükleme maliyeti
    /// yok denecek kadar az; büyük bir oyunda bu tercih tersine dönerdi.
    /// </summary>
    public sealed class RestartOnTap : MonoBehaviour
    {
        [SerializeField] StackGameController controller;

        void Update()
        {
            if (controller.State is not (GameState.Won or GameState.Lost))
            {
                return;
            }

            var pointer = Pointer.current;
            if (pointer != null && pointer.press.wasPressedThisFrame)
            {
                SceneManager.LoadScene(gameObject.scene.buildIndex);
            }
        }
    }
}
