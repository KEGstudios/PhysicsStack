using UnityEditor;
using UnityEngine;

namespace PhysicsStack.EditorTools
{
    /// <summary>
    /// Gecici teshis araci: gokyuzunu siyaha cevirip geri alir.
    ///
    /// Amac tek bir soruyu ayirmak - hiz cizgileri ciziliyor mu, yoksa cizilip
    /// de arka planda mi kayboluyor? Olcum paneli parcaciklarin uretildigini ve
    /// yasadigini gosteriyor, ama "yasiyor" ile "gorunuyor" ayni sey degil.
    /// Arka plani siyah yapmak ikisini kesin olarak ayiriyor:
    ///
    /// - Siyahta gorunuyorlarsa cizim calisiyor, sorun kontrastta.
    /// - Siyahta da gorunmuyorlarsa hic cizilmiyorlar ve kontrastin konuyla
    ///   ilgisi yok.
    ///
    /// Yalnizca gokyuzu malzemesine dokunuyor, sahneye ve palete dokunmuyor;
    /// bu yuzden geri almak icin sahneyi yeniden kurmak gerekmiyor. Paletten
    /// okudugu icin de "geri al" komutu dogru renkleri kendisi buluyor -
    /// eski degerleri bir yere not etmem gerekmiyor.
    /// </summary>
    public static class SkyProbe
    {
        const string SkyMaterialPath = "Assets/_Project/Art/Materials/M_Sky.mat";
        const string PalettePath = "Assets/_Project/Data/Palette.asset";

        [MenuItem("PhysicsStack/Teshis: Arka Plani Siyah Yap")]
        public static void MakeBlack()
        {
            Apply(Color.black, Color.black, "siyah");
        }

        [MenuItem("PhysicsStack/Teshis: Arka Plani Geri Al")]
        public static void Restore()
        {
            var palette = AssetDatabase.LoadAssetAtPath<Palette>(PalettePath);

            if (palette == null)
            {
                Debug.LogError($"[SkyProbe] Palet bulunamadi: {PalettePath}");
                return;
            }

            Apply(palette.skyTop, palette.skyBottom, "paletteki renkler");
        }

        static void Apply(Color top, Color bottom, string label)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(SkyMaterialPath);

            if (material == null)
            {
                Debug.LogError($"[SkyProbe] Gokyuzu malzemesi bulunamadi: {SkyMaterialPath}");
                return;
            }

            material.SetColor("_TopColor", top);
            material.SetColor("_BottomColor", bottom);

            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();

            Debug.Log($"[SkyProbe] Gokyuzu {label} yapildi. Oyunu yeniden baslat.");
        }
    }
}
