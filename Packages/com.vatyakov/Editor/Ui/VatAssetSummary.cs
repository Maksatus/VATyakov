namespace VATyakov.Editor
{
    internal static class VatAssetSummary
    {
        private const string MemoryHint = "Actual memory of the position and rotation textures, including the empty texels of the last block.";
        private const string ClipHint = "Memory is the share of the clip in both textures: its rows in every block.";

        public static void Show(VatAssetSummaryContainer container, VatAsset asset)
        {
            var memory = new VatAssetMemory(asset);
            ShowLayout(container, asset.Layout, memory);
            ShowClips(container, asset, memory);
        }

        private static void ShowLayout(VatAssetSummaryContainer container, VatLayoutInfo info, VatAssetMemory memory)
        {
            container.Vertices.Set(VatText.Number(info.Elements));
            container.Texture.Set(VatText.Size(info), VatText.Blocks(info));
            container.Memory.Set(VatText.Bytes(memory.Total), MemoryHint);
        }

        private static void ShowClips(VatAssetSummaryContainer container, VatAsset asset, VatAssetMemory memory)
        {
            container.Clips.Clear();
            foreach (var clip in asset.Clips)
            {
                var row = new VatInfoRow(clip.Name);
                row.Set(VatText.ClipSummary(clip, memory.Clip(clip)), ClipHint);
                container.Clips.Add(row);
            }
        }
    }
}
