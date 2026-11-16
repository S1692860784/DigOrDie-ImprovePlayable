using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace ImprovePlayable
{
    [HarmonyPatch(typeof(CBullet), "CheckColWithUnits")]
    public static class Patch_CritDamage
    {
        // 替换原版固定的暴击倍率 3，改为从 BulletConfig 动态获取
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo getMultMethod = AccessTools.Method(typeof(BulletConfig), "GetMultiplierFor");

            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldc_I4_3)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, getMultMethod);
                    Debug.Log("[Mod] Transpiler: 已替换暴击倍率。");
                }
                else
                {
                    yield return instruction;
                }
            }
        }
    }
}
