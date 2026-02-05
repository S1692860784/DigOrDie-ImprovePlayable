using UnityEngine;
using HarmonyLib;
using System;
using System.Reflection;

namespace MyFirstMod
{
    public static class ModManager
    {
        // === 怪物 ID 常量管理 ===
        public const string MONSTER_THANOS_MINION = "MyFirstMonster";       // 灭霸小兵 (地面)
        public const string MONSTER_FLAME_LIZARD  = "MyFirstMonster_Lizard"; // 火焰蜥蜴 (飞行)
        public const string MONSTER_ENERGY_CANNON   = "MyFirstMonster_Cannon";   // 【新增】地面远程
        public const string MONSTER_GHOST_WATCHER   = "MyFirstMonster_Watcher";  // 【新增】飞行远程        

        public static void InitializeContent()
        {
            Debug.Log("[Mod] ModManager.InitializeContent() - 开始加载所有自定义内容...");
            
            // === 汉化注册 ===
            Patch_Localization.Register("U_" + MONSTER_THANOS_MINION, "灭霸小兵");
            Patch_Localization.Register("U_" + MONSTER_FLAME_LIZARD,  "火焰蜥蜴");
            Patch_Localization.Register("U_" + MONSTER_ENERGY_CANNON, "能量炮手");   // 【新增】
            Patch_Localization.Register("U_" + MONSTER_GHOST_WATCHER, "幽灵观察者"); // 【新增】

            // ===================================================================
            // --- 1. 武器和子弹 ---
            // ===================================================================
            var purpleBullet = BulletFactory.CreateCustomBullet(
                textureResourceName: ".Resources.PurpleBullet.png", 
                speed: 25f, radius: 0.2f, hasTrail: true, lightColor: Color.magenta, critChance: 0.5f, critMultiplier: 10f
            );
            
            var thanosGun = WeaponFactory.CreateAndRegisterWeapon(
                codeName: "ThanosGun", displayName: "灭霸之枪", description: "无限能量。",
                range: 60f, damage: 100, fireRate: 0.2f, bullet: purpleBullet, sound: "plasma",
                textureResourceName: ".Resources.ThanosGun.png", heatPerShot: 0.15f, coolingRate: 0.5f
            );

            // ===================================================================
            // --- 2. 方块 ---
            // ===================================================================
            var obsidianWall = BlockFactory.CreateAndRegisterBlock(
                codeName: "MyMod_ObsidianWall", displayName: "黑曜石墙", description: "极其坚固的火山岩石墙体。",
                health: 500, hardness: 6000, weight: 500f, blockType: CItem_Wall.Type.WallBlock,
                textureResourceName: ".Resources.ObsidianWall.png"
            );

            // ===================================================================
            // --- 3. 材料 (能量水晶) ---
            // ===================================================================
            CTile energyCrystalTile = CreateTileFromResource(".Resources.EnergyCrystal.png");
            if (energyCrystalTile == null) energyCrystalTile = GItems.iron.m_tileIcon;

            try
            {
                Type materialType = typeof(CItem_Material);
                Type[] constructorArgTypes = { typeof(CTile), typeof(CTile), typeof(ushort), typeof(uint), typeof(CItemCell.Anchor) };
                var constructor = materialType.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, constructorArgTypes, null);
                if (constructor != null)
                {
                    object[] constructorArgs = { energyCrystalTile, energyCrystalTile, (ushort)0, 0xFFFFFFFF, CItemCell.Anchor.NotPlacable };
                    CItem_Material energyCrystal = (CItem_Material)constructor.Invoke(constructorArgs);

                    var trav = Traverse.Create(energyCrystal);
                    trav.Field("m_codeName").SetValue("MyFirstMod_EnergyCrystal");
                    trav.Field("m_nameID").SetValue("MyFirstMod_EnergyCrystal");
                    trav.Field("m_name").SetValue("能量水晶");
                    trav.Field("m_desc").SetValue("蕴含着原始能量的水晶。");
                    trav.Field("m_maxStack").SetValue(250);

                    energyCrystal.m_id = (ushort)GItems.Items.Count;
                    GItems.Items.Add(energyCrystal);
                }
            }
            catch (Exception ex) { Debug.LogError("[Mod] 创建 Energy Crystal 时发生异常: " + ex.ToString()); }

