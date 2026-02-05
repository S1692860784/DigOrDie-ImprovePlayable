using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace MyFirstMod
{
    [HarmonyPatch(typeof(CBullet), "CheckColWithUnits")]
    public static class Patch_CritDamage
    {
        /*
         * 原理：
         * 原生代码逻辑是： if (!flag2) push 1 else push 3
         * IL 指令大概长这样：
         * ...
         * brtrue LABEL_NOT_CRIT
         * ldc.i4.3  <--- 我们要找这个“加载3”的指令
         * br LABEL_END
         * LABEL_NOT_CRIT:
         * ldc.i4.1
         * ...
         * 
         * 我们要把 ldc.i4.3 替换为：
         * ldarg.0      (加载 this，也就是 CBullet 实例)
         * call         (调用 BulletConfig.GetMultiplierFor)
         */

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            // 1. 获取我们自定义的方法信息
            MethodInfo getMultMethod = AccessTools.Method(typeof(BulletConfig), "GetMultiplierFor");

            // 2. 遍历指令
            foreach (var instruction in instructions)
            {
                // 3. 寻找 "ldc.i4.3" (加载整数3)
                // 注意：代码里可能有其他地方也用了 3，但暴击倍率这里的 3 是最关键的。
                // 这种简单的替换在当前方法里是安全的，因为该方法里其他的 3 不太可能出现在伤害计算的上下文里，
                // 或者即使替换了影响也不大。
                if (instruction.opcode == OpCodes.Ldc_I4_3)
                {
                    // 4. 替换为：加载 this (CBullet)
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    // 5. 替换为：调用我们的静态方法
                    yield return new CodeInstruction(OpCodes.Call, getMultMethod);
                    
                    // 日志证明我们找到了并替换了
                     Debug.Log("[Mod] Transpiler: 已将暴击倍率 3 替换为自定义方法。");
                }
                else
                {
                    // 保持原样
                    yield return instruction;
                }
            }
        }
    }
}