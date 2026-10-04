namespace VATyakov.Dev
{
    internal sealed class VatStressMetric
    {
        public readonly string Column;
        public readonly string Marker;
        public readonly bool IsCounter;
        public readonly double Scale;

        private VatStressMetric(string column, string marker, bool isCounter, double scale)
        {
            Column = column;
            Marker = marker;
            IsCounter = isCounter;
            Scale = scale;
        }

        public static VatStressMetric Counter(string column, string counter, double scale)
        {
            return new VatStressMetric(column, counter, true, scale);
        }

        public static VatStressMetric Sample(string column, string marker)
        {
            return new VatStressMetric(column, marker, false, 1d);
        }
    }
}
