using System;
using UnityEditor;

namespace VATyakov.Editor
{
    internal static class VatBakeProgress
    {
        private const string Title = "VAT bake";

        public static void Report(VatClip clip, int frame)
        {
            var info = FormattableString.Invariant($"{clip.Name}: frame {frame + 1}/{clip.FrameCount}");
            if (EditorUtility.DisplayCancelableProgressBar(Title, info, (float)frame / clip.FrameCount))
            {
                throw new VatBakeException("Bake cancelled.");
            }
        }

        public static void Clear()
        {
            EditorUtility.ClearProgressBar();
        }
    }
}
