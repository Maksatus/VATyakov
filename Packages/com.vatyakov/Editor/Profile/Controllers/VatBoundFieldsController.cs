using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    internal sealed class VatBoundFieldsController<T> : IVatController where T : VatEditorContainer, new()
    {
        private readonly SerializedObject _serializedObject;
        private readonly T _container;

        public VatBoundFieldsController(VatProfileContext context, VisualElement parent)
        {
            _serializedObject = context.SerializedObject;
            _container = parent.CreateContainer<T>();
        }

        public void Deactivate()
        {
            _container.Root.Unbind();
            _container.DestroyView();
        }

        public void Activate()
        {
            _container.Root.Bind(_serializedObject);
        }
    }
}
