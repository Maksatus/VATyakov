using System;
using System.Collections.Generic;
using System.Linq;

namespace VATyakov.Dev
{
    internal sealed class VatStressSummary
    {
        private readonly IReadOnlyList<VatStressCapture> _captures;

        public IReadOnlyList<string> Variants { get; }

        public VatStressSummary(IReadOnlyList<VatStressCapture> captures)
        {
            _captures = captures;
            Variants = VatStressVariant.All.Select(variant => variant.Name).Where(name => captures.Any(capture => capture.Variant == name)).ToArray();
        }

        public int Count(string variant)
        {
            return _captures.Count(capture => capture.Variant == variant);
        }

        public double Mean(string variant, Func<VatStressCapture, double> selector)
        {
            var values = _captures.Where(capture => capture.Variant == variant).Select(selector).Where(value => !double.IsNaN(value)).ToArray();
            return values.Length > 0 ? values.Average() : double.NaN;
        }

        public double Metric(string variant, string column)
        {
            return Mean(variant, capture => capture.Stats.Value(column));
        }

        public double Net(string variant, string column)
        {
            return Metric(variant, column) - Metric(VatStressVariant.Empty.Name, column);
        }

        public double Info(string variant, string key)
        {
            return Mean(variant, capture => VatStressFormat.InfoNumber(capture.Info, key));
        }

        public IReadOnlyDictionary<string, string> ReferenceInfo()
        {
            var reference = _captures.FirstOrDefault(capture => capture.Variant == VatStressVariant.Vat.Name) ?? _captures[0];
            return reference.Info;
        }
    }
}
