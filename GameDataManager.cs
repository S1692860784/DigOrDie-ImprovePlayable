using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using HarmonyLib;

namespace MyFirstMod // 【关键】确保所有文件都用这个名字
{
    // --- 1. 将数据结构定义在这里，并设为 public ---
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

    // --- 2. 数据管理器类 ---
    public static class GameDataManager
    {
        // 存储规则的容器
        public static readonly Dictionary<string, CustomDropInfo> CustomDrops = new Dictionary<string, CustomDropInfo>();
        public static readonly List<OreSpawnRule> CustomOreSpawnRules = new List<OreSpawnRule>();

        // 注册方法
        public static void RegisterCustomDrop(string sourceBlockCodeName, string dropItemCodeName, int min, int max)
        {
            if (CustomDrops.ContainsKey(sourceBlockCodeName)) return;
            CustomDrops.Add(sourceBlockCodeName, new CustomDropInfo { ItemCodeName = dropItemCodeName, MinAmount = min, MaxAmount = max });
        }

        public static void RegisterOreSpawn(OreSpawnRule rule)
        {
            CustomOreSpawnRules.Add(rule);
        }

        // 你原有的物理修复逻辑
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

            // 【核心修复】
            // 原逻辑只检查 item is CItem_Wall，导致 CItem_Mineral 等其他方块类型被忽略。
            // 正确的逻辑是检查所有可被放置到世界中的单元格物品，即 CItemCell。
            dataTrav.Field("m_isBlock").SetValue((item is CItemCell) ? 1 : 0);

            if (item is CItem_Wall) dataTrav.Field("m_weight").SetValue(((CItem_Wall)item).m_weight);

            // 这个判断是正确的，继续保留
            if (item is CItemCell)
            {
                dataTrav.Field("m_isBlockDoor").SetValue(((CItemCell)item).IsBlockDoor() ? 1 : 0);
                // 对于静态矿石，IsReceivingForces() 应返回 false，最终写入 0，这是正确的。
                dataTrav.Field("m_isReceivingForces").SetValue(((CItemCell)item).IsReceivingForces() ? 1 : 0);
            }
            newDataArray.SetValue(dataInstance, i);
        }
        sitemsTraverse.Field("m_itemsPluginData").SetValue(newDataArray);
        Debug.Log("[Mod] 物理缓存已修复。");
    }
    catch (Exception ex) { Debug.LogError("[Mod] 重建物理缓存时出错: " + ex.ToString()); }
}
public static void RegisterMonsterDrop(
    string monsterCodeName, 
    string itemCodeName, 
    float chance, 
    int min, 
    int max, 
    Patch_MonsterLoot.DropCondition condition = Patch_MonsterLoot.DropCondition.Always // 默认总是掉落
)
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
        Condition = condition // 记录条件
    });
}
                public static void OverrideMonsterLoot(string monsterCodeName, string itemCodeName, int min, int max, float probability = 1.0f)
        {
            // 1. 找到怪物定义
            var field = typeof(GUnits).GetField(monsterCodeName, BindingFlags.Public | BindingFlags.Static);
            if (field == null) return;
            
            var monsterDesc = field.GetValue(null) as CUnitMonster.CDesc;
            if (monsterDesc == null) return;

            // 2. 找到物品定义
            CItem item = GItems.Items.Find(x => x.m_codeName == itemCodeName);
            if (item == null) return;

            // 3. 直接覆盖 m_loot 数组
            // 创建一个新的 CStack (物品堆)
            CStack newLoot = new CStack(item, (int)((min + max) / 2), probability); // 注意：CStack构造函数可能不同，这里简化演示
            // 实际上 CStack 并没有方便的构造函数，我们需要通过反射或者 CStack.CreateList 来弄
            
            // 更简单粗暴的方法：直接利用 CStack 的工厂方法（如果存在）或者反射创建数组
            // 由于 CStack 比较特殊，这里我提供一个通用的清空方法，你可以先清空，再用之前的 RegisterMonsterDrop 添加
            monsterDesc.m_loot = new CStack[0]; // 清空原生掉落
            
            // 然后调用我们之前的逻辑添加你的掉落
            RegisterMonsterDrop(monsterCodeName, itemCodeName, probability, min, max);
            
            Debug.Log(string.Format("[Mod] 已重置怪物 {0} 的掉落列表。", monsterCodeName));
        }
    }
}