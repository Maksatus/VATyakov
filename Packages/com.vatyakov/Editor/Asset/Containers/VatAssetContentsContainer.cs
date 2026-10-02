namespace VATyakov.Editor
{
    internal sealed class VatAssetContentsContainer : EditorContainer
    {
        private readonly VatObjectLink _mesh = new("Mesh");
        private readonly VatObjectLink _position = new("Position Texture");
        private readonly VatObjectLink _rotation = new("Rotation Texture");
        private readonly VatObjectLink _drift = new("Drift Texture");

        public VatAssetContentsContainer()
        {
            Root.Add(VatUi.Foldout("Contents", "VatAsset.ContentsFoldout").WithElements(
                VatUi.Hint("Created by the baker. Do not edit or reimport the mesh and textures by hand."),
                _mesh,
                _position,
                _rotation,
                _drift));
        }

        public void Show(VatAsset asset)
        {
            _mesh.Set(asset.Mesh);
            _position.Set(asset.PositionTexture);
            _rotation.Set(asset.RotationTexture);
            _drift.Set(asset.DriftTexture);
        }
    }
}
