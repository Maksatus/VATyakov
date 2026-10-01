using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    sealed class VatBoundFieldsController<T> : IController where T : EditorContainer, new()
    {
        readonly SerializedObject _serializedObject;
        readonly T _container;

        public VatBoundFieldsController(VatProfileContext context, VisualElement parent)
        {
            _serializedObject = context.SerializedObject;
            _container = parent.CreateContainer<T>();
        }

        public void Activate() => _container.Root.Bind(_serializedObject);

        public void Deactivate()
        {
            _container.Root.Unbind();
            _container.DestroyView();
        }
    }
}
