using System;

namespace VATyakov
{
    public static class VatTiming
    {
        public static int LoopFrameCount(float length, float fps)
        {
            return Math.Max(1, RoundFrames(length, fps));
        }

        public static int OneShotFrameCount(float length, float fps)
        {
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
            return (int)Math.Round((double)length * fps, MidpointRounding.AwayFromZero);
        }
    }
}