            // ===================================================================
            // --- 4. 矿物 (能量水晶矿) ---
            // ===================================================================
            var energyCrystalOre = BlockFactory.CreateAndRegisterMineral(
                codeName: "MyFirstMod_EnergyCrystalOre", displayName: "能量水晶矿", description: "包含能量水晶。",
                health: 500, textureResourceName: ".Resources.EnergyCrystalOre.png"
            );

            // ===================================================================
            // --- 5. 配方注册 ---
            // ===================================================================
            CItem energyCrystalItem = GItems.Items.Find(i => i != null && i.m_codeName == "MyFirstMod_EnergyCrystal");
            var energyCrystalOreItem = GItems.Items.Find(i => i != null && i.m_codeName == "MyFirstMod_EnergyCrystalOre");

            if (thanosGun != null && energyCrystalItem != null) RecipeManager.RegisterRecipe(thanosGun, GItems.aluminium, 5, energyCrystalItem, 20); 
            if (obsidianWall != null) RecipeManager.RegisterRecipe(obsidianWall, GItems.rock, 10); 
            if (energyCrystalOreItem != null && energyCrystalItem != null)
            {
                CRecipe 分解配方 = new CRecipe(energyCrystalItem, 2, energyCrystalOreItem, 1);
                RecipeManager.RegisterExistingRecipe(分解配方); 
            }
            RecipeManager.ApplyRecipes();

            // ===================================================================
            // --- 6. 掉落与生成 (矿石) ---
            // ===================================================================
            if (energyCrystalOre != null) GameDataManager.RegisterCustomDrop("MyFirstMod_EnergyCrystalOre", "MyFirstMod_EnergyCrystal", 1, 3);

            GameDataManager.RegisterOreSpawn(new OreSpawnRule {
                OreCodeName = "MyFirstMod_EnergyCrystalOre", ReplacesBlockCodeName = "rock", 
                MinY = 400, MaxY = 800, VeinSize = 6, Frequency = 0.1f 
            });
            
            // ===================================================================
            // --- 7. 怪物注册 ---
            // ===================================================================

            // --- 怪物 1: 灭霸小兵 (地面) ---
            MonsterFactory.CreateAndRegisterMonster(
                MONSTER_THANOS_MINION,       
                ".Resources.ThanosMinion.png", 
                500,                         // 血量
                6.0f,                        // 速度
                20,                          // 伤害
                1.5f,                        // 攻击距离
                1.0f,                        // 攻击间隔
                MonsterAIType.Ground         // AI: 地面
            );

            // 掉落配置
            GameDataManager.RegisterMonsterDrop(MONSTER_THANOS_MINION, "MyFirstMod_EnergyCrystal", 1.0f, 2, 5, Patch_MonsterLoot.DropCondition.DayOnly);
            GameDataManager.RegisterMonsterDrop(MONSTER_THANOS_MINION, "MyFirstMod_ObsidianWall", 1.0f, 1, 2, Patch_MonsterLoot.DropCondition.NightOnly);
            SpawnManager.RegisterSpawn(MONSTER_THANOS_MINION, 0f, 2000f, false);


            // --- 怪物 2: 火焰蜥蜴 (飞行) ---
            MonsterFactory.CreateAndRegisterMonster(
                MONSTER_FLAME_LIZARD,          
                ".Resources.FlameLizard.png",  
                200,                           // 血量
                8.0f,                          // 速度
                30,                            // 伤害
                1.2f,                          // 攻击距离
                0.5f,                          // 攻击间隔
                MonsterAIType.Flying           // AI: 飞行
            );
                        // --- 【新增】怪物 3: 能量炮手 (地面远程) ---
            // 创建一个简单的子弹，或者复用灭霸枪的
            var energyBullet = BulletFactory.CreateCustomBullet(
                textureResourceName: ".Resources.PurpleBullet.png", // 你需要创建这个图片
                speed: 15f, radius: 0.3f, hasTrail: true, lightColor: Color.cyan
            );

