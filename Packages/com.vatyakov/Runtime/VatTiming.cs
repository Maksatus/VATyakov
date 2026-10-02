using System;

namespace VATyakov
{
    public static class VatTiming
    {
        public static int LoopFrameCount(float length, float fps)
        {
            ValidateTiming(length, fps);
            return Math.Max(1, RoundFrames(length, fps));
        }

        public static int OneShotFrameCount(float length, float fps)
        {
            ValidateTiming(length, fps);
            return RoundFrames(length, fps) + 1;
        }

        public static int FrameCount(float length, float fps, bool isLooping)
        {
            return isLooping ? LoopFrameCount(length, fps) : OneShotFrameCount(length, fps);
        }

        public static float FrameRate(int frameCount, float length, bool isLooping)
        {
            return (float)(Intervals(frameCount, isLooping) / (double)length);
        }

        public static double FrameTime(int frame, int frameCount, float length, bool isLooping)
        {
            var intervals = Intervals(frameCount, isLooping);
            return intervals > 0 ? frame * (double)length / intervals : 0.0;
        }

        private static int Intervals(int frameCount, bool isLooping)
        {
            return isLooping ? frameCount : frameCount - 1;
        }

        private static int RoundFrames(float length, float fps)
        {
            return (int)Math.Min(Math.Round((double)length * fps, MidpointRounding.AwayFromZero), int.MaxValue - 1);
        }

        private static void ValidateTiming(float length, float fps)
        {
            if (!float.IsFinite(length) || length <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(length), length, "Clip length must be a positive finite number.");
            }

            if (!float.IsFinite(fps) || fps <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(fps), fps, "Bake fps must be a positive finite number.");
            }
        }
    }
}
