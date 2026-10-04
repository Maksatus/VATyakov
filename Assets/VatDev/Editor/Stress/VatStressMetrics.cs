using System;

namespace VATyakov.Dev
{
    internal static class VatStressMetrics
    {
        public const string CpuMain = "cpu_main_ms";
        public const string CpuRender = "cpu_render_ms";
        public const string Gpu = "gpu_ms";
        public const string Drive = "drive_ms";
        public const string VatWrite = "vat_write_ms";
        public const string AnimatorBegin = "animator_begin_ms";
        public const string AnimatorEnd = "animator_end_ms";
        public const string Skinning = "skinning_ms";
        public const string SetPass = "setpass";
        public const string SrpBatcherDraws = "srp_batcher_draws";
        public const string StandardDraws = "standard_draws";
        public const string ShadowCasters = "shadow_casters";

        private const double Nanoseconds = 1e-6;
        private const double Count = 1d;

        public static readonly VatStressMetric[] All =
        {
            VatStressMetric.Counter(CpuMain, "CPU Main Thread Active Time", Nanoseconds),
            VatStressMetric.Counter(CpuRender, "CPU Render Thread Active Time", Nanoseconds),
            VatStressMetric.Counter(Gpu, "GPU Frame Time", Nanoseconds),
            VatStressMetric.Sample(Drive, "VatStress.Drive"),
            VatStressMetric.Sample(VatWrite, "VatAnimator.Write"),
            VatStressMetric.Sample(AnimatorBegin, "PreLateUpdate.DirectorUpdateAnimationBegin"),
            VatStressMetric.Sample(AnimatorEnd, "PreLateUpdate.DirectorUpdateAnimationEnd"),
            VatStressMetric.Sample(Skinning, "PostLateUpdate.UpdateAllSkinnedMeshes"),
            VatStressMetric.Counter(SetPass, "SetPass Calls Count", Count),
            VatStressMetric.Counter(SrpBatcherDraws, "SRP Batcher Draw Calls Count", Count),
            VatStressMetric.Counter(StandardDraws, "Standard Draw Calls Count", Count),
            VatStressMetric.Counter("vertices", "Vertices Count", Count),
            VatStressMetric.Counter(ShadowCasters, "Shadow Casters Count", Count)
        };

        public static readonly string[] AnimationColumns = { Drive, VatWrite, AnimatorBegin, AnimatorEnd, Skinning };

        public static int IndexOf(string column)
        {
            return Array.FindIndex(All, metric => metric.Column == column);
        }
    }
}
