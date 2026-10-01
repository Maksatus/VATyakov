using System.Globalization;
using UnityEngine;

namespace VATyakov.Editor
{
    static class VatBakeLog
    {
        public static void Baked(VatAsset asset, VatBakeResult result)
        {
            Debug.Log(Describe(asset) + Stats(result.Stats), asset);
            if (result.Chirality.Count > 0)
                Debug.LogWarning(Chirality(asset, result.Chirality), asset);
        }

        static string Describe(VatAsset asset)
        {
            var info = asset.Layout;
            var clip = asset.Clips[0];
            return string.Format(CultureInfo.InvariantCulture,
                "VAT '{0}': {1} вертексов, клип '{2}' — {3} кадров, {4:0.###} fps; текстуры {5}×{6} ({7} блок.), {8}",
                asset.name, info.Elements, clip.Name, clip.FrameCount, clip.FrameRate, info.Width, info.Height, info.Blocks,
                VatText.Megabytes(info));
        }

        static string Stats(VatQuantizationStats stats) => string.Format(CultureInfo.InvariantCulture,
            "; max |Δ| {0:0.###} м, max ошибка half {1:0.###} мм, max ошибка поворота {2:0.###}°, вырожденных тангентов {3}",
            stats.MaxOffset, stats.MaxError * 1000f, stats.MaxRotationError, stats.DegenerateTangents);

        static string Chirality(VatAsset asset, VatChirality chirality) =>
            $"VAT '{asset.name}': у {chirality.Count} вертексов знак бинормали в кадрах не совпадает с покоем (первый — {chirality.First}). " +
            "Знак берётся из покоя, normal map на этих вертексах будет отражена. Проверьте зеркальные кости и отрицательный масштаб.";
    }
}
