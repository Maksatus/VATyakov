using UnityEngine;

namespace VATyakov.Editor
{
    internal sealed class VatAssetMemory
    {
        public readonly long Position;
        public readonly long Rotation;
        public readonly long Bone;
        public readonly int PaddingTexels;
        public readonly long Padding;

        private readonly long _frameBytes;

        public long Total => Position + Rotation + Bone;

        public VatAssetMemory(VatAsset asset)
        {
            var info = asset.Layout;
            var texelBytes = TexelBytes(asset.PositionTexture) + TexelBytes(asset.RotationTexture) + TexelBytes(asset.BoneTexture);
            Position = Bytes(asset.PositionTexture);
            Rotation = Bytes(asset.RotationTexture);
            Bone = Bytes(asset.BoneTexture);
            PaddingTexels = info.Blocks * info.Width - info.Elements;
            Padding = PaddingTexels * info.TotalRows * texelBytes;
            _frameBytes = info.Blocks * info.Width * texelBytes;
        }

        public long Clip(VatClip clip)
        {
            return _frameBytes * clip.FrameCount;
        }

        private static long Bytes(Texture2D texture)
        {
            return texture != null ? VatMemory.Bytes(texture) : 0L;
        }

        private static long TexelBytes(Texture2D texture)
        {
            return texture != null ? VatMemory.TexelBytes(texture) : 0L;
        }
    }
}
