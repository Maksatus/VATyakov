using UnityEditor;
using UnityEngine;

namespace VATyakov.Editor
{
    internal static class VatFrameField
    {
        private static readonly GUIContent _label = new("Frame",
            "Frame the template material shows. A fractional value blends two neighbouring frames. " +
            "In Play mode a script sets the frame.");

        public static void Draw(Material material, VatClip clip)
        {
            var frame = material.GetVector(VatShaderIds.Frame);
            var position = frame.x - clip.StartRow + frame.z;
            EditorGUI.BeginChangeCheck();
            position = EditorGUILayout.Slider(_label, position, 0f, clip.FrameCount - 1);
            if (EditorGUI.EndChangeCheck())
            {
                Write(material, clip.Frame(position));
            }
        }

        private static void Write(Material material, Vector4 frame)
        {
            Undo.RecordObject(material, "VAT: Frame");
            material.SetVector(VatShaderIds.Frame, frame);
            EditorUtility.SetDirty(material);
        }
    }
}
