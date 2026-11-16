using System.Collections.Generic;
using UnityEngine;

namespace ImprovePlayable
{
    // 自定义刷怪规则
    public class SpawnRule
    {
        public string MonsterCodeName;
        public float SpawnRate = 1f;
        public float MinDepth = 0f;
        public float MaxDepth = 1000f;
        public bool IsNightOnly = false;
    }

    public static class SpawnManager
    {
        public static List<SpawnRule> Rules = new List<SpawnRule>();

        // 注册刷怪规则（默认等权重）
        public static void RegisterSpawn(
            string monsterCodeName,
            float minDepth = 0f,
            float maxDepth = 1000f,
            bool nightOnly = false,
            float spawnRate = 1f)
        {
            Rules.Add(new SpawnRule
            {
                MonsterCodeName = monsterCodeName,
                MinDepth = minDepth,
                MaxDepth = maxDepth,
                IsNightOnly = nightOnly,
                SpawnRate = spawnRate
            });
            Debug.Log("[Mod] Spawn Rule Registered: " + monsterCodeName);
        }

        // 按位置与时间筛选可刷新的自定义怪物
        public static List<CUnitMonster.CDesc> GetValidMonstersAtPos(Vector2 pos, bool isNight)
        {
            List<CUnitMonster.CDesc> validMonsters = new List<CUnitMonster.CDesc>();

            foreach (var rule in Rules)
            {
                if (rule.IsNightOnly && !isNight) continue;
                if (pos.y < rule.MinDepth || pos.y > rule.MaxDepth) continue;

                if (MonsterFactory.CustomMonsters.ContainsKey(rule.MonsterCodeName))
                {
                    int weight = Mathf.Max(1, Mathf.RoundToInt(rule.SpawnRate));
                    for (int i = 0; i < weight; i++)
                    {
                        validMonsters.Add(MonsterFactory.CustomMonsters[rule.MonsterCodeName].Desc);
                    }
                }
            }

            return validMonsters;
        }
    }
}
