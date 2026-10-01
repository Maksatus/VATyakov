using UnityEditor.UIElements;

namespace VATyakov.Editor
{
    sealed class VatMaterialFieldsContainer : EditorContainer
    {
        public VatMaterialFieldsContainer()
        {
            Root.WithElements(
                VatUi.Hint("Материал-шаблон получает текстуру и клип при каждом бейке. " +
                    "Если он не задан, первый бейк создаст его рядом с профилем из выбранного шейдера."),
                new PropertyField { bindingPath = "_material", label = "Материал-шаблон" },
                new PropertyField { bindingPath = "_shader", label = "Шейдер" });
        }
    }
}
