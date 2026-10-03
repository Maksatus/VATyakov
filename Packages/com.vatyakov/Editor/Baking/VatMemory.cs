using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace VATyakov.Editor
{
    internal static class VatMemory
    {
        public static long TextureBytes(VatLayoutInfo info, VatPositionFormat format)
        {
            return Bytes(info.Width, info.Height, VatVertexFormat.Position(format)) + Bytes(info.Width, info.Height, VatVertexFormat.Rotation);
        }

        public static long Bytes(Texture2D texture)
        {
            return Bytes(texture.width, texture.height, texture.graphicsFormat);
        }

        public static long TexelBytes(Texture2D texture)
        {
            return GraphicsFormatUtility.GetBlockSize(texture.graphicsFormat);
        }

        private static long Bytes(int width, int height, GraphicsFormat format)
        {
            return (long)width * height * GraphicsFormatUtility.GetBlockSize(format);
        }
    }
}
