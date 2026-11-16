using HarmonyLib;

namespace ImprovePlayable
{
    [HarmonyPatch(typeof(CUnitMonster), "Attack_Local")]
    public static class Patch_StimAttackSpeed
    {
        static void Prefix(CUnitMonster __instance, ref float __state)
        {
            __state = 0f;
            if (!StimEnvironmentManager.IsStimActive()) return;
            if (__instance == null || __instance.UDesc == null || __instance.UDesc.m_attackDesc == null) return;
            if (__instance.UDesc.m_attackDesc.m_bulletDesc == null) return;

            float speedMult = StimEnvironmentManager.GetRangedAttackSpeedMultiplier();
            if (speedMult <= 1f) return;

            __state = __instance.UDesc.m_attackDesc.m_cooldown;
            __instance.UDesc.m_attackDesc.m_cooldown = __state / speedMult;
        }

        static void Postfix(CUnitMonster __instance, ref float __state)
        {
            if (__state <= 0f) return;
            if (__instance == null || __instance.UDesc == null || __instance.UDesc.m_attackDesc == null) return;
            __instance.UDesc.m_attackDesc.m_cooldown = __state;
        }
    }
}