            MonsterFactory.CreateAndRegisterMonster(
                MONSTER_ENERGY_CANNON,          // ID
                ".Resources.ThanosMinion.png",  // 贴图
                400,                            // 血量
                4.0f,                           // 速度 (远程怪通常走得慢)
                35,                             // 伤害
                25f,                            // 射程 (非常远)
                2.0f,                           // 攻击间隔 (比较慢)
                MonsterAIType.GroundRanged,     // AI: 地面远程
                energyBullet                    // 子弹
            );
            GameDataManager.RegisterMonsterDrop(MONSTER_ENERGY_CANNON, "MyFirstMod_EnergyCrystal", 1.0f, 5, 10);
            SpawnManager.RegisterSpawn(MONSTER_ENERGY_CANNON, 600f, 1000f, false); // 只在地下深处刷新


            // --- 【新增】怪物 4: 幽灵观察者 (飞行远程) ---
            MonsterFactory.CreateAndRegisterMonster(
                MONSTER_GHOST_WATCHER,          // ID
                ".Resources.FlameLizard.png",  // 贴图
                150,                            // 血量 (脆皮)
                6.0f,                           // 速度
                15,                             // 伤害
                18f,                            // 射程
                1.0f,                           // 攻击间隔
                MonsterAIType.FlyingRanged,     // AI: 飞行远程
                energyBullet                    // 复用能量子弹
            );
            GameDataManager.RegisterMonsterDrop(MONSTER_GHOST_WATCHER, "MyFirstMod_EnergyCrystal", 0.5f, 1, 3);
            SpawnManager.RegisterSpawn(MONSTER_GHOST_WATCHER, 0f, 2000f, true); // 只在夜晚刷新

            // 掉落配置：总是掉落 1-3 个黑曜石墙
            GameDataManager.RegisterMonsterDrop(MONSTER_FLAME_LIZARD, "MyMod_ObsidianWall", 1.0f, 1, 3);
            SpawnManager.RegisterSpawn(MONSTER_FLAME_LIZARD, 0f, 2000f, false);


            // --- 原版怪物掉落修改 ---
            GameDataManager.RegisterMonsterDrop("hound", "MyFirstMod_EnergyCrystal", 0.3f, 1, 2);
            GameDataManager.RegisterMonsterDrop("firefly", "MyFirstMod_EnergyCrystal", 0.5f, 1, 1);
            GameDataManager.RegisterMonsterDrop("bossMadCrab", "MyFirstMod_EnergyCrystal", 1.0f, 10, 20);

            Debug.Log("[Mod] 自然生成规则已注册。");
            Debug.Log("[Mod] 所有自定义内容加载完毕。");        
        }

        // --- 辅助方法 ---
        private static CTile CreateTileFromResource(string resourcePath)
        {
            byte[] imageData = ResourceLoader.LoadResourceBytes(resourcePath);
            if (imageData == null) return null;
            Texture2D texture = ResourceLoader.CreateTexture(imageData);
            if (texture == null) return null;
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), texture.height);
            try
            {
                var assetSpriteType = AccessTools.TypeByName("CAssetSprite");
                var spriteConstructor = AccessTools.Constructor(assetSpriteType, new[] { typeof(string), typeof(string), typeof(Rect) });
                var assetSpriteInstance = spriteConstructor.Invoke(new object[] { "custom_icon", "custom_icon", new Rect() });
                AccessTools.Property(assetSpriteType, "Sprite").SetValue(assetSpriteInstance, sprite, null);
                var assetSpriteArray = Array.CreateInstance(assetSpriteType, 1);
                assetSpriteArray.SetValue(assetSpriteInstance, 0);
                Material baseMaterial = Resources.Load("Materials/SurfaceOpaque") as Material;
                Material customMaterial = new Material(baseMaterial);
                customMaterial.mainTexture = texture;
                var assetMaterialType = AccessTools.TypeByName("CAssetMaterial");
                var materialConstructor = AccessTools.Constructor(assetMaterialType, new[] { typeof(string), typeof(string) });
                var assetMaterialInstance = materialConstructor.Invoke(new object[] { "SurfaceOpaque", (string)null });
                Traverse.Create(assetMaterialInstance).Field("m_asset").SetValue(customMaterial);
                CTile customTile = new CTile(99, 99);
                var customTileTraverse = Traverse.Create(customTile);
                customTileTraverse.Field("m_sprites").SetValue(assetSpriteArray);
                customTileTraverse.Field("m_mat").SetValue(assetMaterialInstance);
                return customTile;
            }
            catch { return null; }
        }
    }
}