using UnityEngine.Experimental.Rendering;

namespace VATyakov.Editor
{
    static class VatMemory
    {
        // Actual size of all textures: padding texels of the last block included (§1.8).
        public static long TextureBytes(VatLayoutInfo info) => Bytes(info, info.PositionFormat) + Bytes(info, info.RotationFormat);

        static long Bytes(VatLayoutInfo info, GraphicsFormat format) =>
            format == GraphicsFormat.None ? 0 : (long)info.Width * info.Height * GraphicsFormatUtility.GetBlockSize(format);
    }
}
