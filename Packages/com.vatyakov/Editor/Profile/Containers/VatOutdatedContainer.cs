using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    internal sealed class VatOutdatedContainer : VatEditorContainer
    {
        public readonly HelpBox Outdated = VatUi.HelpBox(HelpBoxMessageType.Warning);

        public VatOutdatedContainer()
        {
            Root.Add(Outdated);
        }
    }
}
