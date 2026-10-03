namespace VATyakov.Editor
{
    internal static class VatAssetSummary
    {
        private const string MemoryHint = "Actual memory of the animation textures, including the empty texels of the last block.";
        private const string ClipHint = "Memory is the share of the clip in the animation textures: its rows in every block.";
        private const string VertexHint = "Every vertex stores its offset and rotation per frame: any deformation.";
        private const string BoneHint = "Every bone stores its offset, uniform scale and rotation per frame; the shader skins two bones per vertex.";

        public static void Show(VatAssetSummaryContainer container, VatAsset asset)
        {
            var memory = new VatAssetMemory(asset);
            ShowLayout(container, asset, memory);
            container.Fallback.SetMessage(asset.Fallback);
            ShowClips(container, asset, memory);
        }

        private static void ShowLayout(VatAssetSummaryContainer container, VatAsset asset, VatAssetMemory memory)
        {
            var info = asset.Layout;
            var isBone = asset.Mode == VatMode.Bone;
            container.Mode.Set(asset.Mode.ToString(), isBone ? BoneHint : VertexHint);
            container.Vertices.Set(VatText.Number(asset.Mesh.vertexCount));
            container.Bones.SetVisible(isBone);
            container.Bones.Set(VatText.Number(asset.BoneCount));
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
