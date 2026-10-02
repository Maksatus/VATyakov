using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    internal sealed class VatAnimatorClipContainer : EditorContainer
    {
        public readonly PopupField<string> Clip = new("Clip")
        {
            tooltip = "Clip Play On Enable starts. First follows the first clip of the VAT asset.",
        };

        public VatAnimatorClipContainer()
        {
            Clip.AddToClassList(BaseField<string>.alignedFieldUssClassName);
            Root.WithElements(new PropertyField { bindingPath = "_asset", label = "VAT Asset" }, Clip);
        }

        public void Show(VatAsset asset, string clip)
        {
            var choices = new List<string> { FirstChoice(asset) };
            if (asset != null)
            {
                foreach (var c in asset.Clips)
                {
                    choices.Add(c.Name);
                }
            }

            if (!string.IsNullOrEmpty(clip) && !choices.Contains(clip))
            {
                choices.Add(clip);
            }

            Clip.choices = choices;
            Clip.SetValueWithoutNotify(string.IsNullOrEmpty(clip) ? choices[0] : clip);
            Clip.SetEnabled(asset != null);
        }

        private static string FirstChoice(VatAsset asset)
        {
            return asset != null && asset.TryValidate(out _) ? $"First ({asset.Clips[0].Name})" : "First";
        }
    }
}
