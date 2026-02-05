using HarmonyLib;
using System.Collections.Generic;
using System;

namespace MyFirstMod
{
    [HarmonyPatch(typeof(SLoc))]
    [HarmonyPatch("GetText", new Type[] { typeof(string), typeof(bool), typeof(string), typeof(string), typeof(string), typeof(string) })]
    public static class Patch_Localization
    {
        public static Dictionary<string, string> CustomLocales = new Dictionary<string, string>();

        public static void Register(string key, string text)
        {
            if (!CustomLocales.ContainsKey(key)) CustomLocales.Add(key, text);
        }

        static bool Prefix(string id, ref string __result)
        {
            if (CustomLocales.TryGetValue(id, out string text))
            {
                __result = text;
                return false;
            }
            return true;
        }
    }
}
