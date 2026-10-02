using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    internal abstract class VatEditorContainer
    {
        public readonly VisualElement Root = new();

        public void SetVisible(bool isVisible)
        {
            Root.SetVisible(isVisible);
        }

        public void DestroyView()
        {
            Root.RemoveFromHierarchy();
        }
    }
}
