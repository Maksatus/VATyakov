using UnityEngine;
using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    internal sealed class VatAssetMemoryController : IVatController
    {
        private const string MeshHint = "Vertex and index buffers. Bone and Rigid keep the bones or the piece of every vertex in the mesh.";
        private const string TotalHint =
            "Mesh and textures as the GPU holds them in a build. Profile a build: the editor may keep a CPU copy and show twice as much.";
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
            var memory = new VatAssetMemory(asset);
            _container.Mesh.Set(VatText.Bytes(memory.Mesh), MeshHint);
            ShowTexture(_container.PositionTexture, asset.PositionTexture);
            ShowTexture(_container.RotationTexture, asset.RotationTexture);
            ShowTexture(_container.BoneTexture, asset.Mode == VatMode.Bone ? asset.BoneTexture : null);
            ShowTexture(_container.PieceTexture, asset.Mode == VatMode.Rigid ? asset.BoneTexture : null);
            _container.Padding.SetVisible(memory.PaddingTexels > 0);
            _container.Padding.Set(VatText.Padding(memory), PaddingHint);
            _container.Total.Set(VatText.Bytes(memory.Total), TotalHint);
        }

        private static void ShowTexture(VatInfoRow row, Texture2D texture)
        {
            row.SetVisible(texture != null);
            if (texture != null)
            {
                row.Set(VatText.Bytes(VatMemory.Bytes(texture)), $"{VatText.TextureMemory(texture)}, {texture.graphicsFormat}");
            }
        }
    }
}
