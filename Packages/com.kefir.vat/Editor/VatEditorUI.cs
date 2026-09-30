using System.Globalization;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kefir.Vat.Editor
{
    /// <summary>Shared UI Toolkit building blocks of the VAT inspectors: cards, stat tiles, object links.</summary>
    static class VatEditorUI
    {
        const string StyleSheetPath = "Packages/com.kefir.vat/Editor/VatEditor.uss";

        public static VisualElement Root()
        {
            var root = new VisualElement();
            root.AddToClassList("vat-root");
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(StyleSheetPath);
            if (styleSheet != null)
                root.styleSheets.Add(styleSheet);
            return root;
        }

        public static VisualElement Card(VisualElement parent, string title)
        {
            var card = new VisualElement();
            card.AddToClassList("vat-card");
            if (!string.IsNullOrEmpty(title))
            {
                var header = new Label(title);
                header.AddToClassList("vat-card__title");
                card.Add(header);
            }
            parent.Add(card);
            return card;
        }

        public static Foldout Foldout(VisualElement parent, string title, string viewDataKey)
        {
            var foldout = new Foldout { text = title, value = false, viewDataKey = viewDataKey };
            foldout.AddToClassList("vat-foldout");
            parent.Add(foldout);
            return foldout;
        }

        public static Label Hint(VisualElement parent, string text)
        {
            var hint = new Label(text);
            hint.AddToClassList("vat-hint");
            parent.Add(hint);
            return hint;
        }

        public static VisualElement Stats(VisualElement parent)
        {
            var grid = new VisualElement();
            grid.AddToClassList("vat-stats");
            parent.Add(grid);
            return grid;
        }

        public static void Stat(VisualElement grid, string name, string value, string tooltip = null)
        {
            var tile = new VisualElement { tooltip = tooltip };
            tile.AddToClassList("vat-stat");
            var valueLabel = new Label(value);
            valueLabel.AddToClassList("vat-stat__value");
            var nameLabel = new Label(name);
            nameLabel.AddToClassList("vat-stat__name");
            tile.Add(valueLabel);
            tile.Add(nameLabel);
            grid.Add(tile);
        }

        /// <summary>Read-only reference: a label and a button that pings the object in the Project window.</summary>
        public static VisualElement ObjectLink(VisualElement parent, string label, Object target, string tooltip = null)
        {
            var row = new VisualElement { tooltip = tooltip };
            row.AddToClassList("vat-link");
            var caption = new Label(label);
            caption.AddToClassList("vat-link__label");
            row.Add(caption);

            var button = new Button(() => EditorGUIUtility.PingObject(target)) { tooltip = "Показать в Project" };
            button.AddToClassList("vat-link__button");
            if (target != null)
            {
                var icon = new Image { image = AssetPreview.GetMiniThumbnail(target) };
                icon.AddToClassList("vat-link__icon");
                button.Add(icon);
            }
            var name = new Label(target != null ? target.name : "—");
            name.AddToClassList("vat-link__name");
            button.Add(name);
            button.SetEnabled(target != null);
            row.Add(button);

            parent.Add(row);
            return row;
        }

        public static void ClipRow(VisualElement parent, VatClip clip)
        {
            var row = new VisualElement();
            row.AddToClassList("vat-clip");
            var name = new Label(clip.Name);
            name.AddToClassList("vat-clip__name");
            var info = new Label(ClipSummary(clip));
            info.AddToClassList("vat-clip__info");
            row.Add(name);
            row.Add(info);
            parent.Add(row);
        }

        /// <summary>"25 кадров · 30 fps · 0.83 с · цикл".</summary>
        public static string ClipSummary(VatClip clip) => string.Format(CultureInfo.InvariantCulture,
            "{0} {1} · {2:0.##} fps · {3:0.##} с · {4}", clip.FrameCount, Frames(clip.FrameCount), clip.FrameRate, clip.Length,
            clip.Loop ? "цикл" : "один раз");

        public static string Frames(int count)
        {
            int mod100 = count % 100, mod10 = count % 10;
            if (mod100 >= 11 && mod100 <= 14)
                return "кадров";
            return mod10 == 1 ? "кадр" : mod10 >= 2 && mod10 <= 4 ? "кадра" : "кадров";
        }

        public static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

        public static string Size(int width, int height) => string.Format(CultureInfo.InvariantCulture, "{0}×{1}", width, height);

        public static string Megabytes(long bytes) => string.Format(CultureInfo.InvariantCulture, "{0:0.##} МБ", bytes / (1024.0 * 1024.0));
    }
}
