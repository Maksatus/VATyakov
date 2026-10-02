using UnityEditor;
using UnityEngine;

namespace VATyakov.Editor
{
    internal sealed class VatMaterialBinding
    {
        public readonly VatMaterialStatus Status;
        public readonly VatAsset Asset;
        public readonly bool HasClip;
        public readonly int ClipIndex;
        public readonly bool IsStale;

        private VatMaterialBinding(VatMaterialStatus status, VatAsset asset = null, bool hasClip = false, int clipIndex = 0, bool isStale = false)
        {
            Status = status;
            Asset = asset;
            HasClip = hasClip;
            ClipIndex = clipIndex;
            IsStale = isStale;
        }

        public static VatMaterialBinding Read(Material material)
        {
            var texture = material.GetTexture(VatShaderIds.PositionTexture);
            if (texture == null)
            {
                return new VatMaterialBinding(VatMaterialStatus.NotAssigned);
            }

            var asset = OwnerOf(texture);
            if (asset == null || !asset.TryValidate(out _))
            {
                return new VatMaterialBinding(VatMaterialStatus.Foreign);
            }

            var hasClip = VatClipLookup.TryFind(asset, material.GetVector(VatShaderIds.Frame), out var clipIndex);
            return new VatMaterialBinding(VatMaterialStatus.Bound, asset, hasClip, clipIndex, IsStaleFor(material, asset, hasClip));
        }

        private static VatAsset OwnerOf(Texture texture)
        {
            return AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GetAssetPath(texture)) as VatAsset;
        }

        private static bool IsStaleFor(Material material, VatAsset asset, bool hasClip)
        {
            return !hasClip ||
                material.GetTexture(VatShaderIds.PositionTexture) != asset.PositionTexture ||
                material.GetTexture(VatShaderIds.RotationTexture) != asset.RotationTexture ||
                material.GetTexture(VatShaderIds.DriftTexture) != asset.DriftTexture ||
                material.GetVector(VatShaderIds.Layout) != asset.Layout.ShaderLayout;
        }
    }
}
