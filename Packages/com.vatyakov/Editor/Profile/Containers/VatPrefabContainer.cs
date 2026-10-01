using UnityEngine;
using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    sealed class VatPrefabContainer : EditorContainer
    {
        public readonly Button Button = VatUi.SecondaryButton();
        readonly VatObjectLink _link = new VatObjectLink("Префаб");

        public VatPrefabContainer()
        {
            Root.WithElements(
                VatUi.Hint("MeshFilter + MeshRenderer с материалом-шаблоном — для сцены Compare и быстрой проверки. " +
                    "Игровые префабы собираются вручную."),
                _link,
                Button);
        }

        public void Show(GameObject prefab, bool canCreate)
        {
            _link.Set(prefab);
            Button.text = prefab != null ? "Обновить префаб" : "Создать префаб";
            Button.SetEnabled(canCreate);
            Button.tooltip = canCreate ? string.Empty : "Сначала запеките профиль.";
        }
    }
}
