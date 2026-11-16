using HarmonyLib;
using UnityEngine;
using System;
using System.Reflection;

namespace ImprovePlayable
{
    public static class BulletFactory
    {
        public static CBulletDesc CreateCustomBullet(
            string textureResourceName,
            float speed,
            float radius,
            bool hasTrail,
            Color lightColor,
            float critChance = 0.05f,
            float critMultiplier = 5.0f)
        {
            byte[] imgData = ResourceLoader.LoadResourceBytes(textureResourceName);
            if (imgData == null) return null;
            Texture2D tex = ResourceLoader.CreateTexture(imgData);
            if (tex == null) return null;

            string uniqueTextureName = "mod://bullet/" + textureResourceName.GetHashCode();
            if (!Patch_CAssetSprite_Get.CustomTextures.ContainsKey(uniqueTextureName))
            {
                Patch_CAssetSprite_Get.CustomTextures.Add(uniqueTextureName, tex);
            }

            Rect spriteRect = new Rect(0, 0, tex.width, tex.height);
            object assetSprite = CreateAssetSprite(uniqueTextureName, spriteRect);

            CBulletDesc bulletDesc = new CBulletDesc("none", "none", radius, 0f, speed, speed, 0);
            Traverse trv = Traverse.Create(bulletDesc);
            trv.Field("m_sprite").SetValue(assetSprite);

            bulletDesc.m_criticsRate = critChance;
            BulletConfig.SetCritMultiplier(bulletDesc, critMultiplier);

            bulletDesc.m_hasTrail = hasTrail;
            Color24 c24 = new Color24((byte)(lightColor.r * 255), (byte)(lightColor.g * 255), (byte)(lightColor.b * 255));
            trv.Field("m_light").SetValue(c24);

            return bulletDesc;
        }

        public static CBulletDesc CreateCustomBulletFromTemplate(
            CBulletDesc template,
            string textureResourceName,
            Color lightColor,
            bool hasTrail,
            float? critChance = null,
            float? critMultiplier = null)
        {
            if (template == null) return null;

            byte[] imgData = ResourceLoader.LoadResourceBytes(textureResourceName);
            if (imgData == null) return null;
            Texture2D tex = ResourceLoader.CreateTexture(imgData);
            if (tex == null) return null;

            string uniqueTextureName = "mod://bullet/" + textureResourceName.GetHashCode();
            if (!Patch_CAssetSprite_Get.CustomTextures.ContainsKey(uniqueTextureName))
            {
                Patch_CAssetSprite_Get.CustomTextures.Add(uniqueTextureName, tex);
            }

            Rect spriteRect = new Rect(0, 0, tex.width, tex.height);
            object assetSprite = CreateAssetSprite(uniqueTextureName, spriteRect);

            CBulletDesc bulletDesc = ShallowClone(template);
            Traverse trv = Traverse.Create(bulletDesc);
            trv.Field("m_sprite").SetValue(assetSprite);

            bulletDesc.m_hasTrail = hasTrail;
            Color24 c24 = new Color24((byte)(lightColor.r * 255), (byte)(lightColor.g * 255), (byte)(lightColor.b * 255));
            trv.Field("m_light").SetValue(c24);

            if (critChance.HasValue)
            {
                bulletDesc.m_criticsRate = critChance.Value;
            }
            if (critMultiplier.HasValue)
            {
                BulletConfig.SetCritMultiplier(bulletDesc, critMultiplier.Value);
            }

            return bulletDesc;
        }

        private static object CreateAssetSprite(string uniqueTextureName, Rect rect)
        {
            var type = AccessTools.TypeByName("CAssetSprite");
            var ctor = AccessTools.Constructor(type, new[] { typeof(string), typeof(string), typeof(Rect) });
            return ctor.Invoke(new object[] { uniqueTextureName, null, rect });
        }

        private static T ShallowClone<T>(T source) where T : class
        {
            var method = typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic);
            return (T)method.Invoke(source, null);
        }
    }
}
