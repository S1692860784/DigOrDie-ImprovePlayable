using System;
using System.IO;
using UnityEngine;

namespace ImprovePlayable
{
    public static class JsonConfigHelper
    {
        private static string GetConfigDirectory()
        {
            try
            {
                string dir = Path.Combine(Application.dataPath, "../BepInEx/config/ImprovePlayable");
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                return dir;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Mod] 获取配置目录失败: " + ex.Message);
                return null;
            }
        }

        public static T LoadOrCreateConfig<T>(string fileName, string defaultJson) where T : class
        {
            string dir = GetConfigDirectory();
            if (!string.IsNullOrEmpty(dir))
            {
                string filePath = Path.Combine(dir, fileName);
                try
                {
                    if (File.Exists(filePath))
                    {
                        string json = File.ReadAllText(filePath);
                        T loaded = JsonUtility.FromJson<T>(json);
                        if (loaded != null)
                        {
                            Debug.Log("[Mod] 已加载外部 JSON 配置: " + filePath);
                            return loaded;
                        }
                    }

                    // 首次运行或未找到文件，保存默认 JSON 方便玩家和开发者直接查阅与调整
                    if (!string.IsNullOrEmpty(defaultJson))
                    {
                        File.WriteAllText(filePath, defaultJson);
                        Debug.Log("[Mod] 已生成默认配置文件: " + filePath);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[Mod] 加载/创建外部配置文件异常，将降级使用内置数据: " + ex.Message);
                }
            }

            return JsonUtility.FromJson<T>(defaultJson);
        }
    }
}
