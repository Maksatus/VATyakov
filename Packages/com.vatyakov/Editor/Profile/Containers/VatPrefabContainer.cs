using UnityEngine;
using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    internal sealed class VatPrefabContainer : EditorContainer
    {
        public readonly Button Button = VatUi.SecondaryButton();
        private readonly VatObjectLink _link = new("Prefab");

        public VatPrefabContainer()
        {
            Root.WithElements(
                VatUi.Hint("MeshFilter + MeshRenderer with the template material, for the Compare scene and quick checks. " +
                    "Game prefabs are assembled by hand."),
                _link,
                Button);
        }

        public void Show(GameObject prefab, bool canCreate)
        {
            _link.Set(prefab);
            Button.text = prefab != null ? "Update Prefab" : "Create Prefab";
            Button.SetEnabled(canCreate);
            Button.tooltip = canCreate ? string.Empty : "Bake the profile first.";
        }
    }
}
