using UnityEditor;
using UnityEngine;

namespace VATyakov.Editor
{
    enum VatMaterialStatus
    {
        NotAssigned,
        Foreign,
        Bound,
    }

    sealed class VatMaterialBinding
    {
        public readonly VatMaterialStatus Status;
        public readonly VatAsset Asset;
        public readonly int ClipIndex;
        public readonly bool IsStale;

        VatMaterialBinding(VatMaterialStatus status, VatAsset asset = null, int clipIndex = -1, bool isStale = false)
        {
            Status = status;
            Asset = asset;
            ClipIndex = clipIndex;
            IsStale = isStale;
        }

        public static VatMaterialBinding Read(Material material)
        {
            var texture = material.GetTexture(VatShaderIds.PosTex);
            if (texture == null)
                return new VatMaterialBinding(VatMaterialStatus.NotAssigned);

            var asset = OwnerOf(texture);
            if (asset == null || !asset.TryValidate(out _))
                return new VatMaterialBinding(VatMaterialStatus.Foreign);

            int clip = VatClipLookup.Find(asset, material.GetVector(VatShaderIds.Frame));
            return new VatMaterialBinding(VatMaterialStatus.Bound, asset, clip, IsStaleFor(material, asset, clip));
        }

        static VatAsset OwnerOf(Texture texture) =>
            AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GetAssetPath(texture)) as VatAsset;

        static bool IsStaleFor(Material material, VatAsset asset, int clip) =>
            clip < 0 ||
            material.GetTexture(VatShaderIds.PosTex) != asset.PositionTexture ||
            material.GetTexture(VatShaderIds.RotTex) != asset.RotationTexture ||
            material.GetTexture(VatShaderIds.DriftTex) != asset.DriftTexture ||
            material.GetVector(VatShaderIds.Layout) != asset.Layout.ShaderLayout;
    }
}
