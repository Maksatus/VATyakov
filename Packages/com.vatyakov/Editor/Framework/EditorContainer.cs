using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    abstract class EditorContainer
    {
        public readonly VisualElement Root = new VisualElement();

        public void SetVisible(bool visible) => Root.SetVisible(visible);

        public void DestroyView() => Root.RemoveFromHierarchy();
    }
}
