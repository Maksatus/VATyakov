using System.Collections.Generic;

namespace VATyakov.Editor
{
    internal sealed class VatRigidInnerTimes
    {
        private const int FallbackPoints = 3;
        private const double Epsilon = 1e-5;

        public readonly int[] Intervals;
        public readonly float[] Fractions;
        public readonly double[] Times;

        public int Count => Times.Length;

        private VatRigidInnerTimes(List<int> intervals, List<float> fractions, List<double> times)
        {
            Intervals = intervals.ToArray();
            Fractions = fractions.ToArray();
            Times = times.ToArray();
        }

        public static VatRigidInnerTimes Between(VatClip clip, IReadOnlyList<double> sourceTimes)
        {
            var intervals = new List<int>();
            var fractions = new List<float>();
            var times = new List<double>();
            var intervalCount = clip.IsLooping ? clip.FrameCount : clip.FrameCount - 1;
            for (var frame = 0; frame < intervalCount; frame++)
            {
                var start = clip.FrameTime(frame);
                var end = frame + 1 < clip.FrameCount ? clip.FrameTime(frame + 1) : clip.Length;
                foreach (var time in Inside(start, end, sourceTimes))
                {
                    intervals.Add(frame);
                    fractions.Add((float)((time - start) / (end - start)));
                    times.Add(time);
                }
            }

            return new VatRigidInnerTimes(intervals, fractions, times);
        }

        private static IEnumerable<double> Inside(double start, double end, IReadOnlyList<double> sourceTimes)
        {
            if (sourceTimes == null)
            {
                for (var point = 1; point <= FallbackPoints; point++)
                {
                    yield return start + (end - start) * point / (FallbackPoints + 1);
                }

                yield break;
            }

            foreach (var time in sourceTimes)
            {
                if (time > start + Epsilon && time < end - Epsilon)
                {
                    yield return time;
                }
            }
        }
    }
}
