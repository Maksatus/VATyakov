using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    sealed class VatAssetProfileContainer : EditorContainer
    {
        readonly VatObjectLink _link = new VatObjectLink("Профиль бейка", "Здесь меняются настройки и запускается повторный бейк.");
        readonly Label _missing = VatUi.Hint("Профиль, который запекает этот ассет, не найден.");

        public VatAssetProfileContainer()
        {
            Root.Add(VatUi.Card().WithElements(_link, _missing));
        }

        public void Show(VatBakeProfile profile)
        {
            _link.Set(profile);
            _missing.SetVisible(profile == null);
        }
    }
}
