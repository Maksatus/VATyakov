using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    internal sealed class VatProfileLayoutContainer : VatEditorContainer
    {
        public readonly VisualElement Source = VatUi.Card("Source");
        public readonly VisualElement Actions = new();

        public VatProfileLayoutContainer()
        {
            Root.WithElements(Source, Actions);
        }
    }
}
