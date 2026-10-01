using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    sealed class VatProblemsContainer : EditorContainer
    {
        public readonly HelpBox Box = VatUi.HelpBox(HelpBoxMessageType.Error).WithClass("vat-problems");

        public VatProblemsContainer()
        {
            Root.Add(Box);
        }
    }
}
