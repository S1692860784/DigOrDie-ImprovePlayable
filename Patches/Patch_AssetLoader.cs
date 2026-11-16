using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Reflection;

namespace ImprovePlayable
{
    [HarmonyPatch(typeof(CAssetSprite), "get_Sprite")]
    public static class Patch_CAssetSprite_Get
    {
        public static Dictionary<string, Texture2D> CustomTextures = new Dictionary<string, Texture2D>();
        private static Dictionary<int, Sprite> _spriteCache = new Dictionary<int, Sprite>();
        private static FieldInfo m_spriteRect_field;

        static bool Prefix(object __instance, ref Sprite __result)
        {
            try
            {
                string filename = ((CAsset)__instance).Filename;
                if (filename == null || !CustomTextures.ContainsKey(filename)) return true;

                if (m_spriteRect_field == null) m_spriteRect_field = AccessTools.Field(typeof(CAssetSprite), "m_spriteRect");
                Rect storedRect = (Rect)m_spriteRect_field.GetValue(__instance);

                int cacheKey = filename.GetHashCode() ^ storedRect.GetHashCode();
                if (_spriteCache.ContainsKey(cacheKey) && _spriteCache[cacheKey] != null)
                {
                    __result = _spriteCache[cacheKey];
                    return false;
                }

                Texture2D mainTexture = CustomTextures[filename];

                // 将 Unity 坐标系下的 Rect 映射为正确的像素区域
                float unityY = mainTexture.height - (storedRect.y + storedRect.height);
                Rect unityRect = new Rect(storedRect.x, unityY, storedRect.width, storedRect.height);

                Sprite newSprite = Sprite.Create(mainTexture, unityRect, new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
                newSprite.name = filename + "_" + storedRect.y;

                _spriteCache[cacheKey] = newSprite;
                __result = newSprite;
                return false;
            }
            catch
            {
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(CAssetMaterial), "get_Material")]
    public static class Patch_CAssetMaterial_Get
    {
        public static Dictionary<string, Material> CustomMaterialsByFilename = new Dictionary<string, Material>();
        private static FieldInfo m_filename_field;

        static bool Prefix(object __instance, ref Material __result)
        {
            if (m_filename_field == null) m_filename_field = AccessTools.Field(typeof(CAssetMaterial).BaseType, "m_filename");
            if (m_filename_field == null) return true;

            string filename = (string)m_filename_field.GetValue(__instance);
            if (filename != null && CustomMaterialsByFilename.ContainsKey(filename))
            {
                __result = CustomMaterialsByFilename[filename];
                return false;
            }
            return true;
        }
    }
}
