using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

namespace PhysicsStack
{
    /// <summary>
    /// Açılış ekranı. Üç durumu var: tanıtım, ana ekran, seviye listesi.
    ///
    /// Sahne, bekleyen bir tur isteği yoksa bu ekranla açılıyor. Seçim yapılınca
    /// istek <see cref="RunRequest"/>'e yazılıp sahne yeniden yükleniyor; fizik
    /// dünyası da böylece sıfırdan kuruluyor.
    ///
    /// **Neden iki ekran:** önceden tek ekranda oyunun adı, sekiz seviyelik
    /// ızgara, sonsuz mod ve ses düğmesi vardı. Sekiz seviye ekrana ancak
    /// sığıyordu, dokuzuncu sığmayacaktı. Ayrıca ilk açılışta oyuncuya sorulan
    /// soru "hangi seviye" değil "hangi mod" — ızgarayı ilk ekrana koymak o
    /// soruyu sekiz seçenekle birlikte soruyordu.
    ///
    /// **Neden tanıtım:** oyunun adı bir kez ortada duruyor, sonra yukarı
    /// çekiliyor. Aynı yazı hem tanıtım hem başlık işini görüyor; ayrı bir açılış
    /// ekranı kurmak, bir saniye sonra çöpe atılacak ikinci bir kanvas demekti.
    /// Oturumda bir kez oynuyor — her seviye dönüşünde tekrar izlemek üçüncü
    /// seferde bekleme süresine dönüşürdü.
    /// </summary>
    public sealed class MenuUI : MonoBehaviour
    {
        [SerializeField] LevelLibrary levels;
        [SerializeField] Palette palette;

        [Tooltip("Açıkken bütün seviyeler ve sonsuz mod kilitsiz. Kayda yazılmıyor, her açılışta kapalı başlıyor.")]
        [SerializeField] bool unlockEverything;

        enum MenuScreen
        {
            Intro,
            Home,
            Levels,
            Settings,
        }

        /// <summary>
        /// Tanıtım oynatıldı mı? Statik: sahne her tur sonunda yeniden
        /// yükleniyor, yani nesne alanı olsaydı animasyon her menüye dönüşte
        /// baştan oynardı.
        /// </summary>
        static bool introShown;

        /// <summary>
        /// Statik alan oyunun açılışında sıfırlanıyor. Derlenmiş oyunda zaten
        /// böyle oluyor ama Editor'de "Enter Play Mode Options" açıkken alan
        /// oturumlar arasında yaşayabiliyor — ve o zaman animasyon bir kez
        /// oynayıp bir daha hiç görünmüyor. Statik durumu olan her yerde bu
        /// tuzak var; sıfırlaması tek satır.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => introShown = false;

        /// <summary>Adın ortada beklediği, yukarı kaydığı ve düğmelerin belirdiği süreler (sn).</summary>
        const float IntroHold = 0.5f;
        const float IntroSlide = 0.65f;
        const float ButtonsFade = 0.3f;

        /// <summary>
        /// Tanıtım saatinin bir karede ilerleyebileceği en büyük adım (sn).
        ///
        /// Bu sınır olmadan animasyon hiç görünmüyordu. Sahne açılırken ilk
        /// kareler çok uzun sürüyor — shader derlemesi, TMP atlasının
        /// üretilmesi, ses kliplerinin sentezlenmesi hep orada oluyor — ve
        /// duvar saatiyle sürülen bir animasyon o tek karede baştan sona
        /// bitiyor. Yani hata "animasyon çalışmıyor" değil, "animasyon
        /// çizilmeden önce bitiyor"muş.
        ///
        /// Sınır, süreyi kareye bağlıyor: en kötü ihtimalle tanıtım biraz uzun
        /// sürer ama mutlaka çizilir. Açılış animasyonu için doğru takas bu.
        /// </summary>
        const float MaxIntroStep = 0.05f;

        /// <summary>
        /// İlk anlardaki dokunuşlar atlama sayılmıyor. Menüye geçerken parmak
        /// hâlâ ekranda olabiliyor (tur sonu ekranındaki "Menü" düğmesi) ve o
        /// dokunuş animasyonu daha başlamadan atlıyordu.
        /// </summary>
        const float SkipGuard = 0.15f;

        /// <summary>
        /// Seviye listesinde aynı anda görünen satır sayısı. Tam sayı değil:
        /// dördüncü satırın bir kısmı görünüyor ve listenin devam ettiğini
        /// söyleyen şey bu. Kaydırma çubuğu koymak yerine içeriğin kendisini
        /// kırptım — çubuk, dokunmatik bir ekranda kimsenin tutmadığı bir şey.
        /// </summary>
        const float VisibleRows = 3.4f;

        const int Columns = 2;

        /// <summary>Bu kadar kayan bir dokunuş artık dokunuş değil, kaydırma (kanvas birimi).</summary>
        const float TapSlop = 12f;

