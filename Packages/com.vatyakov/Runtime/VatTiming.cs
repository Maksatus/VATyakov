using System;

namespace VATyakov
{
    public static class VatTiming
    {
        private const int MinFrameCount = 2;

        public static int FrameCount(float length, float fps)
        {
            return Math.Max(MinFrameCount, (int)Math.Round((double)length * fps, MidpointRounding.AwayFromZero) + 1);
        }

        public static float FrameRate(int frameCount, float length)
        {
            return (float)((frameCount - 1) / (double)length);
        }

        public static double FrameTime(int frame, int frameCount, float length)
        {
            return frame * (double)length / (frameCount - 1);
        }
    }
}
