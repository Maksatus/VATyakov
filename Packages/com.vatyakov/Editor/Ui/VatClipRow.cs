using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    internal sealed class VatClipRow : VisualElement
    {
        public VatClipRow(VatClip clip)
        {
            this.WithClass("vat-clip").WithElements(
                new Label(clip.Name).WithClass("vat-clip__name"),
                new Label(VatText.ClipSummary(clip)).WithClass("vat-clip__info"));
        }
    }
}
