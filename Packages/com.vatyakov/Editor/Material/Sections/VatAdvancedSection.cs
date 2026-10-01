using UnityEditor;

namespace VATyakov.Editor
{
    sealed class VatAdvancedSection : IVatMaterialSection
    {
        public string Key => "VATyakov.ShaderGUI.Advanced";

        public string Title => "Advanced";

        public void Draw(MaterialEditor editor, MaterialProperty[] properties) => editor.RenderQueueField();
    }
}
