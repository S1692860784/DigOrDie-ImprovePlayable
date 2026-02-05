using BepInEx;
using HarmonyLib;
using UnityEngine;
using System.Collections.Generic; // 引用这个以使用 List

namespace MyFirstMod
{
    [BepInPlugin("com.user.digordiemod", "My First Mod", "1.0.0")]
    public class MyFirstPlugin : BaseUnityPlugin
    {
        void Awake()
        {
            var loader = new GameObject("MyModLoader");
            DontDestroyOnLoad(loader);
            loader.AddComponent<ModLoader>();
            loader.AddComponent<DebugHelper>(); // <--- 挂载调试助手

        }
    }

    public class ModLoader : MonoBehaviour
    {
        private bool _hasLoaded = false;

        void Start()
        {
            Debug.Log(">>> [ModLoader] 启动，开始注册补丁...");

            var harmony = new Harmony("com.user.digordiemod.patches");

            harmony.PatchAll(typeof(InputHandler));
            harmony.PatchAll(typeof(RecipeManager));
            harmony.PatchAll(typeof(Patch_CCell_GetContentSurface)); 
            harmony.PatchAll(typeof(Patch_CritDamage));
            harmony.PatchAll(typeof(Patch_CustomDrops));
            harmony.PatchAll(typeof(Patch_WorldGen));
            harmony.PatchAll(typeof(Patch_MonsterLoot)); // <--- 新增这行
            harmony.PatchAll(typeof(Patch_Localization)); // <--- 新增这行
            harmony.PatchAll(typeof(Patch_CAssetSprite_Get));
            harmony.PatchAll(typeof(Patch_CAssetMaterial_Get));
            harmony.PatchAll(typeof(Patch_MonsterAI));
    RegisterSpawnPatchFor(harmony, "CModeSolo");
    RegisterSpawnPatchFor(harmony, "CModeMulti");
    RegisterSpawnPatchFor(harmony, "CModeDefense");
    RegisterSpawnPatchFor(harmony, "CModeSkyWorld");
    RegisterSpawnPatchFor(harmony, "CModeUnderTheSea");



        Debug.Log("[ModManager] Patch_MonsterDrops registered.");

            Debug.Log(">>> [ModLoader] 补丁注册完毕，正在等待游戏数据加载...");
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
            Debug.Log("[ModLoader] 已为 " + className + " 注册刷怪补丁。");
        }
        else
        {
            Debug.LogError("[ModLoader] 无法为 " + className + " 注册补丁: 方法未找到。");
        }
    }
    catch (System.Exception ex)
    {
        Debug.LogError("[ModLoader] 注册 " + className + " 失败: " + ex.Message);
    }
}
        void Update()
        {
            // 如果已经加载过，就不再执行
            if (_hasLoaded) return;

            // 【核心逻辑】
            // 我们在每一帧检查：游戏的核心数据 (GItems) 和 配方数据 (SDataLua) 是否已经准备好？
            // 只要它们不为空，说明游戏启动完成了（通常在主菜单界面）。
            if (GItems.Items != null && GItems.Items.Count > 0)
            {
                // 再检查一下配方数据是否也好了
                var recipeGroups = SDataLua.GetDescList<CRecipesGroup>("list_recipesgroups");
                if (recipeGroups != null && recipeGroups.Count > 0)
                {
                    InitializeAllModContent();
                    _hasLoaded = true;
                    
                    // 初始化完成后，销毁这个加载器，释放资源
                    Destroy(this.gameObject);
                }
            }
        }

        private void InitializeAllModContent()
        {
            Debug.Log(">>> [ModLoader] 检测到游戏数据已就绪，开始注入 Mod 内容...");

            // 1. 创建物品、方块
            ModManager.InitializeContent();

            // 2. 修复物理缓存 (防止穿墙)
            GameDataManager.RebuildAndExtendItemPluginData();

            // 3. 注入配方 (MK1 - MK5)
            // 此时是在主菜单，注入是安全的，进入游戏后所有机器都会有配方
            RecipeManager.ApplyRecipes();

            Debug.Log(">>> [ModLoader] 全部初始化完成！现在可以开始游戏或生成地图了。");
        }
    }
}