using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using System;

namespace ImprovePlayable
{
    // 通过手动注册的方式补丁化 GetMonstersList
    public static class Patch_SpawnSystem
    {
        public static void Postfix_GetMonstersList(Vector2 pos, ref CUnitMonster.CDesc[] __result)
        {
            try
            {
                bool isNight = SGame.IsNight();
                List<CUnitMonster.CDesc> myMonsters = SpawnManager.GetValidMonstersAtPos(pos, isNight);

                if (myMonsters != null && myMonsters.Count > 0)
                {
                    List<CUnitMonster.CDesc> allMonsters = new List<CUnitMonster.CDesc>();
                    if (__result != null) allMonsters.AddRange(__result);
                    allMonsters.AddRange(myMonsters);
                    __result = allMonsters.ToArray();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[Mod Error] SpawnSystem Patch Failed: " + ex.Message);
            }
        }
    }
}
