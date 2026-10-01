using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    sealed class VatAnimatorFieldsController : IController
    {
        readonly SerializedObject _serializedObject;
        readonly VatAnimatorFieldsContainer _container;

        public VatAnimatorFieldsController(SerializedObject serializedObject, VisualElement parent)
        {
            _serializedObject = serializedObject;
            _container = parent.CreateContainer<VatAnimatorFieldsContainer>();
        }

        public void Activate() => _container.Root.Bind(_serializedObject);

        public void Deactivate()
        {
            _container.Root.Unbind();
            _container.DestroyView();
        }
    }
}
