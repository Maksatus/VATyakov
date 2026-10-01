using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    sealed class VatAssetDefaultClipContainer : EditorContainer
    {
        public readonly PopupField<string> Clip = new PopupField<string>("Default Clip")
        {
            tooltip = "Clip the template material of the bake profile shows in edit mode. Kept after a rebake while a clip with this name exists.",
        };

        public VatAssetDefaultClipContainer()
        {
            Clip.AddToClassList(BaseField<string>.alignedFieldUssClassName);
            Root.Add(VatUi.Card().WithElements(Clip,
                VatUi.Hint("Materials can show another clip: choose it in the material inspector.")));
        }

        public void Show(IReadOnlyList<VatClip> clips, int index)
        {
            var names = new List<string>(clips.Count);
            foreach (var clip in clips)
                names.Add(clip.Name);
            Clip.choices = names;
            Clip.SetValueWithoutNotify(names[index]);
        }
    }
}
