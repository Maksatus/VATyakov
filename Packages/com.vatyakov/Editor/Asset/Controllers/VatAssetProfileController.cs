using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    internal sealed class VatAssetProfileController : IController
    {
        private readonly VatAsset _asset;
        private readonly VatAssetProfileContainer _container;

        public VatAssetProfileController(VatAsset asset, VisualElement parent)
        {
            _asset = asset;
            _container = parent.CreateContainer<VatAssetProfileContainer>();
        }

        public void Deactivate()
        {
            _container.DestroyView();
        }

        public void Activate()
        {
            _container.Show(VatProfileLookup.Find(_asset));
        }
    }
}
