using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;

namespace PhysicsStack
{
    /// <summary>
    /// Tur sırasında sağ üst köşedeki dişli ve açtığı duraklatma paneli.
    ///
    /// **Neden duraklatma:** oyunun içinde ayar düğmesi olacaksa oyunun durması
    /// şart. Fizik dönerken ses çubuğunu sürüklemek, kule devrilirken ayar
    /// yapmak demek — ve oyuncu ayarı açtığı için kaybediyorsa o düğme bir
    /// tuzak. Duraklatma zaten eksik olan bir şeydi: telefonda gelen bir
    /// bildirim ya da yarıda bırakılan bir tur için de gerekiyor.
    ///
    /// **Neden ayrı bir kanvas:** panel yalnızca açıkken var. Tur boyunca kurulu
    /// bekleyen gizli bir kanvas her karede düzen hesabına giriyor ve hiçbir işe
    /// yaramıyor — tur sonu ekranında da aynı karar verilmişti.
    ///
    /// Menüdeki ayarlar ekranıyla aynı denetimleri kullanıyor
    /// (<see cref="SettingsControls"/>); buradaki fazlalık yalnızca "devam" ve
    /// "menü". İlerlemeyi sıfırlama burada yok: tur ortasında verilecek bir
    /// karar değil.
    /// </summary>
    public sealed class PauseUI : MonoBehaviour
    {
        [SerializeField] StackGameController controller;
        [SerializeField] Palette palette;

        Canvas canvas;
        UIButton gearButton;
        UIButton retryButton;

        GameObject panel;
        SettingsControls controls;
        UIButton resumeButton;
        UIButton menuButton;

        UIButton confirmButton;
        UIButton cancelButton;

        /// <summary>Ekranı kaplayan katman. İkisi aynı anda açılamıyor.</summary>
        enum Overlay
        {
            None,
            Pause,
            ConfirmRestart,
        }

        Overlay overlay;

        /// <summary>
        /// Bu kadar kutuluk kuleden sonra yeniden başlatma onay soruyor.
        ///
        /// Eşik olmasının sebebi, sürtünmenin bedelinin de olması. Onay her
        /// zaman sorulsaydı en çok kullanıldığı anda — daha ilk kutuda kule
        /// eğri oturmuş, oyuncu hemen baştan almak istiyor — bir tık fazladan
        /// alırdı ve koruduğu bir şey olmazdı. Boş kulede onay, sorusu olmayan
        /// bir soru.
        /// </summary>
        const int ConfirmAbove = 4;

        void Start()
        {
            // Menüde tur yok; dişlinin de işi yok. Menünün kendi dişlisi var ve
            // ikisi aynı anda görünseydi köşede iki düğme olurdu.
            if (controller.Rules == null)
            {
                enabled = false;
                return;
            }

            UIKit.Use(palette);

            canvas = UIKit.CreateCanvas("PauseCanvas", sortOrder: 15);

            gearButton = UIKit.IconButton(
                canvas.transform,
                new Rect(0.855f, 0.875f, 0.115f, 0.085f),
                UIKit.Gear,
                UIKit.DimTextColor);

            // Yeniden başlatma dişlinin hemen altında, aynı boyda. İkisi birlikte
            // bir sütun oluşturuyor: köşede iki ayrı yerde duran iki düğme, iki
            // ayrı şey gibi okunurdu.
            retryButton = UIKit.IconButton(
                canvas.transform,
                new Rect(0.855f, 0.780f, 0.115f, 0.085f),
                UIKit.Retry,
                UIKit.DimTextColor);

            // Sürükleme bu dikdörtgenleri yok sayıyor. Olmasaydı düğmeye
            // dokunmak aynı anda altındaki kutuyu da yakalardı: iki okuyucu
            // aynı basışı görüyor ve ikisi de kendi işini yapıyor.
            UIBlocker.Register(gearButton.Rect);
            UIBlocker.Register(retryButton.Rect);
        }

        void OnDestroy()
        {
            if (gearButton != null)
            {
                UIBlocker.Unregister(gearButton.Rect);
            }

            if (retryButton != null)
            {
                UIBlocker.Unregister(retryButton.Rect);
            }

            // Sahne değişirken zaman ölçeği geri veriliyor. Duraklatmışken menüye
            // dönen biri, donmuş bir menüyle karşılaşırdı.
            if (overlay != Overlay.None)
            {
                Time.timeScale = 1f;
            }
        }

