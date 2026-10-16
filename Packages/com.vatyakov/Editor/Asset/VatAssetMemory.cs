using UnityEngine;

namespace VATyakov.Editor
{
    internal sealed class VatAssetMemory
    {
        public readonly long Position;
        public readonly long Rotation;
        public readonly long Bone;
        public readonly long Mesh;
        public readonly int PaddingTexels;
        public readonly long Padding;

        private readonly long _frameBytes;

        public long Textures => Position + Rotation + Bone;
        public long Total => Textures + Mesh;

        public VatAssetMemory(VatAsset asset)
        {
            var info = asset.Layout;
            var texelBytes = TexelBytes(asset.PositionTexture) + TexelBytes(asset.RotationTexture) + TexelBytes(asset.BoneTexture);
            Position = Bytes(asset.PositionTexture);
            Rotation = Bytes(asset.RotationTexture);
            Bone = Bytes(asset.BoneTexture);
            Mesh = asset.Mesh != null ? VatMemory.Bytes(asset.Mesh) : 0L;
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
