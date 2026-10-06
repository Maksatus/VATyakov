using UnityEngine;
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
            Show(_container.PositionTexture, asset.PositionTexture);
            Show(_container.RotationTexture, asset.RotationTexture);
            Show(_container.BoneTexture, asset.Mode == VatMode.Bone ? asset.BoneTexture : null);
            Show(_container.PieceTexture, asset.Mode == VatMode.Rigid ? asset.BoneTexture : null);
        }

        private static void Show(VatObjectLink link, Texture2D texture)
        {
            link.SetVisible(texture != null);
            link.Set(texture);
        }
    }
}
