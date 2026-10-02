using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    internal sealed class VatSourceFieldsContainer : EditorContainer
    {
        public readonly VisualElement Skinned = new();
        public readonly VisualElement Alembic = new();

        public VatSourceFieldsContainer()
        {
            Skinned.WithElements(
                new PropertyField { bindingPath = "_source", label = "Skinned Mesh Renderer" },
                new PropertyField { bindingPath = "_clips", label = "Clips" });
            Alembic.WithElements(new PropertyField { bindingPath = "_alembic", label = "Alembic" });
            Root.WithElements(
                new PropertyField { bindingPath = "_kind", label = "Source" },
                Skinned,
                Alembic,
                new PropertyField { bindingPath = "_loop", label = "Loop" },
                new PropertyField { bindingPath = "_fps", label = "Frames Per Second" });
        }
    }
}
