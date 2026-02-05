using UnityEngine;
using HarmonyLib;
using System;
using System.Reflection;

namespace MyFirstMod
{
    public sealed class ModContentOptions
    {
        public bool EnableBlocks = true;
        public bool EnableMonsters = true;
        public bool EnableOres = true;
        public bool EnableWeapons = true;
        public bool EnableRecipes = true;
        public bool EnableLocalization = true;
    }

    public static class ModManager
    {
        // 怪物 ID 常量
        public const string MONSTER_THANOS_MINION = "MyFirstMonster";
        public const string MONSTER_FLAME_LIZARD = "MyFirstMonster_Lizard";
        public const string MONSTER_ENERGY_CANNON = "MyFirstMonster_Cannon";
        public const string MONSTER_GHOST_WATCHER = "MyFirstMonster_Watcher";

        // 已创建的内容引用（用于配方、掉落与调试）
        private static CBulletDesc _purpleBullet;
        private static CBulletDesc _energyBullet;
        private static CItem_Weapon _thanosGun;
        private static CustomItemWall _obsidianWall;
        private static CItem_Material _energyCrystal;
        private static CItem_Mineral _energyCrystalOre;

        public static void InitializeContent(ModContentOptions options)
        {
            if (options == null) options = new ModContentOptions();

            Debug.Log(string.Format(
                "[Mod] Init: Blocks={0}, Monsters={1}, Ores={2}, Weapons={3}, Recipes={4}, Loc={5}",
                options.EnableBlocks, options.EnableMonsters, options.EnableOres,
                options.EnableWeapons, options.EnableRecipes, options.EnableLocalization
            ));

            if (options.EnableLocalization) RegisterLocalization();
            if (options.EnableWeapons) RegisterWeapons();
            if (options.EnableBlocks) RegisterBlocks();
            if (options.EnableOres) RegisterMaterialsAndOres();
            if (options.EnableMonsters) RegisterMonsters();
            if (options.EnableRecipes) RegisterRecipes();
            if (options.EnableOres) RegisterOreDropsAndSpawns();
            if (options.EnableMonsters) RegisterMonsterDropsAndSpawns();

            Debug.Log("[Mod] 自定义内容初始化完成。");
        }

        private static void RegisterLocalization()
        {
            Patch_Localization.Register("U_" + MONSTER_THANOS_MINION, "灭霸小兵");
            Patch_Localization.Register("U_" + MONSTER_FLAME_LIZARD, "火焰蜥蜴");
            Patch_Localization.Register("U_" + MONSTER_ENERGY_CANNON, "能量炮手");
            Patch_Localization.Register("U_" + MONSTER_GHOST_WATCHER, "幽灵观察者");
        }

        private static void RegisterWeapons()
        {
            _purpleBullet = BulletFactory.CreateCustomBullet(
                textureResourceName: ".Resources.PurpleBullet.png",
                speed: 25f,
                radius: 0.2f,
                hasTrail: true,
                lightColor: Color.magenta,
                critChance: 0.5f,
                critMultiplier: 10f
            );

            _thanosGun = WeaponFactory.CreateAndRegisterWeapon(
                codeName: "ThanosGun",
                displayName: "灭霸之枪",
                description: "无限能量。",
                range: 60f,
                damage: 100,
                fireRate: 0.2f,
                bullet: _purpleBullet,
                sound: "plasma",
                textureResourceName: ".Resources.ThanosGun.png",
                heatPerShot: 0.15f,
                coolingRate: 0.5f
            );
        }

        private static void RegisterBlocks()
        {
            _obsidianWall = BlockFactory.CreateAndRegisterBlock(
                codeName: "MyMod_ObsidianWall",
                displayName: "黑曜石墙",
                description: "极其坚固的火山岩石墙体。",
                health: 500,
                hardness: 6000,
                weight: 500f,
                blockType: CItem_Wall.Type.WallBlock,
                textureResourceName: ".Resources.ObsidianWall.png"
            );
        }

        private static void RegisterMaterialsAndOres()
        {
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
                    _energyCrystal = (CItem_Material)constructor.Invoke(constructorArgs);

                    var trav = Traverse.Create(_energyCrystal);
                    trav.Field("m_codeName").SetValue("MyFirstMod_EnergyCrystal");
                    trav.Field("m_nameID").SetValue("MyFirstMod_EnergyCrystal");
                    trav.Field("m_name").SetValue("能量水晶");
                    trav.Field("m_desc").SetValue("蕴含着原始能量的水晶。");
                    trav.Field("m_maxStack").SetValue(250);

                    _energyCrystal.m_id = (ushort)GItems.Items.Count;
                    GItems.Items.Add(_energyCrystal);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[Mod] 创建 Energy Crystal 失败: " + ex);
            }

            _energyCrystalOre = BlockFactory.CreateAndRegisterMineral(
                codeName: "MyFirstMod_EnergyCrystalOre",
                displayName: "能量水晶矿",
                description: "包含能量水晶。",
                health: 500,
                textureResourceName: ".Resources.EnergyCrystalOre.png"
            );
        }

