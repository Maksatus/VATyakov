using System.Linq;
using UnityEditor;
using UnityEngine;

namespace VATyakov.Editor
{
    internal static class VatClipField
    {
        private const int NoSelection = -1;

        private static readonly GUIContent _label = new("Clip",
            "Clip the material shows in edit mode. A rebake keeps it while a clip with this name exists. " +
            "In Play mode a script chooses the clip.");

        public static void Draw(Material material, VatMaterialBinding binding)
        {
            var clips = binding.Asset.Clips;
            var selected = binding.HasClip ? binding.ClipIndex : NoSelection;
            EditorGUI.BeginChangeCheck();
            var clipIndex = EditorGUILayout.Popup(_label, selected, clips.Select(clip => new GUIContent(clip.Name)).ToArray());
            if (EditorGUI.EndChangeCheck())
            {
                Write(material, binding.Asset, clipIndex);
            }

            if (binding.HasClip)
            {
                EditorGUILayout.LabelField(" ", VatText.ClipSummary(clips[binding.ClipIndex]), EditorStyles.miniLabel);
            }
        }

        private static void Write(Material material, VatAsset asset, int clipIndex)
        {
            Undo.RecordObject(material, VatUndo.Clip);
            asset.ApplyTo(material, clipIndex);
            EditorUtility.SetDirty(material);
        }
    }
}
