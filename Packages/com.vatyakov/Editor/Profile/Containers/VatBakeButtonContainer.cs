using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    sealed class VatBakeButtonContainer : EditorContainer
    {
        public readonly Button Button = VatUi.PrimaryButton("Запечь");

        public VatBakeButtonContainer()
        {
            Root.Add(Button);
        }
    }
}