        void Update()
        {
            // Tur bittiyse dişli kayboluyor: tur sonu ekranı zaten kendi
            // seçeneklerini veriyor ve o ekranın üstünde duran ikinci bir düğme,
            // hangisinin ne yaptığını bulanıklaştırır.
            if (controller.State is GameState.Won or GameState.Lost)
            {
                if (overlay == Overlay.None)
                {
                    gearButton.SetVisible(false);
                    retryButton.SetVisible(false);
                }

                return;
            }

            var pointer = Pointer.current;

            if (pointer == null)
            {
                return;
            }

            Vector2 position = pointer.position.ReadValue();

            switch (overlay)
            {
                case Overlay.Pause:
                    UpdatePanel(pointer, position);
                    return;

                case Overlay.ConfirmRestart:
                    UpdateConfirm(pointer, position);
                    return;
            }

            if (!pointer.press.wasPressedThisFrame)
            {
                return;
            }

            if (gearButton.Contains(position))
            {
                SfxPlayer.Play(Sfx.UiTap);
                Open();
                return;
            }

            if (retryButton.Contains(position))
            {
                SfxPlayer.Play(Sfx.UiTap);

                if (controller.TowerBoxes >= ConfirmAbove)
                {
                    AskRestart();
                    return;
                }

                Restart();
            }
        }

        void UpdatePanel(Pointer pointer, Vector2 position)
        {
            if (controls.Dragging(pointer.press.isPressed, position))
            {
                return;
            }

            if (!pointer.press.wasPressedThisFrame)
            {
                return;
            }

            if (controls.Press(position))
            {
                return;
            }

            if (resumeButton.Contains(position))
            {
                SfxPlayer.Play(Sfx.UiTap);
                Close();
                return;
            }

            if (menuButton.Contains(position))
            {
                SfxPlayer.Play(Sfx.UiTap);

                Close();
                RunRequest.Clear();
                SceneManager.LoadScene(gameObject.scene.buildIndex);
            }
        }

        void Open()
        {
            overlay = Overlay.Pause;

            // Zaman ölçeği sıfır: fizik duruyor, sayaçlar duruyor. Çöküş
            // yavaşlatması da aynı değeri oynatıyor ama panel açıkken çöküş
            // zaten ilerlemiyor, yani ikisi çakışmıyor.
            Time.timeScale = 0f;

            gearButton.SetVisible(false);
            retryButton.SetVisible(false);

            panel = UIKit.Panel(
                canvas.transform,
                Vector2.zero,
                Vector2.one,
                new Color(0f, 0f, 0f, 0.55f)).gameObject;

            panel.name = "PausePanel";

            var card = UIKit.Panel(
                panel.transform,
                new Vector2(0.08f, 0.16f),
                new Vector2(0.92f, 0.86f),
                UIKit.PanelColor);

            var title = UIKit.Label(card, "Ara", 64, TextAlignmentOptions.Center);
            title.rectTransform.anchorMin = new Vector2(0.05f, 0.86f);
            title.rectTransform.anchorMax = new Vector2(0.95f, 0.98f);
            title.rectTransform.offsetMin = Vector2.zero;
            title.rectTransform.offsetMax = Vector2.zero;
            UIKit.Fit(title, 28f, 64f);

            var block = UIKit.Panel(card, new Vector2(0.06f, 0.32f), new Vector2(0.94f, 0.84f), new Color(0f, 0f, 0f, 0f));

            controls = new SettingsControls();
            controls.Build(block);

            resumeButton = UIKit.Button(card, new Rect(0.06f, 0.17f, 0.88f, 0.13f), "Devam", 46, 0.015f);
            menuButton = UIKit.Button(card, new Rect(0.06f, 0.03f, 0.88f, 0.12f), "Menü", 40, 0.015f);

            UIKit.Fit(resumeButton.Label, 24f, 46f);
            UIKit.Fit(menuButton.Label, 22f, 40f);
        }

