using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    sealed class VatAssetOverviewController : IController
    {
        readonly VatAsset _asset;
        readonly VatAssetOverviewContainer _container;

        public VatAssetOverviewController(VatAsset asset, VisualElement parent)
        {
            _asset = asset;
            _container = parent.CreateContainer<VatAssetOverviewContainer>();
        }

        public void Activate() => _container.Summary.Show(_asset);

        public void Deactivate() => _container.DestroyView();
    }
}
