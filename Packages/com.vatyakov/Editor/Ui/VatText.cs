using System;
using System.Globalization;
using UnityEngine;

namespace VATyakov.Editor
{
    internal static class VatText
    {
        private const float MillimetersPerMeter = 1000f;
        private const double BytesPerKilobyte = 1024.0;
        private const double BytesPerMegabyte = 1024.0 * 1024.0;
        private const string ByteHint = "One byte per axis within the bounds of the animation, half the memory of half floats: " +
            "the error stays within the Max Position Error of the profile.";

        public static string ClipSummary(VatClip clip)
        {
            var playback = clip.IsLooping ? "loop" : "once";
            return FormattableString.Invariant($"{Count(clip.FrameCount)} · {clip.FrameRate:0.##} fps · {clip.Length:0.##} s · {playback}");
        }

        public static string ClipSummary(VatClip clip, long bytes)
        {
            return $"{ClipSummary(clip)} · {Bytes(bytes)}";
        }

        public static string Estimate(VatBakeEstimate estimate)
        {
            var layout = estimate.Layout;
            if (estimate.Mode == VatMode.Rigid)
            {
                return RigidEstimate(layout);
            }

            if (estimate.Mode == VatMode.Bone)
            {
                return BoneEstimate(estimate.Mode, layout);
            }

            return $"Result: {Clips(layout.Clips.Length)}, {Count(layout.Info.TotalRows)} · texture {Size(layout.Info)} · {MegabytesRange(layout.Info)}";
        }

        public static string Clips(int count)
        {
            return count == 1 ? $"{Number(count)} clip" : $"{Number(count)} clips";
        }

        public static string Blocks(VatLayoutInfo info)
        {
            return info.Blocks > 1 ? $"The mesh is split into {Number(info.Blocks)} blocks across the width." : null;
        }

        public static string Count(int frames)
        {
            return $"{Number(frames)} {Frames(frames)}";
        }

        public static string Frames(int count)
        {
            return count == 1 ? "frame" : "frames";
        }

        public static string Number(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        public static string Size(VatLayoutInfo info)
        {
            return FormattableString.Invariant($"{info.Width}×{info.Height}");
        }

        public static string LoopHint(float gap, bool isClosed, bool isLooping)
        {
            var distance = FormattableString.Invariant($"{gap * MillimetersPerMeter:0.##} mm");
            if (isClosed)
            {
                return isLooping
                    ? $"The end matches the start ({distance}): a loop."
                    : $"The end matches the start ({distance}): looks like a loop, Loop can be enabled.";
            }

            return isLooping
                ? $"The end differs from the start by {distance}: Loop will pop at the seam. Disable Loop or adjust the Time Range of the .abc."
                : $"The end differs from the start by {distance}: not a loop.";
        }

        public static string Millimeters(float meters)
        {
            return FormattableString.Invariant($"{meters * MillimetersPerMeter:0.###} mm");
        }

        public static string Meters(float meters)
        {
            return FormattableString.Invariant($"{meters:0.##} m");
        }

        public static string DriftTravel(float meters)
        {
            return FormattableString.Invariant(
                $"The centroid travels up to {meters:0.##} m from the rest pose. Positions are stored relative to it, so far-travelling bodies stay precise.");
        }

        public static string Megabytes(VatLayoutInfo info, VatPositionFormat format)
        {
            return FormattableString.Invariant($"{MegabytesOf(info, format):0.##} MB");
        }

        public static string Bytes(long bytes)
        {
            if (bytes >= BytesPerMegabyte)
            {
                return FormattableString.Invariant($"{bytes / BytesPerMegabyte:0.##} MB");
            }

            return bytes >= BytesPerKilobyte ? FormattableString.Invariant($"{bytes / BytesPerKilobyte:0.#} KB") : FormattableString.Invariant($"{bytes} B");
        }

        public static string TextureMemory(Texture2D texture)
        {
            return FormattableString.Invariant($"{texture.width}×{texture.height} × {VatMemory.TexelBytes(texture)} B = {Bytes(VatMemory.Bytes(texture))}");
        }

        public static string Padding(VatAssetMemory memory)
        {
            var texels = memory.PaddingTexels == 1 ? "texel" : "texels";
            return FormattableString.Invariant($"{memory.PaddingTexels} {texels} per row = {Bytes(memory.Padding)}");
        }

        public static string MegabytesRange(VatLayoutInfo info)
        {
            return FormattableString.Invariant($"{MegabytesOf(info, VatPositionFormat.Byte):0.##}–{MegabytesOf(info, VatPositionFormat.Half):0.##} MB");
        }

        public static string PositionFormat(VatPositionFormat format)
        {
            return format == VatPositionFormat.Byte ? "8-bit" : "Half";
        }

        public static string PositionFormatHint(VatPositionFormat format, VatPrecision precision)
        {
            return format == VatPositionFormat.Byte
                ? ByteHint
                : $"8-bit positions would give {Millimeters(precision.ByteError)}, over the Max Position Error of the profile: they stay in half floats.";
        }

        private static string BoneEstimate(VatMode mode, VatLayout layout)
        {
            var frames = Count(layout.Info.TotalRows - VatBoneFormat.PivotRows);
            return $"Result: {mode}, {Clips(layout.Clips.Length)}, {frames} · texture {Size(layout.Info)} · {Bytes(VatMemory.BoneTextureBytes(layout.Info))}";
        }

        private static string RigidEstimate(VatLayout layout)
        {
            var frames = Count(layout.Info.TotalRows - VatBoneFormat.PivotRows);
            var pieces = Number(layout.Info.Elements / VatMath.TexelsPerBone);
            var bytes = Bytes(VatMemory.BoneTextureBytes(layout.Info));
            return $"Result: Rigid, {Clips(layout.Clips.Length)}, {frames} · {pieces} pieces or more (deforming meshes split into islands at bake) · " +
                $"texture {Size(layout.Info)} or wider · {bytes} or more";
        }

        private static double MegabytesOf(VatLayoutInfo info, VatPositionFormat format)
        {
            return VatMemory.TextureBytes(info, format) / BytesPerMegabyte;
        }
    }
}
