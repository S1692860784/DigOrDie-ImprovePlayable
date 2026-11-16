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
            ItemsConfigContainer container = JsonConfigHelper.LoadOrCreateConfig<ItemsConfigContainer>("Items.json", null);
            if (container == null || container.weapons == null || container.weapons.Count == 0) return;

            CItem_Weapon template = GItems.gunLaser;
            int baseDamage = template != null && template.m_attackDesc != null ? template.m_attackDesc.m_damage : 0;
            float cooldown = template != null && template.m_attackDesc != null ? template.m_attackDesc.m_cooldown : 0.5f;

            foreach (var cfg in container.weapons)
            {
                if (cfg == null || string.IsNullOrEmpty(cfg.codeName)) continue;

                CBulletDesc bullet = BulletFactory.CreateCustomBullet(
                    textureResourceName: cfg.bulletTexture,
                    speed: cfg.bulletSpeed > 0f ? cfg.bulletSpeed : 25f,
                    radius: 0.2f,
                    hasTrail: true,
                    lightColor: Color.magenta,
                    critChance: cfg.critChance,
                    critMultiplier: cfg.critMultiplier > 0f ? cfg.critMultiplier : 3f
                );

                if (PurpleBullet == null) PurpleBullet = bullet;

                float fireRate = cfg.fireRate > 0f ? cfg.fireRate : (cooldown > 0f ? 1f / cooldown : 2f);
                float damageMultiplier = baseDamage > 0 ? (float)cfg.damage / (float)baseDamage : 1f;

                string desc = !string.IsNullOrEmpty(cfg.description)
                    ? string.Format("{0} 伤害: {1} 射速: {2:0.##}/秒", cfg.description, cfg.damage, fireRate)
                    : string.Format("高能武器。伤害: {0} 射速: {1:0.##}/秒", cfg.damage, fireRate);

                CItem_Weapon weapon = WeaponFactory.CreateAndRegisterWeaponFromTemplate(
                    codeName: cfg.codeName,
                    displayName: cfg.displayName,
                    description: desc,
                    textureResourceName: cfg.textureResource,
                    template: template,
                    bulletOverride: bullet,
                    soundOverride: "plasma",
                    damageMultiplier: damageMultiplier,
                    damageOverride: cfg.damage
                );

                if (ThanosGun == null && cfg.codeName == "ThanosGun") ThanosGun = weapon;
            }
        }
    }
}
