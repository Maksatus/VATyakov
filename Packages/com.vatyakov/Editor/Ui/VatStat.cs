using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    internal sealed class VatStat : VisualElement
    {
        private readonly Label _value = new Label().WithClass("vat-stat__value");

        public VatStat(string name)
        {
            this.WithClass("vat-stat").WithElements(_value, new Label(name).WithClass("vat-stat__name"));
        }

        public void Set(string value, string hint = null)
        {
            _value.text = value;
            tooltip = hint;
        }
    }
}
