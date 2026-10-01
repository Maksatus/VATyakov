namespace VATyakov.Editor
{
    sealed class VatAssetContentsContainer : EditorContainer
    {
        readonly VatObjectLink _mesh = new VatObjectLink("Меш");
        readonly VatObjectLink _position = new VatObjectLink("Текстура позиций");

        public VatAssetContentsContainer()
        {
            Root.Add(VatUi.Foldout("Состав", "VatAsset.ContentsFoldout").WithElements(
                VatUi.Hint("Создаётся бейкером. Меш и текстуру не редактировать и не переимпортировать вручную."),
                _mesh,
                _position));
        }

        public void Show(VatAsset asset)
        {
            _mesh.Set(asset.Mesh);
            _position.Set(asset.PositionTexture);
        }
    }
}