        /// <summary>Tekerlek bir tık çevrildiğinde listenin kayacağı miktar (kanvas birimi).</summary>
        const float WheelStep = 0.5f;

        /// <summary>Bırakıldıktan sonra kaymanın sönme hızı (1/sn).</summary>
        const float ScrollDecay = 7f;

        /// <summary>Geliştirici modunu açan gizli jest: adın üstüne art arda dokunuş.</summary>
        const int DevTapCount = 5;
        const float DevTapWindow = 1.2f;

        readonly List<UIButton> levelButtons = new();

        Canvas canvas;
        MenuScreen screen = MenuScreen.Intro;
        float screenTime;

        TMP_Text title;
        int titleTaps;
        float lastTitleTap;

        GameObject homeRoot;
        CanvasGroup homeGroup;
        GameObject levelsRoot;

        UIButton levelsButton;
        UIButton endlessButton;
        UIButton settingsButton;
        UIButton backButton;

        GameObject settingsRoot;
        SettingsControls settingsControls;
        UIButton resetButton;
        UIButton settingsBackButton;

        /// <summary>
        /// Sıfırlama düğmesine bir kez basıldı mı? İkinci basış siliyor.
        ///
        /// Onay penceresi açmak yerine düğmenin kendisini soruya çevirdim: tek
        /// düğmelik bir kararın ayrı bir pencere kurmaya değmediğini düşünüyorum
        /// ve pencere kapatma dokunuşunu da yönetmek gerekirdi. Ekrandan
        /// çıkınca soru sıfırlanıyor, yani yarım kalmış bir onay geri
        /// dönüldüğünde silmeye devam etmiyor.
        /// </summary>
        bool resetArmed;

        RectTransform viewport;
        RectTransform content;
        int rows;
        float lastViewportHeight;

        float scroll;
        float scrollVelocity;
        float maxScroll;
        bool dragging;
        float dragDistance;
        Vector2 lastPointer;

        // Pop-up ayri bir kanvas degil, menu kanvasinin en son eklenen cocugu.
        // uGUI hiyerarsi sirasina gore ciziyor, yani en son eklenen en ustte
        // kaliyor - ayri kanvas kurup siralama numarasi yonetmeye gerek yok.
        GameObject popup;
        UIButton playButton;
        UIButton closeButton;
        int selectedLevel = -1;

        void Awake()
        {
            // Geliştirici bayrağı Awake'te uygulanıyor: menü çizilmeden önce
            // kilitlerin son hâli belli olmalı.
            Progress.UnlockEverything = unlockEverything;
            UIKit.Use(palette);

            if (RunRequest.HasRequest)
            {
                // Tur oynanıyor: menü hiç kurulmuyor. Gizlenmiş bir kanvas bile
                // her karede düzen hesabı yapar; kurmamak en ucuzu.
                enabled = false;
                return;
            }

            Build();
        }

        void Build()
        {
            canvas = UIKit.CreateCanvas("MenuCanvas", sortOrder: 10);

            UIKit.Panel(canvas.transform, Vector2.zero, Vector2.one, UIKit.PanelColor);

            BuildHome();
            BuildLevels();
            BuildSettings();
            BuildSettingsIcon();

            // Ad en son kuruluyor: uGUI hiyerarşi sırasına göre çizdiği için
            // böylece hem listenin hem kartın üstünde kalıyor.
            title = UIKit.Label(canvas.transform, "PhysicsStack", 110, TextAlignmentOptions.Center);
            UIKit.Fit(title, 44f, 110f);

            BuildDevNotice();

            screen = introShown ? MenuScreen.Home : MenuScreen.Intro;
            screenTime = 0f;

            ApplyTitleRect(introShown ? 1f : 0f);

            homeRoot.SetActive(true);
            levelsRoot.SetActive(false);
            settingsRoot.SetActive(false);
            homeGroup.alpha = introShown ? 1f : 0f;

            // Tanıtım sırasında gizli: ekranda yalnızca oyunun adı olsun.
            // Ayarlar ekranında da gizli, çünkü orada zaten ayarlardasın ve
            // hiçbir şey yapmayan bir düğme, bozuk bir düğmeden ayırt
            // edilemiyor.
            settingsButton.SetVisible(introShown);
        }

        /// <summary>
        /// Geliştirici modu açıkken adın altında duran satır.
        ///
        /// Gizli bir bayrağın görünür bir işareti olması şart: hangi oyunu test
        /// ettiğimi bilmeden ölçüm almak, ölçmemekten daha kötü. Kapalıyken
        /// hiçbir şey kurulmuyor, yani oyuncunun göreceği bir iz yok.
        /// </summary>
        void BuildDevNotice()
        {
            if (!Progress.DevUnlock)
            {
                return;
            }

            var notice = UIKit.Label(canvas.transform, "geliştirici modu: kilitler kapalı", 30, TextAlignmentOptions.Center);

            notice.color = UIKit.DimTextColor;
            notice.rectTransform.anchorMin = new Vector2(0.06f, 0.815f);
            notice.rectTransform.anchorMax = new Vector2(0.94f, 0.865f);
            notice.rectTransform.offsetMin = Vector2.zero;
            notice.rectTransform.offsetMax = Vector2.zero;

            UIKit.Fit(notice, 14f, 30f);
        }

