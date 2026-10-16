using System;

namespace VATyakov.Editor
{
    internal static class VatAssetValidation
    {
        public static bool TryValidate(this VatAsset asset, out string error)
        {
            error = asset.FormatVersion != VatAsset.CurrentFormatVersion ? FormatError(asset) : !IsComplete(asset) ? IncompleteError(asset) : null;
            return error == null;
        }

        private static bool IsComplete(VatAsset asset)
        {
            if (asset.Mesh == null || asset.Clips.Count == 0)
            {
                return false;
            }

            return asset.HasBoneTexture ? asset.BoneTexture != null : IsVertexComplete(asset);
        }

        private static bool IsVertexComplete(VatAsset asset)
        {
            return asset.PositionTexture != null && asset.RotationTexture != null && asset.DriftRows == asset.Layout.TotalRows;
        }

        private static string FormatError(VatAsset asset)
        {
            return FormattableString.Invariant(
                $"VAT asset '{asset.name}' has format version {asset.FormatVersion}, this package plays version {VatAsset.CurrentFormatVersion}. Rebake it.");
        }

        private static string IncompleteError(VatAsset asset)
        {
            return $"VAT asset '{asset.name}' is incomplete (mesh, textures or clips missing). Rebake it.";
        }
    }
}
