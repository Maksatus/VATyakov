using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    internal sealed class VatSourceFieldsController : IController
    {
        private readonly VatProfileContext _context;
        private readonly VatSourceFieldsContainer _container;

        public VatSourceFieldsController(VatProfileContext context, VisualElement parent)
        {
            _context = context;
            _container = parent.CreateContainer<VatSourceFieldsContainer>();
        }

        public void Deactivate()
        {
            _context.Model.Changed.OnCall -= Refresh;
            _container.Root.Unbind();
            _container.DestroyView();
        }

        public void Activate()
        {
            _container.Root.Bind(_context.SerializedObject);
            _context.Model.Changed.OnCall += Refresh;
            Refresh();
        }

        private void Refresh()
        {
            var alembic = _context.Profile.Kind == VatSourceKind.Alembic;
            _container.Skinned.SetVisible(!alembic);
            _container.Alembic.SetVisible(alembic);
        }
    }
}
