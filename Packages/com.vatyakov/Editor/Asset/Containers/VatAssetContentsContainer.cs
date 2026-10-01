namespace VATyakov.Editor
{
    sealed class VatAssetContentsContainer : EditorContainer
    {
        readonly VatObjectLink _mesh = new VatObjectLink("Mesh");
        readonly VatObjectLink _position = new VatObjectLink("Position Texture");
        readonly VatObjectLink _rotation = new VatObjectLink("Rotation Texture");
        readonly VatObjectLink _drift = new VatObjectLink("Drift Texture");

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
