using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace ImprovePlayable
{
    [HarmonyPatch(typeof(CItem_Device), "Use_Local")]
    public static class Patch_Teleporter
    {
        private const string TeleportGroupId = "Teleport";

        static bool Prefix(CItem_Device __instance, CPlayer player, Vector2 mousePos, bool isShift)
        {
            if (__instance.m_groupId != TeleportGroupId) return true;
            if (player == null || player.m_inventory == null) return false;

            CStack stack = player.m_inventory.GetStack(__instance);
            if (stack == null || stack.m_nb <= 0) return false;

            CItemVars vars = __instance.GetVars(player);
            if (GVars.m_simuTimeD <= vars.TimeLastUse + __instance.m_cooldown) return false;

            if (!SWorld.GridRect.Contains(mousePos)) return false;

            int2 targetCell = new int2(mousePos);
            if (!SWorld.GridRect.Contains(targetCell.ToVector2())) return false;

            CCell cell = SWorld.Grid[targetCell.x, targetCell.y];
            if ( !cell.IsPassable()) return false;

            CUnitPlayer unitPlayer = player.m_unitPlayer;
            if (unitPlayer == null) return false;

            unitPlayer.Pos = new Vector2(targetCell.x + 0.5f, targetCell.y + 0.5f);

            MethodInfo activate = AccessTools.Method(typeof(CItem_Device), "ActivateDevice");
            if (activate != null)
            {
                activate.Invoke(__instance, new object[] { player, stack, SAudio.Get("ceilingTurret") });
            }
            else
            {
                vars.TimeLastUse = GVars.SimuTime;
            }

            return false;
        }
    }
}
