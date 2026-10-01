using UnityEditor;

namespace VATyakov.Editor
{
    interface IVatMaterialSection
    {
        string Key { get; }

        string Title { get; }

        void Draw(MaterialEditor editor, MaterialProperty[] properties);
    }
}
