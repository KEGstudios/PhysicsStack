using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PhysicsStack
{
    /// <summary>
    /// Ruzgari ekranda gosterir: merkezden esme yonune uzayan bir cubuk ve
    /// ucunda bir baklava dilimi. Uzunlugu siddeti, yonu yonu veriyor.
    ///
    /// Once dunya nesnesiydi (kadrajin ust kenarina yakin duran ince bir kutu)
    /// ve iki sorunu vardi: HUD yazisiyla ust uste biniyordu, ve dunyada duran
    /// bir nesne oldugu icin kamera hareket ettikce oynuyordu. Oysa bu bir
    /// bilgi, sahnenin bir parcasi degil. Ekran uzayina tasiyinca ikisi de
    /// kendiliginden cozuldu.
    ///
    /// Ihtiyac oynarken cikmisti: ruzgarli seviyede kutu savruluyordu ama ortada
    /// ruzgar oldugunu soyleyen hicbir sey yoktu. Gorunmeyen bir kuvvet zorluk
    /// degil kafa karisikligi uretiyor - oyuncu kendi hatasini ariyor.
    /// </summary>
    public sealed class WindIndicator : MonoBehaviour
    {
        [SerializeField] Wind wind;
        [SerializeField] Palette palette;

        [Tooltip("Birim ruzgar hizi basina cubuk uzunlugu (referans piksel).")]
        [SerializeField] float pixelsPerSpeed = 300f;

        [Tooltip("Cubugun kalinligi (referans piksel).")]
        [SerializeField] float thickness = 14f;

        RectTransform bar;
        RectTransform head;

        /// <summary>
        /// Gosterge ilk ihtiyac duyuldugunda kuruluyor, <c>Start</c>'ta degil.
        ///
        /// Ilk yazisinda <c>Start</c> icinde <c>wind.Active</c>'e bakip ruzgarsiz
        /// seviyede kendini kapatiyordu. Sonuc: ruzgar hicbir seviyede
        /// gorunmuyordu. Sebebi Unity'nin ayni nesnedeki bilesenlerin <c>Start</c>
        /// sirasini garanti etmemesi - gosterge, <see cref="Wind"/> daha
        /// ayarlarini okumadan calisip "ruzgar yok" sonucuna variyordu.
        ///
        /// Tembel kurulum bu bagimliligi tamamen kaldiriyor: karar bir kere degil,
        /// ruzgar gercekten esmeye basladiginda veriliyor. Ruzgarsiz seviyede de
        /// hicbir sey kurulmuyor, yani asil kazanc korunuyor.
        /// </summary>
        void Build()
        {
            UIKit.Use(palette);

            var canvas = UIKit.CreateCanvas("WindCanvas", sortOrder: 4);

            bar = CreatePart(canvas.transform, "WindBar", 0f);
            head = CreatePart(canvas.transform, "WindHead", 45f);
            BuildCaption(canvas.transform);
        }

        /// <summary>
        /// Cubugun altina "RUZGAR" yazisi.
        ///
        /// Ilk oynayan biri hareket eden bir cubugun ne oldugunu anlamiyordu -
        /// gosterge bir seyi dogru anlatiyordu ama neyi anlattigini soylemiyordu.
        /// Simge tasarlamak yerine yazi koymamin sebebi: simge de ogrenilmesi
        /// gereken bir sey, tek kelime degil.
        ///
        /// Yazi solgun ve kucuk: bilgi cubukta, bu sadece cubugun adi. Ayni
        /// puntoda olsaydi gozu kendine ceker ve asil izlenmesi gereken seyden
        /// uzaklastirirdi.
        /// </summary>
        void BuildCaption(Transform parent)
        {
            var label = UIKit.Label(parent, "RÜZGÂR", 26, TextAlignmentOptions.Center);
            label.name = "WindCaption";
            label.color = palette != null ? palette.uiTextDim : Color.gray;

            // Harf arasi acikligi: buyuk harfle yazilmis kisa bir kelime,
            // aralikli dizildiginde etiket gibi okunuyor, bagirma gibi degil.
            label.characterSpacing = 12f;

            var rect = label.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.80f);
            rect.anchorMax = new Vector2(0.5f, 0.80f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(300f, 40f);
            rect.anchoredPosition = new Vector2(0f, -22f);
        }

        /// <summary>
        /// Parcalar ekranin ust orta bandina, HUD yazisinin altina yerlesiyor.
        /// Cubuk merkezden disari dogru buyudugu icin ikisi de ayni noktaya
        /// sabitleniyor; farki konum ve boyut veriyor.
        /// </summary>
        RectTransform CreatePart(Transform parent, string name, float roll)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rect = go.GetComponent<RectTransform>();

            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.80f);
            rect.anchorMax = new Vector2(0.5f, 0.80f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.localRotation = Quaternion.Euler(0f, 0f, roll);

            go.GetComponent<Image>().color = palette != null ? palette.wind : Color.white;
            return rect;
        }

        void LateUpdate()
        {
            if (wind == null || !wind.Active)
            {
                return;
            }

            if (bar == null)
            {
                Build();
            }

            float speed = wind.CurrentForce;
            float length = Mathf.Abs(speed) * pixelsPerSpeed;

            // Salinan ruzgarda hiz sifirdan geciyor; cubuk o anda gorunmez olacak
            // kadar kisaliyor ve bu dogru bilgi: yon degistiriyor.
            bar.sizeDelta = new Vector2(Mathf.Max(length, 2f), thickness);
            bar.anchoredPosition = new Vector2(Mathf.Sign(speed) * length * 0.5f, 0f);

            head.sizeDelta = new Vector2(thickness * 1.7f, thickness * 1.7f);
            head.anchoredPosition = new Vector2(Mathf.Sign(speed) * length, 0f);
        }
    }
}
