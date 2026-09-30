using UnityEditor;
using UnityEngine;

namespace Kefir.Vat.Editor
{
    /// <summary>
    /// Material inspector of the VAT shaders (Shader Graph → Custom Editor GUI). Artists see the surface inputs
    /// and which animation the material plays; the VAT properties themselves are baker data and stay hidden
    /// (they are also Hide In Inspector in the SubGraph). GPU instancing is kept off (§1.6).
    /// </summary>
    public sealed class VatShaderGUI : ShaderGUI
    {
        const string VatPrefix = "_Vat";
        const string SurfaceKey = "Kefir.Vat.ShaderGUI.Surface";
        const string AnimationKey = "Kefir.Vat.ShaderGUI.Animation";
        const string AdvancedKey = "Kefir.Vat.ShaderGUI.Advanced";

        static readonly GUIContent BaseMapLabel = new GUIContent("Текстура", "Основная текстура и цвет, на который она умножается.");
        static readonly GUIContent AnimationLabel = new GUIContent("Анимация", "VAT-ассет, из которого материал берёт анимацию. Клик — показать в Project.");
        static readonly GUIContent ClipLabel = new GUIContent("Клип", "Клип, который играет материал-шаблон.");

        public override void OnGUI(MaterialEditor editor, MaterialProperty[] properties)
        {
            if (Section(SurfaceKey, "Поверхность"))
                DrawSurface(editor, properties);
            if (Section(AnimationKey, "Анимация"))
                DrawAnimation(editor);
            if (Section(AdvancedKey, "Дополнительно"))
                editor.RenderQueueField();
        }

        public override void ValidateMaterial(Material material)
        {
            // State lives in per-unit materials under the SRP Batcher; instancing would split batches for nothing.
            material.enableInstancing = false;
        }

        static bool Section(string key, string title)
        {
            bool expanded = SessionState.GetBool(key, true);
            bool next = EditorGUILayout.BeginFoldoutHeaderGroup(expanded, title);
            EditorGUILayout.EndFoldoutHeaderGroup();
            if (next != expanded)
                SessionState.SetBool(key, next);
            if (next)
                EditorGUILayout.Space(2);
            return next;
        }

        static void DrawSurface(MaterialEditor editor, MaterialProperty[] properties)
        {
            var baseMap = FindProperty("_BaseMap", properties, false);
            var baseColor = FindProperty("_BaseColor", properties, false);
            if (baseMap != null)
                editor.TexturePropertySingleLine(BaseMapLabel, baseMap, baseColor);
            else if (baseColor != null)
                editor.ShaderProperty(baseColor, "Цвет");

            foreach (var property in properties)
            {
                if (property == baseMap || property == baseColor || !IsArtistFacing(property))
                    continue;
                editor.ShaderProperty(property, property.displayName);
            }
            EditorGUILayout.Space();
        }

        static bool IsArtistFacing(MaterialProperty property) =>
            !property.name.StartsWith(VatPrefix, System.StringComparison.Ordinal) &&
            (property.propertyFlags & UnityEngine.Rendering.ShaderPropertyFlags.HideInInspector) == 0;

        static void DrawAnimation(MaterialEditor editor)
        {
            if (editor.targets.Length > 1)
            {
                EditorGUILayout.HelpBox("Выбрано несколько материалов — анимация показывается для одного.", MessageType.Info);
                EditorGUILayout.Space();
                return;
            }

            var material = (Material)editor.target;
            var texture = material.GetTexture(VatShaderIds.PosTex);
            if (texture == null)
            {
                EditorGUILayout.HelpBox("Анимация не назначена. Укажите этот материал как «Материал-шаблон» в профиле бейка и нажмите «Запечь».",
                    MessageType.Info);
                EditorGUILayout.Space();
                return;
            }

            var asset = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GetAssetPath(texture)) as VatAsset;
            if (asset == null || !asset.TryValidate(out _))
            {
                EditorGUILayout.HelpBox("Текстура анимации не из VAT-ассета или ассет повреждён. Перезапеките профиль.", MessageType.Warning);
                EditorGUILayout.Space();
                return;
            }

            ObjectLink(AnimationLabel, asset);

            int clipIndex = FindClip(asset, material.GetVector(VatShaderIds.ClipA));
            var clipRect = EditorGUI.PrefixLabel(EditorGUILayout.GetControlRect(), ClipLabel);
            EditorGUI.LabelField(clipRect, clipIndex >= 0
                ? asset.Clips[clipIndex].Name + "   " + VatEditorUI.ClipSummary(asset.Clips[clipIndex])
                : "—");

            bool stale = clipIndex < 0 || texture != asset.PositionTexture ||
                material.GetVector(VatShaderIds.Layout) != asset.Layout.ShaderLayout;
            if (stale)
            {
                EditorGUILayout.HelpBox("Данные анимации в материале не совпадают с ассетом.", MessageType.Warning);
                if (GUILayout.Button("Обновить из ассета"))
                {
                    Undo.RecordObject(material, "Обновить VAT-материал");
                    asset.ApplyTo(material, Mathf.Max(clipIndex, 0));
                    EditorUtility.SetDirty(material);
                }
            }
            EditorGUILayout.Space();
        }

        static int FindClip(VatAsset asset, Vector4 state)
        {
            if (!float.IsFinite(state.x) || Mathf.Abs(state.x) < 1f)
                return -1;
            VatMath.UnpackClip(state.x, out int startRow, out int frameCount, out bool loop);
            for (int i = 0; i < asset.Clips.Count; i++)
            {
                var clip = asset.Clips[i];
                if (clip.StartRow == startRow && clip.FrameCount == frameCount && clip.Loop == loop)
                    return i;
            }
            return -1;
        }

        /// <summary>Read-only object field: shows the object, pings it on click, never reassigns.</summary>
        static void ObjectLink(GUIContent label, Object target)
        {
            var rect = EditorGUI.PrefixLabel(EditorGUILayout.GetControlRect(), label);
            if (GUI.Button(rect, EditorGUIUtility.ObjectContent(target, target.GetType()), EditorStyles.objectField))
                EditorGUIUtility.PingObject(target);
        }
    }
}
