using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    internal sealed class VatEstimateContainer : VatEditorContainer
    {
        public readonly Label Text = VatUi.Hint();

        public VatEstimateContainer()
        {
            Root.Add(Text);
        }
    }
}
