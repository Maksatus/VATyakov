using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    internal sealed class VatAssetSummaryContainer : VatEditorContainer
    {
        public readonly VatStat Mode = new("Mode");
        public readonly VatStat Vertices = new("Vertices");
        public readonly VatStat Bones = new("Bones");
        public readonly VatStat Pieces = new("Pieces");
        public readonly VatStat Texture = new("Texture");
        public readonly VatStat Positions = new("Positions");
        public readonly VatStat Error = new("Max Error");
        public readonly HelpBox Fallback = VatUi.HelpBox(HelpBoxMessageType.Warning);
        public readonly VisualElement Clips = new();

        public VatAssetSummaryContainer()
        {
            Root.WithElements(VatUi.Stats().WithElements(Mode, Vertices, Bones, Pieces, Texture, Positions, Error), Fallback, Clips);
        }
    }
}
