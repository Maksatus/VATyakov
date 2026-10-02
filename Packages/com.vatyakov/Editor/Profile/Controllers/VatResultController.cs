using UnityEngine;
using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    internal sealed class VatResultController : IVatController
    {
        private readonly VatProfileContext _context;
        private readonly VatResultContainer _container;

        public VatResultController(VatProfileContext context, VisualElement parent)
        {
            _context = context;
            _container = parent.CreateContainer<VatResultContainer>();
        }

        public void Deactivate()
        {
            _context.Model.Changed.OnCall -= Refresh;
            _container.DestroyView();
        }

        public void Activate()
        {
            _context.Model.Changed.OnCall += Refresh;
            Refresh();
        }

        private void Refresh()
        {
            var profile = _context.Profile;
            ShowLinks(profile.Asset, profile.Material);
            ShowSummary(profile.Asset);
        }

        private void ShowLinks(VatAsset asset, Material material)
        {
            var isBaked = asset != null;
            _container.NotBaked.SetVisible(!isBaked);
            _container.Asset.SetVisible(isBaked);
            _container.Material.SetVisible(isBaked);
            _container.Asset.Set(asset);
            _container.Material.Set(material);
        }

        private void ShowSummary(VatAsset asset)
        {
            if (asset == null)
            {
                _container.Invalid.SetMessage(null);
                _container.Summary.SetVisible(false);
                return;
            }

            var isValid = asset.TryValidate(out var error);
            _container.Invalid.SetMessage(error);
            _container.Summary.SetVisible(isValid);
            if (isValid)
            {
                VatAssetSummary.Show(_container.Summary, asset);
            }
        }
    }
}
