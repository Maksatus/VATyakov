using UnityEditor;
using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    static class VatUi
    {
        const string StyleSheetPath = "Packages/com.vatyakov/Editor/Ui/VatEditor.uss";

        public static VisualElement Root()
        {
            var root = new VisualElement().WithClass("vat-root");
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(StyleSheetPath);
            if (styleSheet != null)
                root.styleSheets.Add(styleSheet);
            return root;
        }

        public static VisualElement Card() => new VisualElement().WithClass("vat-card");

        public static VisualElement Card(string title) => Card().WithElements(Title(title));

        public static Label Title(string text) => new Label(text).WithClass("vat-card__title");

        public static Label Hint(string text = "") => new Label(text).WithClass("vat-hint");

        public static Foldout Foldout(string title, string viewDataKey) =>
            new Foldout { text = title, value = false, viewDataKey = viewDataKey }.WithClass("vat-foldout");

        public static HelpBox HelpBox(HelpBoxMessageType type) => new HelpBox(string.Empty, type);

        public static Button PrimaryButton(string text) => new Button { text = text }.WithClass("vat-primary-button");

        public static Button SecondaryButton() => new Button().WithClass("vat-secondary-button");

        public static VisualElement Stats() => new VisualElement().WithClass("vat-stats");

        public static void SetMessage(this HelpBox box, string text)
        {
            box.text = text;
            box.SetVisible(!string.IsNullOrEmpty(text));
        }
    }
}
