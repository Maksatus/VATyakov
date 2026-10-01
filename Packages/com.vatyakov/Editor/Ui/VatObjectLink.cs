using UnityEditor;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace VATyakov.Editor
{
    sealed class VatObjectLink : VisualElement
    {
        readonly Button _button;
        readonly Image _icon = new Image().WithClass("vat-link__icon");
        readonly Label _name = new Label().WithClass("vat-link__name");
        Object _target;

        public VatObjectLink(string label, string hint = null)
        {
            tooltip = hint;
            _button = new Button(Ping) { tooltip = "Ping in Project" }.WithClass("vat-link__button").WithElements(_icon, _name);
            this.WithClass("vat-link").WithElements(new Label(label).WithClass("vat-link__label"), _button);
            Set(null);
        }

        public void Set(Object target)
        {
            _target = target;
            _icon.image = target != null ? AssetPreview.GetMiniThumbnail(target) : null;
            _icon.SetVisible(target != null);
            _name.text = target != null ? target.name : "—";
            _button.SetEnabled(target != null);
        }

        void Ping() => EditorGUIUtility.PingObject(_target);
    }
}
