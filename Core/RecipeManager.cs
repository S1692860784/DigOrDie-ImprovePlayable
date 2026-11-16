using System.Collections.Generic;
using UnityEngine;
using HarmonyLib;
using System;

namespace ImprovePlayable
{
    public static class RecipeManager
    {
        private class CustomRecipeEntry
        {
            public CRecipe Recipe;
            public int MachineLevel;
        }

        private static readonly List<CustomRecipeEntry> CustomRecipes = new List<CustomRecipeEntry>();

        public const int ModMachineLevel = 0;
        public const string ModMachineGroupId = "MOD";
        public static string ModMachineCodeName = "ImprovePlayable_AutoBuilder";

        private static readonly Dictionary<int, string> LevelToGroupIdMap = new Dictionary<int, string>
        {
            { ModMachineLevel, ModMachineGroupId },
            { 1, "MK I" },
            { 2, "MK II" },
            { 3, "MK III" },
            { 4, "MK IV" },
            { 5, "MK V" },
            { 6, "ULTIMATE" }
        };

        public static void SetModMachineCodeName(string codeName)
        {
            if (!string.IsNullOrEmpty(codeName))
            {
                ModMachineCodeName = codeName;
            }
        }

        public static void RegisterRecipe(CItem outputItem, CItem material1, int count1, int machineLevel = ModMachineLevel)
        {
            if (outputItem == null) return;
            CustomRecipes.Add(new CustomRecipeEntry { Recipe = new CRecipe(outputItem, 1, material1, count1), MachineLevel = machineLevel });
        }

        public static void RegisterRecipe(CItem outputItem, CItem material1, int count1, CItem material2, int count2, int machineLevel = ModMachineLevel)
        {
            if (outputItem == null) return;
            CustomRecipes.Add(new CustomRecipeEntry { Recipe = new CRecipe(outputItem, 1, material1, count1, material2, count2), MachineLevel = machineLevel });
        }

        public static void RegisterRecipe(CItem outputItem, CItem material1, int count1, CItem material2, int count2, CItem material3, int count3, int machineLevel = ModMachineLevel)
        {
            if (outputItem == null) return;
            CustomRecipes.Add(new CustomRecipeEntry { Recipe = new CRecipe(outputItem, 1, material1, count1, material2, count2, material3, count3, false), MachineLevel = machineLevel });
        }

        public static void RegisterExistingRecipe(CRecipe recipe, int machineLevel = ModMachineLevel)
        {
            if (recipe == null || recipe.m_out.m_item == null) return;
            CustomRecipes.Add(new CustomRecipeEntry { Recipe = recipe, MachineLevel = machineLevel });
        }

        public static void ApplyRecipes()
        {
            if (CustomRecipes.Count == 0) return;

            List<CRecipesGroup> allGroups = SDataLua.GetDescList<CRecipesGroup>("list_recipesgroups");
            if (allGroups == null) return;

            EnsureModRecipesGroup(allGroups);

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

        private static void EnsureModRecipesGroup(List<CRecipesGroup> allGroups)
        {
            if (allGroups.Exists(g => g != null && g.m_groupId == ModMachineGroupId)) return;

            try
            {
                var group = new CRecipesGroup();

                var recipes = new List<CRecipe>();
                var builders = new List<CItem_MachineAutoBuilder>();
                CItem builderItem = ItemFactory.FindItemByCodeName(ModMachineCodeName);
                CItem_MachineAutoBuilder builder = builderItem as CItem_MachineAutoBuilder;
                if (builder != null)
                {
                    builders.Add(builder);
                }

                var trav = Traverse.Create(group);
                trav.Field("m_groupId").SetValue(ModMachineGroupId);
                trav.Field("m_recipes").SetValue(recipes);
                trav.Field("m_autobuilders").SetValue(builders);

                allGroups.Add(group);
                Debug.Log(string.Format("[Mod] Recipes group added: {0} -> {1}", ModMachineGroupId, ModMachineCodeName));
            }
            catch (Exception ex)
            {
                Debug.LogError("[Mod] Failed to add recipes group: " + ex);
            }
        }
    }
}
