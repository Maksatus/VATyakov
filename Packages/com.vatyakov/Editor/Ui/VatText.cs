using System.Globalization;

namespace VATyakov.Editor
{
    static class VatText
    {
        static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

        public static string ClipSummary(VatClip clip) => string.Format(Invariant, "{0} {1} · {2:0.##} fps · {3:0.##} s · {4}",
            clip.FrameCount, Frames(clip.FrameCount), clip.FrameRate, clip.Length, clip.Loop ? "loop" : "once");

        public static string Estimate(VatLayout layout) =>
            $"Result: {Count(layout.Clips[0].FrameCount)} · texture {Size(layout.Info)} · {Megabytes(layout.Info)}";

        public static string Blocks(VatLayoutInfo info) => info.Blocks > 1 ? $"The mesh is split into {info.Blocks} blocks across the width." : null;

        public static string Count(int frames) => Number(frames) + " " + Frames(frames);

        public static string Frames(int count) => count == 1 ? "frame" : "frames";

        public static string Number(int value) => value.ToString(Invariant);

        public static string Size(VatLayoutInfo info) => string.Format(Invariant, "{0}×{1}", info.Width, info.Height);

        public static string LoopHint(float gap, bool closed, bool loop)
        {
            string distance = string.Format(Invariant, "{0:0.##} mm", gap * 1000f);
            if (closed)
                return loop
                    ? $"The end matches the start ({distance}): a loop."
                    : $"The end matches the start ({distance}): looks like a loop, Loop can be enabled.";
            return loop
                ? $"The end differs from the start by {distance}: Loop will pop at the seam. Disable Loop or adjust the Time Range of the .abc."
                : $"The end differs from the start by {distance}: not a loop.";
        }

        public static string Millimeters(float meters) => string.Format(Invariant, "{0:0.###} mm", meters * 1000f);

        public static string DriftTravel(float meters) =>
            string.Format(Invariant, "The centroid travels up to {0:0.##} m from the rest pose.", meters);

        public static string DriftRule() => string.Format(Invariant,
            "Drift is turned on by the baker when the centroid travels farther than {0:0.#} m or the error without it exceeds {1}.",
            VatDriftPolicy.Distance, Millimeters(VatDriftPolicy.Error));

        public static string Megabytes(VatLayoutInfo info) =>
            string.Format(Invariant, "{0:0.##} MB", VatMemory.TextureBytes(info) / (1024.0 * 1024.0));
    }
}
