using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    sealed class VatAssetContentsController : IController
    {
        readonly VatAsset _asset;
        readonly VatAssetContentsContainer _container;

        public VatAssetContentsController(VatAsset asset, VisualElement parent)
        {
            _asset = asset;
            _container = parent.CreateContainer<VatAssetContentsContainer>();
        }

        public void Activate() => _container.Show(_asset);

        public void Deactivate() => _container.DestroyView();
    }
}
