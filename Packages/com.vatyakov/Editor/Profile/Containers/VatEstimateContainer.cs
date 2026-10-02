using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    internal sealed class VatEstimateContainer : EditorContainer
    {
        private readonly Label _text = VatUi.Hint();

        public VatEstimateContainer()
        {
            Root.Add(_text);
        }

        public void Show(VatBakeEstimate estimate)
        {
            SetVisible(!estimate.IsEmpty);
            if (!estimate.IsEmpty)
            {
                ShowText(estimate);
            }
        }

        private void ShowText(VatBakeEstimate estimate)
        {
            _text.text = estimate.Fits ? VatText.Estimate(estimate.Layout) : estimate.Error;
            _text.EnableInClassList("vat-hint--error", !estimate.Fits);
        }
    }
}
