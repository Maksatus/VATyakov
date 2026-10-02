namespace VATyakov.Editor
{
    internal sealed class VatAssetOverviewContainer : EditorContainer
    {
        public readonly VatAssetSummaryContainer Summary = new();

        public VatAssetOverviewContainer()
        {
            Root.Add(VatUi.Card("Animation").WithElements(Summary.Root));
        }
    }
}
