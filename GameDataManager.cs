using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using HarmonyLib;

namespace MyFirstMod
{
    public struct CustomDropInfo
    {
        public string ItemCodeName;
        public int MinAmount;
        public int MaxAmount;
    }

    public class OreSpawnRule
    {
        public string OreCodeName;
        public string ReplacesBlockCodeName;
        public int MinY;
        public int MaxY;
        public int VeinSize;
        public float Frequency;
    }

    public static class GameDataManager
    {
        public static readonly Dictionary<string, CustomDropInfo> CustomDrops = new Dictionary<string, CustomDropInfo>();
        public static readonly List<OreSpawnRule> CustomOreSpawnRules = new List<OreSpawnRule>();

        public static void RegisterCustomDrop(string sourceBlockCodeName, string dropItemCodeName, int min, int max)
        {
            if (CustomDrops.ContainsKey(sourceBlockCodeName)) return;
            CustomDrops.Add(sourceBlockCodeName, new CustomDropInfo
            {
                ItemCodeName = dropItemCodeName,
                MinAmount = min,
                MaxAmount = max
            });
        }

        public static void RegisterOreSpawn(OreSpawnRule rule)
        {
            CustomOreSpawnRules.Add(rule);
        }

        // 扩容并修复物理缓存，避免自定义方块/矿物穿墙
        public static void RebuildAndExtendItemPluginData()
        {
            try
            {
                var sitemsInst = Traverse.Create(typeof(SItems)).Property("Inst").GetValue();
                if (sitemsInst == null) return;
                var sitemsTraverse = Traverse.Create(sitemsInst);
                var oldDataArray = sitemsTraverse.Field("m_itemsPluginData").GetValue<Array>();
                if (oldDataArray == null) return;

                int newSize = GItems.Items.Count;
                int oldSize = oldDataArray.Length;
                if (newSize <= oldSize) return;

                Debug.Log(string.Format("[Mod] 正在扩容物理缓存: {0} -> {1}", oldSize, newSize));
                Type elementType = oldDataArray.GetType().GetElementType();
                Array newDataArray = Array.CreateInstance(elementType, newSize);
                Array.Copy(oldDataArray, newDataArray, oldSize);

                for (int i = oldSize; i < newSize; i++)
                {
                    object dataInstance = Activator.CreateInstance(elementType);
                    CItem item = GItems.Items[i];
                    var dataTrav = Traverse.Create(dataInstance);

                    // 可放置单元格默认视作方块，但防御塔不应阻挡角色/水
                    if (item is CItem_Defense)
                    {
                        dataTrav.Field("m_isBlock").SetValue(0);
                    }
                    else
                    {
                        dataTrav.Field("m_isBlock").SetValue((item is CItemCell) ? 1 : 0);
                    }

                    if (item is CItem_Wall)
                    {
                        dataTrav.Field("m_weight").SetValue(((CItem_Wall)item).m_weight);
                    }

                    if (item is CItemCell)
                    {
                        if (item is CItem_Defense)
                        {
                            dataTrav.Field("m_isBlockDoor").SetValue(0);
                            dataTrav.Field("m_isReceivingForces").SetValue(0);
                        }
                        else
                        {
                            dataTrav.Field("m_isBlockDoor").SetValue(((CItemCell)item).IsBlockDoor() ? 1 : 0);
                            dataTrav.Field("m_isReceivingForces").SetValue(((CItemCell)item).IsReceivingForces() ? 1 : 0);
                        }
                    }

                    newDataArray.SetValue(dataInstance, i);
                }

                sitemsTraverse.Field("m_itemsPluginData").SetValue(newDataArray);
                Debug.Log("[Mod] 物理缓存已修复。");
            }
            catch (Exception ex)
            {
                Debug.LogError("[Mod] 重建物理缓存出错: " + ex);
            }
        }

        public static void RegisterMonsterDrop(
            string monsterCodeName,
            string itemCodeName,
            float chance,
            int min,
            int max,
            Patch_MonsterLoot.DropCondition condition = Patch_MonsterLoot.DropCondition.Always)
        {
            if (!Patch_MonsterLoot.CustomLootRules.ContainsKey(monsterCodeName))
            {
                Patch_MonsterLoot.CustomLootRules.Add(monsterCodeName, new List<Patch_MonsterLoot.LootRule>());
            }

            Patch_MonsterLoot.CustomLootRules[monsterCodeName].Add(new Patch_MonsterLoot.LootRule
            {
                ItemCodeName = itemCodeName,
                Chance = chance,
                MinAmount = min,
                MaxAmount = max,
                Condition = condition
            });
        }

        // 直接重置原版怪物掉落（谨慎使用）
        public static void OverrideMonsterLoot(string monsterCodeName, string itemCodeName, int min, int max, float probability = 1.0f)
        {
            var field = typeof(GUnits).GetField(monsterCodeName, BindingFlags.Public | BindingFlags.Static);
            if (field == null) return;

            var monsterDesc = field.GetValue(null) as CUnitMonster.CDesc;
            if (monsterDesc == null) return;

            CItem item = GItems.Items.Find(x => x.m_codeName == itemCodeName);
            if (item == null) return;

            monsterDesc.m_loot = new CStack[0];
            RegisterMonsterDrop(monsterCodeName, itemCodeName, probability, min, max);

            Debug.Log(string.Format("[Mod] 已重置怪物 {0} 的掉落列表。", monsterCodeName));
        }
    }
}
