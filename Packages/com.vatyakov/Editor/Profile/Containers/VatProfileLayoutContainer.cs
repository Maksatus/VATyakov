using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    sealed class VatProfileLayoutContainer : EditorContainer
    {
        public readonly VisualElement Source = VatUi.Card("Что запекаем");
        public readonly VisualElement Actions = new VisualElement();
        public readonly VisualElement Result = VatUi.Card("Результат");
        public readonly Foldout Material = VatUi.Foldout("Материал", "VatBakeProfile.MaterialFoldout");
        public readonly Foldout Prefab = VatUi.Foldout("Тестовый префаб", "VatBakeProfile.PrefabFoldout");

        public VatProfileLayoutContainer()
        {
            Root.WithElements(Source, Actions, Result, Material, Prefab);
        }
    }
}
