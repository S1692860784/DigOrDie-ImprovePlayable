using System.Collections.Generic;
using UnityEngine;

namespace ImprovePlayable
{
    public class RecipeModule : IContentModule
    {
        public string Name { get { return "RecipeModule"; } }

        public bool IsEnabled(ModContentOptions options)
        {
            return options.EnableRecipes;
        }

        public void Initialize()
        {
            RecipesConfigContainer container = JsonConfigHelper.LoadOrCreateConfig<RecipesConfigContainer>("Recipes.json", null);
            if (container != null && container.recipes != null)
            {
                foreach (var cfg in container.recipes)
                {
                    if (cfg == null || string.IsNullOrEmpty(cfg.resultCodeName) || cfg.ingredients == null || cfg.ingredients.Count == 0)
                        continue;

                    CItem output = ItemFactory.FindItemByCodeName(cfg.resultCodeName);
                    if (output == null)
                    {
                        Debug.LogWarning(string.Format("[Mod] RecipeModule: 找不到产出物物品 '{0}'", cfg.resultCodeName));
                        continue;
                    }

                    List<CItem> inItems = new List<CItem>();
                    List<int> inCounts = new List<int>();

                    for (int i = 0; i < cfg.ingredients.Count; i++)
                    {
                        var ing = cfg.ingredients[i];
                        if (ing == null || string.IsNullOrEmpty(ing.itemCodeName)) continue;

                        CItem inItem = ItemFactory.FindItemByCodeName(ing.itemCodeName);
                        if (inItem != null)
                        {
                            inItems.Add(inItem);
                            inCounts.Add(ing.count > 0 ? ing.count : 1);
                        }
                    }

                    if (inItems.Count == 1)
                    {
                        RecipeManager.RegisterRecipe(output, inItems[0], inCounts[0], cfg.machineLevel);
                    }
                    else if (inItems.Count == 2)
                    {
                        RecipeManager.RegisterRecipe(output, inItems[0], inCounts[0], inItems[1], inCounts[1], cfg.machineLevel);
                    }
                    else if (inItems.Count >= 3)
                    {
                        RecipeManager.RegisterRecipe(output, inItems[0], inCounts[0], inItems[1], inCounts[1], inItems[2], inCounts[2], cfg.machineLevel);
                    }
                }
            }

            // 为所有探测器自动补齐配方（消耗能量水晶 x 10）
            var energyCrystal = MiningModule.EnergyCrystal;
            var oreDetectors = MiningModule.OreDetectors;
            if (energyCrystal != null && oreDetectors != null)
            {
                foreach (var detector in oreDetectors)
                {
                    if (detector != null)
                    {
                        RecipeManager.RegisterRecipe(detector, energyCrystal, 10);
                    }
                }
            }
        }
    }
}
