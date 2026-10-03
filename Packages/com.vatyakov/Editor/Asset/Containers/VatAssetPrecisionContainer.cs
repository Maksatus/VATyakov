namespace VATyakov.Editor
{
    internal sealed class VatAssetPrecisionContainer : VatEditorContainer
    {
        public readonly VatStat Positions = new("Positions");
        public readonly VatStat Error = new("Max Error");
        public readonly VatStat Drift = new("Drift");

        public VatAssetPrecisionContainer()
        {
            Root.Add(VatUi.Card("Precision").WithElements(VatUi.Stats().WithElements(Positions, Error, Drift)));
        }
    }
}
