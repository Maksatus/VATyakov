namespace VATyakov.Editor
{
    internal sealed class VatAssetMemoryContainer : VatEditorContainer
    {
        public readonly VatInfoRow PositionTexture = new("Position Texture");
        public readonly VatInfoRow RotationTexture = new("Rotation Texture");
        public readonly VatInfoRow BoneTexture = new("Bone Texture");
        public readonly VatInfoRow PieceTexture = new("Piece Texture");
        public readonly VatInfoRow Padding = new("Padding");

        public VatAssetMemoryContainer()
        {
            Root.Add(VatUi.Card("Memory").WithElements(
                VatUi.Hint("Width × height × bytes per texel, as the GPU holds the textures in a build. Profile a build: the editor may keep " +
                    "a CPU copy of any loaded texture and show twice as much."),
                PositionTexture,
                RotationTexture,
                BoneTexture,
                PieceTexture,
                Padding));
        }
    }
}
