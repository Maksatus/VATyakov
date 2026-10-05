using System;
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
            var texture = AnimationTexture(material);
            if (texture == null)
            {
                return new VatMaterialBinding(VatMaterialStatus.NotAssigned);
            }

            var asset = OwnerOf(texture);
            if (asset == null || !asset.TryValidate(out _) || !VatTemplateShader.Fits(material, asset.Mode))
            {
                return new VatMaterialBinding(VatMaterialStatus.Foreign);
            }

            var hasClip = VatClipLookup.TryFind(asset, material.GetVector(VatShaderIds.Frame), out var clipIndex);
            return new VatMaterialBinding(VatMaterialStatus.Bound, asset, hasClip, clipIndex, IsStaleFor(material, asset, hasClip, clipIndex));
        }

        private static Texture AnimationTexture(Material material)
        {
            foreach (VatMode mode in Enum.GetValues(typeof(VatMode)))
            {
                if (material.HasTexture(VatShaderIds.DataTexture(mode)))
                {
                    return material.GetTexture(VatShaderIds.DataTexture(mode));
                }
            }

            return null;
        }

        private static VatAsset OwnerOf(Texture texture)
        {
            return AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GetAssetPath(texture)) as VatAsset;
        }

        private static bool IsStaleFor(Material material, VatAsset asset, bool hasClip, int clipIndex)
        {
            if (!hasClip)
            {
                return true;
            }

            if (material.GetVector(VatShaderIds.Layout) != asset.Layout.ShaderLayout)
            {
                return true;
            }

            return asset.HasBoneTexture ? material.GetTexture(VatShaderIds.DataTexture(asset.Mode)) != asset.BoneTexture : IsVertexStale(material, asset, clipIndex);
        }

        private static bool IsVertexStale(Material material, VatAsset asset, int clipIndex)
        {
            return material.GetTexture(VatShaderIds.PositionTexture) != asset.PositionTexture ||
                material.GetTexture(VatShaderIds.RotationTexture) != asset.RotationTexture ||
                material.GetVector(VatShaderIds.PositionScale) != asset.PositionRange.ShaderScale ||
                material.GetVector(VatShaderIds.Drift) != ShownDrift(material, asset, asset.Clips[clipIndex]);
        }

        private static Vector4 ShownDrift(Material material, VatAsset asset, VatClip clip)
        {
            var frame = material.GetVector(VatShaderIds.Frame);
            var rows = clip.Frame(frame.x - clip.StartRow);
            rows.z = frame.z;
            return asset.Drift(rows);
        }
    }
}
