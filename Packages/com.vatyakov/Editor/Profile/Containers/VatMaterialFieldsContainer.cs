using UnityEditor.UIElements;

namespace VATyakov.Editor
{
    sealed class VatMaterialFieldsContainer : EditorContainer
    {
        public VatMaterialFieldsContainer()
        {
            Root.WithElements(
                VatUi.Hint("The template material receives the textures and clip on every bake. " +
                    "When empty, the first bake creates it next to the profile from the selected shader."),
                new PropertyField { bindingPath = "_material", label = "Template Material" },
                new PropertyField { bindingPath = "_shader", label = "Shader" });
        }
    }
}
