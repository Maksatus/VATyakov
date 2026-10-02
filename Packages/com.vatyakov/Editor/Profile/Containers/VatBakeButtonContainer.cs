using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    internal sealed class VatBakeButtonContainer : EditorContainer
    {
        public readonly Button Button = VatUi.PrimaryButton("Bake");

        public VatBakeButtonContainer()
        {
            Root.Add(Button);
        }
    }
}