        /// <summary>
        /// Adın dikdörtgeni: 0 ortada ve büyük, 1 yukarıda ve küçük. Punto ayrıca
        /// verilmiyor, otomatik boyutlandırma kutuya göre kendi buluyor — yani
        /// animasyonun tamamı tek bir sayıyla sürüyor.
        /// </summary>
        void ApplyTitleRect(float t)
        {
            var rect = title.rectTransform;

            rect.anchorMin = Vector2.Lerp(new Vector2(0.06f, 0.40f), new Vector2(0.06f, 0.86f), t);
            rect.anchorMax = Vector2.Lerp(new Vector2(0.94f, 0.62f), new Vector2(0.94f, 0.96f), t);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// Köşedeki dişli. Simge dosyadan gelmiyor, yıldız gibi kodla
        /// çiziliyor — projede hazır varlık yok ve bu kural simgeler için de
        /// geçerli.
        ///
        /// Yazılı bir "Ayarlar" düğmesi de vardı; köşe simgesi hem daha az yer
        /// kaplıyor hem de mobil oyunlarda aranan yer orası. Tek kaybı
        /// keşfedilebilirlik ve dişli simgesi bu işi zaten evrensel olarak
        /// yapıyor.
        ///
        /// Ana ekranın değil kanvasın çocuğu: seviye listesinde de duruyor.
        /// Ekranlar arası taşınan tek şey bu ve sebebi basit — ayar, oyuncunun
        /// aklına listeye baktığı sırada da gelebiliyor.
        /// </summary>
        void BuildSettingsIcon()
        {
            // Sağ üst köşe ve kenara yaslı. Alt köşedeydi; üst köşe hem mobil
            // oyunların alışkanlığı hem de başparmağın oyun sırasında durduğu
            // yerden uzak.
            settingsButton = UIKit.IconButton(
                canvas.transform,
                new Rect(0.855f, 0.875f, 0.115f, 0.085f),
                UIKit.Gear,
                UIKit.DimTextColor);
        }

        void BuildHome()
        {
            homeRoot = new GameObject("Home", typeof(RectTransform), typeof(CanvasGroup));
            var rect = homeRoot.GetComponent<RectTransform>();

            rect.SetParent(canvas.transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            homeGroup = homeRoot.GetComponent<CanvasGroup>();

            levelsButton = UIKit.Button(rect, new Rect(0.14f, 0.46f, 0.72f, 0.15f), "Seviyeler", 52);
            UIKit.Fit(levelsButton.Label, 28f, 52f);

            BuildEndless(rect);
        }

        /// <summary>
        /// Sonsuz mod düğmesi yalnızca ana ekranda. Seviye listesinde de olsaydı
        /// aynı seçenek iki yerde dururdu ve listenin sonuna inen oyuncu için
        /// "bunun burada ne işi var" sorusu doğardı.
        /// </summary>
        void BuildEndless(RectTransform parent)
        {
            int unlockIndex = levels != null ? levels.EndlessUnlockIndex : int.MaxValue;
            bool unlocked = Progress.IsEndlessUnlocked(unlockIndex);

            string label = unlocked
                ? $"Sonsuz Mod  ·  en iyi {Progress.EndlessBest:0.00}"
                : $"Sonsuz Mod  ·  {unlockIndex + 1}. seviyeyi bitir";

            endlessButton = UIKit.Button(parent, new Rect(0.14f, 0.28f, 0.72f, 0.15f), label, 46);
            endlessButton.SetEnabled(unlocked);
            UIKit.Fit(endlessButton.Label, 22f, 46f);
        }

        void BuildLevels()
        {
            levelsRoot = new GameObject("Levels", typeof(RectTransform));
            var root = levelsRoot.GetComponent<RectTransform>();

            root.SetParent(canvas.transform, false);
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            // Görüş penceresi: içeriği kırpan kutu. RectMask2D bir çizim
            // özelliği, EventSystem istemiyor — bu projede uGUI'nin dokunma
            // altyapısı hiç kurulu değil ve kırpma onsuz da çalışıyor.
            var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewport = viewportGo.GetComponent<RectTransform>();

            viewport.SetParent(root, false);
            viewport.anchorMin = new Vector2(0.04f, 0.17f);
            viewport.anchorMax = new Vector2(0.96f, 0.82f);
            viewport.offsetMin = Vector2.zero;
            viewport.offsetMax = Vector2.zero;

            // İçerik pencereden uzun ve üstten asılı. Çocukları normalize
            // koordinatlarla yerleşiyor: ekran boyutu değişince yalnızca
            // içeriğin yüksekliğini güncellemek yetiyor, düğmeleri tek tek
            // yeniden konumlandırmak gerekmiyor.
            var contentGo = new GameObject("Content", typeof(RectTransform));
            content = contentGo.GetComponent<RectTransform>();

            content.SetParent(viewport, false);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = Vector2.zero;
            content.anchoredPosition = Vector2.zero;

            BuildLevelGrid();

            backButton = UIKit.Button(root, new Rect(0.28f, 0.05f, 0.44f, 0.09f), "Geri", 40);
            UIKit.Fit(backButton.Label, 22f, 40f);
        }

        /// <summary>
        /// Ayarlar ekranı: ses seviyesi, grafik kalitesi, kamera sarsıntısı ve
        /// ilerlemeyi sıfırlama.
        ///
        /// Dördü de "oyuncunun oyun hakkındaki tercihi" başlığına giriyor ve
        /// hepsi kalıcı. Buraya koymadığım şeyler de var: kare hızı sınırı ve
        /// çözünürlük ayrı seçenek değil, grafik kalitesinin içinde — üç ayrı
        /// kol vermek, oyuncuya kendi sorununu teşhis ettirmek olurdu.
        /// </summary>
        void BuildSettings()
        {
            settingsRoot = new GameObject("Settings", typeof(RectTransform));
            var root = settingsRoot.GetComponent<RectTransform>();

            root.SetParent(canvas.transform, false);
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            // Ses, kalite ve sarsıntı denetimleri ayrı bir sınıfta: aynı üç
            // denetim duraklatma panelinde de var ve iki kopya tutmak,
            // dördüncü ayarda birini güncellemeyi unutmak demekti.
            var block = UIKit.Panel(root, new Vector2(0.08f, 0.30f), new Vector2(0.92f, 0.84f), new Color(0f, 0f, 0f, 0f));

            settingsControls = new SettingsControls();
            settingsControls.Build(block);

            resetButton = UIKit.Button(root, new Rect(0.14f, 0.17f, 0.72f, 0.10f), ResetText(), 34);
            UIKit.Fit(resetButton.Label, 18f, 34f);

            settingsBackButton = UIKit.Button(root, new Rect(0.28f, 0.04f, 0.44f, 0.09f), "Geri", 40);
            UIKit.Fit(settingsBackButton.Label, 22f, 40f);

            RefreshSettings();
        }

        string ResetText() => resetArmed
            ? "emin misin? tekrar dokun"
            : "İlerlemeyi sıfırla";

        void RefreshSettings()
        {
            resetButton.Label.text = ResetText();
            settingsControls.Refresh();
        }

        void ShowSettings()
        {
            homeRoot.SetActive(false);
            levelsRoot.SetActive(false);
            settingsRoot.SetActive(true);
            settingsButton.SetVisible(false);

            screen = MenuScreen.Settings;

            resetArmed = false;

            RefreshSettings();
        }

        /// <summary>
        /// Ayarlar ekranı. Ses çubuğu sürüklenebiliyor, geri kalanı dokunuş.
        ///
        /// Ses çubuğunda karar basışta veriliyor ama sürükleme bırakışa kadar
        /// devam ediyor: parmağını çubuğun üstüne koyup kaydıran biri sesi
        /// ayarlarken duymak istiyor, bıraktığında değil.
        /// </summary>
        void UpdateSettings()
        {
            var pointer = Pointer.current;

            if (pointer == null)
            {
                return;
            }

            Vector2 position = pointer.position.ReadValue();

            if (settingsControls.Dragging(pointer.press.isPressed, position))
            {
                return;
            }

            if (!pointer.press.wasPressedThisFrame)
            {
                return;
            }

            if (settingsControls.Press(position))
            {
                return;
            }

            if (resetButton.Contains(position))
            {
                TapReset();
                return;
            }

            if (settingsBackButton.Contains(position))
            {
                SfxPlayer.Play(Sfx.UiTap);
                ShowHome();
            }
        }

        void TapReset()
        {
            if (!resetArmed)
            {
                resetArmed = true;
                SfxPlayer.Play(Sfx.UiTap);
                RefreshSettings();
                return;
            }

            Progress.Clear();
            resetArmed = false;

            // Kilitler değişti, yani seviye listesi artık yanlış. Yeniden
            // kurmak yerine sahneyi baştan yüklüyorum: menü kendini zaten
            // sıfırdan kuruyor ve iki ayrı "listeyi tazele" yolu tutmak,
            // ikisinin ayrışması demek.
            SfxPlayer.Play(Sfx.Lose);
            RunRequest.Clear();
            SceneManager.LoadScene(gameObject.scene.buildIndex);
        }

        /// <summary>
        /// Seviyeler iki sütunlu bir ızgarada, satırlar içerik kutusunun
        /// normalize koordinatlarında. Bir satırın yüksekliği görünen satır
        /// sayısına bağlı, seviye sayısına değil: dokuzuncu seviye eklendiğinde
        /// düğmeler küçülmüyor, liste uzuyor. Eski hâlinde tersiydi ve sekizinci
        /// seviyede sınıra dayanmıştık.
        /// </summary>
        void BuildLevelGrid()
        {
            int count = levels != null ? levels.Count : 0;
            rows = Mathf.Max(1, Mathf.CeilToInt(count / (float)Columns));

            float rowHeight = 1f / rows;

            for (int i = 0; i < count; i++)
            {
                int row = i / Columns;
                int column = i % Columns;

                var area = new Rect(
                    column / (float)Columns,
                    1f - (row + 1) * rowHeight,
                    1f / Columns,
                    rowHeight);

                var level = levels.Get(i);
                var button = UIKit.Button(content, area, level != null ? level.title : $"Seviye {i + 1}", 44, 0.02f);

                bool unlocked = Progress.IsLevelUnlocked(i);
                button.SetEnabled(unlocked);

                // Ad yukari kayiyor, altina yildiz siralaniyor. Yaziyi ortada
                // birakip yildizi ustune koysaydim ikisi cakisirdi; TMP etiketi
                // dugmenin tamamini kapliyor.
                button.Label.rectTransform.anchorMin = new Vector2(0f, 0.40f);
                button.Label.rectTransform.anchorMax = new Vector2(1f, 0.94f);
                UIKit.Fit(button.Label, 20f, 44f);

                // Kilitli seviyede yildiz gostermiyorum: kazanilmamis uc solgun
                // yildiz, kilitli olan ile sifir yildizla gecilen seviyeyi ayni
                // gosterirdi.
                if (unlocked)
                {
                    int stars = level != null ? level.StarsFor(Progress.LevelBest(i)) : 0;
                    UIKit.StarRow(button.Rect, new Rect(0.30f, 0.08f, 0.40f, 0.30f), stars);
                }

                levelButtons.Add(button);
            }
        }

        /// <summary>
        /// İçeriğin yüksekliğini pencereye göre kurar. Kare başına değil, yalnızca
        /// pencere yüksekliği değiştiğinde çağrılıyor. Tarayıcıda pencere
        /// boyutlandırılabiliyor ve düzenin buna sessizce bozulması, ancak birinin
        /// pencereyi küçültmesiyle ortaya çıkan türden bir hata olurdu.
        /// </summary>
        void LayoutScroll()
        {
            float height = viewport.rect.height;

            if (height <= 0f)
            {
                return;
            }

            float contentHeight = height * rows / VisibleRows;

            content.sizeDelta = new Vector2(0f, contentHeight);
            maxScroll = Mathf.Max(0f, contentHeight - height);
            lastViewportHeight = height;

            ApplyScroll(scroll);
        }

        void ApplyScroll(float value)
        {
            scroll = Mathf.Clamp(value, 0f, maxScroll);
            content.anchoredPosition = new Vector2(0f, scroll);
        }

        void Update()
        {
            switch (screen)
            {
                case MenuScreen.Intro:
                    UpdateIntro();
                    return;

                case MenuScreen.Home:
                    UpdateHome();
                    return;

                case MenuScreen.Levels:
                    UpdateLevels();
                    return;

                case MenuScreen.Settings:
                    UpdateSettings();
                    return;
            }
        }

        /// <summary>
        /// Tanıtım: ad ortada bekliyor, yukarı kayıyor, düğmeler beliriyor.
        /// Ekrana dokunmak animasyonu atlıyor — atlanamayan bir açılış
        /// animasyonu, ikinci izleyişte oyuncuyu bekletmekten başka bir şey
        /// değil.
        ///
        /// Ölçekten bağımsız zaman kullanılıyor: menüde zaman ölçeği bir, ama
        /// çöküş yavaşlatması <c>Time.timeScale</c>'i oynatıyor ve bir gün
        /// menüye o durumda dönülürse animasyonun ağır çekime girmesi, sebebi
        /// aranacak bir hata olurdu.
        /// </summary>
        void UpdateIntro()
        {
            screenTime += Mathf.Min(Time.unscaledDeltaTime, MaxIntroStep);

            var pointer = Pointer.current;
            bool skipped = screenTime > SkipGuard && pointer != null && pointer.press.wasPressedThisFrame;

            float slide = Mathf.Clamp01((screenTime - IntroHold) / IntroSlide);

            // Yumuşak giriş-çıkış. Doğrusal kayma mekanik duruyor: yazı sabit
            // hızla gidip aniden duruyor ve göz o duruşu bir hata gibi okuyor.
            ApplyTitleRect(skipped ? 1f : slide * slide * (3f - 2f * slide));

            homeGroup.alpha = skipped
                ? 1f
                : Mathf.Clamp01((screenTime - IntroHold - IntroSlide * 0.6f) / ButtonsFade);

            if (!skipped && homeGroup.alpha < 1f)
            {
                return;
            }

            ApplyTitleRect(1f);
            homeGroup.alpha = 1f;

            introShown = true;
            screen = MenuScreen.Home;
            screenTime = 0f;

            settingsButton.SetVisible(true);
        }

        void UpdateHome()
        {
            var pointer = Pointer.current;

            if (pointer == null || !pointer.press.wasPressedThisFrame)
            {
                return;
            }

            Vector2 position = pointer.position.ReadValue();

            if (TapTitle(position))
            {
                return;
            }

            if (levelsButton.Contains(position))
            {
                SfxPlayer.Play(Sfx.UiTap);
                ShowLevels();
                return;
            }

            if (endlessButton != null && endlessButton.Contains(position))
            {
                Launch(StackMode.Endless, 0);
                return;
            }

            if (settingsButton.Contains(position))
            {
                SfxPlayer.Play(Sfx.UiTap);
                ShowSettings();
            }
        }

        /// <summary>
        /// Oyunun adına art arda beş dokunuş geliştirici modunu açıp kapatıyor.
        ///
        /// Gizli bir jest gerekiyordu çünkü tarayıcıda Inspector yok ve telefonda
        /// 13. seviyeyi test etmenin başka yolu da yok. Görünür bir düğme
        /// olamazdı: oyuncunun bulabileceği bir "hepsini aç" düğmesi, seviye
        /// sıralamasını anlamsız kılar.
        ///
        /// Neden ad: ekranda zaten duran, hiçbir işi olmayan tek şey o. Ayrı bir
        /// gizli bölge (mesela köşe) tanımlasaydım, oyuncunun kazara bulma
        /// ihtimali daha yüksek olurdu — köşeye dokunmak sıradan bir hareket,
        /// başlığa beş kez dokunmak değil.
        ///
        /// Süre penceresi art arda dokunuşları ayırıyor: aralıklı beş dokunuş
        /// jest sayılmıyor, yoksa menüde oyalanan biri kazara açardı.
        /// </summary>
        bool TapTitle(Vector2 position)
        {
            if (!RectTransformUtility.RectangleContainsScreenPoint(title.rectTransform, position))
            {
                return false;
            }

            float now = Time.unscaledTime;

            titleTaps = now - lastTitleTap <= DevTapWindow ? titleTaps + 1 : 1;
            lastTitleTap = now;

            if (titleTaps < DevTapCount)
            {
                return true;
            }

            titleTaps = 0;
            Progress.DevUnlock = !Progress.DevUnlock;

            // Kilitler değişti, yani seviye listesi ve sonsuz mod düğmesi artık
            // yanlış. Sahneyi baştan yüklemek en ucuzu: menü kendini zaten
            // sıfırdan kuruyor.
            SfxPlayer.Play(Progress.DevUnlock ? Sfx.Win : Sfx.Lose);
            RunRequest.Clear();
            SceneManager.LoadScene(gameObject.scene.buildIndex);

            return true;
        }

        void ShowLevels()
        {
            homeRoot.SetActive(false);
            levelsRoot.SetActive(true);

            screen = MenuScreen.Levels;

            scroll = 0f;
            scrollVelocity = 0f;
            dragging = false;

            LayoutScroll();
        }

        void ShowHome()
        {
            levelsRoot.SetActive(false);
            settingsRoot.SetActive(false);
            homeRoot.SetActive(true);
            settingsButton.SetVisible(true);

            screen = MenuScreen.Home;
        }

        /// <summary>
        /// Seviye listesi: kaydırma ve dokunuş.
        ///
        /// Dokunuş burada **bırakışta** okunuyor, basışta değil. Kaydırılabilen
        /// bir listede basış anında karar vermek, listeyi kaydırmak isteyen her
        /// parmağın altındaki seviyeyi açardı. Oyunun geri kalanında karar
        /// basışta veriliyor ve orada doğrusu o: sabit bir düğmenin altında
        /// kaydırma diye bir ihtimal yok.
        /// </summary>
        void UpdateLevels()
        {
            if (!Mathf.Approximately(viewport.rect.height, lastViewportHeight))
            {
                LayoutScroll();
            }

            var pointer = Pointer.current;
            Vector2 position = pointer != null ? pointer.position.ReadValue() : Vector2.zero;

            if (popup != null)
            {
                UpdatePopup(pointer, position);
                return;
            }

            if (pointer == null)
            {
                dragging = false;
                Glide();
                return;
            }

            if (pointer.press.wasPressedThisFrame)
            {
                if (settingsButton.Contains(position))
                {
                    SfxPlayer.Play(Sfx.UiTap);
                    ShowSettings();
                    return;
                }

                if (backButton.Contains(position))
                {
                    SfxPlayer.Play(Sfx.UiTap);
                    ShowHome();
                    return;
                }

                if (RectTransformUtility.RectangleContainsScreenPoint(viewport, position))
                {
                    dragging = true;
                    dragDistance = 0f;
                    scrollVelocity = 0f;
                    lastPointer = position;
                }
            }

            if (dragging)
            {
                Drag(position);

                if (!pointer.press.isPressed)
                {
                    dragging = false;

                    if (dragDistance < TapSlop * canvas.scaleFactor)
                    {
                        Tap(position);
                    }
                }

                return;
            }

            Wheel();
            Glide();
        }

        void Drag(Vector2 position)
        {
            float pixels = position.y - lastPointer.y;
            lastPointer = position;

            // Dokunuş mu kaydırma mı sorusu toplam yol üzerinden cevaplanıyor,
            // baştan sona mesafe üzerinden değil: parmağını aşağı indirip geri
            // getiren biri listeyi kaydırmıştır, seviyeye dokunmamıştır.
            dragDistance += Mathf.Abs(pixels);

            float delta = pixels / canvas.scaleFactor;
            ApplyScroll(scroll + delta);

            // Hız anlık değerin biraz yumuşatılmışı: tek karelik bir sıçrama
            // bırakma anına denk gelirse liste fırlıyordu.
            if (Time.unscaledDeltaTime > 0f)
            {
                scrollVelocity = Mathf.Lerp(scrollVelocity, delta / Time.unscaledDeltaTime, 0.5f);
            }
        }

        /// <summary>
        /// Bırakıldıktan sonra sönerek devam eden kayma. Ataleti elle yazmamın
        /// sebebi <c>ScrollRect</c>'in EventSystem istemesi: bu projede uGUI'nin
        /// olay altyapısı hiç kurulu değil ve yalnızca bu liste için kurmak,
        /// bütün dokunuş okumasını ikinci bir sisteme taşımak olurdu.
        /// </summary>
        void Glide()
        {
            if (Mathf.Abs(scrollVelocity) < 1f)
            {
                scrollVelocity = 0f;
                return;
            }

            ApplyScroll(scroll + scrollVelocity * Time.unscaledDeltaTime);
            scrollVelocity *= Mathf.Exp(-ScrollDecay * Time.unscaledDeltaTime);

            // Kenara dayandıysa hız da bitiyor. Yoksa liste sınırda dururken
            // atalet arka planda sönmeye devam ediyor ve parmağı bıraktıktan
            // yarım saniye sonra liste kendiliğinden ters yöne kayabiliyor.
            if (scroll <= 0f || scroll >= maxScroll)
            {
                scrollVelocity = 0f;
            }
        }

        void Wheel()
        {
            var mouse = Mouse.current;

            if (mouse == null)
            {
                return;
            }

            float wheel = mouse.scroll.ReadValue().y;

            if (Mathf.Abs(wheel) > 0.01f)
            {
                scrollVelocity = 0f;
                ApplyScroll(scroll - wheel * WheelStep);
            }
        }

        void Tap(Vector2 position)
        {
            // Pencerenin dışındaki düğmeler yok sayılıyor: kırpılmış bir düğme
            // görünmüyor ama dikdörtgeni hâlâ orada duruyor ve dokunuşu
            // yakalayabilir.
            if (!RectTransformUtility.RectangleContainsScreenPoint(viewport, position))
            {
                return;
            }

            for (int i = 0; i < levelButtons.Count; i++)
            {
                if (levelButtons[i].Contains(position))
                {
                    SfxPlayer.Play(Sfx.UiTap);
                    OpenPopup(i);
                    return;
                }
            }
        }

        void UpdatePopup(Pointer pointer, Vector2 position)
        {
            // Pop-up acikken arkadaki liste tamamen yok sayiliyor. Yalnizca
            // gorsel olarak ustunu ortmek yetmez: parmak kartin yanindaki
            // bosluga dokundugunda arkadaki seviye baslardi.
            if (pointer == null || !pointer.press.wasPressedThisFrame)
            {
                return;
            }

            if (playButton.Contains(position))
            {
                Launch(StackMode.Level, selectedLevel);
            }
            else if (closeButton.Contains(position))
            {
                SfxPlayer.Play(Sfx.UiTap);
                ClosePopup();
            }
        }

        /// <summary>
        /// Seviye kartı: ad, kazanılan yıldızlar, zorluk ve tur başlatma.
        ///
        /// Seviyeye dokunmak eskiden turu doğrudan başlatıyordu. Araya bu ekranı
        /// koymamın sebebi yıldızlar: oyuncunun "bu seviyeden kaç yıldız aldım
        /// ve üç yıldız için ne gerekiyor" sorusunu soracağı bir yer gerekiyordu.
        /// Bu bilgiyi ızgaradaki düğmeye sığdırmayı denedim; sekiz düğmenin
        /// sekizi de dört satır bilgi bağırınca ızgara okunmaz oluyor.
        ///
        /// Kart her açılışta yeniden kuruluyor, gizlenip gösterilmiyor: içeriği
        /// zaten seviyeye göre baştan sona değişiyor ve tek bir kart nesnesini
        /// güncel tutmak, kurmaktan daha çok kod olurdu.
        /// </summary>
        void OpenPopup(int index)
        {
            selectedLevel = index;

            var level = levels != null ? levels.Get(index) : null;

            // Karartma perdesi kartın da altında duruyor ve ekranın tamamını
            // kaplıyor: arkadaki liste görünmeye devam ediyor ama geri planda
            // olduğu belli oluyor.
            popup = UIKit.Panel(
                canvas.transform,
                Vector2.zero,
                Vector2.one,
                new Color(0f, 0f, 0f, 0.45f)).gameObject;

            popup.name = "LevelPopup";

            // Kart açıkken köşedeki simge de kapanıyor: karartma perdesinin
            // üstünde duran tıklanabilir bir düğme, perdenin "arkası şu an
            // kapalı" mesajını bozar.
            settingsButton.SetVisible(false);

            // Kart yükseklikte cömert. İlk ölçüler dikey telefonda sığıyordu ama
            // yatay ekranda kanvas 1920x1080 referans birime oturuyor ve kart
            // kısalıyor: aynı dört satır aynı puntoda artık sığmıyordu. Ekrandan
            // bağımsız düzen, oranı değişen bir kutuya sabit sayıda satır
            // sığdırmak demek değilmiş.
            var card = UIKit.Panel(
                popup.transform,
                new Vector2(0.08f, 0.20f),
                new Vector2(0.92f, 0.82f),
                UIKit.PanelColor);

            var cardTitle = UIKit.Label(card, level != null ? level.title : $"Seviye {index + 1}", 72, TextAlignmentOptions.Top);
            cardTitle.rectTransform.anchorMin = new Vector2(0.04f, 0.78f);
            cardTitle.rectTransform.anchorMax = new Vector2(0.96f, 0.97f);
            cardTitle.rectTransform.offsetMin = Vector2.zero;
            cardTitle.rectTransform.offsetMax = Vector2.zero;
            UIKit.Fit(cardTitle, 34f, 72f);

            int best = Progress.LevelBest(index);
            int stars = level != null ? level.StarsFor(best) : 0;

            UIKit.StarRow(card, new Rect(0.32f, 0.55f, 0.36f, 0.21f), stars);

            var info = UIKit.Label(card, Describe(level, best), 34, TextAlignmentOptions.Top);
            info.color = UIKit.DimTextColor;
            info.rectTransform.anchorMin = new Vector2(0.05f, 0.24f);
            info.rectTransform.anchorMax = new Vector2(0.95f, 0.53f);
            info.rectTransform.offsetMin = Vector2.zero;
            info.rectTransform.offsetMax = Vector2.zero;
            UIKit.Fit(info, 16f, 34f);

            playButton = UIKit.Button(card, new Rect(0.05f, 0.03f, 0.55f, 0.18f), "Oyna", 46, 0.02f);
            closeButton = UIKit.Button(card, new Rect(0.62f, 0.03f, 0.33f, 0.18f), "Kapat", 42, 0.02f);

            UIKit.Fit(playButton.Label, 26f, 46f);
            UIKit.Fit(closeButton.Label, 24f, 42f);
        }

        /// <summary>
        /// Kartın bilgi metni. Üç yıldızın kaç kutu istediği burada açıkça
        /// yazıyor: yıldız sistemi ancak eşiği bilinirse bir hedef olur, yoksa
        /// tur sonunda öğrenilen bir sürprizdir.
        /// </summary>
        static string Describe(LevelDefinition level, int best)
        {
            if (level == null)
            {
                return string.Empty;
            }

            string hazard = level.HazardLabel;
            string threat = string.IsNullOrEmpty(hazard) ? "" : $"  ·  {hazard}";

            // Oynanmamış seviyede hiçbir şey yazmıyor. "Henüz geçmedin" bilgi
            // taşımıyordu: yıldızlar zaten boş, oyuncu bunu görüyor. Boş
            // olduğunu ayrıca yazmak, ekranı bilgi değil metinle dolduruyor.
            string record = best > 0 ? $"\nen düşük: {best} kutu" : string.Empty;

            return
                $"zorluk {level.Difficulty}/5{threat}\n" +
                $"hedef yükseklik {level.targetHeight:0.0}\n" +
                $"3 yıldız: {level.StarBoxes} kutu  ·  sınır: {level.BoxLimit}" +
                record;
        }

        void ClosePopup()
        {
            if (popup != null)
            {
                Destroy(popup);
            }

            popup = null;
            playButton = null;
            closeButton = null;
            selectedLevel = -1;

            settingsButton.SetVisible(true);
        }

        void Launch(StackMode mode, int levelIndex)
        {
            SfxPlayer.Play(Sfx.UiTap);

            RunRequest.Set(mode, levelIndex);
            SceneManager.LoadScene(gameObject.scene.buildIndex);
        }
    }
}
