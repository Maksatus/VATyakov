using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    internal sealed class VatAssetOverviewController : IController
    {
        private readonly VatAsset _asset;
        private readonly VatAssetOverviewContainer _container;

        public VatAssetOverviewController(VatAsset asset, VisualElement parent)
        {
            _asset = asset;
            _container = parent.CreateContainer<VatAssetOverviewContainer>();
        }

        public void Deactivate()
        {
            _container.DestroyView();
        }

        public void Activate()
        {
            _container.Summary.Show(_asset);
        }
    }
}
