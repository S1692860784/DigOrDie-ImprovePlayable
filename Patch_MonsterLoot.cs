using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;

namespace MyFirstMod
{
    [HarmonyPatch(typeof(CUnitMonster), "OnDeath")]
    public class Patch_MonsterLoot
    {
        public static Dictionary<string, List<LootRule>> CustomLootRules = new Dictionary<string, List<LootRule>>();

        // 掉落条件枚举
        public enum DropCondition
        {
            Always,     // 总是掉落
            DayOnly,    // 仅白天
            NightOnly   // 仅夜晚
        }

        public struct LootRule
        {
            public string ItemCodeName; 
            public float Chance;        
            public int MinAmount;       
            public int MaxAmount;
            public DropCondition Condition; // 新增条件字段
        }

        static void Postfix(CUnitMonster __instance, CUnit attacker)
        {
            // 基础检查
            if (attacker == null || !(attacker is CUnitPlayer || attacker is CUnitDefense || attacker is CUnitDrone)) return;

            string monsterCodeName = __instance.UDesc.m_codeName;
            if (!CustomLootRules.ContainsKey(monsterCodeName)) return;

            // 获取当前时间状态
            bool isNight = SGame.IsNight();

            foreach (var rule in CustomLootRules[monsterCodeName])
            {
                // === 条件检查 ===
                if (rule.Condition == DropCondition.NightOnly && !isNight) continue; // 要求夜晚但现在是白天 -> 跳过
                if (rule.Condition == DropCondition.DayOnly && isNight) continue;    // 要求白天但现在是夜晚 -> 跳过

                // === 概率检查 ===
                if (UnityEngine.Random.value <= rule.Chance)
                {
                    CItem itemToDrop = GItems.Items.Find(i => i != null && i.m_codeName == rule.ItemCodeName);
                    if (itemToDrop != null)
                    {
                        int amount = UnityEngine.Random.Range(rule.MinAmount, rule.MaxAmount + 1);
                        SPickups.CreatePickup(itemToDrop, (float)amount, __instance.PosCenter, true, null, null, false);
                    }
                }
            }
        }
    }
}