using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    sealed class VatAnimatorClipContainer : EditorContainer
    {
        public readonly PopupField<string> Clip = new PopupField<string>("Clip")
        {
            tooltip = "Clip Play On Enable starts. Default follows the default clip of the VAT asset.",
        };

        public VatAnimatorClipContainer()
        {
            Clip.AddToClassList(BaseField<string>.alignedFieldUssClassName);
            Root.WithElements(new PropertyField { bindingPath = "_asset", label = "VAT Asset" }, Clip);
        }

        // Choice 0 is the asset's default clip; a name the asset lost stays visible, so it is not replaced silently.
        public void Show(VatAsset asset, string clip)
        {
            var choices = new List<string> { DefaultChoice(asset) };
            if (asset != null)
                foreach (var c in asset.Clips)
                    choices.Add(c.Name);
            if (!string.IsNullOrEmpty(clip) && !choices.Contains(clip))
                choices.Add(clip);

            Clip.choices = choices;
            Clip.SetValueWithoutNotify(string.IsNullOrEmpty(clip) ? choices[0] : clip);
            Clip.SetEnabled(asset != null);
        }

        static string DefaultChoice(VatAsset asset) =>
            asset != null && asset.TryValidate(out _) ? $"Default ({asset.Clips[asset.DefaultClipIndex].Name})" : "Default";
    }
}
