using System.Linq;
using UnityEditor;
using UnityEngine;

namespace VATyakov.Editor
{
    // Switches the clip this material shows in edit mode (§1.6), starting at its frame 0.
    static class VatClipField
    {
        static readonly GUIContent Label = new GUIContent("Clip",
            "Clip the material shows in edit mode. The default clip of the template material is chosen in the VAT asset inspector. " +
            "In Play mode a script chooses the clip.");

        public static void Draw(Material material, VatMaterialBinding binding)
        {
            var clips = binding.Asset.Clips;
            EditorGUI.BeginChangeCheck();
            int index = EditorGUILayout.Popup(Label, binding.ClipIndex, clips.Select(c => new GUIContent(c.Name)).ToArray());
            if (EditorGUI.EndChangeCheck() && index >= 0)
                Write(material, binding.Asset, index);
            if (binding.ClipIndex >= 0)
                EditorGUILayout.LabelField(" ", VatText.ClipSummary(clips[binding.ClipIndex]), EditorStyles.miniLabel);
        }

        static void Write(Material material, VatAsset asset, int clipIndex)
        {
            Undo.RecordObject(material, "VAT: Clip");
            asset.ApplyTo(material, clipIndex);
            EditorUtility.SetDirty(material);
        }
    }
}
