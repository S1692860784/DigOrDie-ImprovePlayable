using HarmonyLib;
using UnityEngine;

namespace MyFirstMod 
{
    [HarmonyPatch(typeof(SWorld))]
    [HarmonyPatch("DestroyCell")]
    public class Patch_CustomDrops
    {
        static bool Prefix(int2 pos)
        {
            if (!SWorld.GridRect.Contains(pos.ToVector2())) return true;
            
            CItem destroyedItem = SWorld.Grid[pos.x, pos.y].GetContent();
            if (destroyedItem == null) return true;

            if (GameDataManager.CustomDrops.ContainsKey(destroyedItem.m_codeName))
            {
                var dropInfo = GameDataManager.CustomDrops[destroyedItem.m_codeName];
                CItem itemToDrop = GItems.Items.Find(x => x != null && x.m_codeName == dropInfo.ItemCodeName);

                if (itemToDrop != null)
                {
                    int amount = UnityEngine.Random.Range(dropInfo.MinAmount, dropInfo.MaxAmount + 1);
                    if (amount > 0)
                    {
                        SPickups.CreatePickup(itemToDrop, (float)amount, pos + Vector2.one * 0.5f, true, null, null, false);
                    }
                }

                // 【修改】使用反射获取 SWorld 单例，绕过编译器检查
                var worldInst = Traverse.Create(typeof(SSingleton<SWorld>)).Property("Inst").GetValue<SWorld>();
                if (worldInst != null)
                {
                    worldInst.SetContent(pos, null);
                }
                
                return false; 
            }
            return true;
        }
    }
}