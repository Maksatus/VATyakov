using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    internal sealed class VatProfileLayoutContainer : VatEditorContainer
    {
        public readonly VisualElement Source = VatUi.Card("Source");
        public readonly VisualElement Actions = new();
        public readonly VisualElement Result = VatUi.Card("Result");
        public readonly Foldout Material = VatUi.Foldout("Material", "VatBakeProfile.MaterialFoldout");
        public readonly Foldout Prefab = VatUi.Foldout("Test Prefab", "VatBakeProfile.PrefabFoldout");

        public VatProfileLayoutContainer()
        {
            Root.WithElements(Source, Actions, Result, Material, Prefab);
        }
    }
}
