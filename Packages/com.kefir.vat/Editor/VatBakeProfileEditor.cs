using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Kefir.Vat.Editor
{
    [CustomEditor(typeof(VatBakeProfile))]
    sealed class VatBakeProfileEditor : UnityEditor.Editor
    {
        HelpBox _problems;
        Button _bake;
        Label _summary;

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            InspectorElement.FillDefaultInspector(root, serializedObject, this);

            _problems = new HelpBox(string.Empty, HelpBoxMessageType.Error);
            root.Add(_problems);

            _bake = new Button(Bake) { text = "Bake" };
            _bake.style.height = 28;
            _bake.style.marginTop = 6;
            root.Add(_bake);

            _summary = new Label();
            _summary.style.whiteSpace = WhiteSpace.Normal;
            _summary.style.marginTop = 4;
            root.Add(_summary);

            root.TrackSerializedObjectValue(serializedObject, _ => Refresh());
            Refresh();
            return root;
        }

        void Refresh()
        {
            var profile = (VatBakeProfile)target;
            var problems = VatBaker.Validate(profile);
            _problems.text = string.Join("\n", problems);
            _problems.style.display = problems.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            _bake.SetEnabled(problems.Count == 0);
            _summary.text = VatBaker.Describe(profile.Asset);
        }

        void Bake()
        {
            try
            {
                VatBaker.Bake((VatBakeProfile)target);
            }
            catch (VatBakeException e)
            {
                EditorUtility.DisplayDialog("VAT bake", e.Message, "OK");
            }

            serializedObject.Update();
            Refresh();
        }
    }
}
