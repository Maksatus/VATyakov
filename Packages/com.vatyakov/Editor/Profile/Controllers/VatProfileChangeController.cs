using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    internal sealed class VatProfileChangeController : IController
    {
        private readonly VatProfileContext _context;
        private readonly VisualElement _parent;
        private readonly VisualElement _tracker = new();

        public VatProfileChangeController(VatProfileContext context, VisualElement parent)
        {
            _context = context;
            _parent = parent;
        }

        public void Deactivate()
        {
            _tracker.RemoveFromHierarchy();
        }

        public void Activate()
        {
            _parent.Add(_tracker);
            _tracker.TrackSerializedObjectValue(_context.SerializedObject, OnChanged);
        }

        private void OnChanged(SerializedObject _)
        {
            _context.Model.Changed.Call();
        }
    }
}
