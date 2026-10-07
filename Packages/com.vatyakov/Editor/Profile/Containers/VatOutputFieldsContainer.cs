using UnityEditor.UIElements;

namespace VATyakov.Editor
{
    internal sealed class VatOutputFieldsContainer : VatEditorContainer
    {
        public VatOutputFieldsContainer()
        {
            Root.WithElements(
                VatUi.Hint("Every bake writes into the VAT asset and the template material. When empty, the first bake creates the VAT asset " +
                    "next to the profile and the material next to the VAT asset, from the selected shader."),
                new PropertyField { bindingPath = "_asset", label = "VAT Asset" },
                new PropertyField { bindingPath = "_material", label = "Template Material" },
                new PropertyField { bindingPath = "_shader", label = "Shader" });
        }
    }
}
