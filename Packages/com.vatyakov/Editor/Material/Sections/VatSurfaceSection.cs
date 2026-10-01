using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace VATyakov.Editor
{
    // VAT properties are baker data and stay hidden.
    sealed class VatSurfaceSection : IVatMaterialSection
    {
        static readonly GUIContent BaseMapLabel = new GUIContent("Текстура", "Основная текстура и цвет, на который она умножается.");

        public string Key => "VATyakov.ShaderGUI.Surface";

        public string Title => "Поверхность";

        public void Draw(MaterialEditor editor, MaterialProperty[] properties)
        {
            var baseMap = Find("_BaseMap", properties);
            var baseColor = Find("_BaseColor", properties);
            DrawBase(editor, baseMap, baseColor);
            DrawOthers(editor, properties, baseMap, baseColor);
        }

        static void DrawBase(MaterialEditor editor, MaterialProperty baseMap, MaterialProperty baseColor)
        {
            if (baseMap != null)
                editor.TexturePropertySingleLine(BaseMapLabel, baseMap, baseColor);
            else if (baseColor != null)
                editor.ShaderProperty(baseColor, "Цвет");
        }

        static void DrawOthers(MaterialEditor editor, MaterialProperty[] properties, MaterialProperty baseMap, MaterialProperty baseColor)
        {
            foreach (var property in properties)
                if (property != baseMap && property != baseColor && IsArtistFacing(property))
                    editor.ShaderProperty(property, property.displayName);
        }

        static MaterialProperty Find(string name, MaterialProperty[] properties) => Array.Find(properties, p => p.name == name);

        static bool IsArtistFacing(MaterialProperty property) =>
            !property.name.StartsWith("_Vat", StringComparison.Ordinal) &&
            (property.propertyFlags & ShaderPropertyFlags.HideInInspector) == 0;
    }
}
