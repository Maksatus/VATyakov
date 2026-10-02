using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    internal sealed class VatAssetErrorController : IController
    {
        private readonly VatAsset _asset;
        private readonly VatAssetErrorContainer _container;

        public VatAssetErrorController(VatAsset asset, VisualElement parent)
        {
            _asset = asset;
            _container = parent.CreateContainer<VatAssetErrorContainer>();
        }

        public void Deactivate()
        {
            _container.DestroyView();
        }

        public void Activate()
        {
            _asset.TryValidate(out var error);
            _container.Box.SetMessage(error);
        }
    }
}
