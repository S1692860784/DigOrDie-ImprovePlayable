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

        private static readonly Dictionary<int, string> LevelToGroupIdMap = new Dictionary<int, string>
        {
            { 1, "MK I" },
            { 2, "MK II" },
            { 3, "MK III" },
            { 4, "MK IV" },
            { 5, "MK V" },
            { 6, "ULTIMATE" }
        };

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

        public static void ApplyRecipes()
        {
            if (CustomRecipes.Count == 0) return;

            List<CRecipesGroup> allGroups = SDataLua.GetDescList<CRecipesGroup>("list_recipesgroups");
            if (allGroups == null) return;

            foreach (var entry in CustomRecipes)
            {
                string targetId = LevelToGroupIdMap.ContainsKey(entry.MachineLevel)
                    ? LevelToGroupIdMap[entry.MachineLevel]
                    : "MK I";

                CRecipesGroup targetGroup = allGroups.Find(g => g.m_groupId == targetId);

                if (targetGroup != null)
                {
                    entry.Recipe.m_groupId = targetId;
                    if (!targetGroup.m_recipes.Exists(r => r.IsSameRecipe(entry.Recipe)))
                    {
                        targetGroup.m_recipes.Add(entry.Recipe);
                        Debug.Log(string.Format("[Mod] 配方已注入: {0} -> {1}", entry.Recipe.m_out.m_item.m_codeName, targetId));
                    }
                }
                else
                {
                    Debug.LogError(string.Format("[Mod] 找不到配方组: {0}", targetId));
                }
            }

            CustomRecipes.Clear();
        }
    }
}
