using HarmonyLib;
using System;
using System.Reflection;

namespace ImprovePlayable
{
    public static class DeviceFactory
    {
        public static CItem_Device CreateAndRegisterDeviceFromTemplate(
            CItem_Device template,
            string codeName,
            string displayName,
            string description,
            string groupId = null,
            CItem_Device.Type? type = null,
            float? customValue = null,
            float? cooldown = null,
            float? duration = null)
        {
            if (template == null) return null;
            if (GItems.Items.Exists(x => x != null && x.m_codeName == codeName)) return null;

            CItem_Device newDevice = ShallowClone(template);
            var trav = Traverse.Create(newDevice);
            trav.Field("m_codeName").SetValue(codeName);
            trav.Field("m_locTextId").SetValue(codeName);
            trav.Field("m_name").SetValue(displayName);
            trav.Field("m_desc").SetValue(description);

            if (groupId != null) trav.Field("m_groupId").SetValue(groupId);
            if (type.HasValue) trav.Field("m_type").SetValue(type.Value);
            if (customValue.HasValue) trav.Field("m_customValue").SetValue(customValue.Value);
            if (cooldown.HasValue) trav.Field("m_cooldown").SetValue(cooldown.Value);
            if (duration.HasValue) trav.Field("m_duration").SetValue(duration.Value);

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
