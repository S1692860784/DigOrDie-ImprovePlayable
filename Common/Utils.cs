using HarmonyLib;
using System.Reflection;

namespace ImprovePlayable
{
    public static class GameUtils
    {
        private static SBullets _sBulletsInstance;

        public static SBullets GetSBullets()
        {
            if (_sBulletsInstance == null)
            {
                // 使用 AccessTools 安全地获取 protected static 属性 'Inst'
                PropertyInfo instProperty = AccessTools.Property(typeof(SSingleton<SBullets>), "Inst");
                if (instProperty != null)
                {
                    _sBulletsInstance = (SBullets)instProperty.GetValue(null, null);
                }
            }
            return _sBulletsInstance;
        }
    }
}