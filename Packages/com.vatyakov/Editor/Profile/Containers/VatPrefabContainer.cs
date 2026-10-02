using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    internal sealed class VatPrefabContainer : VatEditorContainer
    {
        public readonly VatObjectLink Prefab = new("Prefab");
        public readonly Button Button = VatUi.SecondaryButton();

        public VatPrefabContainer()
        {
            Root.WithElements(
                VatUi.Hint("Mesh Filter + Mesh Renderer with the template material, for the Compare scene and quick checks. " +
                    "Game prefabs are assembled by hand."),
                Prefab,
                Button);
        }
    }
}
