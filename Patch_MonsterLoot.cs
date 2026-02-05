using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;

namespace MyFirstMod
{
    [HarmonyPatch(typeof(CUnitMonster), "OnDeath")]
    public class Patch_MonsterLoot
    {
        public static Dictionary<string, List<LootRule>> CustomLootRules = new Dictionary<string, List<LootRule>>();

        public enum DropCondition
        {
            Always,
            DayOnly,
            NightOnly
        }

        public struct LootRule
        {
            public string ItemCodeName;
            public float Chance;
            public int MinAmount;
            public int MaxAmount;
            public DropCondition Condition;
        }

        static void Postfix(CUnitMonster __instance, CUnit attacker)
        {
            if (attacker == null || !(attacker is CUnitPlayer || attacker is CUnitDefense || attacker is CUnitDrone)) return;

            string monsterCodeName = __instance.UDesc.m_codeName;
            if (!CustomLootRules.ContainsKey(monsterCodeName)) return;

            bool isNight = SGame.IsNight();

            foreach (var rule in CustomLootRules[monsterCodeName])
            {
                if (rule.Condition == DropCondition.NightOnly && !isNight) continue;
                if (rule.Condition == DropCondition.DayOnly && isNight) continue;

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
