using System.Linq;
using UnityEditor;
using UnityEngine;

namespace VATyakov.Editor
{
    internal static class VatClipField
    {
        private static readonly GUIContent _label = new("Clip",
            "Clip the material shows in edit mode. A rebake keeps it while a clip with this name exists. " +
            "In Play mode a script chooses the clip.");

        public static void Draw(Material material, VatMaterialBinding binding)
        {
            var clips = binding.Asset.Clips;
            EditorGUI.BeginChangeCheck();
            var index = EditorGUILayout.Popup(_label, binding.ClipIndex, clips.Select(c => new GUIContent(c.Name)).ToArray());
            if (EditorGUI.EndChangeCheck() && index >= 0)
            {
                Write(material, binding.Asset, index);
            }

            if (binding.ClipIndex >= 0)
            {
                EditorGUILayout.LabelField(" ", VatText.ClipSummary(clips[binding.ClipIndex]), EditorStyles.miniLabel);
            }
        }

        private static void Write(Material material, VatAsset asset, int clipIndex)
        {
            Undo.RecordObject(material, "VAT: Clip");
            asset.ApplyTo(material, clipIndex);
            EditorUtility.SetDirty(material);
        }
    }
}
