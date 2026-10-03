using UnityEditor;
using UnityEngine;

namespace VATyakov.Editor
{
    internal static class VatAnimationFields
    {
        private static readonly GUIContent _assetLabel = new("VAT Asset", "VAT asset the material takes its animation from. Click to ping it in Project.");

        public static void Draw(MaterialEditor editor)
        {
            if (editor.targets.Length > 1)
            {
                EditorGUILayout.HelpBox("Several materials are selected: the animation is shown for one.", MessageType.Info);
            }
            else
            {
                Draw((Material)editor.target);
            }
        }

        private static void Draw(Material material)
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

        private static void DrawBound(Material material, VatMaterialBinding binding)
        {
            VatObjectLinkField.Draw(_assetLabel, binding.Asset);
            VatClipField.Draw(material, binding);
            if (binding.HasClip)
            {
                VatFrameField.Draw(material, binding);
            }

            if (binding.IsStale)
            {
                DrawStale(material, binding);
            }
        }

        private static void DrawStale(Material material, VatMaterialBinding binding)
        {
            EditorGUILayout.HelpBox("Animation data in the material does not match the asset.", MessageType.Warning);
            if (GUILayout.Button("Update from Asset"))
            {
                Reapply(material, binding);
            }
        }

        private static void Reapply(Material material, VatMaterialBinding binding)
        {
            Undo.RecordObject(material, VatUndo.Material);
            binding.Asset.ApplyTo(material, binding.HasClip ? binding.ClipIndex : 0);
            EditorUtility.SetDirty(material);
        }
    }
}
