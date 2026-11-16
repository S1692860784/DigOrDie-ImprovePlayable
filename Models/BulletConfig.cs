using System.Collections.Generic;
using UnityEngine;

namespace ImprovePlayable
{
    public static class BulletConfig
    {
        private static Dictionary<CBulletDesc, int> _critMultipliers = new Dictionary<CBulletDesc, int>();

        public static void SetCritMultiplier(CBulletDesc desc, float multiplier)
        {
            int val = Mathf.RoundToInt(multiplier);
            if (!_critMultipliers.ContainsKey(desc))
            {
                _critMultipliers.Add(desc, val);
            }
            else
            {
                _critMultipliers[desc] = val;
            }
        }

        public static int GetMultiplierFor(CBullet bullet)
        {
            if (bullet != null && bullet.Desc != null)
            {
                if (_critMultipliers.ContainsKey(bullet.Desc))
                {
                    // 找到了自定义配置，返回配置的倍率
                    return _critMultipliers[bullet.Desc];
                }
            }
            // 没找到配置，返回原版默认的 3 倍
            return 3; 
        }
    }
}