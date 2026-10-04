using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace VATyakov.Dev
{
    internal static class VatStressCsv
    {
        private static readonly string[] _infoColumns =
        {
            "visible_units", "lod0_units", "transition_share", "shadow_cascades", "total_used_mb", "total_reserved_mb", "gfx_used_mb"
        };

        public static void Write(string path, IReadOnlyList<VatStressCapture> captures)
        {
            var lines = new List<string> { Header() };
            lines.AddRange(captures.Select(Row));
            File.WriteAllLines(path, lines);
        }

        private static string Header()
        {
            var columns = new List<string> { "index", "variant", "started", "frames", "dropped_frames", "frame_median_ms", "frame_p95_ms" };
            columns.AddRange(VatStressMetrics.All.Select(metric => metric.Column));
            columns.Add("animation_cpu_ms");
            columns.AddRange(_infoColumns);
            columns.AddRange(new[] { "thermal_status_start", "thermal_status_end", "battery_start_c", "battery_end_c" });
            columns.AddRange(new[] { "soc_start_c", "soc_end_c", "skin_start_c", "skin_end_c", "profile" });
            return string.Join(",", columns);
        }

        private static string Row(VatStressCapture capture)
        {
            var stats = capture.Stats;
            var values = new List<string>
            {
                VatStressFormat.Integer(capture.Index),
                capture.Variant,
                capture.StartedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                VatStressFormat.Integer(stats.Frames),
                VatStressFormat.Integer(stats.DroppedFrames),
                VatStressFormat.CsvNumber(stats.FrameMedian),
                VatStressFormat.CsvNumber(stats.FrameP95)
            };

            values.AddRange(stats.Values.Select(value => VatStressFormat.CsvNumber(value)));
            values.Add(VatStressFormat.CsvNumber(stats.AnimationCpu()));
            values.AddRange(_infoColumns.Select(column => VatStressFormat.Info(capture.Info, column)));
            values.Add(VatStressFormat.Integer(capture.ThermalBefore.Status));
            values.Add(VatStressFormat.Integer(capture.ThermalAfter.Status));
            values.Add(VatStressFormat.CsvNumber(capture.ThermalBefore.Battery, "0.0"));
            values.Add(VatStressFormat.CsvNumber(capture.ThermalAfter.Battery, "0.0"));
            values.Add(VatStressFormat.CsvNumber(capture.ThermalBefore.Soc, "0.0"));
            values.Add(VatStressFormat.CsvNumber(capture.ThermalAfter.Soc, "0.0"));
            values.Add(VatStressFormat.CsvNumber(capture.ThermalBefore.Skin, "0.0"));
            values.Add(VatStressFormat.CsvNumber(capture.ThermalAfter.Skin, "0.0"));
            values.Add(Path.GetFileName(capture.ProfileFile));
            return string.Join(",", values);
        }
    }
}
