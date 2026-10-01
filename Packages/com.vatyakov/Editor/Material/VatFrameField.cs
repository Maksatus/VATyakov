using UnityEditor;
using UnityEngine;

namespace VATyakov.Editor
{
    // Edit mode shows a static frame of the template (§1.6); playback in Play mode is the driver's job.
    static class VatFrameField
    {
        static readonly GUIContent Label = new GUIContent("Кадр",
            "Какой кадр показывает материал-шаблон. Дробное значение — смесь двух соседних кадров. " +
            "В Play mode кадр выставляет скрипт.");

        public static void Draw(Material material, VatClip clip)
        {
            var frame = material.GetVector(VatShaderIds.Frame);
            float position = frame.x - clip.StartRow + frame.z;
            EditorGUI.BeginChangeCheck();
            position = EditorGUILayout.Slider(Label, position, 0f, clip.FrameCount - 1);
            if (EditorGUI.EndChangeCheck())
                Write(material, clip.Frame(position));
        }

        static void Write(Material material, Vector4 frame)
        {
            Undo.RecordObject(material, "VAT: кадр");
            material.SetVector(VatShaderIds.Frame, frame);
            EditorUtility.SetDirty(material);
        }
    }
}
