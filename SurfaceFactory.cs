using UnityEngine;
using HarmonyLib;
using System;
using System.Reflection;

namespace MyFirstMod
{
    public static class SurfaceFactory
    {
        public static CSurface CreateCustomSurface(Texture2D texture)
        {
            try
            {
                // 1. 【关键修改】使用 SurfaceOpaque 而不是 SurfaceBorders
                // SurfaceBorders 会尝试混合地形并切掉边角（导致你的方块缺角）
                // SurfaceOpaque 是用于实心物体、机器或方正建筑的
                Material baseMat = Resources.Load("Materials/SurfaceOpaque") as Material;
                if (baseMat == null) 
                {
                    // 备用方案，万一找不到 Opaque
                    baseMat = Resources.Load("Materials/SurfaceBorders") as Material;
                }

                Material customMat = new Material(baseMat);
                customMat.mainTexture = texture;

                // 2. 创建 CAssetMaterial 壳
                var assetMatType = AccessTools.TypeByName("CAssetMaterial");
                var matCtor = AccessTools.Constructor(assetMatType, new Type[] { typeof(string), typeof(string) });
                var assetMat = matCtor.Invoke(new object[] { "custom_obsidian", null });
                Traverse.Create(assetMat).Field("m_asset").SetValue(customMat);

                // 3. 创建纯净的 Surface (木箱结构)
                // topTileI: -1 (无顶盖)
                CSurface newSurface = new CSurface(null, 0, -1, -1, false, null, null, false);

                // 4. 注入材质
                var surfaceTrav = Traverse.Create(newSurface);
                surfaceTrav.Field("m_surfaceMat").SetValue(assetMat);
                surfaceTrav.Field("m_matTop").SetValue(assetMat);
                surfaceTrav.Field("m_topIcon").SetValue(null);

                return newSurface;
            }
            catch (Exception ex)
            {
                Debug.LogError("[Mod] SurfaceFactory Error: " + ex.ToString());
                return null;
            }
        }
    }
}