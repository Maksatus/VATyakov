using UnityEditor;
using UnityEngine.UIElements;

namespace Kefir.Vat.Editor
{
    /// <summary>
    /// Read-only VatAsset inspector: everything in the asset is written by the baker, so nothing is editable here.
    /// Shows what the asset holds, its size and the profile that bakes it.
    /// </summary>
    [CustomEditor(typeof(VatAsset))]
    sealed class VatAssetEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var asset = (VatAsset)target;
            var root = VatEditorUI.Root();

            if (!asset.TryValidate(out var error))
            {
                root.Add(new HelpBox(error, HelpBoxMessageType.Error));
                AddProfileLink(root, asset);
                return root;
            }

            var info = asset.Layout;
            var overview = VatEditorUI.Card(root, "Анимация");
            var stats = VatEditorUI.Stats(overview);
            VatEditorUI.Stat(stats, "Вертексы", VatEditorUI.Number(info.Elements));
            VatEditorUI.Stat(stats, "Текстура", VatEditorUI.Size(info.Width, info.Height),
                info.Blocks > 1 ? $"Меш разбит на {info.Blocks} блока по ширине." : null);
            VatEditorUI.Stat(stats, "Память", VatEditorUI.Megabytes(VatBaker.PositionTextureBytes(info)),
                "Фактический размер текстуры в памяти, включая пустые тексели последнего блока.");

            var clips = VatEditorUI.Card(root, asset.Clips.Count == 1 ? "Клип" : "Клипы");
            foreach (var clip in asset.Clips)
                VatEditorUI.ClipRow(clips, clip);

            AddProfileLink(root, asset);

            var contents = VatEditorUI.Foldout(root, "Состав", "VatAsset.ContentsFoldout");
            VatEditorUI.Hint(contents, "Создаётся бейкером. Меш и текстуру не редактировать и не переимпортировать вручную.");
            VatEditorUI.ObjectLink(contents, "Меш", asset.Mesh);
            VatEditorUI.ObjectLink(contents, "Текстура позиций", asset.PositionTexture);
            return root;
        }

        static void AddProfileLink(VisualElement root, VatAsset asset)
        {
            var card = VatEditorUI.Card(root, null);
            var profile = FindProfile(asset);
            VatEditorUI.ObjectLink(card, "Профиль бейка", profile, "Здесь меняются настройки и запускается повторный бейк.");
            if (profile == null)
                VatEditorUI.Hint(card, "Профиль, который запекает этот ассет, не найден.");
        }

        static VatBakeProfile FindProfile(VatAsset asset)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(VatBakeProfile)))
            {
                var profile = AssetDatabase.LoadAssetAtPath<VatBakeProfile>(AssetDatabase.GUIDToAssetPath(guid));
                if (profile != null && profile.Asset == asset)
                    return profile;
            }
            return null;
        }
    }
}
