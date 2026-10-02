using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    internal sealed class VatAssetSummaryContainer : VatEditorContainer
    {
        public readonly VatStat Vertices = new("Vertices");
        public readonly VatStat Texture = new("Texture");
        public readonly VatStat Memory = new("Memory");
        public readonly VisualElement Clips = new();

        public VatAssetSummaryContainer()
        {
            Root.WithElements(VatUi.Stats().WithElements(Vertices, Texture, Memory), Clips);
        }
    }
}
