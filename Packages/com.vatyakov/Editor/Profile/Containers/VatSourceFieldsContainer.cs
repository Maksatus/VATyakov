using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    sealed class VatSourceFieldsContainer : EditorContainer
    {
        public readonly VisualElement Skinned = new VisualElement();
        public readonly VisualElement Alembic = new VisualElement();

        public VatSourceFieldsContainer()
        {
            Skinned.WithElements(
                new PropertyField { bindingPath = "_source", label = "Skinned Mesh Renderer" },
                new PropertyField { bindingPath = "_clip", label = "Clip" });
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
