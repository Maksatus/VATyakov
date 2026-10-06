using System;
using UnityEditor;

namespace VATyakov.Editor
{
    internal static class VatBakeProgress
    {
        private const string Title = "VAT bake";

        public static void Report(VatClip clip, int frame)
        {
            Report(FormattableString.Invariant($"{clip.Name}: frame {frame + 1}/{clip.FrameCount}"), (float)frame / clip.FrameCount);
        }

        public static void ReportBetween(VatClip clip, int sample, int sampleCount)
        {
            Report(FormattableString.Invariant($"{clip.Name}: source sample {sample + 1}/{sampleCount} between frames"), (float)sample / sampleCount);
        }

        public static void Clear()
        {
            EditorUtility.ClearProgressBar();
        }

        private static void Report(string info, float progress)
        {
            if (EditorUtility.DisplayCancelableProgressBar(Title, info, progress))
            {
                throw new VatBakeException("Bake cancelled.");
            }
        }
    }
}
