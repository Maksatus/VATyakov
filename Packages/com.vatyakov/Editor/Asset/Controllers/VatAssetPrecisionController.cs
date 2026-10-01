using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    sealed class VatAssetPrecisionController : IController
    {
        readonly VatAsset _asset;
        readonly VatAssetPrecisionContainer _container;

        public VatAssetPrecisionController(VatAsset asset, VisualElement parent)
        {
            _asset = asset;
            _container = parent.CreateContainer<VatAssetPrecisionContainer>();
        }

        public void Activate() => _container.Show(_asset);

        public void Deactivate() => _container.DestroyView();
    }
}
