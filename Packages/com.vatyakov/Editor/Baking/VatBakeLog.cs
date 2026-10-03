using System;
using System.Linq;
using UnityEngine;

namespace VATyakov.Editor
{
    internal static class VatBakeLog
    {
        private const float MillimetersPerMeter = 1000f;
        private const string ChiralityHint =
            "The sign comes from rest, so the normal map is mirrored on them. Check mirrored bones and negative scale.";
        private const string SeamHint =
            "Their frame makes an odd number of turns over the loop: normal and tangent pass through zero between the last and the first frame.";

        public static void Baked(VatAsset asset, VatBakeResult result)
        {
            Debug.Log($"{Describe(asset)}{Positions(result)}{Stats(result.Stats)}", asset);
            if (result.Chirality.Count > 0)
            {
                Debug.LogWarning(Chirality(asset, result.Chirality), asset);
            }

            if (result.Signs.SeamCount > 0)
            {
                Debug.LogWarning(Seams(asset, result.Signs), asset);
            }

            foreach (var warning in result.Warnings)
            {
                Debug.LogWarning($"VAT '{asset.name}': {warning}", asset);
            }
        }

        private static string Describe(VatAsset asset)
        {
            var info = asset.Layout;
            var clips = string.Join(", ", asset.Clips.Select(Describe));
            var textures = FormattableString.Invariant($"textures {info.Width}×{info.Height} ({info.Blocks} blocks), {VatText.Megabytes(info)}");
            return FormattableString.Invariant($"VAT '{asset.name}': {info.Elements} vertices, {clips}; {textures}");
        }

        private static string Describe(VatClip clip)
        {
            var lastRow = clip.StartRow + clip.FrameCount - 1;
            return FormattableString.Invariant($"clip '{clip.Name}': rows {clip.StartRow}–{lastRow}, {clip.FrameRate:0.###} fps");
        }

        private static string Positions(VatBakeResult result)
        {
            var error = result.Precision.Error * MillimetersPerMeter;
            return FormattableString.Invariant(
                $"; centroid travels {result.Precision.MaxDrift:0.###} m, max |Δ| {result.MaxOffset:0.###} m, max half error {error:0.###} mm");
        }

        private static string Stats(VatQuantizationStats stats)
        {
            return FormattableString.Invariant($", max rotation error {stats.MaxRotationError:0.###}°, degenerate tangents {stats.DegenerateTangents}");
        }

        private static string Chirality(VatAsset asset, VatChirality chirality)
        {
            var flipped = FormattableString.Invariant($"{chirality.Count} vertices flip the bitangent sign relative to rest (first: {chirality.First})");
            return $"VAT '{asset.name}': {flipped}. {ChiralityHint}";
        }

        private static string Seams(VatAsset asset, VatRotationSigns signs)
        {
            var seams = FormattableString.Invariant($"{signs.SeamCount} vertices flip the rotation sign at the loop seam (first: {signs.FirstSeam})");
            return $"VAT '{asset.name}': {seams}. {SeamHint}";
        }
    }
}
