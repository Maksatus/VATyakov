using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    sealed class VatAnimatorFieldsContainer : EditorContainer
    {
        public VatAnimatorFieldsContainer()
        {
            Root.WithElements(
                new PropertyField { bindingPath = "_playOnEnable", label = "Play On Enable" },
                new PropertyField { bindingPath = "_speed", label = "Speed" });
        }
    }
}
