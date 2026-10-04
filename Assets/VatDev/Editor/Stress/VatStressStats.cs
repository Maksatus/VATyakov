namespace VATyakov.Dev
{
    internal sealed class VatStressStats
    {
        public readonly int Frames;
        public readonly int DroppedFrames;
        public readonly double FrameMedian;
        public readonly double FrameP95;
        public readonly double[] Values;

        public VatStressStats(int frames, int droppedFrames, double frameMedian, double frameP95, double[] values)
        {
            Frames = frames;
            DroppedFrames = droppedFrames;
            FrameMedian = frameMedian;
            FrameP95 = frameP95;
            Values = values;
        }

        public double Value(string column)
        {
            return Values[VatStressMetrics.IndexOf(column)];
        }

        public double AnimationCpu()
        {
            var sum = 0d;
            foreach (var column in VatStressMetrics.AnimationColumns)
            {
                sum += Value(column);
            }

            return sum;
        }
    }
}
