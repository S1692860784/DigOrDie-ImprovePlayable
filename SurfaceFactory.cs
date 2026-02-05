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
                Material baseMat = Resources.Load("Materials/SurfaceOpaque") as Material;
                if (baseMat == null)
                {
                    baseMat = Resources.Load("Materials/SurfaceBorders") as Material;
                }

                Material customMat = new Material(baseMat);
                customMat.mainTexture = texture;

                var assetMatType = AccessTools.TypeByName("CAssetMaterial");
                var matCtor = AccessTools.Constructor(assetMatType, new Type[] { typeof(string), typeof(string) });
                var assetMat = matCtor.Invoke(new object[] { "custom_surface", null });
                Traverse.Create(assetMat).Field("m_asset").SetValue(customMat);

                CSurface newSurface = new CSurface(null, 0, -1, -1, false, null, null, false);

                var surfaceTrav = Traverse.Create(newSurface);
                surfaceTrav.Field("m_surfaceMat").SetValue(assetMat);
                surfaceTrav.Field("m_matTop").SetValue(assetMat);
                surfaceTrav.Field("m_topIcon").SetValue(null);

                return newSurface;
            }
            catch (Exception ex)
            {
                Debug.LogError("[Mod] SurfaceFactory Error: " + ex);
                return null;
            }
        }
    }
}
