using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using System;

namespace MyFirstMod
{
    // 注意：这里去掉了 [HarmonyPatch] 标签，因为我们要手动注册
    public static class Patch_SpawnSystem
    {
        // 这是通用的 Postfix 方法
        public static void Postfix_GetMonstersList(Vector2 pos, ref CUnitMonster.CDesc[] __result)
        {
            try
            {
                // 1. 获取当前环境是否夜晚
                bool isNight = SGame.IsNight();

                // 2. 获取符合条件的自定义怪物
                List<CUnitMonster.CDesc> myMonsters = SpawnManager.GetValidMonstersAtPos(pos, isNight);

                if (myMonsters != null && myMonsters.Count > 0)
                {
                    List<CUnitMonster.CDesc> allMonsters = new List<CUnitMonster.CDesc>();
                    if (__result != null)
                    {
                        allMonsters.AddRange(__result);
                    }
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