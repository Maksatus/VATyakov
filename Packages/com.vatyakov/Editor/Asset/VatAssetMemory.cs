namespace VATyakov.Editor
{
    internal sealed class VatAssetMemory
    {
        public readonly long Position;
        public readonly long Rotation;
        public readonly int PaddingTexels;
        public readonly long Padding;

        private readonly long _frameBytes;

        public long Total => Position + Rotation;

        public VatAssetMemory(VatAsset asset)
        {
            var info = asset.Layout;
            var texelBytes = VatMemory.TexelBytes(asset.PositionTexture) + VatMemory.TexelBytes(asset.RotationTexture);
            Position = VatMemory.Bytes(asset.PositionTexture);
            Rotation = VatMemory.Bytes(asset.RotationTexture);
            PaddingTexels = info.Blocks * info.Width - info.Elements;
            Padding = PaddingTexels * info.TotalRows * texelBytes;
            _frameBytes = info.Blocks * info.Width * texelBytes;
        }

        public long Clip(VatClip clip)
        {
            return _frameBytes * clip.FrameCount;
        }
    }
}
