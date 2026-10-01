using System.Globalization;
using UnityEngine;

namespace VATyakov.Editor
{
    static class VatBakeLog
    {
        public static void Baked(VatAsset asset, VatBakeResult result) => Debug.Log(Describe(asset) + Stats(result.Stats), asset);

        static string Describe(VatAsset asset)
        {
            var info = asset.Layout;
            var clip = asset.Clips[0];
            return string.Format(CultureInfo.InvariantCulture,
                "VAT '{0}': {1} вертексов, клип '{2}' — {3} кадров, {4:0.###} fps; _VatPosTex {5}×{6} ({7} блок.), {8}",
                asset.name, info.Elements, clip.Name, clip.FrameCount, clip.FrameRate, info.Width, info.Height, info.Blocks,
                VatText.Megabytes(info));
        }

        static string Stats(VatQuantizationStats stats) => string.Format(CultureInfo.InvariantCulture,
            "; max |Δ| {0:0.###} м, max ошибка half {1:0.###} мм", stats.MaxOffset, stats.MaxError * 1000f);
    }
}
