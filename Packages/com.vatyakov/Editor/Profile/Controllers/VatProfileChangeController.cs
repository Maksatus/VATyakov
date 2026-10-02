using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    internal sealed class VatProfileChangeController : IVatController
    {
        private readonly VatProfileContext _context;
        private readonly VatTrackerContainer _container;

        public VatProfileChangeController(VatProfileContext context, VisualElement parent)
        {
            _context = context;
            _container = parent.CreateContainer<VatTrackerContainer>();
        }

        public void Deactivate()
        {
            _container.Root.Unbind();
            _container.DestroyView();
        }

        public void Activate()
        {
            _container.Root.TrackSerializedObjectValue(_context.SerializedObject, OnSerializedObjectChanged);
        }

        private void OnSerializedObjectChanged(SerializedObject _)
        {
            _context.Model.Changed.Call();
        }
    }
}
