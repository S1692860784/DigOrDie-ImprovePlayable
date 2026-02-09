using UnityEngine;
using HarmonyLib;
using System.Collections.Generic;
using System;
using System.Reflection;
using System.Runtime.Serialization;

namespace MyFirstMod
{
    public enum MonsterAIType
    {
        Ground,
        Flying,
        GroundRanged,
        FlyingRanged,
        Boss
    }

    public static class MonsterFactory
    {
        public class CustomMonsterData
        {
            public CUnitMonster.CDesc Desc;
            public MonsterAIType AIType;
            public CBulletDesc BulletDesc;
        }

        public static Dictionary<string, CustomMonsterData> CustomMonsters = new Dictionary<string, CustomMonsterData>();

        public static CUnitMonster.CDesc CreateAndRegisterMonster(
            string codeName,
            string textureResourceName,
            int hp,
            float speed,
            int damage,
            float range,
            float attackCooldown,
            MonsterAIType aiType = MonsterAIType.Ground,
            int[] frameCounts = null)
        {
            return CreateAndRegisterMonster_Internal(codeName, textureResourceName, hp, speed, damage, range, attackCooldown, aiType, null, frameCounts);
        }

        public static CUnitMonster.CDesc CreateAndRegisterMonster(
            string codeName,
            string textureResourceName,
            int hp,
            float speed,
            int damage,
            float range,
            float attackCooldown,
            MonsterAIType aiType,
            CBulletDesc bulletDesc,
            int[] frameCounts = null)
        {
            return CreateAndRegisterMonster_Internal(codeName, textureResourceName, hp, speed, damage, range, attackCooldown, aiType, bulletDesc, frameCounts);
        }

        private static CUnitMonster.CDesc CreateAndRegisterMonster_Internal(
            string codeName,
            string textureResourceName,
            int hp,
            float speed,
            int damage,
            float range,
            float attackCooldown,
            MonsterAIType aiType,
            CBulletDesc bulletDesc,
            int[] frameCounts = null)
        {
            if (frameCounts == null || frameCounts.Length != 4)
            {
                frameCounts = new int[] { 4, 4, 4, 4 };
            }

            byte[] imageData = ResourceLoader.LoadResourceBytes(textureResourceName);
            if (imageData == null)
            {
                Debug.LogError("[Mod] MonsterFactory: 无法加载图片 " + textureResourceName);
                return null;
            }

            Texture2D texture = ResourceLoader.CreateTexture(imageData);
            if (texture == null) return null;

            string uniqueTextureName = "mod://" + codeName + "/texture";
            if (!Patch_CAssetSprite_Get.CustomTextures.ContainsKey(uniqueTextureName))
            {
                Patch_CAssetSprite_Get.CustomTextures.Add(uniqueTextureName, texture);
            }

            int frameW = texture.width / 4;
            int frameH = texture.height / 4;
            if (texture.width % 4 != 0 || texture.height % 4 != 0)
            {
                Debug.LogWarning(string.Format("[Mod] 怪物贴图尺寸不是 4x4 帧表: {0} ({1}x{2})", codeName, texture.width, texture.height));
            }

            CTilesList anims = (CTilesList)FormatterServices.GetUninitializedObject(typeof(CTilesList));
            anims.m_texture = uniqueTextureName;
            anims.m_textureSize = texture.width;

            Material sharedCustomMaterial = CreateSharedMaterial(codeName, texture);
            anims.m_stand = CreateAnimTile(uniqueTextureName, texture, 0, 0, frameCounts[0], frameW, frameH, sharedCustomMaterial);
            anims.m_run = CreateAnimTile(uniqueTextureName, texture, 0, 1, frameCounts[1], frameW, frameH, sharedCustomMaterial);
            anims.m_fight = CreateAnimTile(uniqueTextureName, texture, 0, 2, frameCounts[2], frameW, frameH, sharedCustomMaterial);
            anims.m_dead = CreateAnimTile(uniqueTextureName, texture, 0, 3, frameCounts[3], frameW, frameH, sharedCustomMaterial);
            anims.m_jump = anims.m_run;
            anims.m_hurt = anims.m_stand;
            anims.m_standWall = anims.m_stand;
            anims.m_runWall = anims.m_run;
            anims.m_fightWall = anims.m_fight;
            anims.m_hurtWall = anims.m_hurt;

            float widthRatio = (aiType == MonsterAIType.Ground || aiType == MonsterAIType.GroundRanged) ? 0.6f : 0.8f;
            float colliderWidth = (frameW / 100f) * widthRatio;
            float colliderHeight = (frameH / 100f) * 0.9f;
            Vector2 size = new Vector2(colliderWidth, colliderHeight);

            CAttackDesc attackDesc = new CAttackDesc(range, damage, 1, attackCooldown, 0f, 5f, null, "punch");
            if (bulletDesc != null)
            {
                bulletDesc.m_dispertionAngleRad = 0.2f;
            }

            CUnitMonster.CDesc monsterDesc = new CUnitMonster.CDesc(1, speed, size, hp, 0, attackDesc, anims, new CStack[0]);
            monsterDesc.m_codeName = codeName;
            monsterDesc.m_id = (byte)GUnits.UDescs.Count;

            GUnits.UDescs.Add(monsterDesc);
            if (!CustomMonsters.ContainsKey(codeName))
            {
                CustomMonsters.Add(codeName, new CustomMonsterData
                {
                    Desc = monsterDesc,
                    AIType = aiType,
                    BulletDesc = bulletDesc
                });
            }

            Debug.Log(string.Format(">>> 怪物 '{0}' 注册完成 (AI: {1}, Size: {2})", codeName, aiType, size));
            return monsterDesc;
        }

