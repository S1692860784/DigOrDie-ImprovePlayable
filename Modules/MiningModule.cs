using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace ImprovePlayable
{
    public class MiningModule : IContentModule
    {
        public string Name { get { return "MiningModule"; } }

        public static CItem_Material EnergyCrystal { get; private set; }
        public static CItem_Mineral EnergyCrystalOre { get; private set; }
        public static List<CItem_Device> OreDetectors { get; private set; }

        public bool IsEnabled(ModContentOptions options)
        {
            return options.EnableOres;
        }

        static MiningModule()
        {
            OreDetectors = new List<CItem_Device>();
        }

        public void Initialize()
        {
            ItemsConfigContainer container = JsonConfigHelper.LoadOrCreateConfig<ItemsConfigContainer>("Items.json", null);
            if (container != null)
            {
                RegisterMaterials(container.materials);
                RegisterOres(container.ores);
            }
            RegisterDetectors();
        }

        private void RegisterMaterials(List<MaterialConfig> materials)
        {
            if (materials == null) return;

            foreach (var mat in materials)
            {
                if (mat == null || string.IsNullOrEmpty(mat.codeName)) continue;

                CTile tile = ResourceLoader.CreateTileFromResource(mat.textureResource);
                if (tile == null) tile = GItems.iron.m_tileIcon;

                try
                {
                    Type materialType = typeof(CItem_Material);
                    Type[] constructorArgTypes = { typeof(CTile), typeof(CTile), typeof(ushort), typeof(uint), typeof(CItemCell.Anchor) };
                    var constructor = materialType.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, constructorArgTypes, null);
                    if (constructor != null)
                    {
                        object[] constructorArgs = { tile, tile, (ushort)0, 0xFFFFFFFF, CItemCell.Anchor.NotPlacable };
                        CItem_Material item = (CItem_Material)constructor.Invoke(constructorArgs);

                        var trav = Traverse.Create(item);
                        trav.Field("m_codeName").SetValue(mat.codeName);
                        trav.Field("m_nameID").SetValue(mat.codeName);
                        trav.Field("m_name").SetValue(mat.displayName);
                        trav.Field("m_desc").SetValue(mat.description);
                        trav.Field("m_maxStack").SetValue(mat.maxStack > 0 ? mat.maxStack : 250);

                        item.m_id = (ushort)GItems.Items.Count;
                        GItems.Items.Add(item);

                        if (EnergyCrystal == null && mat.codeName == "ImprovePlayable_EnergyCrystal")
                        {
                            EnergyCrystal = item;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError(string.Format("[Mod] 创建材料 '{0}' 失败: {1}", mat.codeName, ex.Message));
                }
            }
        }

        private void RegisterOres(List<OreConfig> ores)
        {
            if (ores == null) return;

            foreach (var ore in ores)
            {
                if (ore == null || string.IsNullOrEmpty(ore.codeName)) continue;

                CItem_Mineral mineral = BlockFactory.CreateAndRegisterMineral(
                    codeName: ore.codeName,
                    displayName: ore.displayName,
                    description: ore.description,
                    health: ore.health > 0 ? ore.health : 50,
                    textureResourceName: ore.textureResource
                );

                if (EnergyCrystalOre == null && ore.codeName == "ImprovePlayable_EnergyCrystalOre")
                {
                    EnergyCrystalOre = mineral;
                }

                // 注册挖掘掉落物
                if (!string.IsNullOrEmpty(ore.dropItemCodeName))
                {
                    GameDataManager.RegisterCustomDrop(
                        ore.codeName,
                        ore.dropItemCodeName,
                        ore.minDrop > 0 ? ore.minDrop : 1,
                        ore.maxDrop >= ore.minDrop ? ore.maxDrop : 3
                    );
                }

                // 注册矿脉自然生成规则
                if (!string.IsNullOrEmpty(ore.replaceBlockCodeName) && ore.frequency > 0f)
                {
                    GameDataManager.RegisterOreSpawn(new OreSpawnRule
                    {
                        OreCodeName = ore.codeName,
                        ReplacesBlockCodeName = ore.replaceBlockCodeName,
                        MinY = ore.spawnMinY,
                        MaxY = ore.spawnMaxY,
                        VeinSize = ore.veinSize > 0 ? ore.veinSize : 12,
                        Frequency = ore.frequency
                    });
                }
            }
        }

        private void RegisterDetectors()
        {
            OreDetectors.Clear();
            ModManager.DetectorTargets.Clear();

            List<CItem_Mineral> minerals = GetVanillaMinerals();
            if (EnergyCrystalOre != null && !minerals.Contains(EnergyCrystalOre))
            {
                minerals.Add(EnergyCrystalOre);
            }

            foreach (var mineral in minerals)
            {
                if (mineral == null) continue;
                string oreCode = mineral.m_codeName;
                string oreName = GetItemName(mineral);

                string detectorCode = "ImprovePlayable_Detector_" + oreCode;
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
                    OreDetectors.Add(detector);
                    ModManager.DetectorTargets[detectorCode] = oreCode;
                }
            }
        }

        private static readonly HashSet<string> DetectorBlacklist = new HashSet<string>
        {
            "dirt", "dirtRed", "rock", "hardRock", "sand"
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
            float duration = 0f, cooldown = 0f, range = 0f;
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
    }
}
