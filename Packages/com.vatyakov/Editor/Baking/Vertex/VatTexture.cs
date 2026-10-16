using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace VATyakov.Editor
{
    internal static class VatTexture
    {
        public static Texture2D Create<T>(VatLayoutInfo info, GraphicsFormat format, string name, T[] data) where T : struct
        {
            var texture = new Texture2D(info.Width, info.Height, format, TextureCreationFlags.None)
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
