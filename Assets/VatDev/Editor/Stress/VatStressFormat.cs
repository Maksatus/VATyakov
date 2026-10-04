using System.Collections.Generic;
using System.Globalization;

namespace VATyakov.Dev
{
    internal static class VatStressFormat
    {
        private const string Missing = "—";

        public static string Number(double value, string format = "0.00")
        {
            return double.IsNaN(value) ? Missing : value.ToString(format, CultureInfo.InvariantCulture);
        }

        public static string CsvNumber(double value, string format = "0.###")
        {
            return double.IsNaN(value) ? string.Empty : value.ToString(format, CultureInfo.InvariantCulture);
        }

        public static string Integer(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        public static string Info(IReadOnlyDictionary<string, string> info, string key)
        {
            return info.TryGetValue(key, out var value) ? value : Missing;
        }

        public static double InfoNumber(IReadOnlyDictionary<string, string> info, string key)
        {
            return info.TryGetValue(key, out var value) ? double.Parse(value, CultureInfo.InvariantCulture) : double.NaN;
        }
    }
}
