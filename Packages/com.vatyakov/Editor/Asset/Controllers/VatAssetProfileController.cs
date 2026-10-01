using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    sealed class VatAssetProfileController : IController
    {
        readonly VatAsset _asset;
        readonly VatAssetProfileContainer _container;

        public VatAssetProfileController(VatAsset asset, VisualElement parent)
        {
            _asset = asset;
            _container = parent.CreateContainer<VatAssetProfileContainer>();
        }

        public void Activate() => _container.Show(VatProfileLookup.Find(_asset));

        public void Deactivate() => _container.DestroyView();
    }
}
