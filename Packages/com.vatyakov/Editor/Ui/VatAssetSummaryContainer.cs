using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    internal sealed class VatAssetSummaryContainer : EditorContainer
    {
        private readonly VatStat _vertices = new("Vertices");
        private readonly VatStat _texture = new("Texture");
        private readonly VatStat _memory = new("Memory");
        private readonly VisualElement _clips = new();

        public VatAssetSummaryContainer()
        {
            Root.WithElements(VatUi.Stats().WithElements(_vertices, _texture, _memory), _clips);
        }

        public void Show(VatAsset asset)
        {
            ShowLayout(asset.Layout);
            ShowClips(asset);
        }

        private void ShowLayout(VatLayoutInfo info)
        {
            _vertices.Set(VatText.Number(info.Elements));
            _texture.Set(VatText.Size(info), VatText.Blocks(info));
            _memory.Set(VatText.Megabytes(info),
                "Actual memory of the position, rotation and drift textures, including the empty texels of the last block.");
        }

        private void ShowClips(VatAsset asset)
        {
            _clips.Clear();
            foreach (var clip in asset.Clips)
            {
                _clips.Add(new VatClipRow(clip));
            }
        }
    }
}
