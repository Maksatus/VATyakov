using UnityEngine;
using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    internal sealed class VatResultContainer : EditorContainer
    {
        private readonly Label _notBaked = VatUi.Hint("Not baked yet.");
        private readonly VatObjectLink _asset = new("Animation", "VAT asset: mesh and animation textures.");
        private readonly VatObjectLink _material = new("Material", "Template material with this animation.");
        private readonly HelpBox _invalid = VatUi.HelpBox(HelpBoxMessageType.Warning);
        private readonly VatAssetSummaryContainer _summary = new();

        public VatResultContainer()
        {
            Root.WithElements(_notBaked, _asset, _material, _invalid, _summary.Root);
        }

        public void ShowNotBaked()
        {
            SetBaked(false);
            _invalid.SetMessage(null);
            _summary.SetVisible(false);
        }

        public void ShowInvalid(VatAsset asset, Material material, string error)
        {
            ShowLinks(asset, material);
            _invalid.SetMessage(error);
            _summary.SetVisible(false);
        }

        public void ShowBaked(VatAsset asset, Material material)
        {
            ShowLinks(asset, material);
            _invalid.SetMessage(null);
            _summary.SetVisible(true);
            _summary.Show(asset);
        }

        private void ShowLinks(VatAsset asset, Material material)
        {
            SetBaked(true);
            _asset.Set(asset);
            _material.Set(material);
        }

        private void SetBaked(bool baked)
        {
            _notBaked.SetVisible(!baked);
            _asset.SetVisible(baked);
            _material.SetVisible(baked);
        }
    }
}
