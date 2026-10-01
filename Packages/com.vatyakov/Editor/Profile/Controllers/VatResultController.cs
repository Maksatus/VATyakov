using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    sealed class VatResultController : IController
    {
        readonly VatProfileContext _context;
        readonly VatResultContainer _container;

        public VatResultController(VatProfileContext context, VisualElement parent)
        {
            _context = context;
            _container = parent.CreateContainer<VatResultContainer>();
        }

        public void Activate()
        {
            _context.Model.Changed.OnCall += Refresh;
            Refresh();
        }

        public void Deactivate()
        {
            _context.Model.Changed.OnCall -= Refresh;
            _container.DestroyView();
        }

        void Refresh()
        {
            var profile = _context.Profile;
            var asset = profile.Asset;
            if (asset == null)
                _container.ShowNotBaked();
            else if (!asset.TryValidate(out var error))
                _container.ShowInvalid(asset, profile.Material, error);
            else
                _container.ShowBaked(asset, profile.Material);
        }
    }
}
