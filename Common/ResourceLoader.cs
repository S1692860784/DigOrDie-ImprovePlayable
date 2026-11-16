using UnityEngine;
using System.IO;
using System.Reflection;

namespace ImprovePlayable
{
    public static class ResourceLoader
    {
        // 从嵌入资源加载原始图片字节
        public static byte[] LoadResourceBytes(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                Debug.LogError(string.Format("[Mod] 无法找到嵌入资源: {0}", resourceName));
                return null;
            }

            byte[] buffer = new byte[stream.Length];
            stream.Read(buffer, 0, buffer.Length);
            return buffer;
        }

        // 从图片数据创建 Texture2D
        public static Texture2D CreateTexture(byte[] imageData)
        {
            if (imageData == null || imageData.Length == 0) return null;

            Texture2D texture = new Texture2D(2, 2);
            if (texture.LoadImage(imageData))
            {
                return texture;
            }
            Debug.LogError("[Mod] 无法从图片数据创建 Texture2D。");
            return null;
        }
    }
}
