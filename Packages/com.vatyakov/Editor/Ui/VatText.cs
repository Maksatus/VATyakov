using System;
using System.Globalization;

namespace VATyakov.Editor
{
    internal static class VatText
    {
        private const float MillimetersPerMeter = 1000f;
        private const double BytesPerMegabyte = 1024.0 * 1024.0;

        public static string ClipSummary(VatClip clip)
        {
            var playback = clip.IsLooping ? "loop" : "once";
            return FormattableString.Invariant($"{Count(clip.FrameCount)} · {clip.FrameRate:0.##} fps · {clip.Length:0.##} s · {playback}");
        }

        public static string Estimate(VatLayout layout)
        {
            return $"Result: {Clips(layout.Clips.Length)}, {Count(layout.Info.TotalRows)} · texture {Size(layout.Info)} · {Megabytes(layout.Info)}";
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

        public static string Megabytes(VatLayoutInfo info)
        {
            return FormattableString.Invariant($"{VatMemory.TextureBytes(info) / BytesPerMegabyte:0.##} MB");
        }
    }
}
