using System.Collections.Generic;
using UnityEngine;

namespace PhysicsStack
{
    /// <summary>
    /// Oyunun içindeki arayüz parçalarının kayıt defteri: "bu dokunuş bir
    /// düğmenin üstünde mi?"
    ///
    /// Menüde böyle bir şeye gerek yoktu, çünkü orada dokunuşu okuyan tek bir
    /// yer var. Oyunun içine ayar düğmesi girince iki okuyucu oldu: düğme ve
    /// sürükleme. İkisi de aynı basışı görüyor ve hiçbir şey yapılmazsa
    /// düğmeye dokunmak aynı anda kutuyu da yakalıyor.
    ///
    /// uGUI'nin bu iş için <c>EventSystem</c>'i var ve tam olarak bunu yapıyor.
    /// Kurmadım çünkü projedeki bütün dokunuş okuması <c>Pointer</c> üzerinden
    /// yürüyor; EventSystem'i yalnızca "üstümde düğme var mı" sorusu için
    /// eklemek, ikinci bir girdi sistemi getirip ikisini senkronda tutmak
    /// olurdu. Sorunun tamamı bir dikdörtgen listesi ve bir döngü.
    ///
    /// Kayıt kalkarken silinmesi şart: sahne yeniden yüklendiğinde yok edilmiş
    /// dikdörtgenler listede kalırsa, oyunun ortasında görünmez bir engel
    /// oluşur ve sebebi aranırken hiçbir şey görünmez.
    /// </summary>
    public static class UIBlocker
    {
        static readonly List<RectTransform> rects = new();

        public static void Register(RectTransform rect)
        {
            if (rect != null && !rects.Contains(rect))
            {
                rects.Add(rect);
            }
        }

        public static void Unregister(RectTransform rect)
        {
            rects.Remove(rect);
        }

        /// <summary>
        /// Verilen ekran noktası kayıtlı bir dikdörtgenin içinde mi? Gizlenmiş
        /// olanlar sayılmıyor: kapalı bir düğmenin arkasındaki kutu
        /// yakalanabilmeli.
        /// </summary>
        public static bool Blocks(Vector2 screenPoint)
        {
            for (int i = rects.Count - 1; i >= 0; i--)
            {
                var rect = rects[i];

                if (rect == null)
                {
                    rects.RemoveAt(i);
                    continue;
                }

                if (rect.gameObject.activeInHierarchy &&
                    RectTransformUtility.RectangleContainsScreenPoint(rect, screenPoint))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
