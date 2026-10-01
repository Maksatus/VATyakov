using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    sealed class VatAssetErrorController : IController
    {
        readonly VatAsset _asset;
        readonly VatAssetErrorContainer _container;

        public VatAssetErrorController(VatAsset asset, VisualElement parent)
        {
            _asset = asset;
            _container = parent.CreateContainer<VatAssetErrorContainer>();
        }

        public void Activate()
        {
            _asset.TryValidate(out var error);
            _container.Box.SetMessage(error);
        }

        public void Deactivate() => _container.DestroyView();
    }
}
