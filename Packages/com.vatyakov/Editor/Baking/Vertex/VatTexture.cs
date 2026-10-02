using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace VATyakov.Editor
{
    internal static class VatTexture
    {
        public static Texture2D Create<T>(VatLayoutInfo info, GraphicsFormat format, string name, T[] data) where T : struct
        {
            return Create(info.Width, info.Height, format, name, data);
        }

        public static Texture2D Create<T>(int width, int height, GraphicsFormat format, string name, T[] data) where T : struct
        {
            var texture = new Texture2D(width, height, format, TextureCreationFlags.None)
            {
                name = name,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                anisoLevel = 0,
            };
            texture.SetPixelData(data, 0);
            texture.Apply(false, false);
            return texture;
        }
    }
}
