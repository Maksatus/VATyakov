using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    internal sealed class VatAnimatorFieldsController : IController
    {
        private readonly SerializedObject _serializedObject;
        private readonly VatAnimatorFieldsContainer _container;

        public VatAnimatorFieldsController(SerializedObject serializedObject, VisualElement parent)
        {
            _serializedObject = serializedObject;
            _container = parent.CreateContainer<VatAnimatorFieldsContainer>();
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
