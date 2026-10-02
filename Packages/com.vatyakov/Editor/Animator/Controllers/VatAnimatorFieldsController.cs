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
        }
    }
}
