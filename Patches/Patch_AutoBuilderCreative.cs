using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace ImprovePlayable
{
    [HarmonyPatch(typeof(CItem_MachineAutoBuilder), "GetRecipes")]
    public static class Patch_AutoBuilderCreative
    {
        private static readonly MethodInfo MemberwiseCloneMethod =
            typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic);

        static void Postfix(CItem_MachineAutoBuilder __instance, ref List<CRecipe> __result)
        {
            if (__instance == null || __result == null) return;
            if (!IsCreativeMode()) return;
            if (!string.Equals(__instance.m_codeName, RecipeManager.ModMachineCodeName, StringComparison.OrdinalIgnoreCase)) return;

            List<CRecipe> freeRecipes = new List<CRecipe>(__result.Count);
            for (int i = 0; i < __result.Count; i++)
            {
                freeRecipes.Add(CloneRecipeFree(__result[i]));
            }
            __result = freeRecipes;
        }

        private static bool IsCreativeMode()
        {
            try
            {
                CParams p = SDataLua.GetDescParamsCopy(false);
                if (p != null && (p.m_startCheated || p.m_devMode))
                {
                    return true;
                }
            }
            catch
            {
            }

            try
            {
                CParams p = SDataLua.GetDescParamsCopy(true);
                if (p != null && (p.m_startCheated || p.m_devMode))
                {
                    return true;
                }
            }
            catch
            {
            }

            try
            {
                var m = AccessTools.Method(typeof(SDataOptions), "get_CheatedPlayed");
                if (m != null)
                {
                    object val = m.Invoke(null, null);
                    if (val is bool && (bool)val) return true;
                }
            }
            catch
            {
            }

            try
            {
                var field = AccessTools.Field(typeof(GVars), "m_achievementsLocked");
                if (field != null)
                {
                    object val = field.GetValue(null);
                    if (val is bool && (bool)val) return true;
                }
            }
            catch
            {
            }

            try
            {
                SScreenDebug debug = SSingletonScreen<SScreenDebug>.Inst;
                if (debug == null) return false;
                var field = AccessTools.Field(typeof(SScreenDebug), "m_displayDebug");
                if (field != null)
                {
                    return (bool)field.GetValue(debug);
                }
            }
            catch
            {
            }

            return false;
        }

        private static CRecipe CloneRecipeFree(CRecipe recipe)
        {
            if (recipe == null) return null;
            CRecipe clone = (CRecipe)MemberwiseCloneMethod.Invoke(recipe, null);
            clone.m_in1 = CloneStackZero(recipe.m_in1);
            clone.m_in2 = CloneStackZero(recipe.m_in2);
            clone.m_in3 = CloneStackZero(recipe.m_in3);
            return clone;
        }

        private static CStack CloneStackZero(CStack stack)
        {
            if (stack == null) return null;
            CStack clone = (CStack)MemberwiseCloneMethod.Invoke(stack, null);
            clone.m_nb = 0;
            return clone;
        }
    }
}
