using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    internal sealed class VatAssetProfileController : IVatController
    {
        private const string OutdatedMessage = "Out of date: the source or the bake settings changed after this bake. Rebake it from the profile.";

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
            var asset = _context.Asset;
            var profile = VatProfileLookup.Find(asset);
            _container.Profile.Set(profile);
            _container.Missing.SetVisible(profile == null);
            _container.Outdated.SetMessage(profile != null && VatSourceHash.IsOutdated(profile, asset) ? OutdatedMessage : null);
        }
    }
}
