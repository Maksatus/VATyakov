using System.Globalization;
using System.Linq;
using UnityEngine;

namespace VATyakov.Editor
{
    internal static class VatBakeLog
    {
        public static void Baked(VatAsset asset, VatBakeResult result)
        {
            Debug.Log(Describe(asset) + Positions(result) + Stats(result.Stats), asset);
            if (result.Chirality.Count > 0)
            {
                Debug.LogWarning(Chirality(asset, result.Chirality), asset);
            }

            foreach (var warning in result.Warnings)
            {
                Debug.LogWarning($"VAT '{asset.name}': {warning}", asset);
            }
        }

        private static string Describe(VatAsset asset)
        {
            var info = asset.Layout;
            return string.Format(CultureInfo.InvariantCulture,
                "VAT '{0}': {1} vertices, {2}; textures {3}×{4} ({5} blocks), {6}",
                asset.name, info.Elements, string.Join(", ", asset.Clips.Select(Describe)), info.Width, info.Height, info.Blocks,
                VatText.Megabytes(info));
        }

        private static string Describe(VatClip clip)
        {
            return string.Format(CultureInfo.InvariantCulture,
                "clip '{0}': rows {1}–{2}, {3:0.###} fps", clip.Name, clip.StartRow, clip.StartRow + clip.FrameCount - 1, clip.FrameRate);
        }

        private static string Positions(VatBakeResult result)
        {
            return string.Format(CultureInfo.InvariantCulture,
                "; centroid travels {0:0.###} m, max |Δ| {1:0.###} m, max half error {2:0.###} mm",
                result.Precision.MaxDrift, result.MaxOffset, result.Precision.Error * 1000f);
        }

        private static string Stats(VatQuantizationStats stats)
        {
            return string.Format(CultureInfo.InvariantCulture,
                ", max rotation error {0:0.###}°, degenerate tangents {1}", stats.MaxRotationError, stats.DegenerateTangents);
        }

        private static string Chirality(VatAsset asset, VatChirality chirality)
        {
            return $"VAT '{asset.name}': {chirality.Count} vertices flip the bitangent sign relative to rest (first: {chirality.First}). " +
            "The sign comes from rest, so the normal map is mirrored on them. Check mirrored bones and negative scale.";
        }
    }
}
