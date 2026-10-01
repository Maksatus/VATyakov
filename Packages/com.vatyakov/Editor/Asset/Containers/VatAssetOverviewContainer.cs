namespace VATyakov.Editor
{
    sealed class VatAssetOverviewContainer : EditorContainer
    {
        public readonly VatAssetSummaryContainer Summary = new VatAssetSummaryContainer();

        public VatAssetOverviewContainer()
        {
            Root.Add(VatUi.Card("Animation").WithElements(Summary.Root));
        }
    }
}
