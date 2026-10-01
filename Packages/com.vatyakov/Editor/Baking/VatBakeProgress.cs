using UnityEditor;

namespace VATyakov.Editor
{
    static class VatBakeProgress
    {
        public static void Report(VatClip clip, int frame)
        {
            string info = $"{clip.Name}: кадр {frame + 1}/{clip.FrameCount}";
            if (EditorUtility.DisplayCancelableProgressBar("VAT bake", info, (float)frame / clip.FrameCount))
                throw new VatBakeException("Бейк отменён.");
        }

        public static void Clear() => EditorUtility.ClearProgressBar();
    }
}
