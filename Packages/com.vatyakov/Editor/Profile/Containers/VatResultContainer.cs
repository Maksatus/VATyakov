using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    internal sealed class VatResultContainer : VatEditorContainer
    {
        public readonly Label NotBaked = VatUi.Hint("Not baked yet.");
        public readonly VatObjectLink Asset = new("VAT Asset", "VAT asset: mesh and animation textures.");
        public readonly VatObjectLink Material = new("Material", "Template material with this animation.");
        public readonly HelpBox Invalid = VatUi.HelpBox(HelpBoxMessageType.Warning);
        public readonly HelpBox Outdated = VatUi.HelpBox(HelpBoxMessageType.Warning);
        public readonly VatAssetSummaryContainer Summary = new();

        public VatResultContainer()
        {
            Root.WithElements(NotBaked, Asset, Material, Invalid, Outdated, Summary.Root);
        }
    }
}
