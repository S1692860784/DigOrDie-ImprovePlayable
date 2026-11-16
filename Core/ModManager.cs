using System.Collections.Generic;
using UnityEngine;

namespace ImprovePlayable
{
    public sealed class ModContentOptions
    {
        public bool EnableBlocks = true;
        public bool EnableDefenses = true;
        public bool EnableMachines = true;
        public bool EnableWeapons = true;
        public bool EnableDevices = true;
        public bool EnableOres = true;
        public bool EnableMonsters = true;
        public bool EnableRecipes = true;
        public bool EnableLocalization = true;
    }

    public static class ModManager
    {
        // 怪物 ID 常量（供外部及调试命令直接引用）
        public const string MONSTER_THANOS_MINION = "ImprovePlayable_ThanosMinion";
        public const string MONSTER_FLAME_LIZARD = "ImprovePlayable_FlameLizard";
        public const string MONSTER_ENERGY_CANNON = "ImprovePlayable_EnergyCannon";
        public const string MONSTER_GHOST_WATCHER = "ImprovePlayable_GhostWatcher";

        // 探测器目标字典与查询接口
        public static readonly Dictionary<string, string> DetectorTargets = new Dictionary<string, string>();

        public static bool TryGetDetectorTarget(string detectorCodeName, out string mineralCodeName)
        {
            return DetectorTargets.TryGetValue(detectorCodeName, out mineralCodeName);
        }

        // 模块列表：各模块保持单一职责，支持独立开关与按序初始化
        private static readonly List<IContentModule> _modules = new List<IContentModule>
        {
            new LocalizationModule(),
            new WeaponModule(),
            new BlockModule(),
            new MiningModule(),
            new MachineModule(),
            new DefenseModule(),
            new DeviceModule(),
            new MonsterModule(),       // 数据驱动的怪物模块 (从 JSON 读取)
            new SpawnAndDropModule(),
            new RecipeModule()
        };

        public static void InitializeContent(ModContentOptions options)
        {
            if (options == null) options = new ModContentOptions();

            Debug.Log(string.Format(
                "[Mod] 开始模块化初始化: Blocks={0}, Monsters={1}, Ores={2}, Weapons={3}, Recipes={4}, Loc={5}",
                options.EnableBlocks, options.EnableMonsters, options.EnableOres,
                options.EnableWeapons, options.EnableRecipes, options.EnableLocalization
            ));

            for (int i = 0; i < _modules.Count; i++)
            {
                var module = _modules[i];
                if (module != null && module.IsEnabled(options))
                {
                    try
                    {
                        module.Initialize();
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError(string.Format("[Mod] 模块 {0} 初始化失败: {1}", module.Name, ex));
                    }
                }
            }

            StimEnvironmentManager.RegisterStimEvent();

            Debug.Log("[Mod] 全部模块初始化完毕。");
        }
    }
}
