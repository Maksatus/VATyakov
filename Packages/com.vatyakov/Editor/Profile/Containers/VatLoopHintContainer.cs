using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    sealed class VatLoopHintContainer : EditorContainer
    {
        public readonly HelpBox Box = VatUi.HelpBox(HelpBoxMessageType.Info);

        public VatLoopHintContainer()
        {
            Root.Add(Box);
        }
    }
}
