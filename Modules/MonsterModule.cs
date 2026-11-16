using System;
using UnityEngine;

namespace ImprovePlayable
{
    public class MonsterModule : IContentModule
    {
        public string Name { get { return "MonsterModule"; } }

        public bool IsEnabled(ModContentOptions options)
        {
            return options.EnableMonsters;
        }

        private const string DefaultMonstersJson = @"{
  ""monsters"": [
    {
      ""codeName"": ""ImprovePlayable_ThanosMinion"",
      ""displayName"": ""灭霸小兵"",
      ""textureResource"": "".Resources.ThanosMinion.png"",
      ""hp"": 500,
      ""speed"": 6.0,
      ""damage"": 20,
      ""range"": 1.5,
      ""attackCooldown"": 1.0,
      ""aiType"": ""Ground"",
      ""bulletTexture"": """",
      ""bulletSpeed"": 0.0,
      ""spawnMinY"": 0.0,
      ""spawnMaxY"": 2000.0,
      ""spawnSkyOnly"": false
    },
    {
      ""codeName"": ""ImprovePlayable_FlameLizard"",
      ""displayName"": ""火焰蜥蜴"",
      ""textureResource"": "".Resources.FlameLizard.png"",
      ""hp"": 200,
      ""speed"": 8.0,
      ""damage"": 30,
      ""range"": 1.2,
      ""attackCooldown"": 0.5,
      ""aiType"": ""Flying"",
      ""bulletTexture"": """",
      ""bulletSpeed"": 0.0,
      ""spawnMinY"": 0.0,
      ""spawnMaxY"": 2000.0,
      ""spawnSkyOnly"": false
    },
    {
      ""codeName"": ""ImprovePlayable_EnergyCannon"",
      ""displayName"": ""能量炮手"",
      ""textureResource"": "".Resources.ThanosMinion.png"",
      ""hp"": 400,
      ""speed"": 4.0,
      ""damage"": 35,
      ""range"": 25.0,
      ""attackCooldown"": 2.0,
      ""aiType"": ""GroundRanged"",
      ""bulletTexture"": "".Resources.PurpleBullet.png"",
      ""bulletSpeed"": 15.0,
      ""spawnMinY"": 600.0,
      ""spawnMaxY"": 1000.0,
      ""spawnSkyOnly"": false
    },
    {
      ""codeName"": ""ImprovePlayable_GhostWatcher"",
      ""displayName"": ""幽灵观察者"",
      ""textureResource"": "".Resources.FlameLizard.png"",
      ""hp"": 150,
      ""speed"": 6.0,
      ""damage"": 15,
      ""range"": 18.0,
      ""attackCooldown"": 1.0,
      ""aiType"": ""FlyingRanged"",
      ""bulletTexture"": "".Resources.PurpleBullet.png"",
      ""bulletSpeed"": 15.0,
      ""spawnMinY"": 0.0,
      ""spawnMaxY"": 2000.0,
      ""spawnSkyOnly"": true
    }
  ]
}";

        public void Initialize()
        {
            MonsterConfigContainer container = JsonConfigHelper.LoadOrCreateConfig<MonsterConfigContainer>(
                "Monsters.json",
                DefaultMonstersJson
            );

            if (container == null || container.monsters == null || container.monsters.Count == 0)
            {
                Debug.LogWarning("[Mod] MonsterModule: 未读取到有效的怪物配置。");
                return;
            }

            CBulletDesc sharedRangedBullet = null;

            foreach (var cfg in container.monsters)
            {
                if (cfg == null || string.IsNullOrEmpty(cfg.codeName)) continue;

                // 注册本地化显示名称
                if (!string.IsNullOrEmpty(cfg.displayName))
                {
                    Patch_Localization.Register("U_" + cfg.codeName, cfg.displayName);
                }

                MonsterAIType aiType = ParseAIType(cfg.aiType);

                // 远程投射物处理
                CBulletDesc bulletDesc = null;
                if (!string.IsNullOrEmpty(cfg.bulletTexture))
                {
                    if (sharedRangedBullet == null)
                    {
                        sharedRangedBullet = BulletFactory.CreateCustomBullet(
                            textureResourceName: cfg.bulletTexture,
                            speed: cfg.bulletSpeed > 0f ? cfg.bulletSpeed : 15f,
                            radius: 0.3f,
                            hasTrail: true,
                            lightColor: Color.cyan
                        );
                    }
                    bulletDesc = sharedRangedBullet;
                }

                // 注册怪物数据实体
                MonsterFactory.CreateAndRegisterMonster(
                    codeName: cfg.codeName,
                    textureResourceName: cfg.textureResource,
                    hp: cfg.hp,
                    speed: cfg.speed,
                    damage: cfg.damage,
                    range: cfg.range,
                    attackCooldown: cfg.attackCooldown,
                    aiType: aiType,
                    bulletDesc: bulletDesc
                );

                // 注册自然刷怪深度范围
                SpawnManager.RegisterSpawn(cfg.codeName, cfg.spawnMinY, cfg.spawnMaxY, cfg.spawnSkyOnly);
            }

            Debug.Log(string.Format("[Mod] MonsterModule: 成功通过 JSON 加载并注册了 {0} 种怪物。", container.monsters.Count));
        }

        private static MonsterAIType ParseAIType(string aiTypeStr)
        {
            if (string.IsNullOrEmpty(aiTypeStr)) return MonsterAIType.Ground;

            try
            {
                return (MonsterAIType)Enum.Parse(typeof(MonsterAIType), aiTypeStr, true);
            }
            catch
            {
                return MonsterAIType.Ground;
            }
        }
    }
}
