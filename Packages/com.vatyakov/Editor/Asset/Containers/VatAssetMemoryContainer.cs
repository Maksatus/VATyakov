namespace VATyakov.Editor
{
    internal sealed class VatAssetMemoryContainer : VatEditorContainer
    {
        public readonly VatInfoRow Mesh = new("Mesh");
        public readonly VatInfoRow PositionTexture = new("Position Texture");
        public readonly VatInfoRow RotationTexture = new("Rotation Texture");
        public readonly VatInfoRow BoneTexture = new("Bone Texture");
        public readonly VatInfoRow PieceTexture = new("Piece Texture");
        public readonly VatInfoRow Padding = new("Padding");
        public readonly VatInfoRow Total = new("Total");

        public VatAssetMemoryContainer()
        {
            Root.Add(VatUi.Card("Memory").WithElements(Mesh, PositionTexture, RotationTexture, BoneTexture, PieceTexture, Padding, Total));
        }
    }
}
