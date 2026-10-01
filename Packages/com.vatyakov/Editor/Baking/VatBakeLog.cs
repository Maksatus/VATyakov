using System.Globalization;
using UnityEngine;

namespace VATyakov.Editor
{
    static class VatBakeLog
    {
        public static void Baked(VatAsset asset, VatBakeResult result)
        {
            Debug.Log(Describe(asset) + Positions(asset, result) + Stats(result.Stats), asset);
            if (result.Chirality.Count > 0)
                Debug.LogWarning(Chirality(asset, result.Chirality), asset);
            foreach (string warning in result.Warnings)
                Debug.LogWarning($"VAT '{asset.name}': {warning}", asset);
        }

        static string Describe(VatAsset asset)
        {
            var info = asset.Layout;
            var clip = asset.Clips[0];
            return string.Format(CultureInfo.InvariantCulture,
                "VAT '{0}': {1} vertices, clip '{2}': {3} frames, {4:0.###} fps; textures {5}×{6} ({7} blocks), {8}",
                asset.name, info.Elements, clip.Name, clip.FrameCount, clip.FrameRate, info.Width, info.Height, info.Blocks,
                VatText.Megabytes(info));
        }

        static string Positions(VatAsset asset, VatBakeResult result)
        {
            var precision = result.Precision;
            return string.Format(CultureInfo.InvariantCulture,
                "; drift {0} (centroid travels {1:0.###} m), max |Δ| {2:0.###} m, max half error {3:0.###} mm with drift, {4:0.###} mm without",
                asset.Layout.Drift ? "on" : "off", precision.MaxDrift, result.MaxOffset, precision.ErrorWithDrift * 1000f,
                precision.ErrorWithoutDrift * 1000f);
        }

        static string Stats(VatQuantizationStats stats) => string.Format(CultureInfo.InvariantCulture,
            ", max rotation error {0:0.###}°, degenerate tangents {1}", stats.MaxRotationError, stats.DegenerateTangents);

        static string Chirality(VatAsset asset, VatChirality chirality) =>
            $"VAT '{asset.name}': {chirality.Count} vertices flip the bitangent sign relative to rest (first: {chirality.First}). " +
            "The sign comes from rest, so the normal map is mirrored on them. Check mirrored bones and negative scale.";
    }
}
