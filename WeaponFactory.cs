using HarmonyLib;
using UnityEngine;
using System;
using System.Reflection;
using System.Collections.Generic;

namespace MyFirstMod
{
    public static class WeaponFactory
    {
        public static readonly List<CItem> CustomWeapons = new List<CItem>();

        public static CItem_Weapon CreateAndRegisterWeaponFromTemplate(
            string codeName,
            string displayName,
            string description,
            string textureResourceName,
            CItem_Weapon template,
            CBulletDesc bulletOverride,
            string soundOverride,
            float damageMultiplier,
            int damageOverride = 0)
        {
            if (template == null) return null;
            if (GItems.Items.Exists(x => x != null && x.m_codeName == codeName)) return null;

            byte[] imgData = ResourceLoader.LoadResourceBytes(textureResourceName);
            if (imgData == null) return null;
            Texture2D customTex = ResourceLoader.CreateTexture(imgData);
            if (customTex == null) return null;

            CTile worldTile = CreateWeaponTile(customTex, false);
            CTile iconTile = CreateWeaponTile(customTex, true);
            if (worldTile == null || iconTile == null) return null;

            CItem_Weapon newGun = ShallowClone(template);
            var trav = Traverse.Create(newGun);
            trav.Field("m_codeName").SetValue(codeName);
            trav.Field("m_name").SetValue(displayName);
            trav.Field("m_desc").SetValue(description);
            trav.Field("m_tile").SetValue(worldTile);
            trav.Field("m_tileIcon").SetValue(iconTile);

            CAttackDesc baseAttack = template.m_attackDesc;
            int damage = baseAttack.m_damage;
            if (damageOverride > 0)
            {
                damage = damageOverride;
            }
            else if (damageMultiplier > 0f)
            {
                damage = Mathf.RoundToInt(baseAttack.m_damage * damageMultiplier);
            }
            CAttackDesc newAttack = new CAttackDesc(
                baseAttack.m_range,
                damage,
                baseAttack.m_nbAttacks,
                baseAttack.m_cooldown,
                baseAttack.m_knockbackOwn,
                baseAttack.m_knockbackTarget,
                bulletOverride,
                soundOverride);

            trav.Field("m_attackDesc").SetValue(newAttack);

            newGun.m_id = (ushort)GItems.Items.Count;
            GItems.Items.Add(newGun);
            CustomWeapons.Add(newGun);

            Debug.Log(string.Format(">>> 武器 '{0}' 已注册。", displayName));
            return newGun;
        }

        public static CItem_Weapon CreateAndRegisterWeapon(
            string codeName, string displayName, string description,
            float range, int damage, float fireRate,
            CBulletDesc bullet, string sound,
            string textureResourceName,
            float heatPerShot = 0f,
            float coolingRate = 10f)
        {
            if (GItems.Items.Exists(x => x != null && x.m_codeName == codeName)) return null;

            byte[] imgData = ResourceLoader.LoadResourceBytes(textureResourceName);
            if (imgData == null) return null;
            Texture2D customTex = ResourceLoader.CreateTexture(imgData);
            if (customTex == null) return null;

            CTile worldTile = CreateWeaponTile(customTex, false);
            CTile iconTile = CreateWeaponTile(customTex, true);
            if (worldTile == null || iconTile == null) return null;

            CAttackDesc attack = new CAttackDesc(range, damage, 1, fireRate, heatPerShot, coolingRate, bullet, sound);
            CItem_Weapon newGun = new CItem_Weapon(worldTile, iconTile, heatPerShot, true, attack);

            newGun.m_codeName = codeName;
            newGun.m_categoryId = GItems.gunShotgun.m_categoryId;

            Traverse trv = Traverse.Create(newGun);
            trv.Field("m_name").SetValue(displayName);
            trv.Field("m_desc").SetValue(description);

            newGun.m_id = (ushort)GItems.Items.Count;
            GItems.Items.Add(newGun);
            CustomWeapons.Add(newGun);

            Debug.Log(string.Format(">>> 武器 '{0}' 已注册。", displayName));
            return newGun;
        }

        private static CTile CreateWeaponTile(Texture2D texture, bool isUi)
        {
            try
            {
                Vector2 pivot;
                float ppu;

                if (isUi)
                {
                    pivot = new Vector2(0.5f, 0.5f);
                    ppu = Mathf.Max(texture.width, texture.height) * 1.1f;
                }
                else
                {
                    pivot = new Vector2(0.2f, 0.5f);
                    ppu = texture.width / 1.2f;
                }

                Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), pivot, ppu);

                var assetSpriteType = AccessTools.TypeByName("CAssetSprite");
                var spriteCtor = AccessTools.Constructor(assetSpriteType, new[] { typeof(string), typeof(string), typeof(Rect) });
                var assetSprite = spriteCtor.Invoke(new object[] { "custom_weapon", null, new Rect() });
                Traverse.Create(assetSprite).Property("Sprite").SetValue(sprite);

                var spriteArray = Array.CreateInstance(assetSpriteType, 1);
                spriteArray.SetValue(assetSprite, 0);

                Material customMat = new Material(Shader.Find("Sprites/Default"));
                customMat.mainTexture = texture;

                var assetMatType = AccessTools.TypeByName("CAssetMaterial");
                var matCtor = AccessTools.Constructor(assetMatType, new[] { typeof(string), typeof(string) });
                var assetMat = matCtor.Invoke(new object[] { "custom_weapon_mat", null });
                Traverse.Create(assetMat).Field("m_asset").SetValue(customMat);

                CTile tile = new CTile(99, 99);
                var tileTrav = Traverse.Create(tile);
                tileTrav.Field("m_sprites").SetValue(spriteArray);
                tileTrav.Field("m_mat").SetValue(assetMat);
                tileTrav.Field("m_tileSize").SetValue(new int2(texture.width, texture.height));

                return tile;
            }
            catch (Exception ex)
            {
                Debug.LogError("[Mod] CreateWeaponTile 失败: " + ex);
                return null;
            }
        }

        private static T ShallowClone<T>(T source) where T : class
        {
            var method = typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic);
            return (T)method.Invoke(source, null);
        }
    }
}