        private static void RegisterMonsters()
        {
            MonsterFactory.CreateAndRegisterMonster(
                MONSTER_THANOS_MINION,
                ".Resources.ThanosMinion.png",
                500,
                6.0f,
                20,
                1.5f,
                1.0f,
                MonsterAIType.Ground
            );

            MonsterFactory.CreateAndRegisterMonster(
                MONSTER_FLAME_LIZARD,
                ".Resources.FlameLizard.png",
                200,
                8.0f,
                30,
                1.2f,
                0.5f,
                MonsterAIType.Flying
            );

            if (_energyBullet == null)
            {
                _energyBullet = BulletFactory.CreateCustomBullet(
                    textureResourceName: ".Resources.PurpleBullet.png",
                    speed: 15f,
                    radius: 0.3f,
                    hasTrail: true,
                    lightColor: Color.cyan
                );
            }

            MonsterFactory.CreateAndRegisterMonster(
                MONSTER_ENERGY_CANNON,
                ".Resources.ThanosMinion.png",
                400,
                4.0f,
                35,
                25f,
                2.0f,
                MonsterAIType.GroundRanged,
                _energyBullet
            );

            MonsterFactory.CreateAndRegisterMonster(
                MONSTER_GHOST_WATCHER,
                ".Resources.FlameLizard.png",
                150,
                6.0f,
                15,
                18f,
                1.0f,
                MonsterAIType.FlyingRanged,
                _energyBullet
            );
        }

        private static void RegisterRecipes()
        {
            if (_thanosGun != null && _energyCrystal != null)
            {
                RecipeManager.RegisterRecipe(_thanosGun, GItems.aluminium, 5, _energyCrystal, 20);
            }

            if (_obsidianWall != null)
            {
                RecipeManager.RegisterRecipe(_obsidianWall, GItems.rock, 10);
            }

            CItem energyCrystalOreItem = GItems.Items.Find(i => i != null && i.m_codeName == "MyFirstMod_EnergyCrystalOre");
            if (energyCrystalOreItem != null && _energyCrystal != null)
            {
                CRecipe recycleRecipe = new CRecipe(_energyCrystal, 2, energyCrystalOreItem, 1);
                RecipeManager.RegisterExistingRecipe(recycleRecipe);
            }
        }

        private static void RegisterOreDropsAndSpawns()
        {
            if (_energyCrystalOre != null)
            {
                GameDataManager.RegisterCustomDrop("MyFirstMod_EnergyCrystalOre", "MyFirstMod_EnergyCrystal", 1, 3);
            }

            GameDataManager.RegisterOreSpawn(new OreSpawnRule
            {
                OreCodeName = "MyFirstMod_EnergyCrystalOre",
                ReplacesBlockCodeName = "rock",
                MinY = 400,
                MaxY = 800,
                VeinSize = 6,
                Frequency = 0.1f
            });
        }

        private static void RegisterMonsterDropsAndSpawns()
        {
            GameDataManager.RegisterMonsterDrop(
                MONSTER_THANOS_MINION,
                "MyFirstMod_EnergyCrystal",
                1.0f,
                2,
                5,
                Patch_MonsterLoot.DropCondition.DayOnly
            );

            GameDataManager.RegisterMonsterDrop(
                MONSTER_THANOS_MINION,
                "MyMod_ObsidianWall",
                1.0f,
                1,
                2,
                Patch_MonsterLoot.DropCondition.NightOnly
            );

            SpawnManager.RegisterSpawn(MONSTER_THANOS_MINION, 0f, 2000f, false);

            GameDataManager.RegisterMonsterDrop(MONSTER_ENERGY_CANNON, "MyFirstMod_EnergyCrystal", 1.0f, 5, 10);
            SpawnManager.RegisterSpawn(MONSTER_ENERGY_CANNON, 600f, 1000f, false);

            GameDataManager.RegisterMonsterDrop(MONSTER_GHOST_WATCHER, "MyFirstMod_EnergyCrystal", 0.5f, 1, 3);
            SpawnManager.RegisterSpawn(MONSTER_GHOST_WATCHER, 0f, 2000f, true);

            GameDataManager.RegisterMonsterDrop(MONSTER_FLAME_LIZARD, "MyMod_ObsidianWall", 1.0f, 1, 3);
            SpawnManager.RegisterSpawn(MONSTER_FLAME_LIZARD, 0f, 2000f, false);

            GameDataManager.RegisterMonsterDrop("hound", "MyFirstMod_EnergyCrystal", 0.3f, 1, 2);
            GameDataManager.RegisterMonsterDrop("firefly", "MyFirstMod_EnergyCrystal", 0.5f, 1, 1);
            GameDataManager.RegisterMonsterDrop("bossMadCrab", "MyFirstMod_EnergyCrystal", 1.0f, 10, 20);
        }

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
            catch
            {
                return null;
            }
        }
    }
}
