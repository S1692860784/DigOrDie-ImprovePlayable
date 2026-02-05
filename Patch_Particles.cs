using HarmonyLib;
using UnityEngine;

namespace MyFirstMod
{
    // 确保在 ModLoader 中调用了 harmony.PatchAll()
    [HarmonyPatch(typeof(CCell), "GetContentSurface")]
    public static class Patch_CCell_GetContentSurface
    {
        static void Postfix(CCell __instance, ref CSurface __result)
        {
            // 1. 基础检查
            if (__instance.m_contentId == 0) return;
            if (__instance.m_contentId >= GItems.Items.Count) return;

            // 2. 如果原生方法已经返回了东西（比如它是矿物），我们就不管了
            if (__result != null) return;

            // 3. 检查是不是我们的自定义墙
            CItem item = GItems.Items[__instance.m_contentId];
            if (item is CustomItemWall)
            {
                CustomItemWall customWall = (CustomItemWall)item;
                if (customWall.m_surface != null)
                {
                    // 4. 强行覆盖返回值
                    __result = customWall.m_surface;
                    
                    // (调试用，确认 Hook 生效后可删除)
                    // Debug.Log(">>> [Hook生效] 正在为自定义方块提供 Surface");
                }
            }
        }
    }
}