using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    internal sealed class VatAssetContentsController : IVatController
    {
        private readonly VatAssetContext _context;
        private readonly VatAssetContentsContainer _container;

        public VatAssetContentsController(VatAssetContext context, VisualElement parent)
        {
            _context = context;
            _container = parent.CreateContainer<VatAssetContentsContainer>();
        }

        public void Deactivate()
        {
            _container.DestroyView();
        }

        public void Activate()
        {
            var asset = _context.Asset;
            _container.Mesh.Set(asset.Mesh);
            _container.PositionTexture.Set(asset.PositionTexture);
            _container.RotationTexture.Set(asset.RotationTexture);
        }
    }
}
