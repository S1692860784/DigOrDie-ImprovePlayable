using UnityEngine;
using System.IO;
using System.Reflection;

namespace MyFirstMod
{
    public static class ResourceLoader
    {
        /// <summary>
        /// 从嵌入资源中加载原始图片数据。
        /// </summary>
        public static byte[] LoadResourceBytes(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                Debug.LogError(string.Format("[Mod] 无法找到嵌入的资源: {0}", resourceName));
                return null;
            }

            byte[] buffer = new byte[stream.Length];
            stream.Read(buffer, 0, buffer.Length);
            return buffer;
        }

        /// <summary>
        /// 从图片数据创建一个 Texture2D 对象。
        /// </summary>
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