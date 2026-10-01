using UnityEditor;
using UnityEngine;

namespace VATyakov.Editor
{
    // §1.6: the default clip goes into the asset and into the template material of its bake profile.
    // Other materials keep the clip chosen in their own inspector.
    static class VatDefaultClip
    {
        public static void Set(VatAsset asset, int clipIndex) => Set(asset, clipIndex, TemplateOf(asset));

        public static void Set(VatAsset asset, int clipIndex, Material template)
        {
            Undo.RecordObjects(template != null ? new Object[] { asset, template } : new Object[] { asset }, "VAT: Default Clip");
            asset.SetDefaultClip(clipIndex);
            EditorUtility.SetDirty(asset);
            if (template == null)
                return;
            asset.ApplyTo(template, clipIndex);
            EditorUtility.SetDirty(template);
        }

        static Material TemplateOf(VatAsset asset)
        {
            var profile = VatProfileLookup.Find(asset);
            return profile != null ? profile.Material : null;
        }
    }
}
