using HarmonyLib;
using UnityEngine;

namespace ImprovePlayable
{
    [HarmonyPatch(typeof(CCell), "GetContentSurface")]
    public static class Patch_CCell_GetContentSurface
    {
        static void Postfix(CCell __instance, ref CSurface __result)
        {
            if (__instance.m_contentId == 0) return;
            if (__instance.m_contentId >= GItems.Items.Count) return;
            if (__result != null) return;

            CItem item = GItems.Items[__instance.m_contentId];
            if (item is CustomItemWall)
            {
                CustomItemWall customWall = (CustomItemWall)item;
                if (customWall.m_surface != null)
                {
                    __result = customWall.m_surface;
                }
            }
        }
    }
}
