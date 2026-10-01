using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    sealed class VatProfileChangeController : IController
    {
        readonly VatProfileContext _context;
        readonly VisualElement _parent;
        readonly VisualElement _tracker = new VisualElement();

        public VatProfileChangeController(VatProfileContext context, VisualElement parent)
        {
            _context = context;
            _parent = parent;
        }

        public void Activate()
        {
            _parent.Add(_tracker);
            _tracker.TrackSerializedObjectValue(_context.SerializedObject, OnChanged);
        }

        public void Deactivate() => _tracker.RemoveFromHierarchy();

        void OnChanged(SerializedObject _) => _context.Model.Changed.Call();
    }
}
