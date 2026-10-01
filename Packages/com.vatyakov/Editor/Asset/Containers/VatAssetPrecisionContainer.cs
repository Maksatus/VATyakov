namespace VATyakov.Editor
{
    // §1.2: the error after fp16 sampling, with drift and without it (§2.2).
    sealed class VatAssetPrecisionContainer : EditorContainer
    {
        readonly VatStat _drift = new VatStat("Drift");
        readonly VatStat _withDrift = new VatStat("Error with Drift");
        readonly VatStat _withoutDrift = new VatStat("Error without Drift");

        public VatAssetPrecisionContainer()
        {
            Root.Add(VatUi.Card("Precision").WithElements(
                VatUi.Stats().WithElements(_drift, _withDrift, _withoutDrift),
                VatUi.Hint(VatText.DriftRule())));
        }

        public void Show(VatAsset asset)
        {
            var precision = asset.Precision;
            _drift.Set(asset.Layout.Drift ? "On" : "Off", VatText.DriftTravel(precision.MaxDrift));
            _withDrift.Set(VatText.Millimeters(precision.ErrorWithDrift), "Max position error the shader reconstructs with drift.");
            _withoutDrift.Set(VatText.Millimeters(precision.ErrorWithoutDrift), "Max position error the shader would reconstruct without drift.");
        }
    }
}
