using UnityEngine;

namespace ImprovePlayable
{
    public class DefenseModule : IContentModule
    {
        public string Name { get { return "DefenseModule"; } }

        public static CItem_Defense EnergyTurret { get; private set; }
        public static CBulletDesc TurretBullet { get; private set; }

        public bool IsEnabled(ModContentOptions options)
        {
            return options.EnableDefenses;
        }

        public void Initialize()
        {
            ItemsConfigContainer container = JsonConfigHelper.LoadOrCreateConfig<ItemsConfigContainer>("Items.json", null);
            if (container == null || container.defenses == null || container.defenses.Count == 0) return;

            CItem_Defense template = GItems.turret360;
            if (template == null) return;

            foreach (var cfg in container.defenses)
            {
                if (cfg == null || string.IsNullOrEmpty(cfg.codeName)) continue;

                if (TurretBullet == null && template.m_attack != null)
                {
                    TurretBullet = BulletFactory.CreateCustomBulletFromTemplate(
                        template.m_attack.m_bulletDesc,
                        !string.IsNullOrEmpty(cfg.bulletTexture) ? cfg.bulletTexture : ".Resources.PurpleBullet.png",
                        Color.magenta,
                        hasTrail: false
                    );
                }

                ushort hp = cfg.hp > 0 ? (ushort)cfg.hp : (ushort)50;
                float rangeDetection = cfg.rangeDetection > 0f ? cfg.rangeDetection : 20f;
                float attackRange = cfg.attackRange > 0f ? cfg.attackRange : 16f;
                int attackDamage = cfg.attackDamage > 0 ? cfg.attackDamage : 8;
                int attackNb = cfg.attackNb > 0 ? cfg.attackNb : 2;
                float attackCooldown = cfg.attackCooldown > 0f ? cfg.attackCooldown : 0.5f;

                string desc = !string.IsNullOrEmpty(cfg.description)
                    ? cfg.description
                    : string.Format("防御炮塔。伤害: {0} 射程: {1} 冷却: {2:0.##} 探测: {3}", attackDamage, attackRange, attackCooldown, rangeDetection);

                CItem_Defense defense = DefenseFactory.CreateAndRegisterDefenseFromTemplate(
                    template: template,
                    codeName: cfg.codeName,
                    displayName: cfg.displayName,
                    description: desc,
                    hp: hp,
                    rangeDetection: rangeDetection,
                    angleMin: -9999f,
                    angleMax: 9999f,
                    attackRange: attackRange,
                    attackDamage: attackDamage,
                    attackNb: attackNb,
                    attackCooldown: attackCooldown,
                    knockbackOwn: 0f,
                    knockbackTarget: 2f,
                    bulletDesc: TurretBullet ?? WeaponModule.PurpleBullet,
                    soundName: "plasma"
                );

                if (EnergyTurret == null && cfg.codeName == "ImprovePlayable_TurretEnergy")
                {
                    EnergyTurret = defense;
                }
            }
        }
    }
}
