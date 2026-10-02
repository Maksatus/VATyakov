using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    internal abstract class EditorContainer
    {
        public readonly VisualElement Root = new();

        public void SetVisible(bool visible)
        {
            Root.SetVisible(visible);
        }

        public void DestroyView()
        {
            Root.RemoveFromHierarchy();
        }
    }
}
