using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Kefir.Vat.Editor
{
    /// <summary>
    /// Artist-facing profile inspector: what to bake up front, the result as read-only links,
    /// the template material and the test prefab folded away.
    /// </summary>
    [CustomEditor(typeof(VatBakeProfile))]
    sealed class VatBakeProfileEditor : UnityEditor.Editor
    {
        Label _estimate;
        HelpBox _problems;
        Button _bake;
        VisualElement _result;
        VisualElement _prefabLink;
        Button _prefabButton;

        VatBakeProfile Profile => (VatBakeProfile)target;

        public override VisualElement CreateInspectorGUI()
        {
            var root = VatEditorUI.Root();

            var source = VatEditorUI.Card(root, "Что запекаем");
            source.Add(Field("_source", "Персонаж"));
            source.Add(Field("_clip", "Анимация"));
            source.Add(Field("_fps", "Кадров в секунду"));
            _estimate = VatEditorUI.Hint(source, string.Empty);

            _problems = new HelpBox(string.Empty, HelpBoxMessageType.Error);
            _problems.AddToClassList("vat-problems");
            root.Add(_problems);

            _bake = new Button(Bake) { text = "Запечь" };
            _bake.AddToClassList("vat-primary-button");
            root.Add(_bake);

            _result = VatEditorUI.Card(root, null); // title is added by RefreshResult

            var advanced = VatEditorUI.Foldout(root, "Материал", "VatBakeProfile.MaterialFoldout");
            VatEditorUI.Hint(advanced, "Материал-шаблон получает текстуру и клип при каждом бейке. " +
                "Если он не задан, первый бейк создаст его рядом с профилем из выбранного шейдера.");
            advanced.Add(Field("_material", "Материал-шаблон"));
            advanced.Add(Field("_shader", "Шейдер"));

            var prefab = VatEditorUI.Foldout(root, "Тестовый префаб", "VatBakeProfile.PrefabFoldout");
            VatEditorUI.Hint(prefab, "MeshFilter + MeshRenderer с материалом-шаблоном — для сцены Compare и быстрой проверки. " +
                "Игровые префабы собираются вручную.");
            _prefabLink = new VisualElement();
            prefab.Add(_prefabLink);
            _prefabButton = new Button(CreatePrefab);
            _prefabButton.AddToClassList("vat-secondary-button");
            prefab.Add(_prefabButton);

            root.TrackSerializedObjectValue(serializedObject, _ => Refresh());
            Refresh();
            return root;
        }

        PropertyField Field(string property, string label) => new PropertyField(serializedObject.FindProperty(property), label);

        void Refresh()
        {
            var profile = Profile;
            var problems = VatBaker.Validate(profile);
            _problems.text = string.Join("\n", problems);
            _problems.style.display = problems.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;

            bool estimateOk = RefreshEstimate(profile);
            _bake.SetEnabled(problems.Count == 0 && estimateOk);

            RefreshResult(profile);

            bool baked = profile.Asset != null && profile.Asset.TryValidate(out _) && profile.Material != null;
            _prefabLink.Clear();
            VatEditorUI.ObjectLink(_prefabLink, "Префаб", profile.Prefab);
            _prefabButton.text = profile.Prefab != null ? "Обновить префаб" : "Создать префаб";
            _prefabButton.SetEnabled(baked);
            _prefabButton.tooltip = baked ? string.Empty : "Сначала запеките профиль.";
        }

        /// <summary>What the bake will produce with the current settings. False when the bake cannot fit.</summary>
        bool RefreshEstimate(VatBakeProfile profile)
        {
            _estimate.RemoveFromClassList("vat-hint--error");
            var mesh = profile.Source != null ? profile.Source.sharedMesh : null;
            if (mesh == null || profile.Clip == null || !(profile.Fps > 0f))
            {
                _estimate.style.display = DisplayStyle.None;
                return true;
            }

            _estimate.style.display = DisplayStyle.Flex;
            try
            {
                var layout = VatLayout.ForVertex(mesh.vertexCount,
                    new[] { new VatLayout.ClipRequest(profile.Clip.name, profile.Clip.length, profile.Fps) });
                var info = layout.Info;
                int frames = layout.Clips[0].FrameCount;
                _estimate.text = $"Получится {VatEditorUI.Number(frames)} {VatEditorUI.Frames(frames)} · текстура " +
                    $"{VatEditorUI.Size(info.Width, info.Height)} · {VatEditorUI.Megabytes(VatBaker.PositionTextureBytes(info))}";
                return true;
            }
            catch (VatBakeException e)
            {
                _estimate.text = e.Message;
                _estimate.AddToClassList("vat-hint--error");
                return false;
            }
        }

        void RefreshResult(VatBakeProfile profile)
        {
            _result.Clear();
            var title = new Label("Результат");
            title.AddToClassList("vat-card__title");
            _result.Add(title);

            var asset = profile.Asset;
            if (asset == null)
            {
                VatEditorUI.Hint(_result, "Ещё не запечено.");
                return;
            }

            VatEditorUI.ObjectLink(_result, "Анимация", asset, "VAT-ассет: меш и текстура анимации.");
            VatEditorUI.ObjectLink(_result, "Материал", profile.Material, "Материал-шаблон с этой анимацией.");
            if (!asset.TryValidate(out var error))
            {
                var help = new HelpBox(error, HelpBoxMessageType.Warning);
                _result.Add(help);
                return;
            }

            var info = asset.Layout;
            VatEditorUI.ClipRow(_result, asset.Clips[0]);
            var stats = VatEditorUI.Stats(_result);
            VatEditorUI.Stat(stats, "Вертексы", VatEditorUI.Number(info.Elements));
            VatEditorUI.Stat(stats, "Текстура", VatEditorUI.Size(info.Width, info.Height),
                info.Blocks > 1 ? $"Меш разбит на {info.Blocks} блока по ширине." : null);
            VatEditorUI.Stat(stats, "Память", VatEditorUI.Megabytes(VatBaker.PositionTextureBytes(info)));
        }

        void Bake()
        {
            try
            {
                VatBaker.Bake(Profile);
            }
            catch (VatBakeException e)
            {
                EditorUtility.DisplayDialog("VAT", e.Message, "OK");
            }

            serializedObject.Update();
            Refresh();
        }

        void CreatePrefab()
        {
            try
            {
                var prefab = VatBaker.CreatePrefab(Profile);
                EditorGUIUtility.PingObject(prefab);
            }
            catch (VatBakeException e)
            {
                EditorUtility.DisplayDialog("VAT", e.Message, "OK");
            }

            serializedObject.Update();
            Refresh();
        }
    }
}
