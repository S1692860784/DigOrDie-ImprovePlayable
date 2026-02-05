using System.Collections.Generic;
using UnityEngine;

namespace MyFirstMod
{
    // 定义生成规则
    public class SpawnRule
    {
        public string MonsterCodeName; // 怪物ID
        public float SpawnRate;        // 权重 (目前简单处理：只要符合条件就加入候选池)
        
        // 条件 (可选)
        public float MinDepth = 0f;    // 最小深度 (Y轴，0是地表，越大越深)
        public float MaxDepth = 1000f; 
        public bool IsNightOnly = false; // 是否只在夜晚生成
    }

    public static class SpawnManager
    {
        // 存储所有自定义生成规则
        public static List<SpawnRule> Rules = new List<SpawnRule>();

        // 注册方法
        public static void RegisterSpawn(string monsterCodeName, float minDepth = 0f, float maxDepth = 1000f, bool nightOnly = false)
        {
            Rules.Add(new SpawnRule 
            { 
                MonsterCodeName = monsterCodeName,
                MinDepth = minDepth,
                MaxDepth = maxDepth,
                IsNightOnly = nightOnly
            });
            Debug.Log("[Mod] Spawn Rule Registered: " + monsterCodeName);
        }

        // 获取当前位置应该刷新的自定义怪物列表
        public static List<CUnitMonster.CDesc> GetValidMonstersAtPos(Vector2 pos, bool isNight)
        {
            List<CUnitMonster.CDesc> validMonsters = new List<CUnitMonster.CDesc>();
            
            // 计算深度：游戏中 Y=0 是顶部还是底部？通常 SWorld.Gs.y 是最大高度
            // Dig or Die 中，Y 越大越高。地表大约在 SWorld.Gs.y 的 2/3 处。
            // 我们简单处理：pos.y 就是高度。
            
            foreach (var rule in Rules)
            {
                // 1. 检查夜晚条件
                if (rule.IsNightOnly && !isNight) continue;

                // 2. 检查高度条件 (暂时忽略，先让它到处都能刷)
                // if (pos.y < rule.MinDepth || pos.y > rule.MaxDepth) continue;

                // 3. 找到怪物定义
                if (MonsterFactory.CustomMonsters.ContainsKey(rule.MonsterCodeName))
                {
                    // 【修复】取出 .Desc
                    validMonsters.Add(MonsterFactory.CustomMonsters[rule.MonsterCodeName].Desc);
                }
            }
            return validMonsters;
        }
    }
}