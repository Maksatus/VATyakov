namespace VATyakov.Editor
{
    internal static class VatAssetSummary
    {
        private const string MemoryHint = "Actual memory of the position, rotation and drift textures, including the empty texels of the last block.";

        public static void Show(VatAssetSummaryContainer container, VatAsset asset)
        {
            ShowLayout(container, asset.Layout);
            ShowClips(container, asset);
        }

        private static void ShowLayout(VatAssetSummaryContainer container, VatLayoutInfo info)
        {
            container.Vertices.Set(VatText.Number(info.Elements));
            container.Texture.Set(VatText.Size(info), VatText.Blocks(info));
            container.Memory.Set(VatText.Megabytes(info), MemoryHint);
        }

        private static void ShowClips(VatAssetSummaryContainer container, VatAsset asset)
        {
            container.Clips.Clear();
            foreach (var clip in asset.Clips)
            {
                container.Clips.Add(new VatClipRow(clip));
            }
        }
    }
}
