using UnityEngine;
using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    sealed class VatResultContainer : EditorContainer
    {
        readonly Label _notBaked = VatUi.Hint("Not baked yet.");
        readonly VatObjectLink _asset = new VatObjectLink("Animation", "VAT asset: mesh and animation textures.");
        readonly VatObjectLink _material = new VatObjectLink("Material", "Template material with this animation.");
        readonly HelpBox _invalid = VatUi.HelpBox(HelpBoxMessageType.Warning);
        readonly VatAssetSummaryContainer _summary = new VatAssetSummaryContainer();

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

        void ShowLinks(VatAsset asset, Material material)
        {
            SetBaked(true);
            _asset.Set(asset);
            _material.Set(material);
        }

        void SetBaked(bool baked)
        {
            _notBaked.SetVisible(!baked);
            _asset.SetVisible(baked);
            _material.SetVisible(baked);
        }
    }
}
