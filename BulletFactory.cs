using HarmonyLib;
using UnityEngine;
using System;
using System.Reflection;

namespace MyFirstMod
{
    public static class BulletFactory
    {
        public static CBulletDesc CreateCustomBullet(
            string textureResourceName, // 修改：传入资源路径，而不是 textureName
            float speed, 
            float radius, 
            bool hasTrail,
            Color lightColor,
            float critChance = 0.05f,
            float critMultiplier = 5.0f)
        {
            // 1. 加载纹理
            byte[] imgData = ResourceLoader.LoadResourceBytes(textureResourceName);
            if (imgData == null) return null;
            Texture2D tex = ResourceLoader.CreateTexture(imgData);
            if (tex == null) return null;

            // 2. 【核心修复】创建唯一的纹理名，并注册
            // 名字可以简单点，比如基于资源路径的哈希
            string uniqueTextureName = "mod://bullet/" + textureResourceName.GetHashCode();
            
            if (!Patch_CAssetSprite_Get.CustomTextures.ContainsKey(uniqueTextureName))
            {
                Patch_CAssetSprite_Get.CustomTextures.Add(uniqueTextureName, tex);
            }

            // 3. 创建 CAssetSprite，传入正确的唯一文件名和 Rect
            // 子弹通常是单帧图片，所以 Rect 就是整个纹理
            Rect spriteRect = new Rect(0, 0, tex.width, tex.height);
            object assetSprite = CreateAssetSprite(uniqueTextureName, spriteRect);

            // 4. 创建子弹描述
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

        // 【核心修复】修改方法签名，接收唯一文件名和 Rect
        private static object CreateAssetSprite(string uniqueTextureName, Rect rect)
        {
            var type = AccessTools.TypeByName("CAssetSprite");
            var ctor = AccessTools.Constructor(type, new[] { typeof(string), typeof(string), typeof(Rect) });
            
            // 构造时传入正确的唯一文件名
            var instance = ctor.Invoke(new object[] { uniqueTextureName, null, rect });
            
            // 不需要再手动设置 Sprite 属性了，Patch 会接管
            return instance;
        }
    }
}