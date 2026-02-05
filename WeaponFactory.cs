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

        public static CItem_Weapon CreateAndRegisterWeapon(
            string codeName, string displayName, string description,
            float range, int damage, float fireRate,
            CBulletDesc bullet, string sound,
            string textureResourceName,
            float heatPerShot = 0f, 
            float coolingRate = 10f) 
        {
            if (GItems.Items.Exists(x => x != null && x.m_codeName == codeName)) return null;

            // 1. 加载图片
            byte[] imgData = ResourceLoader.LoadResourceBytes(textureResourceName);
            if (imgData == null) return null;
            Texture2D customTex = ResourceLoader.CreateTexture(imgData);

            // 2. 创建 Tile
            CTile worldTile = CreateWeaponTile(customTex, false);
            CTile iconTile = CreateWeaponTile(customTex, true);

            if (worldTile == null || iconTile == null) return null;

            // 3. 创建武器
            CAttackDesc attack = new CAttackDesc(range, damage, 1, fireRate, heatPerShot, coolingRate, bullet, sound);
            
            // --- 【修复点】第3个参数传入 heatPerShot ---
            CItem_Weapon newGun = new CItem_Weapon(worldTile, iconTile, heatPerShot, true, attack);

            newGun.m_codeName = codeName;
            newGun.m_categoryId = GItems.gunShotgun.m_categoryId;

            Traverse trv = Traverse.Create(newGun);
            trv.Field("m_name").SetValue(displayName);
            trv.Field("m_desc").SetValue(description);

            newGun.m_id = (ushort)GItems.Items.Count;
            GItems.Items.Add(newGun);
            CustomWeapons.Add(newGun);
            
            Debug.Log(string.Format(">>> 武器 '{0}' 已注入。", displayName));
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
    }
}