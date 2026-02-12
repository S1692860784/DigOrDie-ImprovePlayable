using UnityEngine;
using HarmonyLib;
using System;
using System.Reflection;
using System.Collections.Generic;

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
        private static CBulletDesc _turretBullet;
        private static CItem_Weapon _thanosGun;
        private static CustomItemWall _obsidianWall;
        private static CItem_Material _energyCrystal;
        private static CItem_Mineral _energyCrystalOre;
        private static CItem_Device _energyCrystalArmor;
        private static CItem_Device _teleporter;
        private static CItem_Device _superFlashLight;
        private static CItem_Defense _energyTurret;
        private static List<CItem_Device> _oreDetectors = new List<CItem_Device>();
        private static CItem _modAssembler;
        private static CItem _uraniumGenerator;

        public static readonly Dictionary<string, string> DetectorTargets = new Dictionary<string, string>();

        public static bool TryGetDetectorTarget(string detectorCodeName, out string mineralCodeName)
        {
            return DetectorTargets.TryGetValue(detectorCodeName, out mineralCodeName);
        }

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
            if (options.EnableWeapons) RegisterDevices();
            if (options.EnableBlocks) RegisterMachines();
            if (options.EnableBlocks) RegisterDefenses();
            if (options.EnableRecipes) RegisterRecipes();
            if (options.EnableOres) RegisterOreDropsAndSpawns();
            if (options.EnableMonsters) RegisterMonsterDropsAndSpawns();
            StimEnvironmentManager.RegisterStimEvent();

            Debug.Log("[Mod] 自定义内容初始化完成。");
        }

        private static void RegisterLocalization()
        {
            Patch_Localization.Register("U_" + MONSTER_THANOS_MINION, "灭霸小兵");
            Patch_Localization.Register("U_" + MONSTER_FLAME_LIZARD, "火焰蜥蜴");
            Patch_Localization.Register("U_" + MONSTER_ENERGY_CANNON, "能量炮手");
            Patch_Localization.Register("U_" + MONSTER_GHOST_WATCHER, "幽灵观察者");
            Patch_Localization.Register("ENV_" + StimEnvironmentManager.StimEventId, "兴奋剂");
        }

        private static void RegisterWeapons()
        {
            _purpleBullet = BulletFactory.CreateCustomBullet(
                textureResourceName: ".Resources.PurpleBullet.png",
                speed: 25f,
                radius: 0.2f,
                hasTrail: true,
                lightColor: Color.magenta,
                critChance: 0.05f,
                critMultiplier: 3f
            );

            CItem_Weapon template = GItems.gunLaser;
            int baseDamage = template != null && template.m_attackDesc != null ? template.m_attackDesc.m_damage : 0;
            float cooldown = template != null && template.m_attackDesc != null ? template.m_attackDesc.m_cooldown : 0.5f;
            float fireRate = cooldown > 0f ? 1f / cooldown : 0f;
            int finalDamage = 15;
            float damageMultiplier = baseDamage > 0 ? (float)finalDamage / (float)baseDamage : 1f;

            string desc = string.Format("高能步枪，发射能量弹。伤害: {0} 射速: {1:0.##}/秒", finalDamage, fireRate);

            _thanosGun = WeaponFactory.CreateAndRegisterWeaponFromTemplate(
                codeName: "ThanosGun",
                displayName: "能量步枪",
                description: desc,
                textureResourceName: ".Resources.ThanosGun.png",
                template: template,
                bulletOverride: _purpleBullet,
                soundOverride: "plasma",
                damageMultiplier: damageMultiplier,
                damageOverride: finalDamage
            );
        }

        private static void RegisterBlocks()
        {
            string desc = "坚硬的火山岩石墙体。耐久: 500 硬度: 6000 重量: 500";
            _obsidianWall = BlockFactory.CreateAndRegisterBlock(
                codeName: "MyMod_ObsidianWall",
                displayName: "黑曜石墙",
                description: desc,
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
                    trav.Field("m_desc").SetValue("蕴含能量的水晶，可用于高级配方。堆叠: 250");
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
                description: "可提取能量水晶。耐久: 50",
                health: 50,
                textureResourceName: ".Resources.EnergyCrystalOre.png"
            );
        }

        private static void RegisterDevices()
        {
            _oreDetectors.Clear();
            DetectorTargets.Clear();

            RegisterArmor();
            RegisterTeleporter();
            RegisterSuperFlashlight();

            List<CItem_Mineral> minerals = GetVanillaMinerals();
            if (_energyCrystalOre != null)
            {
                minerals.Add(_energyCrystalOre);
            }

            foreach (var mineral in minerals)
            {
                if (mineral == null) continue;
                string oreCode = mineral.m_codeName;
                string oreName = GetItemName(mineral);

                string detectorCode = "MyFirstMod_Detector_" + oreCode;
                string detectorName = oreName + "探测器";
                string detectorDesc = BuildDetectorDesc(GItems.metalDetector, oreName);

                CItem_Device detector = DeviceFactory.CreateAndRegisterDeviceFromTemplate(
                    template: GItems.metalDetector,
                    codeName: detectorCode,
                    displayName: detectorName,
                    description: detectorDesc
                );

                if (detector != null)
                {
                    _oreDetectors.Add(detector);
                    DetectorTargets[detectorCode] = oreCode;
                }
            }
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
                RecipeManager.RegisterRecipe(_thanosGun, GItems.aluminium, 5, _energyCrystal, 10);
            }

            if (_obsidianWall != null)
            {
                RecipeManager.RegisterRecipe(_obsidianWall, GItems.rock, 10);
            }

            if (_energyCrystal != null)
            {
                foreach (var detector in _oreDetectors)
                {
                    if (detector != null)
                    {
                        RecipeManager.RegisterRecipe(detector, _energyCrystal, 10);
                    }
                }
            }

            if (_superFlashLight != null && _energyCrystal != null)
            {
                RecipeManager.RegisterRecipe(_superFlashLight, _energyCrystal, 5, GItems.lightGem, 5);
            }

            if (_modAssembler != null)
            {
                RecipeManager.RegisterRecipe(_modAssembler, GItems.iron, 5, machineLevel: 1);
            }

            if (_uraniumGenerator != null && _energyCrystal != null)
            {
                RecipeManager.RegisterRecipe(_uraniumGenerator, GItems.uranium, 1, _energyCrystal, 5);
            }

            if (_energyCrystalArmor != null && _energyCrystal != null)
            {
                RecipeManager.RegisterRecipe(_energyCrystalArmor, _energyCrystal, 10);
            }

            if (_teleporter != null && _energyCrystal != null)
            {
                RecipeManager.RegisterRecipe(_teleporter, _energyCrystal, 10);
            }

            if (_energyTurret != null && _energyCrystal != null)
            {
                RecipeManager.RegisterRecipe(_energyTurret, _energyCrystal, 10);
            }
        }

        private static void RegisterMachines()
        {
            CItem autoBuilderMk1 = ItemFactory.FindItemByCodeName("autoBuilderMK1");
            if (autoBuilderMk1 != null)
            {
                _modAssembler = ItemFactory.CreateAndRegisterItemFromTemplate(
                    autoBuilderMk1,
                    "autoBuilderMyMod",
                    "modV1",
                    "用于制造模组物品的专用组装机。放置后使用交互键打开。"
                );

                if (_modAssembler != null)
                {
                    RecipeManager.SetModMachineCodeName(_modAssembler.m_codeName);
                }
            }

            CItem reactor = ItemFactory.FindItemByCodeName("reactor");
            if (reactor == null)
            {
                reactor = ItemFactory.FindItemByCodeName("generatorSun");
            }

            if (reactor != null)
            {
                _uraniumGenerator = ItemFactory.CreateAndRegisterItemFromTemplate(
                    reactor,
                    "MyFirstMod_UraniumGenerator",
                    "铀发电机",
                    "铀能自律发电机，制造后即可持续发电（5kW）。"
                );
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
                MaxY = 1000,
                VeinSize = 12,
                Frequency = 0.19f
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

        private static readonly HashSet<string> DetectorBlacklist = new HashSet<string>
        {
            "dirt",
            "dirtRed",
            "rock",
            "hardRock",
            "sand"
        };

        private static List<CItem_Mineral> GetVanillaMinerals()
        {
            List<CItem_Mineral> list = new List<CItem_Mineral>();
            FieldInfo[] fields = typeof(GItems).GetFields(BindingFlags.Public | BindingFlags.Static);
            foreach (var field in fields)
            {
                if (typeof(CItem_Mineral).IsAssignableFrom(field.FieldType))
                {
                    CItem_Mineral item = field.GetValue(null) as CItem_Mineral;
                    if (item != null && !IsBlacklistedMineral(item)) list.Add(item);
                }
            }
            return list;
        }

        private static bool IsBlacklistedMineral(CItem_Mineral item)
        {
            string code = item.m_codeName;
            if (string.IsNullOrEmpty(code)) return false;
            return DetectorBlacklist.Contains(code);
        }


        private static string GetItemName(CItem item)
        {
            if (item == null) return "未知";
            string name = Traverse.Create(item).Field("m_name").GetValue<string>();
            if (!string.IsNullOrEmpty(name)) return name;
            return item.m_codeName;
        }

        private static string BuildDetectorDesc(CItem_Device template, string oreName)
        {
            float duration = 0f;
            float cooldown = 0f;
            float range = 0f;
            if (template != null)
            {
                duration = Traverse.Create(template).Field("m_duration").GetValue<float>();
                cooldown = Traverse.Create(template).Field("m_cooldown").GetValue<float>();
                range = Traverse.Create(template).Field("m_customValue").GetValue<float>();
            }

            if (duration > 0f || cooldown > 0f || range > 0f)
            {
                return string.Format("探测对象: {0}。范围: {1:0.#} 格 持续: {2:0.##}秒 冷却: {3:0.##}秒", oreName, range, duration, cooldown);
            }
            return string.Format("探测对象: {0}。", oreName);
        }

        private static void RegisterArmor()
        {
            CItem_Device template = GItems.armorMk1;
            if (template == null) return;

            _energyCrystalArmor = DeviceFactory.CreateAndRegisterDeviceFromTemplate(
                template: template,
                codeName: "MyFirstMod_EnergyCrystalArmor",
                displayName: "能量水晶护甲",
                description: "基础护甲。防御: 2"
            );

            if (_energyCrystalArmor != null)
            {
                Traverse.Create(_energyCrystalArmor).Field("m_customValue").SetValue(2f);
            }
        }

        private static void RegisterTeleporter()
        {
            CItem_Device template = GItems.metalDetector;
            if (template == null) return;

            string desc = "瞬移到鼠标位置。冷却: 30秒";
            _teleporter = DeviceFactory.CreateAndRegisterDeviceFromTemplate(
                template: template,
                codeName: "MyFirstMod_Teleporter",
                displayName: "能量传送器",
                description: desc,
                groupId: "Teleport",
                type: CItem_Device.Type.Activable,
                customValue: 0f,
                cooldown: 30f,
                duration: 0f
            );
        }

        private static void RegisterSuperFlashlight()
        {
            CItem_Device template = GItems.flashLightMK2;
            if (template == null) template = GItems.flashLight;
            if (template == null) return;

            float superRange = 80f;
            string desc = string.Format("超强手电，亮度极高。照明范围: {0}", superRange);

            _superFlashLight = DeviceFactory.CreateAndRegisterDeviceFromTemplate(
                template: template,
                codeName: "MyFirstMod_SuperFlashLight",
                displayName: "能量手电",
                description: desc,
                customValue: superRange
            );
        }

        private static void RegisterDefenses()
        {
            CItem_Defense template = GItems.turret360;
            if (template == null) return;

            if (_turretBullet == null && template.m_attack != null)
            {
                _turretBullet = BulletFactory.CreateCustomBulletFromTemplate(
                    template.m_attack.m_bulletDesc,
                    ".Resources.PurpleBullet.png",
                    Color.magenta,
                    hasTrail: false
                );
            }

            // 原版 turret360 数值（由 DLL 读取）：HP 25, 探测 10, 攻击范围 8, 伤害 4, 冷却 0.6, 击退 1
            ushort hp = 50;
            float rangeDetection = 20f;
            float angleMin = -9999f;
            float angleMax = 9999f;
            float attackRange = 16f;
            int attackDamage = 8;
            int attackNb = 2;
            float attackCooldown = 0.5f;
            float knockbackOwn = 0f;
            float knockbackTarget = 2f;

            string desc = string.Format("基础炮塔改良型。伤害: {0} 射程: {1} 冷却: {2:0.##} 探测: {3}",
                attackDamage, attackRange, attackCooldown, rangeDetection);

            _energyTurret = DefenseFactory.CreateAndRegisterDefenseFromTemplate(
                template: template,
                codeName: "MyFirstMod_TurretEnergy",
                displayName: "能量炮塔",
                description: desc,
                hp: hp,
                rangeDetection: rangeDetection,
                angleMin: angleMin,
                angleMax: angleMax,
                attackRange: attackRange,
                attackDamage: attackDamage,
                attackNb: attackNb,
                attackCooldown: attackCooldown,
                knockbackOwn: knockbackOwn,
                knockbackTarget: knockbackTarget,
                bulletDesc: _turretBullet ?? _purpleBullet,
                soundName: "plasma"
            );
        }
    }
}
