using HarmonyLib;
using System.Reflection;

namespace MyFirstMod
{
    public static class DeviceFactory
    {
        public static CItem_Device CreateAndRegisterDeviceFromTemplate(
            CItem_Device template,
            string codeName,
            string displayName,
            string description)
        {
            if (template == null) return null;
            if (GItems.Items.Exists(x => x != null && x.m_codeName == codeName)) return null;

            CItem_Device newDevice = ShallowClone(template);
            var trav = Traverse.Create(newDevice);
            trav.Field("m_codeName").SetValue(codeName);
            trav.Field("m_locTextId").SetValue(codeName);
            trav.Field("m_name").SetValue(displayName);
            trav.Field("m_desc").SetValue(description);

            newDevice.m_id = (ushort)GItems.Items.Count;
            GItems.Items.Add(newDevice);
            return newDevice;
        }

        private static T ShallowClone<T>(T source) where T : class
        {
            var method = typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic);
            return (T)method.Invoke(source, null);
        }
    }
}
