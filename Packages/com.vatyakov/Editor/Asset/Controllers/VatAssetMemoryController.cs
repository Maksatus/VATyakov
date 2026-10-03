using UnityEngine;
using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    internal sealed class VatAssetMemoryController : IVatController
    {
        private const string PaddingHint = "Empty texels at the end of the last block: the blocks share one width, the vertices may not fill it.";

        private readonly VatAssetContext _context;
        private readonly VatAssetMemoryContainer _container;

        public VatAssetMemoryController(VatAssetContext context, VisualElement parent)
        {
            _context = context;
            _container = parent.CreateContainer<VatAssetMemoryContainer>();
        }

        public void Deactivate()
        {
            _container.DestroyView();
        }

        public void Activate()
        {
            var asset = _context.Asset;
            ShowTexture(_container.PositionTexture, asset.PositionTexture);
            ShowTexture(_container.RotationTexture, asset.RotationTexture);
            ShowTexture(_container.BoneTexture, asset.BoneTexture);
            _container.Padding.Set(VatText.Padding(new VatAssetMemory(asset)), PaddingHint);
        }

        private static void ShowTexture(VatInfoRow row, Texture2D texture)
        {
            row.SetVisible(texture != null);
            if (texture != null)
            {
                row.Set(VatText.TextureMemory(texture), texture.graphicsFormat.ToString());
            }
        }
    }
}
