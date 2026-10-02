using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    internal sealed class VatAssetPrecisionController : IController
    {
        private readonly VatAsset _asset;
        private readonly VatAssetPrecisionContainer _container;

        public VatAssetPrecisionController(VatAsset asset, VisualElement parent)
        {
            _asset = asset;
            _container = parent.CreateContainer<VatAssetPrecisionContainer>();
        }

        public void Deactivate()
        {
            _container.DestroyView();
        }

        public void Activate()
        {
            _container.Show(_asset);
        }
    }
}
