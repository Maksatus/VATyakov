using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    internal sealed class VatAssetErrorController : IVatController
    {
        private readonly VatAssetContext _context;
        private readonly VatAssetErrorContainer _container;

        public VatAssetErrorController(VatAssetContext context, VisualElement parent)
        {
            _context = context;
            _container = parent.CreateContainer<VatAssetErrorContainer>();
        }

        public void Deactivate()
        {
            _container.DestroyView();
        }

        public void Activate()
        {
            _context.Asset.TryValidate(out var error);
            _container.Box.SetMessage(error);
        }
    }
}
