using HarmonyLib;
using System;
using System.Reflection;

namespace ImprovePlayable
{
    public static class ItemFactory
    {
        public static T CreateAndRegisterItemFromTemplate<T>(
            T template,
            string codeName,
            string displayName,
            string description) where T : CItem
        {
            if (template == null) return null;
            if (GItems.Items.Exists(x => x != null && x.m_codeName == codeName)) return null;

            T newItem = ShallowClone(template);
            var trav = Traverse.Create(newItem);
            trav.Field("m_codeName").SetValue(codeName);
            trav.Field("m_locTextId").SetValue(codeName);
            trav.Field("m_name").SetValue(displayName);
            trav.Field("m_desc").SetValue(description);

            var nameIdField = newItem.GetType().GetField("m_nameID", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (nameIdField != null && nameIdField.FieldType == typeof(string))
            {
                nameIdField.SetValue(newItem, codeName);
            }

            newItem.m_id = (ushort)GItems.Items.Count;
            GItems.Items.Add(newItem);
            return newItem;
        }

        public static CItem FindItemByCodeName(string codeName)
        {
            if (string.IsNullOrEmpty(codeName)) return null;
            for (int i = 0; i < GItems.Items.Count; i++)
            {
                CItem item = GItems.Items[i];
                if (item != null && string.Equals(item.m_codeName, codeName, StringComparison.OrdinalIgnoreCase))
                {
                    return item;
                }
            }
            return null;
        }

        private static T ShallowClone<T>(T source) where T : class
        {
            var method = typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic);
            return (T)method.Invoke(source, null);
        }
    }
}
