using HarmonyLib;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace MyFirstMod
{
    public static class BlockFactory
    {
        public static readonly List<CItem> CustomBlocks = new List<CItem>();

        // 世界方块用贴图宽度做 PPU，保证 1 个格子大小稳定
        private static CTile CreateWorldTile(Texture2D texture)
        {
            float ppu = texture.width;
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), ppu);
            return CreateTile(sprite, texture, "SurfaceOpaque");
        }

        // UI 图标用高度做 PPU，保证在格子内居中且不溢出
        private static CTile CreateIconTile(Texture2D texture)
        {
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), texture.height);
            return CreateTile(sprite, texture, "SurfaceOpaque");
        }

        // 矿物叠加层使用带边界混合的材质，便于透明区域正确叠加
        private static CTile CreateOverlayTile(Texture2D texture)
        {
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), texture.height);
            return CreateTile(sprite, texture, "SurfaceBorders");
        }

        public static CItem_Mineral CreateAndRegisterMineral(
            string codeName, string displayName, string description,
            ushort health, string textureResourceName)
        {
            if (GItems.Items.Exists(x => x != null && x.m_codeName == codeName)) return null;

            byte[] imageData = ResourceLoader.LoadResourceBytes(textureResourceName);
            if (imageData == null) return null;
            Texture2D customTexture = ResourceLoader.CreateTexture(imageData);
            if (customTexture == null) return null;

            CItem_Mineral template = GItems.iron;
            if (template == null) return null;

            CItem_Mineral newMineral = ShallowClone(template);

            CSurface mineralSurface = new CSurface("rock", 0, -1, -1, false, null, null, false);
            CTile mineralOverlayTile = CreateOverlayTile(customTexture);
            CTile worldDisplayTile = CreateWorldTile(customTexture);
            CTile iconTile = CreateIconTile(customTexture);

            Traverse.Create(mineralSurface).Field("m_topIcon").SetValue(mineralOverlayTile);

            Color32 avgColor = CalculateAverageColor(customTexture);

            var trav = Traverse.Create(newMineral);
            trav.Field("m_surface").SetValue(mineralSurface);
            trav.Field("m_tile").SetValue(worldDisplayTile);
            trav.Field("m_tileIcon").SetValue(iconTile);
            trav.Field("m_codeName").SetValue(codeName);
            trav.Field("m_name").SetValue(displayName);
            trav.Field("m_desc").SetValue(description);
            trav.Field("m_hpMax").SetValue(health);
            trav.Field("m_mainColor").SetValue(avgColor);
            trav.Field("m_canBeMetalDetected").SetValue(true);

            newMineral.m_isDisplayInSameDirection = true;
            newMineral.m_id = (ushort)GItems.Items.Count;
            GItems.Items.Add(newMineral);
            CustomBlocks.Add(newMineral);

            Debug.Log(string.Format(">>> 矿物 '{0}' 已创建。", displayName));
            return newMineral;
        }

        public static CustomItemWall CreateAndRegisterBlock(
            string codeName, string displayName, string description,
            ushort health, int hardness, float weight,
            CItem_Wall.Type blockType, string textureResourceName)
        {
            if (GItems.Items.Exists(x => x != null && x.m_codeName == codeName)) return null;
            byte[] imageData = ResourceLoader.LoadResourceBytes(textureResourceName);
            if (imageData == null) return null;
            Texture2D customTexture = ResourceLoader.CreateTexture(imageData);
            if (customTexture == null) return null;
            customTexture.wrapMode = TextureWrapMode.Clamp;

            CSurface customSurface = SurfaceFactory.CreateCustomSurface(customTexture);
            if (customSurface == null) return null;

            CTile worldTile = CreateWorldTile(customTexture);
            CTile iconTile = CreateIconTile(customTexture);
            if (worldTile == null || iconTile == null) return null;

            CustomItemWall newBlock = new CustomItemWall(worldTile, iconTile, health, 12282173U, hardness, weight, blockType, customSurface);
            Color32 avgColor = CalculateAverageColor(customTexture);
            newBlock.m_mainColor = avgColor;
            newBlock.m_codeName = codeName;
            Traverse.Create(newBlock).Field("m_name").SetValue(displayName);
            Traverse.Create(newBlock).Field("m_desc").SetValue(description);
            Traverse.Create(newBlock).Field("m_machineGroup").SetValue("autocrafter_mk1");

            newBlock.m_id = (ushort)GItems.Items.Count;
            GItems.Items.Add(newBlock);
            CustomBlocks.Add(newBlock);

            Debug.Log(string.Format(">>> 方块 '{0}' 已创建。", displayName));
            return newBlock;
        }

        private static CTile CreateTile(Sprite sprite, Texture2D texture, string materialName)
        {
            try
            {
                var assetSpriteType = AccessTools.TypeByName("CAssetSprite");
                var spriteConstructor = AccessTools.Constructor(assetSpriteType, new[] { typeof(string), typeof(string), typeof(Rect) });
                var assetSpriteInstance = spriteConstructor.Invoke(new object[] { "custom", "custom", new Rect() });
                AccessTools.Property(assetSpriteType, "Sprite").SetValue(assetSpriteInstance, sprite, null);
                var assetSpriteArray = Array.CreateInstance(assetSpriteType, 1);
                assetSpriteArray.SetValue(assetSpriteInstance, 0);

                Material baseMaterial = Resources.Load(string.Format("Materials/{0}", materialName)) as Material;
                Material customMaterial = new Material(baseMaterial);
                customMaterial.mainTexture = texture;

                var assetMaterialType = AccessTools.TypeByName("CAssetMaterial");
                var materialConstructor = AccessTools.Constructor(assetMaterialType, new[] { typeof(string), typeof(string) });
                var assetMaterialInstance = materialConstructor.Invoke(new object[] { materialName, (string)null });
                Traverse.Create(assetMaterialInstance).Field("m_asset").SetValue(customMaterial);

                CTile customTile = new CTile(99, 99);
                var customTileTraverse = Traverse.Create(customTile);
                customTileTraverse.Field("m_sprites").SetValue(assetSpriteArray);
                customTileTraverse.Field("m_mat").SetValue(assetMaterialInstance);
                return customTile;
            }
            catch (Exception ex)
            {
                Debug.LogError(string.Format("[Mod] CreateTile 失败: {0}", ex));
                return null;
            }
        }

        private static Color32 CalculateAverageColor(Texture2D tex)
        {
            Color[] pixels = tex.GetPixels();
            float r = 0, g = 0, b = 0;
            foreach (var p in pixels) { r += p.r; g += p.g; b += p.b; }
            Color c = new Color(r / pixels.Length, g / pixels.Length, b / pixels.Length);
            return (Color32)c;
        }

        private static T ShallowClone<T>(T source) where T : class
        {
            var method = typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic);
            return (T)method.Invoke(source, null);
        }
    }
}
