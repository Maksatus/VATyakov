using UnityEngine.Experimental.Rendering;

namespace VATyakov.Editor
{
    internal static class VatMemory
    {
        public static long TextureBytes(VatLayoutInfo info)
        {
            return Bytes(info.Width, info.Height, VatVertexFormat.Position) + Bytes(info.Width, info.Height, VatVertexFormat.Rotation) +
            Bytes(VatMath.DriftWidth, info.TotalRows, VatVertexFormat.Drift);
        }

        private static long Bytes(int width, int height, GraphicsFormat format)
        {
            return (long)width * height * GraphicsFormatUtility.GetBlockSize(format);
        }
    }
}
