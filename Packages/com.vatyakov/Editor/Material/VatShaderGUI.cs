using UnityEditor;
using UnityEngine;

namespace VATyakov.Editor
{
    // Referenced by name from the Shader Graph Custom Editor GUI field.
    public sealed class VatShaderGUI : ShaderGUI
    {
        readonly IVatMaterialSection[] _sections =
        {
            new VatSurfaceSection(),
            new VatAnimationSection(),
            new VatAdvancedSection(),
        };

        public override void OnGUI(MaterialEditor editor, MaterialProperty[] properties)
        {
            foreach (var section in _sections)
                DrawSection(section, editor, properties);
        }

        // §1.6: per-unit materials under the SRP Batcher, instancing would only split batches.
        public override void ValidateMaterial(Material material) => material.enableInstancing = false;

        static void DrawSection(IVatMaterialSection section, MaterialEditor editor, MaterialProperty[] properties)
        {
            if (!VatFoldoutHeader.Draw(section.Key, section.Title))
                return;
            section.Draw(editor, properties);
            EditorGUILayout.Space();
        }
    }
}
