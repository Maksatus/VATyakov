using UnityEngine.Experimental.Rendering;

namespace VATyakov.Editor
{
    static class VatMemory
    {
        // Actual size of all textures: padding texels of the last block included (§1.8).
        public static long TextureBytes(VatLayoutInfo info) =>
            Bytes(info.Width, info.Height, info.PositionFormat) + Bytes(info.Width, info.Height, info.RotationFormat) +
            Bytes(VatMath.DriftWidth, info.TotalRows, info.DriftFormat);

        static long Bytes(int width, int height, GraphicsFormat format) =>
            format == GraphicsFormat.None ? 0 : (long)width * height * GraphicsFormatUtility.GetBlockSize(format);
    }
}