        private static Material CreateSharedMaterial(string codeName, Texture2D texture)
        {
            Material baseMaterial = Resources.Load("Materials/Units") as Material;
            if (baseMaterial == null) baseMaterial = Resources.Load("Materials/SurfaceOpaque") as Material;

            Material customMat = new Material(baseMaterial);
            customMat.mainTexture = texture;

            string materialFilename = "mod://" + codeName + "/material";
            Patch_CAssetMaterial_Get.CustomMaterialsByFilename[materialFilename] = customMat;
            return customMat;
        }

        private static CTile CreateAnimTile(
            string uniqueTextureName,
            Texture2D texture,
            int startCol,
            int row,
            int nbFrames,
            int frameW,
            int frameH,
            Material sharedCustomMaterial)
        {
            CTile tile = new CTile(startCol, row, nbFrames, frameW, frameH);
            tile.m_textureName = uniqueTextureName;
            tile.m_textureSize = texture.width;

            Type assetSpriteType = AccessTools.TypeByName("CAssetSprite");
            CAssetSprite[] spriteAssets = new CAssetSprite[nbFrames];
            for (int i = 0; i < nbFrames; i++)
            {
                float pixelX = (startCol + i) * frameW;
                float pixelY = row * frameH;
                Rect frameRect = new Rect(pixelX, pixelY, frameW, frameH);
                object assetSpriteInstance = AccessTools.Constructor(assetSpriteType, new[] { typeof(string), typeof(string), typeof(Rect) })
                    .Invoke(new object[] { uniqueTextureName, null, frameRect });
                spriteAssets[i] = (CAssetSprite)assetSpriteInstance;
            }

            Type assetMaterialType = AccessTools.TypeByName("CAssetMaterial");
            string materialFilename = uniqueTextureName.Replace("/texture", "/material");
            object assetMaterialInstance = AccessTools.Constructor(assetMaterialType, new[] { typeof(string), typeof(string) })
                .Invoke(new object[] { materialFilename, null });

            Traverse.Create(tile).Field("m_sprites").SetValue(spriteAssets);
            Traverse.Create(tile).Field("m_mat").SetValue(assetMaterialInstance);
            return tile;
        }
    }
}
