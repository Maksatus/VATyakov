using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    internal sealed class VatInfoRow : VisualElement
    {
        private readonly Label _info = new Label().WithClass("vat-row__info");

        public VatInfoRow(string name)
        {
            this.WithClass("vat-row").WithElements(new Label(name).WithClass("vat-row__name"), _info);
        }

        public void Set(string info, string hint = null)
        {
            _info.text = info;
            tooltip = hint;
        }
    }
}
