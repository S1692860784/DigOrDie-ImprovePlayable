using HarmonyLib;
using UnityEngine;
using System;
using System.Reflection;

namespace MyFirstMod
{
    // 调试热键与工具方法（仅在配置启用时注入）
    public class InputHandler
    {
        [HarmonyPatch(typeof(SGame), "OnUpdate")]
        [HarmonyPostfix]
        public static void InGameUpdate()
        {
            if (!SGame.Playing || G.m_player == null) return;

            // ===== 调试热键 =====
            if (Input.GetKeyDown(KeyCode.F1)) ShowPlayerCoordinates();

            if (Input.GetKeyDown(KeyCode.F5)) GiveItemAsPickup("ThanosGun");

            if (Input.GetKeyDown(KeyCode.F8)) PlaceBlockAtCursor("MyFirstMod_EnergyCrystalOre");

            if (Input.GetKeyDown(KeyCode.F9))
            {
                int maxY = Mathf.Min(800, SWorld.Gs.y - 10);
                ForceGenerateOre("MyFirstMod_EnergyCrystalOre", "rock", 50, maxY, 0.05f);
            }

            if (Input.GetKeyDown(KeyCode.F10)) DumpAllMonsterIds();

            if (Input.GetKeyDown(KeyCode.F11)) SpawnCustomMonster(ModManager.MONSTER_THANOS_MINION);

            if (Input.GetKeyDown(KeyCode.F12)) SpawnCustomMonster(ModManager.MONSTER_FLAME_LIZARD);

            if (Input.GetKeyDown(KeyCode.F3)) SpawnCustomMonster(ModManager.MONSTER_ENERGY_CANNON);

            if (Input.GetKeyDown(KeyCode.F4)) SpawnCustomMonster(ModManager.MONSTER_GHOST_WATCHER);
        }

        private static void DumpAllMonsterIds()
        {
            Debug.Log(">>> [Mod] 开始扫描全部怪物 ID");
            var fields = typeof(GUnits).GetFields(BindingFlags.Public | BindingFlags.Static);
            foreach (var field in fields)
            {
                if (typeof(CUnitMonster.CDesc).IsAssignableFrom(field.FieldType))
                {
                    try
                    {
                        var desc = field.GetValue(null) as CUnitMonster.CDesc;
                        if (desc != null) Debug.Log(string.Format("发现怪物: ID='{0}'", desc.m_codeName));
                    }
                    catch { }
                }
            }
            SSingletonScreen<SScreenHud>.Inst.AddPopText("Monster IDs Logged!", G.m_player.Pos, SScreenHud.PopTextType.Bonus);
        }

        private static void SpawnCustomMonster(string codeName)
        {
            if (!MonsterFactory.CustomMonsters.ContainsKey(codeName))
            {
                Debug.LogError("[Mod] 找不到自定义怪物定义: " + codeName);
                return;
            }
            SpawnMonsterInternal(MonsterFactory.CustomMonsters[codeName].Desc);
        }

        private static void SpawnMonsterInternal(CUnitMonster.CDesc desc)
        {
            if (G.m_player == null) return;

            Vector2 spawnPos = G.m_player.Pos + Vector2.right * 5f;

            try
            {
                SUnits.SpawnUnit(desc, spawnPos, 65535);
                Debug.Log(string.Format("已在玩家右侧生成怪物 '{0}'。", desc.m_codeName));
            }
            catch (Exception ex)
            {
                Debug.LogError("生成怪物失败: " + ex);
            }
        }

        private static void ForceGenerateOre(string oreCodeName, string replaceCodeName, int minY, int maxY, float chance)
        {
            Debug.Log("[Mod] 开始强制生成矿脉...");
            CItem oreItem = GItems.Items.Find(i => i != null && i.m_codeName == oreCodeName);
            CItem replaceItem = GItems.Items.Find(i => i != null && i.m_codeName == replaceCodeName);

            if (oreItem == null || replaceItem == null || !(oreItem is CItemCell)) return;

            var worldInst = Traverse.Create(typeof(SSingleton<SWorld>)).Property("Inst").GetValue<SWorld>();
            int count = 0;
            int width = SWorld.Gs.x;
            int height = SWorld.Gs.y;

            for (int x = 5; x < width - 5; x++)
            {
                for (int y = minY; y < maxY; y++)
                {
                    if (y >= height - 5) continue;
                    if (SWorld.Grid[x, y].m_contentId == replaceItem.m_id)
                    {
                        if (UnityEngine.Random.value < chance / 100f)
                        {
                            GenerateVeinSimple(worldInst, x, y, (CItemCell)oreItem, replaceItem.m_id, width, height);
                            count++;
                        }
                    }
                }
            }
            SSingletonScreen<SScreenHud>.Inst.AddPopText("Gen: " + count + " veins", G.m_player.Pos, SScreenHud.PopTextType.Bonus);
        }

        private static void GenerateVeinSimple(SWorld world, int startX, int startY, CItemCell ore, ushort replaceId, int mapW, int mapH)
        {
            int cx = startX;
            int cy = startY;
            for (int i = 0; i < 6; i++)
            {
                if (cx < 2 || cx >= mapW - 2 || cy < 2 || cy >= mapH - 2) break;
                if (SWorld.Grid[cx, cy].m_contentId == replaceId) world.SetContent(cx, cy, ore);
                cx += UnityEngine.Random.Range(-1, 2);
                cy += UnityEngine.Random.Range(-1, 2);
            }
        }

        private static void ShowPlayerCoordinates()
        {
            if (G.m_player == null) return;
            Vector2 pos = G.m_player.Pos;
            SSingletonScreen<SScreenHud>.Inst.AddPopText(
                string.Format("POS: {0:F0}, {1:F0}", pos.x, pos.y),
                pos + Vector2.up * 2,
                SScreenHud.PopTextType.Bonus);
        }

        private static void GiveItemAsPickup(string codeName, float amount = 1f)
        {
            CItem itemToGive = GItems.Items.Find(item => item != null && item.m_codeName == codeName);
            if (itemToGive != null && G.m_player != null)
            {
                SPickups.CreatePickup(itemToGive, amount, G.m_player.Pos, true, G.m_player, null, true);
            }
        }

        private static void PlaceBlockAtCursor(string codeName)
        {
            if (G.m_player == null) return;
            CItem itemToPlace = GItems.Items.Find(item => item != null && item.m_codeName == codeName);
            if (itemToPlace == null || !(itemToPlace is CItemCell)) return;
            try
            {
                Vector2 mouseWorldPos = SGame.MouseWorldPos;
                int2 aimedPos = new int2(mouseWorldPos);
                var sWorldInstance = Traverse.Create(typeof(SSingleton<SWorld>)).Property("Inst").GetValue<SWorld>();
                if (sWorldInstance != null) sWorldInstance.SetContent(aimedPos.x, aimedPos.y, (CItemCell)itemToPlace, false);
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.ToString());
            }
        }
    }
}
