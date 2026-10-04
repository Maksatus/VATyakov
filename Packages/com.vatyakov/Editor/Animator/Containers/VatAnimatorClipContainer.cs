using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    internal sealed class VatAnimatorClipContainer : VatEditorContainer
    {
        public readonly PopupField<string> Clip = new("Clip")
        {
            tooltip = "Clip Play On Enable starts. First follows the first clip of the VAT asset.",
        };

        public readonly HelpBox Problem = VatUi.HelpBox(HelpBoxMessageType.Error);

        public VatAnimatorClipContainer()
        {
            Clip.AddToClassList(BaseField<string>.alignedFieldUssClassName);
            Root.WithElements(new PropertyField { bindingPath = VatAnimatorContext.AssetPath, label = "VAT Asset" }, Clip, Problem);
        }
    }
}
