using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    internal sealed class VatAnimatorFieldsController : IVatController
    {
        private readonly VatAnimatorContext _context;
        private readonly VatAnimatorFieldsContainer _container;

        public VatAnimatorFieldsController(VatAnimatorContext context, VisualElement parent)
        {
            _context = context;
            _container = parent.CreateContainer<VatAnimatorFieldsContainer>();
        }

        public void Deactivate()
        {
            _container.Root.Unbind();
            _container.DestroyView();
        }

        public void Activate()
        {
            _container.Root.Bind(_context.SerializedObject);
            _container.Root.TrackPropertyValue(_context.SerializedObject.FindProperty(VatAnimatorContext.SpeedPath), OnSpeedChanged);
        }

        private void OnSpeedChanged(SerializedProperty speed)
        {
            if (!EditorApplication.isPlaying)
            {
                return;
            }

            foreach (var target in _context.SerializedObject.targetObjects)
            {
                ((VatAnimator)target).Speed = speed.floatValue;
            }
        }
    }
}
