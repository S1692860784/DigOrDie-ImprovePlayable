// BlockFactory.cs
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

        // 【渲染和图标修复】
        // 我们创建两个独立的 Tile 创建方法，因为世界方块和UI图标的 'pixelsPerUnit' (PPU) 不同。
        private static CTile CreateWorldTile(Texture2D texture)
        {
            // 【修复核心】
            // 之前是 128f，如果图片大就会导致方块变大。
            // 现在改成 texture.width (图片的实际宽度)。
            // 这样做的意思是：无论图片多大，都让它在游戏里只占 1.0 的宽度（正好一格）。
            float ppu = (float)texture.width;

            // 如果图片不是正方形，可能会有点变形，通常建议用 Math.Max(width, height) 来确保它塞进格子里
            // 但对于一般的方块贴图，用宽度做基准通常是最好的。

            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), ppu);
            return CreateTile(sprite, texture, "SurfaceOpaque");
        }

        private static CTile CreateIconTile(Texture2D texture)
        {
            // PPU = texture.height, 这是确保图标在UI格子中不大不小，正好填满的关键
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), texture.height);
            return CreateTile(sprite, texture, "SurfaceOpaque");
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

            // 1. CSurface 构造
            CSurface mineralSurface = new CSurface("rock", 0, -1, -1, false, null, null, false);

            // 2. Tile 创建
            CTile mineralOverlayTile = CreateWorldTile(customTexture);
            CTile worldDisplayTile = CreateWorldTile(customTexture); 
            CTile iconTile = CreateIconTile(customTexture);       

            // 3. 注入 Surface 图标
            Traverse.Create(mineralSurface).Field("m_topIcon").SetValue(mineralOverlayTile);
            
            // 4. 计算颜色
            Color avgColor = CalculateAverageColor(customTexture);

            // 5. 创建实例
            CItem_Mineral newMineral = new CItem_Mineral(worldDisplayTile, iconTile, health, (uint)avgColor.GetHashCode(), mineralSurface, false);

            // =========================================================
            // 【核心修复】 禁止随机旋转！让水晶永远头朝上！
            // =========================================================
            newMineral.m_isDisplayInSameDirection = true; 

            // 6. 其他属性
            newMineral.m_codeName = codeName;
            var trav = Traverse.Create(newMineral);
            trav.Field("m_name").SetValue(displayName);
            trav.Field("m_desc").SetValue(description);
            
            newMineral.m_id = (ushort)GItems.Items.Count;
            GItems.Items.Add(newMineral);
            CustomBlocks.Add(newMineral);

            Debug.Log(string.Format(">>> 矿物 '{0}' 创建成功 (已修正旋转问题)。", displayName));
            return newMineral;
        }

        // ===================================================================
        // --- 以下是你已有的、可以正常工作的方法，我将它们完整地包含进来 ---
        // ===================================================================
        public static CustomItemWall CreateAndRegisterBlock(
            string codeName, string displayName, string description,
            ushort health, int hardness, float weight,
            CItem_Wall.Type blockType, string textureResourceName)
        {
            if (GItems.Items.Exists(x => x != null && x.m_codeName == codeName)) return null;
            byte[] imageData = ResourceLoader.LoadResourceBytes(textureResourceName);
            if (imageData == null) return null;
            Texture2D customTexture = ResourceLoader.CreateTexture(imageData);
            customTexture.wrapMode = TextureWrapMode.Clamp;
            if (customTexture == null) return null;
            CSurface customSurface = SurfaceFactory.CreateCustomSurface(customTexture);
            if (customSurface == null) return null;
            
            CTile worldTile = CreateWorldTile(customTexture);
            CTile iconTile = CreateIconTile(customTexture);

            if (worldTile == null || iconTile == null) return null;
            CustomItemWall newBlock = new CustomItemWall(worldTile, iconTile, health, 12282173U, hardness, weight, blockType, customSurface);
            Color avgColor = CalculateAverageColor(customTexture);
            newBlock.m_mainColor = avgColor;
            newBlock.m_codeName = codeName;
            Traverse.Create(newBlock).Field("m_name").SetValue(displayName);
            Traverse.Create(newBlock).Field("m_desc").SetValue(description);
            Traverse.Create(newBlock).Field("m_machineGroup").SetValue("autocrafter_mk1");
            newBlock.m_id = (ushort)GItems.Items.Count;
            GItems.Items.Add(newBlock);
            CustomBlocks.Add(newBlock);
            Debug.Log(string.Format(">>> 方块 '{0}' (带自定义粒子) 已完成。", displayName));
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
            catch(Exception ex)
            {
                Debug.LogError(string.Format("[Mod] CreateTile 失败: {0}", ex));
                return null;
            }
        }
        
        private static Color CalculateAverageColor(Texture2D tex)
        {
            Color[] pixels = tex.GetPixels();
            float r = 0, g = 0, b = 0;
            foreach (var p in pixels) { r += p.r; g += p.g; b += p.b; }
            return new Color(r / pixels.Length, g / pixels.Length, b / pixels.Length);
        }
    }
}