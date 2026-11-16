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
            RegisterMaterialsAndOres();
            RegisterDetectors();
        }

        private void RegisterMaterialsAndOres()
        {
            CTile energyCrystalTile = ResourceLoader.CreateTileFromResource(".Resources.EnergyCrystal.png");
            if (energyCrystalTile == null) energyCrystalTile = GItems.iron.m_tileIcon;

            try
            {
                Type materialType = typeof(CItem_Material);
                Type[] constructorArgTypes = { typeof(CTile), typeof(CTile), typeof(ushort), typeof(uint), typeof(CItemCell.Anchor) };
                var constructor = materialType.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, constructorArgTypes, null);
                if (constructor != null)
                {
                    object[] constructorArgs = { energyCrystalTile, energyCrystalTile, (ushort)0, 0xFFFFFFFF, CItemCell.Anchor.NotPlacable };
                    EnergyCrystal = (CItem_Material)constructor.Invoke(constructorArgs);

                    var trav = Traverse.Create(EnergyCrystal);
                    trav.Field("m_codeName").SetValue("ImprovePlayable_EnergyCrystal");
                    trav.Field("m_nameID").SetValue("ImprovePlayable_EnergyCrystal");
                    trav.Field("m_name").SetValue("能量水晶");
                    trav.Field("m_desc").SetValue("蕴含能量的水晶，可用于高级配方。堆叠: 250");
                    trav.Field("m_maxStack").SetValue(250);

                    EnergyCrystal.m_id = (ushort)GItems.Items.Count;
                    GItems.Items.Add(EnergyCrystal);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[Mod] 创建 Energy Crystal 失败: " + ex);
            }

            EnergyCrystalOre = BlockFactory.CreateAndRegisterMineral(
                codeName: "ImprovePlayable_EnergyCrystalOre",
                displayName: "能量水晶矿",
                description: "可提取能量水晶。耐久: 50",
                health: 50,
                textureResourceName: ".Resources.EnergyCrystalOre.png"
            );
        }

        private void RegisterDetectors()
        {
            OreDetectors.Clear();
            ModManager.DetectorTargets.Clear();

            List<CItem_Mineral> minerals = GetVanillaMinerals();
            if (EnergyCrystalOre != null)
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
    }
}
