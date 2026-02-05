// Patch_WorldGen.cs (终极诊断版)
using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;

namespace MyFirstMod
{
    // 我们尝试 Hook 一个更早且更稳定的点：SWorld.GenerateEmptyWorld 的后面
    // 或者保持 OnStartGame_FinalStep，但加满日志
    [HarmonyPatch(typeof(SWorld), "OnStartGame_FinalStep")] 
    public class Patch_WorldGen
    {
        private struct SpawnData
        {
            public OreSpawnRule Rule;
            public CItemCell OreItem;
            public ushort ReplaceId;
        }

        static void Postfix()
        {
            // 如果这行日志没出来，说明 Hook 根本没生效！
            Debug.Log(">>> [Mod] ！！！世界生成 Hook (OnStartGame_FinalStep) 被触发了！！！ <<<");
            
            // 检查规则列表是否为空
            if (GameDataManager.CustomOreSpawnRules.Count == 0)
            {
                Debug.LogWarning("[Mod] 警告：没有注册任何矿石生成规则！");
                return;
            }

            int worldWidth = SWorld.Gs.x;
            int worldHeight = SWorld.Gs.y;
            Debug.Log(string.Format("[Mod] 地图尺寸: {0}x{1}", worldWidth, worldHeight));
            
            var validRules = new List<SpawnData>();

            foreach (var rule in GameDataManager.CustomOreSpawnRules)
            {
                CItem oreItem = GItems.Items.Find(x => x != null && x.m_codeName == rule.OreCodeName);
                CItem replaceItem = GItems.Items.Find(x => x != null && x.m_codeName == rule.ReplacesBlockCodeName);

                if (oreItem is CItemCell && replaceItem != null)
                {
                    validRules.Add(new SpawnData { Rule = rule, OreItem = (CItemCell)oreItem, ReplaceId = replaceItem.m_id });
                }
                else
                {
                    Debug.LogError(string.Format("[Mod] 配置错误：找不到 {0} 或 {1}", rule.OreCodeName, rule.ReplacesBlockCodeName));
                }
            }

            foreach (var data in validRules)
            {
                int safeMinY = Mathf.Clamp(data.Rule.MinY, 0, worldHeight - 1);
                int safeMaxY = Mathf.Clamp(data.Rule.MaxY, 0, worldHeight - 1);

                int heightRange = safeMaxY - safeMinY;
                if (heightRange <= 0) heightRange = 1; 

                // 强制至少尝试生成 100 次，用于测试
                int totalAttempts = Mathf.Max(100, Mathf.RoundToInt((worldWidth * heightRange) / 100f * data.Rule.Frequency));
                
                Debug.Log(string.Format("[Mod] 开始生成 {0}，尝试次数: {1}", data.Rule.OreCodeName, totalAttempts));

                int successCount = 0;
                for (int i = 0; i < totalAttempts; i++)
                {
                    int x = UnityEngine.Random.Range(5, worldWidth - 5);
                    int y = UnityEngine.Random.Range(safeMinY, safeMaxY);
                    
                    if (SWorld.Grid[x, y].m_contentId == data.ReplaceId)
                    {
                        GenerateVein(x, y, data.OreItem, data.ReplaceId, data.Rule.VeinSize, worldWidth, worldHeight);
                        successCount++;
                    }
                }
                Debug.Log(string.Format("[Mod] 生成结束。成功放置了 {0} 个矿脉节点。", successCount));
            }
        }

        private static void GenerateVein(int startX, int startY, CItemCell oreItem, ushort replaceId, int size, int mapW, int mapH)
        {
            int cx = startX;
            int cy = startY;
            var worldInst = Traverse.Create(typeof(SSingleton<SWorld>)).Property("Inst").GetValue<SWorld>();
            if (worldInst == null) return;

            for (int i = 0; i < size; i++)
            {
                if (cx < 2 || cx >= mapW - 2 || cy < 2 || cy >= mapH - 2) break;
                if (SWorld.Grid[cx, cy].m_contentId == replaceId)
                {
                    worldInst.SetContent(cx, cy, oreItem);
                }
                cx += UnityEngine.Random.Range(-1, 2);
                cy += UnityEngine.Random.Range(-1, 2);
            }
        }
    }
}