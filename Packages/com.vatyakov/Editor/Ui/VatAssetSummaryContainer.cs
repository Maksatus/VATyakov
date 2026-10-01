using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    sealed class VatAssetSummaryContainer : EditorContainer
    {
        readonly VatStat _vertices = new VatStat("Vertices");
        readonly VatStat _texture = new VatStat("Texture");
        readonly VatStat _memory = new VatStat("Memory");
        readonly VisualElement _clips = new VisualElement();

        public VatAssetSummaryContainer()
        {
            Root.WithElements(VatUi.Stats().WithElements(_vertices, _texture, _memory), _clips);
        }

        public void Show(VatAsset asset)
        {
            ShowLayout(asset.Layout);
            ShowClips(asset);
        }

        void ShowLayout(VatLayoutInfo info)
        {
            _vertices.Set(VatText.Number(info.Elements));
            _texture.Set(VatText.Size(info), VatText.Blocks(info));
            _memory.Set(VatText.Megabytes(info),
                "Actual memory of the position, rotation and drift textures, including the empty texels of the last block.");
        }

        void ShowClips(VatAsset asset)
        {
            _clips.Clear();
            foreach (var clip in asset.Clips)
                _clips.Add(new VatClipRow(clip));
        }
    }
}
