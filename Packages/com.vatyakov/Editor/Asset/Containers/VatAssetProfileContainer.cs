using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    internal sealed class VatAssetProfileContainer : VatEditorContainer
    {
        public readonly VatObjectLink Profile = new("Bake Profile", "Change the settings and rebake here.");
        public readonly Label Missing = VatUi.Hint("No profile bakes this asset.");

        public VatAssetProfileContainer()
        {
            Root.Add(VatUi.Card().WithElements(Profile, Missing));
        }
    }
}
