using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace ImprovePlayable
{
    [BepInPlugin("com.shuaji.digordie.improveplayable", "improve playable", "1.0.0")]
    public class ImprovePlayablePlugin : BaseUnityPlugin
    {
        void Awake()
        {
            ModConfig.Bind(Config);

            var loader = new GameObject("ImprovePlayable_Loader");
            DontDestroyOnLoad(loader);
            loader.AddComponent<ModLoader>();

            if (!ModConfig.EnableDebugHotkeys.Value)
            {
                ModConfig.EnableDebugHotkeys.Value = true;
            }
            loader.AddComponent<DebugHelper>();
        }
    }

    internal static class ModConfig
    {
        public static ConfigEntry<bool> EnableBlocks;
        public static ConfigEntry<bool> EnableMonsters;
        public static ConfigEntry<bool> EnableOres;
        public static ConfigEntry<bool> EnableWeapons;
        public static ConfigEntry<bool> EnableRecipes;
        public static ConfigEntry<bool> EnableLocalization;
        public static ConfigEntry<bool> EnableDebugHotkeys;

        public static void Bind(ConfigFile config)
        {
            EnableBlocks = config.Bind("Content", "EnableBlocks", true, "是否加载自定义方块�?);
            EnableMonsters = config.Bind("Content", "EnableMonsters", true, "是否加载自定义怪物与刷怪逻辑�?);
            EnableOres = config.Bind("Content", "EnableOres", true, "是否加载自定义矿物与矿脉生成�?);
            EnableWeapons = config.Bind("Content", "EnableWeapons", true, "是否加载自定义武器与子弹�?);
            EnableRecipes = config.Bind("Content", "EnableRecipes", true, "是否注入配方到工作台�?);
            EnableLocalization = config.Bind("Content", "EnableLocalization", true, "是否加载本地化文本�?);
            EnableDebugHotkeys = config.Bind("Debug", "EnableHotkeys", false, "是否启用调试热键与调试助手�?);
        }

        public static ModContentOptions ToContentOptions()
        {
            return new ModContentOptions
            {
                EnableBlocks = EnableBlocks.Value,
                EnableMonsters = EnableMonsters.Value,
                EnableOres = EnableOres.Value,
                EnableWeapons = EnableWeapons.Value,
                EnableRecipes = EnableRecipes.Value,
                EnableLocalization = EnableLocalization.Value
            };
        }
    }

    public class ModLoader : MonoBehaviour
    {
        private bool _hasLoaded = false;

        void Start()
        {
            Debug.Log(">>> [ModLoader] 启动，开始注册补�?..");

            var harmony = new Harmony("com.shuaji.digordie.improveplayable.patches");

            harmony.PatchAll(typeof(InputHandler));

            harmony.PatchAll(typeof(Patch_CCell_GetContentSurface));
            harmony.PatchAll(typeof(Patch_CritDamage));

            if (ModConfig.EnableOres.Value)
            {
                harmony.PatchAll(typeof(Patch_CustomDrops));
                harmony.PatchAll(typeof(Patch_WorldGen));
            }

            if (ModConfig.EnableMonsters.Value)
            {
                harmony.PatchAll(typeof(Patch_MonsterLoot));
                harmony.PatchAll(typeof(Patch_MonsterAI));
                harmony.PatchAll(typeof(Patch_StimAttackSpeed));
            }

            if (ModConfig.EnableWeapons.Value)
            {
                harmony.PatchAll(typeof(Patch_DeviceDetector));
                harmony.PatchAll(typeof(Patch_Teleporter));
            }

            if (ModConfig.EnableRecipes.Value)
            {
                harmony.PatchAll(typeof(Patch_AutoBuilderCreative));
            }

            if (ModConfig.EnableLocalization.Value)
            {
                harmony.PatchAll(typeof(Patch_Localization));
            }

            harmony.PatchAll(typeof(Patch_CAssetSprite_Get));
            harmony.PatchAll(typeof(Patch_CAssetMaterial_Get));

            if (ModConfig.EnableMonsters.Value)
            {
                RegisterSpawnPatchFor(harmony, "CModeSolo");
                RegisterSpawnPatchFor(harmony, "CModeMulti");
                RegisterSpawnPatchFor(harmony, "CModeDefense");
                RegisterSpawnPatchFor(harmony, "CModeSkyWorld");
                RegisterSpawnPatchFor(harmony, "CModeUnderTheSea");
                Debug.Log("[ModLoader] Monster patches registered.");
            }

            Debug.Log(">>> [ModLoader] 补丁注册完毕，正在等待游戏数据加�?..");
        }

        private void RegisterSpawnPatchFor(Harmony harmony, string className)
        {
            try
            {
                var type = AccessTools.TypeByName(className);
                if (type == null)
                {
                    Debug.LogWarning("[ModLoader] 找不到模式类: " + className + " (可能版本不同)");
                    return;
                }

                var original = AccessTools.Method(type, "GetMonstersList");
                var postfix = AccessTools.Method(typeof(Patch_SpawnSystem), "Postfix_GetMonstersList");

                if (original != null && postfix != null)
                {
                    harmony.Patch(original, null, new HarmonyMethod(postfix));
                    Debug.Log("[ModLoader] 已为 " + className + " 注册刷怪补丁�?);
                }
                else
                {
                    Debug.LogError("[ModLoader] 无法�?" + className + " 注册补丁: 方法未找到�?);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[ModLoader] 注册 " + className + " 失败: " + ex.Message);
            }
        }

        void Update()
        {
            if (_hasLoaded) return;

            if (GItems.Items != null && GItems.Items.Count > 0)
            {
                var recipeGroups = SDataLua.GetDescList<CRecipesGroup>("list_recipesgroups");
                if (recipeGroups != null && recipeGroups.Count > 0)
                {
                    InitializeAllModContent();
                    _hasLoaded = true;
                    Destroy(this.gameObject);
                }
            }
        }

        private void InitializeAllModContent()
        {
            Debug.Log(">>> [ModLoader] 检测到游戏数据已就绪，开始注�?Mod 内容...");

            ModContentOptions options = ModConfig.ToContentOptions();
            ModManager.InitializeContent(options);

            if (options.EnableBlocks || options.EnableOres || options.EnableWeapons)
            {
                GameDataManager.RebuildAndExtendItemPluginData();
            }

            if (options.EnableRecipes)
            {
                RecipeManager.ApplyRecipes();
            }

            Debug.Log(">>> [ModLoader] 全部初始化完成！现在可以开始游戏或生成地图了�?);
        }
    }
}
