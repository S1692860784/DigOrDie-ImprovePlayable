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
            return options.EnableWeapons;
        }

        public void Initialize()
        {
            RegisterArmor();
            RegisterTeleporter();
            RegisterSuperFlashlight();
        }

        private void RegisterArmor()
        {
            CItem_Device template = GItems.armorMk1;
            if (template == null) return;

            EnergyCrystalArmor = DeviceFactory.CreateAndRegisterDeviceFromTemplate(
                template: template,
                codeName: "ImprovePlayable_EnergyCrystalArmor",
                displayName: "能量水晶护甲",
                description: "基础护甲。防御: 2"
            );

            if (EnergyCrystalArmor != null)
            {
                Traverse.Create(EnergyCrystalArmor).Field("m_customValue").SetValue(2f);
            }
        }

        private void RegisterTeleporter()
        {
            CItem_Device template = GItems.metalDetector;
            if (template == null) return;

            string desc = "瞬移到鼠标位置。冷却: 30秒";
            Teleporter = DeviceFactory.CreateAndRegisterDeviceFromTemplate(
                template: template,
                codeName: "ImprovePlayable_Teleporter",
                displayName: "能量传送器",
                description: desc,
                groupId: "Teleport",
                type: CItem_Device.Type.Activable,
                customValue: 0f,
                cooldown: 30f,
                duration: 0f
            );
        }

        private void RegisterSuperFlashlight()
        {
            CItem_Device template = GItems.flashLightMK2;
            if (template == null) template = GItems.flashLight;
            if (template == null) return;

            float superRange = 80f;
            string desc = string.Format("超强手电，亮度极高。照明范围: {0}", superRange);

            SuperFlashLight = DeviceFactory.CreateAndRegisterDeviceFromTemplate(
                template: template,
                codeName: "ImprovePlayable_SuperFlashLight",
                displayName: "能量手电",
                description: desc,
                customValue: superRange
            );
        }
    }
}
