using UnityEditor;

namespace VATyakov.Editor
{
    internal static class VatBakeProgress
    {
        public static void Report(VatClip clip, int frame)
        {
            var info = $"{clip.Name}: frame {frame + 1}/{clip.FrameCount}";
            if (EditorUtility.DisplayCancelableProgressBar("VAT bake", info, (float)frame / clip.FrameCount))
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
