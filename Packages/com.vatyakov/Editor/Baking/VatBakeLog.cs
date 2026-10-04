using System;
using System.Linq;
using UnityEngine;

namespace VATyakov.Editor
{
    internal static class VatBakeLog
    {
        private const float MillimetersPerMeter = 1000f;
        private const float BoneErrorBudget = 1e-3f;
        private const string ChiralityHint =
            "The sign comes from rest, so the normal map is mirrored on them. Check mirrored bones and negative scale.";
        private const string SeamHint =
            "Their frame makes an odd number of turns over the loop: normal and tangent pass through zero between the last and the first frame.";
        private const string BoneSeamHint =
            "They make an odd number of turns over the loop: between the last and the first frame they spin the long way round.";
        private const string BoneErrorHint =
            "The joints are far from the root or the bones scale a lot: half precision of the bone texture does not hold 1 mm.";

        public static void Baked(VatAsset asset, VatBakeResult result)
        {
            Debug.Log(result.Mode == VatMode.Bone ? DescribeBones(asset) : $"{Describe(asset)}{Positions(result)}{Stats(result.Stats)}", asset);
            if (!string.IsNullOrEmpty(result.Fallback))
            {
                Debug.LogWarning($"VAT '{asset.name}': {result.Fallback}", asset);
            }

            if (result.Chirality != null && result.Chirality.Count > 0)
            {
                Debug.LogWarning(Chirality(asset, result.Chirality), asset);
            }

            if (result.Signs.SeamCount > 0)
            {
                Debug.LogWarning(Seams(asset, result), asset);
            }

            if (result.Mode == VatMode.Bone && result.Precision.Error > BoneErrorBudget)
            {
                Debug.LogWarning($"VAT '{asset.name}': max error {VatText.Millimeters(result.Precision.Error)} is over 1 mm. {BoneErrorHint}", asset);
            }

            foreach (var warning in result.Warnings)
            {
                Debug.LogWarning($"VAT '{asset.name}': {warning}", asset);
            }
        }

        private static string Describe(VatAsset asset)
        {
            var info = asset.Layout;
            var memory = VatText.Megabytes(info, asset.PositionFormat);
            var textures = FormattableString.Invariant($"textures {info.Width}×{info.Height} ({info.Blocks} blocks), {memory}");
            return FormattableString.Invariant($"VAT '{asset.name}': {info.Elements} vertices, {Clips(asset)}; {textures}");
        }

        private static string DescribeBones(VatAsset asset)
        {
            var info = asset.Layout;
            var texture = $"texture {VatText.Size(info)}, {VatText.Bytes(VatMemory.Bytes(asset.BoneTexture))}";
            var mesh = FormattableString.Invariant($"{asset.BoneCount} bones, {asset.Mesh.vertexCount} vertices{ExtraMeshes(asset)}");
            return $"VAT '{asset.name}': Bone, {mesh}, {Clips(asset)}; {texture}; max error {VatText.Millimeters(asset.Precision.Error)}";
        }

        private static string ExtraMeshes(VatAsset asset)
        {
            var count = asset.ExtraMeshes.Count;
            var vertices = asset.ExtraMeshes.Sum(mesh => mesh.vertexCount);
            return count > 0 ? FormattableString.Invariant($", extra meshes: {count} ({vertices} vertices)") : string.Empty;
        }

        private static string Clips(VatAsset asset)
        {
            return string.Join(", ", asset.Clips.Select(Describe));
        }

        private static string Describe(VatClip clip)
        {
            var lastRow = clip.StartRow + clip.FrameCount - 1;
            return FormattableString.Invariant($"clip '{clip.Name}': rows {clip.StartRow}–{lastRow}, {clip.FrameRate:0.###} fps");
        }

        private static string Positions(VatBakeResult result)
        {
            var precision = result.Precision;
            var format = VatText.PositionFormat(result.PositionFormat).ToLowerInvariant();
            var travel = FormattableString.Invariant($"; centroid travels {precision.MaxDrift:0.###} m, max |Δ| {result.MaxOffset:0.###} m");
            var error = precision.Error * MillimetersPerMeter;
            var byteError = precision.ByteError * MillimetersPerMeter;
            return FormattableString.Invariant($"{travel}, {format} positions, max error {error:0.###} mm, 8-bit error {byteError:0.###} mm");
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

        private static string Seams(VatAsset asset, VatBakeResult result)
        {
            var signs = result.Signs;
            var elements = result.Mode == VatMode.Bone ? "bones" : "vertices";
            var hint = result.Mode == VatMode.Bone ? BoneSeamHint : SeamHint;
            var seams = FormattableString.Invariant($"{signs.SeamCount} {elements} flip the rotation sign at the loop seam (first: {signs.FirstSeam})");
            return $"VAT '{asset.name}': {seams}. {hint}";
        }
    }
}
