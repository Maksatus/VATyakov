using UnityEditor.UIElements;

namespace VATyakov.Editor
{
    internal sealed class VatOutputFieldsContainer : VatEditorContainer
    {
        public VatOutputFieldsContainer()
        {
            Root.WithElements(
                VatUi.Hint("Every bake writes here. When empty, the first bake creates them next to the profile."),
                new PropertyField { bindingPath = "_asset", label = "VAT Asset" },
                new PropertyField { bindingPath = "_material", label = "Template Material" });
        }
    }
}
