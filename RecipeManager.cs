// RecipeManager.cs
using System.Collections.Generic;
using UnityEngine;

namespace MyFirstMod
{
    public static class RecipeManager
    {
        private class CustomRecipeEntry
        {
            public CRecipe Recipe;
            public int MachineLevel;
        }

        private static readonly List<CustomRecipeEntry> CustomRecipes = new List<CustomRecipeEntry>();

        // 【修正】根据日志显示的真实 ID，修正映射表
        // 注意：是罗马数字，且中间有空格
        private static readonly Dictionary<int, string> LevelToGroupIdMap = new Dictionary<int, string>
        {
            { 1, "MK I" },
            { 2, "MK II" },
            { 3, "MK III" },
            { 4, "MK IV" },
            { 5, "MK V" },
            { 6, "ULTIMATE" }
        };

        // ==================== 注册方法 (保持不变) ====================
        public static void RegisterRecipe(CItem outputItem, CItem material1, int count1, int machineLevel = 1)
        {
            if (outputItem == null) return;
            CustomRecipes.Add(new CustomRecipeEntry { Recipe = new CRecipe(outputItem, 1, material1, count1), MachineLevel = machineLevel });
        }

        public static void RegisterRecipe(CItem outputItem, CItem material1, int count1, CItem material2, int count2, int machineLevel = 1)
        {
            if (outputItem == null) return;
            CustomRecipes.Add(new CustomRecipeEntry { Recipe = new CRecipe(outputItem, 1, material1, count1, material2, count2), MachineLevel = machineLevel });
        }

        public static void RegisterExistingRecipe(CRecipe recipe, int machineLevel = 1)
        {
            if (recipe == null || recipe.m_out.m_item == null) return;
            CustomRecipes.Add(new CustomRecipeEntry { Recipe = recipe, MachineLevel = machineLevel });
        }

        // ==================== 注入方法 ====================
        public static void ApplyRecipes()
        {
            if (CustomRecipes.Count == 0) return;

            List<CRecipesGroup> allGroups = SDataLua.GetDescList<CRecipesGroup>("list_recipesgroups");
            if (allGroups == null) return;

            foreach (var entry in CustomRecipes)
            {
                // 获取正确的 ID (例如 "MK I")
                string targetId = LevelToGroupIdMap.ContainsKey(entry.MachineLevel) ? LevelToGroupIdMap[entry.MachineLevel] : "MK I";
                
                // 精确查找
                CRecipesGroup targetGroup = allGroups.Find(g => g.m_groupId == targetId);

                if (targetGroup != null)
                {
                    entry.Recipe.m_groupId = targetId;
                    if (!targetGroup.m_recipes.Exists(r => r.IsSameRecipe(entry.Recipe)))
                    {
                        targetGroup.m_recipes.Add(entry.Recipe);
                        // 这次肯定会看到这行成功的日志！
                        Debug.Log(string.Format("[Mod] 成功！配方 {0} 已注入 {1}", entry.Recipe.m_out.m_item.m_codeName, targetId));
                    }
                }
                else
                {
                    Debug.LogError(string.Format("[Mod] 依然找不到组: {0}。请检查罗马数字拼写。", targetId));
                }
            }
            CustomRecipes.Clear();
        }
    }
}