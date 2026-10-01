using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    sealed class VatSourceFieldsController : IController
    {
        readonly VatProfileContext _context;
        readonly VatSourceFieldsContainer _container;

        public VatSourceFieldsController(VatProfileContext context, VisualElement parent)
        {
            _context = context;
            _container = parent.CreateContainer<VatSourceFieldsContainer>();
        }

        public void Activate()
        {
            _container.Root.Bind(_context.SerializedObject);
            _context.Model.Changed.OnCall += Refresh;
            Refresh();
        }

        public void Deactivate()
        {
            _context.Model.Changed.OnCall -= Refresh;
            _container.Root.Unbind();
            _container.DestroyView();
        }

        void Refresh()
        {
            bool alembic = _context.Profile.Kind == VatSourceKind.Alembic;
            _container.Skinned.SetVisible(!alembic);
            _container.Alembic.SetVisible(alembic);
        }
    }
}
