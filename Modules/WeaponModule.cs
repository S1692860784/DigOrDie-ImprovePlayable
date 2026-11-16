using UnityEngine;

namespace ImprovePlayable
{
    public class WeaponModule : IContentModule
    {
        public string Name { get { return "WeaponModule"; } }

        public static CBulletDesc PurpleBullet { get; private set; }
        public static CItem_Weapon ThanosGun { get; private set; }

        public bool IsEnabled(ModContentOptions options)
        {
            return options.EnableWeapons;
        }

        public void Initialize()
        {
            PurpleBullet = BulletFactory.CreateCustomBullet(
                textureResourceName: ".Resources.PurpleBullet.png",
                speed: 25f,
                radius: 0.2f,
                hasTrail: true,
                lightColor: Color.magenta,
                critChance: 0.05f,
                critMultiplier: 3f
            );

            CItem_Weapon template = GItems.gunLaser;
            int baseDamage = template != null && template.m_attackDesc != null ? template.m_attackDesc.m_damage : 0;
            float cooldown = template != null && template.m_attackDesc != null ? template.m_attackDesc.m_cooldown : 0.5f;
            float fireRate = cooldown > 0f ? 1f / cooldown : 0f;
            int finalDamage = 15;
            float damageMultiplier = baseDamage > 0 ? (float)finalDamage / (float)baseDamage : 1f;

            string desc = string.Format("高能步枪，发射能量弹。伤害: {0} 射速: {1:0.##}/秒", finalDamage, fireRate);

            ThanosGun = WeaponFactory.CreateAndRegisterWeaponFromTemplate(
                codeName: "ThanosGun",
                displayName: "能量步枪",
                description: desc,
                textureResourceName: ".Resources.ThanosGun.png",
                template: template,
                bulletOverride: PurpleBullet,
                soundOverride: "plasma",
                damageMultiplier: damageMultiplier,
                damageOverride: finalDamage
            );
        }
    }
}