        /// <summary>
        /// Yeniden başlatma onayı. Ayar paneliyle aynı iskeleti kullanıyor —
        /// aynı karartma, aynı kart, aynı düğme boyu — çünkü ikisi de "oyun
        /// durdu, bir şey soruyorum" anlamına geliyor ve farklı görünmeleri o
        /// ortak anlamı bozardı.
        ///
        /// Vazgeçen düğme üstte, onaylayan altta — duraklatma panelindeki
        /// "Devam / Menü" sırasıyla aynı: üstteki seni bulunduğun yerde
        /// bırakıyor, alttaki turu bitiriyor. İki panelin aynı dili konuşması,
        /// alttaki düğmenin ne yapacağını okumadan bilmeyi sağlıyor.
        ///
        /// Tehlikeli seçeneği bilerek zor ulaşılan yere koymadım, çünkü buradaki
        /// risk o değil: onay ekranın ortasında açılıyor, onu açan düğme ise sağ
        /// üst köşede. Refleksle ikinci kez aynı yere basmak onaylamaya
        /// varmıyor.
        /// </summary>
        void AskRestart()
        {
            overlay = Overlay.ConfirmRestart;
            Time.timeScale = 0f;

            gearButton.SetVisible(false);
            retryButton.SetVisible(false);

            panel = UIKit.Panel(
                canvas.transform,
                Vector2.zero,
                Vector2.one,
                new Color(0f, 0f, 0f, 0.55f)).gameObject;

            panel.name = "ConfirmPanel";

            var card = UIKit.Panel(
                panel.transform,
                new Vector2(0.1f, 0.36f),
                new Vector2(0.9f, 0.64f),
                UIKit.PanelColor);

            // Soruda kulenin boyu geçiyor. "Emin misin?" diye sormak oyuncuya
            // neyi kaybedeceğini söylemiyor; asıl karar veren sayı o.
            var title = UIKit.Label(
                card,
                controller.TowerBoxes + " kutuluk kule\nyeniden başlasın mı?",
                48,
                TextAlignmentOptions.Center);

            title.rectTransform.anchorMin = new Vector2(0.06f, 0.42f);
            title.rectTransform.anchorMax = new Vector2(0.94f, 0.94f);
            title.rectTransform.offsetMin = Vector2.zero;
            title.rectTransform.offsetMax = Vector2.zero;
            UIKit.Fit(title, 22f, 48f);

            cancelButton = UIKit.Button(card, new Rect(0.06f, 0.22f, 0.88f, 0.16f), "Devam et", 42, 0.015f);
            confirmButton = UIKit.Button(card, new Rect(0.06f, 0.04f, 0.88f, 0.16f), "Yeniden başlat", 38, 0.015f);

            UIKit.Fit(cancelButton.Label, 22f, 42f);
            UIKit.Fit(confirmButton.Label, 20f, 38f);
        }

        void UpdateConfirm(Pointer pointer, Vector2 position)
        {
            if (!pointer.press.wasPressedThisFrame)
            {
                return;
            }

            if (cancelButton.Contains(position))
            {
                SfxPlayer.Play(Sfx.UiTap);
                Close();
                return;
            }

            if (confirmButton.Contains(position))
            {
                // Kapatmak zaman ölçeğini geri veriyor ve bu şart: sahne sıfır
                // ölçekle yeniden yüklenirse yeni tur donmuş başlar.
                Close();
                Restart();
            }
        }

        void Close()
        {
            overlay = Overlay.None;
            Time.timeScale = 1f;

            if (panel != null)
            {
                Destroy(panel);
            }

            panel = null;
            controls = null;
            resumeButton = null;
            menuButton = null;
            confirmButton = null;
            cancelButton = null;

            gearButton.SetVisible(true);
            retryButton.SetVisible(true);
        }

        /// <summary>
        /// Turu baştan başlatır.
        ///
        /// Düğme önce hiç onay sormuyordu ve gerekçesi şuydu: yeniden başlatmaya
        /// basan oyuncu turu zaten çöpe atmış oluyor. Kontrol noktaları gelince
        /// bu doğru olmaktan çıktı — sonsuz modda yirmi kutuluk, birkaç
        /// dakikalık bir tırmanış tek bir yanlış dokunuşla gidebiliyor ve o
        /// dokunuş ekranın en çok kullanılan köşesinde duruyor.
        ///
        /// Onay yine de her zaman sorulmuyor; <see cref="ConfirmAbove"/>
        /// kutudan alçak kulelerde düğme doğrudan çalışıyor.
        /// </summary>
        void Restart()
        {
            SfxPlayer.Play(Sfx.UiTap);

            RunRequest.Set(controller.Mode, controller.LevelIndex);
            SceneManager.LoadScene(gameObject.scene.buildIndex);
        }
    }
}
