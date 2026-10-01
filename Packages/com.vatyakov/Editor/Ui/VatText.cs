using System.Globalization;

namespace VATyakov.Editor
{
    static class VatText
    {
        static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

        public static string ClipSummary(VatClip clip) => string.Format(Invariant, "{0} {1} · {2:0.##} fps · {3:0.##} с · {4}",
            clip.FrameCount, Frames(clip.FrameCount), clip.FrameRate, clip.Length, clip.Loop ? "цикл" : "один раз");

        public static string Estimate(VatLayout layout) =>
            $"Получится {Count(layout.Clips[0].FrameCount)} · текстура {Size(layout.Info)} · {Megabytes(layout.Info)}";

        public static string Blocks(VatLayoutInfo info) => info.Blocks > 1 ? $"Меш разбит на {info.Blocks} блока по ширине." : null;

        public static string Count(int frames) => Number(frames) + " " + Frames(frames);

        public static string Frames(int count)
        {
            int mod100 = count % 100, mod10 = count % 10;
            if (mod100 >= 11 && mod100 <= 14)
                return "кадров";
            return mod10 == 1 ? "кадр" : mod10 >= 2 && mod10 <= 4 ? "кадра" : "кадров";
        }

        public static string Number(int value) => value.ToString(Invariant);

        public static string Size(VatLayoutInfo info) => string.Format(Invariant, "{0}×{1}", info.Width, info.Height);

        public static string Megabytes(VatLayoutInfo info) =>
            string.Format(Invariant, "{0:0.##} МБ", VatMemory.TextureBytes(info) / (1024.0 * 1024.0));
    }
}
