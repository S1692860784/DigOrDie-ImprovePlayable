using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace MyFirstMod
{
    public static class Patch_DeviceDetector
    {
        private static readonly string MetalDetectorGroupId = "MetalDetector";

        [HarmonyPatch(typeof(CItem_Device), "OnDisplayHud")]
        [HarmonyPostfix]
        static void Postfix_DrawWave(CItem_Device __instance, SScreen screen)
        {
            string targetOre;
            if (!ModManager.TryGetDetectorTarget(__instance.m_codeName, out targetOre)) return;
            if (__instance.m_groupId != MetalDetectorGroupId) return;

            DrawDetectorWave(__instance, screen);
        }

        [HarmonyPatch(typeof(CItem_Device), "Use_Local")]
        [HarmonyPrefix]
        static bool Prefix_UseLocal(CItem_Device __instance, CPlayer player, Vector2 mousePos, bool isShift)
        {
            string targetOre;
            if (!ModManager.TryGetDetectorTarget(__instance.m_codeName, out targetOre))
            {
                return true;
            }

            CStack stack = player.m_inventory.GetStack(__instance);
            if (stack == null || stack.m_nb <= 0) return false;

            CUnitPlayer unitPlayer = player.m_unitPlayer;
            if (unitPlayer == null) return false;

            ClearOtherDetectorWaves(player);

            List<Vector2> list = new List<Vector2> { unitPlayer.PosCenter };
            float range = __instance.m_customValue;
            float rangeSqr = range * range;
            int rangeInt = Mathf.CeilToInt(range);
            Rect gridRectCam = SWorld.GridRectCam;

            for (int i = -rangeInt; i < rangeInt; i++)
            {
                for (int j = -rangeInt; j < rangeInt; j++)
                {
                    int2 pos = unitPlayer.PosCell + new int2(i, j);
                    if (gridRectCam.Contains(pos.ToVector2()))
                    {
                        CItem_Mineral mineral = SWorld.Grid[pos.x, pos.y].GetContent() as CItem_Mineral;
                        if (mineral != null && mineral.m_codeName == targetOre && (float)(pos - unitPlayer.PosCell).sqrMagnitude < rangeSqr)
                        {
                            list.Add(new Vector2((float)pos.x + UnityEngine.Random.value, (float)pos.y + UnityEngine.Random.value));
                        }
                    }
                }
            }

            CItemVars vars = __instance.GetVars(player);
            vars.TimeLastUse = GVars.SimuTime;
            vars.ItemsDetectedPos = list;

            MethodInfo activate = AccessTools.Method(typeof(CItem_Device), "ActivateDevice");
            if (activate != null)
            {
                activate.Invoke(__instance, new object[] { player, stack, SAudio.Get("ceilingTurret") });
            }

            return false;
        }

        private static void ClearOtherDetectorWaves(CPlayer player)
        {
            if (player == null || player.m_inventory == null) return;

            for (int i = 0; i < player.m_inventory.Items.Count; i++)
            {
                CItem_Device device = player.m_inventory.Items[i].m_item as CItem_Device;
                if (device == null) continue;
                if (device.m_groupId != MetalDetectorGroupId) continue;

                CItemVars vars = device.GetVars(player);
                if (vars == null) continue;

                // 不能写 null，底层 setter 会抛异常
                vars.ItemsDetectedPos = new List<Vector2>();
                vars.TimeLastUse = float.MinValue;
            }
        }

        private static void DrawDetectorWave(CItem_Device device, SScreen screen)
        {
            CItemVars myVars = device.GetMyVars();
            float num = device.m_customValue / 80f;
            float num2 = GVars.SimuTime - myVars.TimeLastUse;

            List<Vector2> itemsDetectedPos = myVars.ItemsDetectedPos;
            if (itemsDetectedPos == null || num2 < 0f || num2 >= num * 2f) return;

            CAssetSprite sprite = SResources.GetSprite("particles/particles_big", "radar");
            for (int i = 0; i < itemsDetectedPos.Count; i++)
            {
                float num3 = (itemsDetectedPos[i] - itemsDetectedPos[0]).magnitude / 80f;
                if (num2 >= num3 && num2 < num3 + num)
                {
                    if (num2 < num3 + SMain.SimuDeltaTime)
                    {
                        SAudio.Get("ceilingTurret").Play(itemsDetectedPos[i], 1f);
                    }
                    Vector2 vector = SMisc.WorldToGuiPoint(itemsDetectedPos[i]);
                    float num4 = SMisc.WorldToGuiDist((num2 - num3) * 80f);
                    CMesh<CMeshSprite>.Get(screen, false).DrawGui(
                        new Rect(vector.x - num4, vector.y - num4, 2f * num4, 2f * num4),
                        sprite.Sprite,
                        new Color(1f, 1f, 1f, 0.5f * (1f - num3 / num) * Mathf.Clamp01((num3 + num - num2) / num)),
                        0f,
                        false);
                }
            }
        }
    }
}
