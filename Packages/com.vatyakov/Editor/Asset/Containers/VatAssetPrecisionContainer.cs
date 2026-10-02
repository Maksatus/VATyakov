namespace VATyakov.Editor
{
    internal sealed class VatAssetPrecisionContainer : EditorContainer
    {
        private readonly VatStat _error = new("Max Error");
        private readonly VatStat _drift = new("Drift");

        public VatAssetPrecisionContainer()
        {
            Root.Add(VatUi.Card("Precision").WithElements(VatUi.Stats().WithElements(_error, _drift)));
        }

        public void Show(VatAsset asset)
        {
            var precision = asset.Precision;
            _error.Set(VatText.Millimeters(precision.Error), "Max position error the shader reconstructs, fp16 sampling included.");
            _drift.Set(VatText.Meters(precision.MaxDrift), VatText.DriftTravel(precision.MaxDrift));
        }
    }
}
