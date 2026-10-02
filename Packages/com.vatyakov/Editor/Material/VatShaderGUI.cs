using UnityEditor;
using UnityEngine;

namespace VATyakov.Editor
{
    public sealed class VatShaderGUI : ShaderGUI
    {
        public override void OnGUI(MaterialEditor editor, MaterialProperty[] properties)
        {
            Header("Surface");
            VatSurfaceFields.Draw(editor, properties);
            Header("Animation");
            VatAnimationFields.Draw(editor);
            Header("Advanced");
            editor.RenderQueueField();
        }

        public override void ValidateMaterial(Material material)
        {
            material.enableInstancing = false;
        }

        private static void Header(string title)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        }
    }
}
