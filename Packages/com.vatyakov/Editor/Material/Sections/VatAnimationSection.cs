using UnityEditor;
using UnityEngine;

namespace VATyakov.Editor
{
    sealed class VatAnimationSection : IVatMaterialSection
    {
        static readonly GUIContent AnimationLabel = new GUIContent("Animation", "VAT asset the material takes its animation from. Click to ping it in Project.");
        public string Key => "VATyakov.ShaderGUI.Animation";

        public string Title => "Animation";

        public void Draw(MaterialEditor editor, MaterialProperty[] properties)
        {
            if (editor.targets.Length > 1)
                EditorGUILayout.HelpBox("Several materials are selected: the animation is shown for one.", MessageType.Info);
            else
                Draw((Material)editor.target);
        }

        static void Draw(Material material)
        {
            var binding = VatMaterialBinding.Read(material);
            switch (binding.Status)
            {
                case VatMaterialStatus.NotAssigned:
                    EditorGUILayout.HelpBox("No animation assigned. Set this material as the Template Material of a bake profile and press Bake.",
                        MessageType.Info);
                    break;
                case VatMaterialStatus.Foreign:
                    EditorGUILayout.HelpBox("The animation texture is not from a VAT asset or the asset is broken. Rebake the profile.", MessageType.Warning);
                    break;
                default:
                    DrawBound(material, binding);
                    break;
            }
        }

        static void DrawBound(Material material, VatMaterialBinding binding)
        {
            VatObjectLinkField.Draw(AnimationLabel, binding.Asset);
            VatClipField.Draw(material, binding);
            if (binding.ClipIndex >= 0)
                VatFrameField.Draw(material, binding.Asset.Clips[binding.ClipIndex]);
            if (binding.IsStale)
                DrawStale(material, binding);
        }

        static void DrawStale(Material material, VatMaterialBinding binding)
        {
            EditorGUILayout.HelpBox("Animation data in the material does not match the asset.", MessageType.Warning);
            if (GUILayout.Button("Update from Asset"))
                Reapply(material, binding);
        }

        static void Reapply(Material material, VatMaterialBinding binding)
        {
            Undo.RecordObject(material, "Update VAT Material");
            binding.Asset.ApplyTo(material, Mathf.Max(binding.ClipIndex, 0));
            EditorUtility.SetDirty(material);
        }
    }
}
