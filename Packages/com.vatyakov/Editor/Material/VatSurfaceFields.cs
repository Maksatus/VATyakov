using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace VATyakov.Editor
{
    internal static class VatSurfaceFields
    {
        private static readonly GUIContent _baseMapLabel = new("Base Map", "Base texture and the color it is multiplied by.");

        public static void Draw(MaterialEditor editor, MaterialProperty[] properties)
        {
            var baseMap = Find("_BaseMap", properties);
            var baseColor = Find("_BaseColor", properties);
            DrawBase(editor, baseMap, baseColor);
            DrawOthers(editor, properties, baseMap, baseColor);
        }

        private static void DrawBase(MaterialEditor editor, MaterialProperty baseMap, MaterialProperty baseColor)
        {
            if (baseMap != null)
            {
                editor.TexturePropertySingleLine(_baseMapLabel, baseMap, baseColor);
            }
            else if (baseColor != null)
            {
                editor.ShaderProperty(baseColor, "Color");
            }
        }

        private static void DrawOthers(MaterialEditor editor, MaterialProperty[] properties, MaterialProperty baseMap, MaterialProperty baseColor)
        {
            foreach (var property in properties)
            {
                if (property != baseMap && property != baseColor && IsArtistFacing(property))
                {
                    editor.ShaderProperty(property, property.displayName);
                }
            }
        }

        private static MaterialProperty Find(string name, MaterialProperty[] properties)
        {
            return Array.Find(properties, p => p.name == name);
        }

        private static bool IsArtistFacing(MaterialProperty property)
        {
            return !property.name.StartsWith("_Vat", StringComparison.Ordinal) &&
            (property.propertyFlags & ShaderPropertyFlags.HideInInspector) == 0;
        }
    }
}
