namespace VATyakov.Editor
{
    internal sealed class VatAssetContentsContainer : VatEditorContainer
    {
        public readonly VatObjectLink Mesh = new("Mesh");
        public readonly VatObjectLink PositionTexture = new("Position Texture");
        public readonly VatObjectLink RotationTexture = new("Rotation Texture");
        public readonly VatObjectLink BoneTexture = new("Bone Texture");

        public VatAssetContentsContainer()
        {
            Root.Add(VatUi.Foldout("Contents", "VatAsset.ContentsFoldout").WithElements(
                VatUi.Hint("Created by the baker. Do not edit or reimport the mesh and textures by hand."),
                Mesh,
                PositionTexture,
                RotationTexture,
                BoneTexture));
        }
    }
}
