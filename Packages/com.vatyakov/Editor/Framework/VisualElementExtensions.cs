using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    static class VisualElementExtensions
    {
        public static T CreateContainer<T>(this VisualElement parent) where T : EditorContainer, new()
        {
            var container = new T();
            parent.Add(container.Root);
            return container;
        }

        public static T WithElements<T>(this T element, params VisualElement[] children) where T : VisualElement
        {
            foreach (var child in children)
                element.Add(child);
            return element;
        }

        public static T WithClass<T>(this T element, string className) where T : VisualElement
        {
            element.AddToClassList(className);
            return element;
        }

        public static void SetVisible(this VisualElement element, bool visible) =>
            element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }
}
