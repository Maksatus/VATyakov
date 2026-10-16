namespace VATyakov.Editor
{
    internal static class VatAssetSummary
    {
        private const string ErrorHint = "Max position error of the VAT against the source on the baked frames, half precision included.";
        private const string ClipHint = "Memory is the share of the clip in the animation textures: its rows in every block.";
        private const string VertexHint = "Every vertex stores its offset and rotation per frame: any deformation.";
        private const string BoneHint = "Every bone stores its offset, uniform scale and rotation per frame; the shader skins two bones per vertex.";
        private const string RigidHint = "Every piece stores its offset, uniform scale and rotation per frame; a hidden piece has scale 0.";

        public static void Show(VatAssetSummaryContainer container, VatAsset asset)
        {
            var memory = new VatAssetMemory(asset);
            ShowLayout(container, asset);
            container.Fallback.SetMessage(asset.Fallback);
            ShowClips(container, asset, memory);
        }

        private static void ShowLayout(VatAssetSummaryContainer container, VatAsset asset)
        {
            var info = asset.Layout;
            var isBone = asset.Mode == VatMode.Bone;
            var isRigid = asset.Mode == VatMode.Rigid;
            container.Mode.Set(asset.Mode.ToString(), isBone ? BoneHint : isRigid ? RigidHint : VertexHint);
            container.Vertices.Set(VatText.Number(asset.Mesh.vertexCount));
            container.Bones.SetVisible(isBone);
            container.Bones.Set(VatText.Number(asset.BoneCount));
            container.Pieces.SetVisible(isRigid);
            container.Pieces.Set(VatText.Number(asset.BoneCount));
            container.Texture.Set(VatText.Size(info), VatText.Blocks(info));
            container.Positions.SetVisible(asset.Mode == VatMode.Vertex);
            container.Positions.Set(VatText.PositionFormat(asset.PositionFormat), VatText.PositionFormatHint(asset.PositionFormat, asset.Precision));
            container.Error.Set(VatText.Millimeters(asset.Precision.Error), ErrorHint);
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
