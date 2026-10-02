using UnityEditor;
using UnityEngine.UIElements;

namespace VATyakov.Editor
{
    internal static class VatUi
    {
        private const string StyleSheetPath = "Packages/com.vatyakov/Editor/Ui/VatEditor.uss";

        public static VisualElement Root()
        {
            var root = new VisualElement().WithClass("vat-root");
            root.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(StyleSheetPath));
            return root;
        }

        public static VisualElement Card()
        {
            return new VisualElement().WithClass("vat-card");
        }

        public static VisualElement Card(string title)
        {
            return Card().WithElements(Title(title));
        }

        public static Label Title(string text)
        {
            return new Label(text).WithClass("vat-card__title");
        }

        public static Label Hint(string text = "")
        {
            return new Label(text).WithClass("vat-hint");
        }

        public static Foldout Foldout(string title, string viewDataKey)
        {
            return new Foldout { text = title, value = false, viewDataKey = viewDataKey }.WithClass("vat-foldout");
        }

        public static HelpBox HelpBox(HelpBoxMessageType type)
        {
            return new HelpBox(string.Empty, type);
        }

        public static Button PrimaryButton(string text)
        {
            return new Button { text = text }.WithClass("vat-primary-button");
        }

        public static Button SecondaryButton()
        {
            return new Button().WithClass("vat-secondary-button");
        }

        public static VisualElement Stats()
        {
            return new VisualElement().WithClass("vat-stats");
        }

        public static void SetMessage(this HelpBox box, string text)
        {
            box.text = text;
            box.SetVisible(!string.IsNullOrEmpty(text));
        }
    }
}
