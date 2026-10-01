using System;

namespace VATyakov
{
    // §1.3: how a clip is laid out in frames.
    public static class VatTiming
    {
        // Loop: F = max(1, round(L·fps)), the last frame does not repeat the first.
        public static int LoopFrameCount(float length, float fps)
        {
            ValidateTiming(length, fps);
            return Math.Max(1, RoundFrames(length, fps));
        }

        // One-shot: F = round(L·fps) + 1, the last frame is the clip end.
        public static int OneShotFrameCount(float length, float fps)
        {
            ValidateTiming(length, fps);
            return RoundFrames(length, fps) + 1;
        }

        public static int FrameCount(float length, float fps, bool loop) =>
            loop ? LoopFrameCount(length, fps) : OneShotFrameCount(length, fps);

        // fps_eff: F / L for a loop, (F − 1) / L for a one-shot.
        public static float FrameRate(int frameCount, float length, bool loop) =>
            (float)(Intervals(frameCount, loop) / (double)length);

        // t_k = k·L / F for a loop, k·L / (F − 1) for a one-shot.
        public static double FrameTime(int frame, int frameCount, float length, bool loop)
        {
            int intervals = Intervals(frameCount, loop);
            return intervals > 0 ? frame * (double)length / intervals : 0.0;
        }

        static int Intervals(int frameCount, bool loop) => loop ? frameCount : frameCount - 1;

        // Saturated: a huge L·fps must fail the height check (§1.1), not wrap around int.
        static int RoundFrames(float length, float fps) =>
            (int)Math.Min(Math.Round((double)length * fps, MidpointRounding.AwayFromZero), int.MaxValue - 1);

        static void ValidateTiming(float length, float fps)
        {
            if (!float.IsFinite(length) || length <= 0f)
                throw new ArgumentOutOfRangeException(nameof(length), length, "Clip length must be a positive finite number.");
            if (!float.IsFinite(fps) || fps <= 0f)
                throw new ArgumentOutOfRangeException(nameof(fps), fps, "Bake fps must be a positive finite number.");
        }
    }
}
