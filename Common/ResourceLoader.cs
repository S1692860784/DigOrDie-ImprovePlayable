using UnityEngine;
using System.IO;
using System.Reflection;

namespace ImprovePlayable
{
    public static class ResourceLoader
    {
        // 从嵌入资源加载原始图片字节
        public static byte[] LoadResourceBytes(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                Debug.LogError(string.Format("[Mod] 无法找到嵌入资源: {0}", resourceName));
                return null;
            }

            byte[] buffer = new byte[stream.Length];
            stream.Read(buffer, 0, buffer.Length);
            return buffer;
        }

        // 从图片数据创建 Texture2D
        public static Texture2D CreateTexture(byte[] imageData)
        {
            if (imageData == null || imageData.Length == 0) return null;

            Texture2D texture = new Texture2D(2, 2);
            if (texture.LoadImage(imageData))
            {
                return texture;
            }
            Debug.LogError("[Mod] 无法从图片数据创建 Texture2D。");
            return null;
        }

        // 从嵌入图片资源创建游戏内部使用的 CTile
        public static CTile CreateTileFromResource(string resourcePath)
        {
            byte[] imageData = LoadResourceBytes(resourcePath);
            if (imageData == null) return null;
            Texture2D texture = CreateTexture(imageData);
            if (texture == null) return null;
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), texture.height);

            try
            {
                var assetSpriteType = HarmonyLib.AccessTools.TypeByName("CAssetSprite");
                var spriteConstructor = HarmonyLib.AccessTools.Constructor(assetSpriteType, new[] { typeof(string), typeof(string), typeof(Rect) });
                var assetSpriteInstance = spriteConstructor.Invoke(new object[] { "custom_icon", "custom_icon", new Rect() });
                HarmonyLib.AccessTools.Property(assetSpriteType, "Sprite").SetValue(assetSpriteInstance, sprite, null);
                var assetSpriteArray = System.Array.CreateInstance(assetSpriteType, 1);
                assetSpriteArray.SetValue(assetSpriteInstance, 0);

                Material baseMaterial = Resources.Load("Materials/SurfaceOpaque") as Material;
                Material customMaterial = new Material(baseMaterial);
                customMaterial.mainTexture = texture;

                var assetMaterialType = HarmonyLib.AccessTools.TypeByName("CAssetMaterial");
                var materialConstructor = HarmonyLib.AccessTools.Constructor(assetMaterialType, new[] { typeof(string), typeof(string) });
                var assetMaterialInstance = materialConstructor.Invoke(new object[] { "SurfaceOpaque", (string)null });
                HarmonyLib.Traverse.Create(assetMaterialInstance).Field("m_asset").SetValue(customMaterial);

                CTile customTile = new CTile(99, 99);
                var customTileTraverse = HarmonyLib.Traverse.Create(customTile);
                customTileTraverse.Field("m_sprites").SetValue(assetSpriteArray);
                customTileTraverse.Field("m_mat").SetValue(assetMaterialInstance);
                return customTile;
            }
            catch
            {
                return null;
            }
        }
    }
}
