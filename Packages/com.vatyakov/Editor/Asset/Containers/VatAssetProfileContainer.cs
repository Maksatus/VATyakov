using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    sealed class VatAssetProfileContainer : EditorContainer
    {
        readonly VatObjectLink _link = new VatObjectLink("Bake Profile", "Change the settings and rebake here.");
        readonly Label _missing = VatUi.Hint("No profile bakes this asset.");

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
