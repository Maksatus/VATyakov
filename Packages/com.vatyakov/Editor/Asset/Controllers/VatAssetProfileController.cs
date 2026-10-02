using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    internal sealed class VatAssetProfileController : IVatController
    {
        private readonly VatAssetContext _context;
        private readonly VatAssetProfileContainer _container;

        public VatAssetProfileController(VatAssetContext context, VisualElement parent)
        {
            _context = context;
            _container = parent.CreateContainer<VatAssetProfileContainer>();
        }

        public void Deactivate()
        {
            _container.DestroyView();
        }

        public void Activate()
        {
            var profile = VatProfileLookup.Find(_context.Asset);
            _container.Profile.Set(profile);
            _container.Missing.SetVisible(profile == null);
        }
    }
}
