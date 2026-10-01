using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    sealed class VatAssetErrorContainer : EditorContainer
    {
        public readonly HelpBox Box = VatUi.HelpBox(HelpBoxMessageType.Error);

        public VatAssetErrorContainer()
        {
            Root.Add(Box);
        }
    }
}
