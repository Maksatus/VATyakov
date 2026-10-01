namespace VATyakov.Editor
{
    static class VatMemory
    {
        // Actual size: padding texels of the last block included (§1.8).
        public static long PositionTextureBytes(VatLayoutInfo info) => (long)info.Width * info.Height * 8;
    }
}
