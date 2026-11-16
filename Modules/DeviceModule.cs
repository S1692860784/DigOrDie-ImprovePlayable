using HarmonyLib;
using UnityEngine;

namespace ImprovePlayable
{
    public class DeviceModule : IContentModule
    {
        public string Name { get { return "DeviceModule"; } }

        public static CItem_Device EnergyCrystalArmor { get; private set; }
        public static CItem_Device Teleporter { get; private set; }
        public static CItem_Device SuperFlashLight { get; private set; }

        public bool IsEnabled(ModContentOptions options)
        {
            return options.EnableDevices;
        }

        public void Initialize()
        {
            ItemsConfigContainer container = JsonConfigHelper.LoadOrCreateConfig<ItemsConfigContainer>("Items.json", null);
            if (container == null || container.devices == null || container.devices.Count == 0) return;

            foreach (var cfg in container.devices)
            {
                if (cfg == null || string.IsNullOrEmpty(cfg.codeName)) continue;

                if (cfg.deviceType == "Armor")
                {
                    CItem_Device template = GItems.armorMk1;
                    if (template == null) continue;

                    CItem_Device armor = DeviceFactory.CreateAndRegisterDeviceFromTemplate(
                        template: template,
                        codeName: cfg.codeName,
                        displayName: cfg.displayName,
                        description: cfg.description
                    );

                    if (armor != null)
                    {
                        Traverse.Create(armor).Field("m_customValue").SetValue(cfg.customValue > 0f ? cfg.customValue : 2f);
                        if (EnergyCrystalArmor == null && cfg.codeName == "ImprovePlayable_EnergyCrystalArmor")
                        {
                            EnergyCrystalArmor = armor;
                        }
                    }
                }
                else if (cfg.deviceType == "Teleport")
                {
                    CItem_Device template = GItems.metalDetector;
                    if (template == null) continue;

                    CItem_Device tp = DeviceFactory.CreateAndRegisterDeviceFromTemplate(
                        template: template,
                        codeName: cfg.codeName,
                        displayName: cfg.displayName,
                        description: cfg.description,
                        groupId: "Teleport",
                        type: CItem_Device.Type.Activable,
                        customValue: cfg.customValue,
                        cooldown: cfg.cooldown > 0f ? cfg.cooldown : 30f,
                        duration: cfg.duration
                    );

                    if (Teleporter == null && cfg.codeName == "ImprovePlayable_Teleporter")
                    {
                        Teleporter = tp;
                    }
                }
                else if (cfg.deviceType == "Flashlight")
                {
                    CItem_Device template = GItems.flashLightMK2 ?? GItems.flashLight;
                    if (template == null) continue;

                    float range = cfg.customValue > 0f ? cfg.customValue : 80f;
                    string desc = !string.IsNullOrEmpty(cfg.description) ? cfg.description : string.Format("超强手电。照明范围: {0}", range);

                    CItem_Device light = DeviceFactory.CreateAndRegisterDeviceFromTemplate(
                        template: template,
                        codeName: cfg.codeName,
                        displayName: cfg.displayName,
                        description: desc,
                        customValue: range
                    );

                    if (SuperFlashLight == null && cfg.codeName == "ImprovePlayable_SuperFlashLight")
                    {
                        SuperFlashLight = light;
                    }
                }
            }
        }
    }
}
