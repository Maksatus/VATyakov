using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace VATyakov.Dev
{
    internal static class VatStressMarkdown
    {
        private static readonly (string Title, VatStressVariant Smr, VatStressVariant Vat)[] _cases =
        {
            ("Random clips", VatStressVariant.Smr, VatStressVariant.Vat),
            ("CrossFade every frame", VatStressVariant.SmrCrossFade, VatStressVariant.VatCrossFade),
            ("LOD0 forced", VatStressVariant.SmrLod0, VatStressVariant.VatLod0)
        };

        private static readonly (string Title, string Column)[] _netMetrics =
        {
            ("CPU main, ms over empty", VatStressMetrics.CpuMain),
            ("CPU render, ms over empty", VatStressMetrics.CpuRender),
            ("GPU, ms over empty", VatStressMetrics.Gpu)
        };

        public static void Write(string path, IReadOnlyList<VatStressCapture> captures, VatStressSettings settings, string urpVersion, string invalidReason)
        {
            var summary = new VatStressSummary(captures);
            var lines = new List<string>();
            var info = summary.ReferenceInfo();
            var date = captures[0].StartedAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
            lines.Add($"# VAT stress: {VatStressFormat.Info(info, "device")}, {date}");
            lines.Add(string.Empty);
            if (invalidReason.Length > 0)
            {
                lines.Add($"**INVALID.** {invalidReason}");
                lines.Add(string.Empty);
            }

            AddConfiguration(lines, info, settings, urpVersion);
            AddResults(lines, summary);
            AddComparison(lines, summary);
            File.WriteAllLines(path, lines);
        }

        private static void AddConfiguration(List<string> lines, IReadOnlyDictionary<string, string> info, VatStressSettings settings, string urpVersion)
        {
            lines.Add("## Configuration");
            lines.Add(string.Empty);
            lines.Add("| | |");
            lines.Add("|---|---|");
            lines.Add($"| Device | {Info(info, "device")}, {Info(info, "system_memory_mb")} MB |");
            lines.Add($"| GPU | {Info(info, "gpu")}, {Info(info, "graphics_api")} |");
            lines.Add($"| Unity, URP | {Info(info, "unity")}, {urpVersion} |");
            lines.Add($"| Scripting backend | {Info(info, "scripting_backend")}, development build {Info(info, "development_build")} |");
            lines.Add($"| Resolution | {Info(info, "resolution")}, render scale {Info(info, "render_scale")} |");
            lines.Add($"| MSAA, HDR | {Info(info, "msaa")}, {Info(info, "hdr")} |");
            var shadows = $"{Info(info, "shadow_cascades")} cascades, distance {Info(info, "shadow_distance")} m, {Info(info, "shadow_resolution")} px";
            lines.Add($"| Shadows | {shadows} |");
            lines.Add($"| Target frame rate, vSync | {Info(info, "target_frame_rate")}, {Info(info, "vsync")} |");
            lines.Add($"| Units | {Info(info, "units")} |");
            lines.Add($"| Vertices per unit | LOD0 {Info(info, "vertices_lod0")}, LOD1 {Info(info, "vertices_lod1")} |");
            lines.Add(FormattableString.Invariant($"| Capture | {settings.CaptureFrames} frames after {settings.WarmupSeconds:0.#} s of warmup |"));
            lines.Add("| Thermal Status | 0 for the whole session, otherwise it stops as invalid |");
            lines.Add(string.Empty);
        }

        private static void AddResults(List<string> lines, VatStressSummary summary)
        {
            lines.Add("## Results");
            lines.Add(string.Empty);
            lines.Add("Mean of the captures of a variant; frame and counters are medians of a capture, markers are means.");
            lines.Add(string.Empty);
            lines.Add("| Variant | Captures | Battery, C | Frame, ms | Frame p95 | CPU main | CPU render | GPU | Animation CPU | " +
                "VatAnimator.Write | Animator | Skinning | Drive | SetPass | SRP Batcher draws | Shadow casters | Visible | LOD0 | In transition | " +
                "Total used, MB | Total reserved, MB | Gfx used, MB |");
            lines.Add("|---|" + string.Concat(Enumerable.Repeat("---:|", 21)));
            foreach (var variant in summary.Variants)
            {
                lines.Add(ResultRow(summary, variant));
            }

            lines.Add(string.Empty);
        }

        private static string ResultRow(VatStressSummary summary, string variant)
        {
            var cells = new List<string>
            {
                variant,
                VatStressFormat.Integer(summary.Count(variant)),
                VatStressFormat.Number(summary.Mean(variant, capture => capture.ThermalAfter.Battery), "0.0"),
                Number(summary.Mean(variant, capture => capture.Stats.FrameMedian)),
                Number(summary.Mean(variant, capture => capture.Stats.FrameP95)),
                Number(summary.Metric(variant, VatStressMetrics.CpuMain)),
                Number(summary.Metric(variant, VatStressMetrics.CpuRender)),
                Number(summary.Metric(variant, VatStressMetrics.Gpu)),
                Number(summary.Mean(variant, capture => capture.Stats.AnimationCpu())),
                Number(summary.Metric(variant, VatStressMetrics.VatWrite)),
                Number(summary.Metric(variant, VatStressMetrics.AnimatorBegin) + summary.Metric(variant, VatStressMetrics.AnimatorEnd)),
                Number(summary.Metric(variant, VatStressMetrics.Skinning)),
                Number(summary.Metric(variant, VatStressMetrics.Drive)),
                Count(summary.Metric(variant, VatStressMetrics.SetPass)),
                Count(summary.Metric(variant, VatStressMetrics.SrpBatcherDraws)),
                Count(summary.Metric(variant, VatStressMetrics.ShadowCasters)),
                Count(summary.Info(variant, "visible_units")),
                Count(summary.Info(variant, "lod0_units")),
                VatStressFormat.Number(summary.Info(variant, "transition_share") * 100d, "0") + "%",
                Count(summary.Info(variant, "total_used_mb")),
                Count(summary.Info(variant, "total_reserved_mb")),
                Count(summary.Info(variant, "gfx_used_mb"))
            };

            return $"| {string.Join(" | ", cells)} |";
        }

        private static void AddComparison(List<string> lines, VatStressSummary summary)
        {
            lines.Add("## VAT vs SMR");
            lines.Add(string.Empty);
            lines.Add("| Case | Metric | SMR | VAT | SMR / VAT |");
            lines.Add("|---|---|---:|---:|---:|");
            foreach (var (title, smr, vat) in _cases)
            {
                if (summary.Count(smr.Name) == 0 || summary.Count(vat.Name) == 0)
                {
                    continue;
                }

                foreach (var (metric, column) in _netMetrics)
                {
                    lines.Add(ComparisonRow(title, metric, summary.Net(smr.Name, column), summary.Net(vat.Name, column)));
                }

                var smrAnimation = summary.Mean(smr.Name, capture => capture.Stats.AnimationCpu());
                var vatAnimation = summary.Mean(vat.Name, capture => capture.Stats.AnimationCpu());
                lines.Add(ComparisonRow(title, "Animation CPU, ms", smrAnimation, vatAnimation));
            }

            lines.Add(string.Empty);
        }

        private static string ComparisonRow(string title, string metric, double smr, double vat)
        {
            return $"| {title} | {metric} | {Number(smr)} | {Number(vat)} | {VatStressFormat.Number(smr / vat, "0.0")} |";
        }

        private static string Info(IReadOnlyDictionary<string, string> info, string key)
        {
            return VatStressFormat.Info(info, key);
        }

        private static string Number(double value)
        {
            return VatStressFormat.Number(value);
        }

        private static string Count(double value)
        {
            return VatStressFormat.Number(value, "0");
        }
    }
}
