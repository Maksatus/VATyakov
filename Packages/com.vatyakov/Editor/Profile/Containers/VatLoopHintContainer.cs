using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    internal sealed class VatLoopHintContainer : VatEditorContainer
    {
        public readonly HelpBox Box = VatUi.HelpBox(HelpBoxMessageType.Info);

        public VatLoopHintContainer()
        {
            Root.Add(Box);
        }
    }
}
