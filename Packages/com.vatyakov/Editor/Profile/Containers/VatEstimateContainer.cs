using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    sealed class VatEstimateContainer : EditorContainer
    {
        readonly Label _text = VatUi.Hint();

        public VatEstimateContainer()
        {
            Root.Add(_text);
        }

        public void Show(VatBakeEstimate estimate)
        {
            SetVisible(!estimate.IsEmpty);
            if (!estimate.IsEmpty)
                ShowText(estimate);
        }

        void ShowText(VatBakeEstimate estimate)
        {
            _text.text = estimate.Fits ? VatText.Estimate(estimate.Layout) : estimate.Error;
            _text.EnableInClassList("vat-hint--error", !estimate.Fits);
        }
    }
}
