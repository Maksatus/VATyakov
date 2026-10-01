using UnityEditor;
using UnityEngine;

namespace VATyakov.Editor
{
    sealed class VatAnimationSection : IVatMaterialSection
    {
        static readonly GUIContent AnimationLabel = new GUIContent("Анимация", "VAT-ассет, из которого материал берёт анимацию. Клик — показать в Project.");
        static readonly GUIContent ClipLabel = new GUIContent("Клип", "Клип, который играет материал-шаблон.");

        public string Key => "VATyakov.ShaderGUI.Animation";

        public string Title => "Анимация";

        public void Draw(MaterialEditor editor, MaterialProperty[] properties)
        {
            if (editor.targets.Length > 1)
                EditorGUILayout.HelpBox("Выбрано несколько материалов — анимация показывается для одного.", MessageType.Info);
            else
                Draw((Material)editor.target);
        }

        static void Draw(Material material)
        {
            var binding = VatMaterialBinding.Read(material);
            switch (binding.Status)
            {
                case VatMaterialStatus.NotAssigned:
                    EditorGUILayout.HelpBox("Анимация не назначена. Укажите этот материал как «Материал-шаблон» в профиле бейка и нажмите «Запечь».",
                        MessageType.Info);
                    break;
                case VatMaterialStatus.Foreign:
                    EditorGUILayout.HelpBox("Текстура анимации не из VAT-ассета или ассет повреждён. Перезапеките профиль.", MessageType.Warning);
                    break;
                default:
                    DrawBound(material, binding);
                    break;
            }
        }

        static void DrawBound(Material material, VatMaterialBinding binding)
        {
            VatObjectLinkField.Draw(AnimationLabel, binding.Asset);
            DrawClip(binding);
            if (binding.ClipIndex >= 0)
                VatFrameField.Draw(material, binding.Asset.Clips[binding.ClipIndex]);
            if (binding.IsStale)
                DrawStale(material, binding);
        }

        static void DrawClip(VatMaterialBinding binding)
        {
            var rect = EditorGUI.PrefixLabel(EditorGUILayout.GetControlRect(), ClipLabel);
            EditorGUI.LabelField(rect, ClipText(binding));
        }

        static string ClipText(VatMaterialBinding binding)
        {
            if (binding.ClipIndex < 0)
                return "—";
            var clip = binding.Asset.Clips[binding.ClipIndex];
            return clip.Name + "   " + VatText.ClipSummary(clip);
        }

        static void DrawStale(Material material, VatMaterialBinding binding)
        {
            EditorGUILayout.HelpBox("Данные анимации в материале не совпадают с ассетом.", MessageType.Warning);
            if (GUILayout.Button("Обновить из ассета"))
                Reapply(material, binding);
        }

        static void Reapply(Material material, VatMaterialBinding binding)
        {
            Undo.RecordObject(material, "Обновить VAT-материал");
            binding.Asset.ApplyTo(material, Mathf.Max(binding.ClipIndex, 0));
            EditorUtility.SetDirty(material);
        }
    }
}
