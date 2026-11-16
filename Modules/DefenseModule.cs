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
            return options.EnableBlocks;
        }

        public void Initialize()
        {
            CItem_Defense template = GItems.turret360;
            if (template == null) return;

            if (TurretBullet == null && template.m_attack != null)
            {
                TurretBullet = BulletFactory.CreateCustomBulletFromTemplate(
                    template.m_attack.m_bulletDesc,
                    ".Resources.PurpleBullet.png",
                    Color.magenta,
                    hasTrail: false
                );
            }

            // 原版 turret360 数值（由 DLL 读取）：HP 25, 探测 10, 攻击范围 8, 伤害 4, 冷却 0.6, 击退 1
            ushort hp = 50;
            float rangeDetection = 20f;
            float angleMin = -9999f;
            float angleMax = 9999f;
            float attackRange = 16f;
            int attackDamage = 8;
            int attackNb = 2;
            float attackCooldown = 0.5f;
            float knockbackOwn = 0f;
            float knockbackTarget = 2f;

            string desc = string.Format("基础炮塔改良型。伤害: {0} 射程: {1} 冷却: {2:0.##} 探测: {3}",
                attackDamage, attackRange, attackCooldown, rangeDetection);

            EnergyTurret = DefenseFactory.CreateAndRegisterDefenseFromTemplate(
                template: template,
                codeName: "ImprovePlayable_TurretEnergy",
                displayName: "能量炮塔",
                description: desc,
                hp: hp,
                rangeDetection: rangeDetection,
                angleMin: angleMin,
                angleMax: angleMax,
                attackRange: attackRange,
                attackDamage: attackDamage,
                attackNb: attackNb,
                attackCooldown: attackCooldown,
                knockbackOwn: knockbackOwn,
                knockbackTarget: knockbackTarget,
                bulletDesc: TurretBullet ?? WeaponModule.PurpleBullet,
                soundName: "plasma"
            );
        }
    }
}
