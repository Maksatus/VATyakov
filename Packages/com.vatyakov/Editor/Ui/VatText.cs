using System.Globalization;

namespace VATyakov.Editor
{
    internal static class VatText
    {
        private static readonly CultureInfo _invariant = CultureInfo.InvariantCulture;

        public static string ClipSummary(VatClip clip)
        {
            return string.Format(_invariant, "{0} {1} · {2:0.##} fps · {3:0.##} s · {4}",
                clip.FrameCount, Frames(clip.FrameCount), clip.FrameRate, clip.Length, clip.Loop ? "loop" : "once");
        }

        public static string Estimate(VatLayout layout)
        {
            return $"Result: {Clips(layout.Clips.Length)}, {Count(layout.Info.TotalRows)} · texture {Size(layout.Info)} · {Megabytes(layout.Info)}";
        }

        public static string Clips(int count)
        {
            return Number(count) + (count == 1 ? " clip" : " clips");
        }

        public static string Blocks(VatLayoutInfo info)
        {
            return info.Blocks > 1 ? $"The mesh is split into {info.Blocks} blocks across the width." : null;
        }

        public static string Count(int frames)
        {
            return Number(frames) + " " + Frames(frames);
        }

        public static string Frames(int count)
        {
            return count == 1 ? "frame" : "frames";
        }

        public static string Number(int value)
        {
            return value.ToString(_invariant);
        }

        public static string Size(VatLayoutInfo info)
        {
            return string.Format(_invariant, "{0}×{1}", info.Width, info.Height);
        }

        public static string LoopHint(float gap, bool closed, bool loop)
        {
            var distance = string.Format(_invariant, "{0:0.##} mm", gap * 1000f);
            if (closed)
            {
                return loop
                    ? $"The end matches the start ({distance}): a loop."
                    : $"The end matches the start ({distance}): looks like a loop, Loop can be enabled.";
            }

            return loop
                ? $"The end differs from the start by {distance}: Loop will pop at the seam. Disable Loop or adjust the Time Range of the .abc."
                : $"The end differs from the start by {distance}: not a loop.";
        }

        public static string Millimeters(float meters)
        {
            return string.Format(_invariant, "{0:0.###} mm", meters * 1000f);
        }

        public static string Meters(float meters)
        {
            return string.Format(_invariant, "{0:0.##} m", meters);
        }

        public static string DriftTravel(float meters)
        {
            return string.Format(_invariant,
                "The centroid travels up to {0:0.##} m from the rest pose. Positions are stored relative to it, so far-travelling bodies stay precise.",
                meters);
        }

        public static string Megabytes(VatLayoutInfo info)
        {
            return string.Format(_invariant, "{0:0.##} MB", VatMemory.TextureBytes(info) / (1024.0 * 1024.0));
        }
    }
}
