using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    internal sealed class VatProblemsContainer : VatEditorContainer
    {
        public readonly HelpBox Box = VatUi.HelpBox(HelpBoxMessageType.Error).WithClass("vat-problems");

        public VatProblemsContainer()
        {
            Root.Add(Box);
        }
    }
}
