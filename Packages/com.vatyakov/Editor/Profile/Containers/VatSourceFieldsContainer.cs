using UnityEditor.UIElements;

namespace VATyakov.Editor
{
    sealed class VatSourceFieldsContainer : EditorContainer
    {
        public VatSourceFieldsContainer()
        {
            Root.WithElements(
                new PropertyField { bindingPath = "_source", label = "Персонаж" },
                new PropertyField { bindingPath = "_clip", label = "Анимация" },
                new PropertyField { bindingPath = "_fps", label = "Кадров в секунду" });
        }
    }
}
