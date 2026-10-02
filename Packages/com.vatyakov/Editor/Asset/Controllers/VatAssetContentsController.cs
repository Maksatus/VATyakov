using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    internal sealed class VatAssetContentsController : IController
    {
        private readonly VatAsset _asset;
        private readonly VatAssetContentsContainer _container;

        public VatAssetContentsController(VatAsset asset, VisualElement parent)
        {
            _asset = asset;
            _container = parent.CreateContainer<VatAssetContentsContainer>();
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
