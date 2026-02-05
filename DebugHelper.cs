using UnityEngine;

namespace MyFirstMod
{
    public class DebugHelper : MonoBehaviour
    {
        private bool _isFastForward = false;
        private float _lastToggleTime = 0f;

        void Start()
        {
            Debug.Log("[Mod Debug] 调试助手启动。按 F6 加速。");
        }

        void Update()
        {
            // 防抖动，避免一帧触发多次
            if (Time.unscaledTime - _lastToggleTime < 0.2f) return;

            if (Input.GetKeyDown(KeyCode.F6))
            {
                _lastToggleTime = Time.unscaledTime;
                _isFastForward = !_isFastForward;
                
                // 屏幕提示
                string status = _isFastForward ? "加速开启 (10x)" : "加速关闭 (1x)";
                if (G.m_player != null)
                {
                    SSingletonScreen<SScreenHud>.Inst.AddPopText(status, G.m_player.Pos, SScreenHud.PopTextType.Bonus);
                }
                Debug.Log("[Mod Debug] " + status);
            }

            // 强制锁定逻辑
            // 只有当游戏没有暂停（Time.timeScale != 0）时才应用加速
            // 这样不会影响 F2 打开背包时的暂停功能
            if (_isFastForward)
            {
                if (Time.timeScale > 0.1f) 
                {
                    Time.timeScale = 10.0f;
                }
            }
            else
            {
                // 如果关闭加速，且游戏未暂停，恢复 1.0
                if (Time.timeScale > 1.1f)
                {
                    Time.timeScale = 1.0f;
                }
            }
        }
    }
}