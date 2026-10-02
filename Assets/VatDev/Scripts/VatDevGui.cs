using UnityEngine;

namespace VATyakov.Dev
{
    internal static class VatDevGui
    {
        public const float Margin = 12f;
        public const float ButtonHeight = 56f;

        private const float ReferenceDpi = 160f;
        private const int ButtonFontSize = 20;

        public static float Scale()
        {
            return Mathf.Max(1f, Screen.dpi / ReferenceDpi);
        }

        public static GUIStyle ButtonStyle(float scale)
        {
            return new GUIStyle(GUI.skin.button) { fontSize = Mathf.RoundToInt(ButtonFontSize * scale) };
        }

        public static GUIStyle LabelStyle(int fontSize, TextAnchor alignment, float scale)
        {
            return new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(fontSize * scale), alignment = alignment };
        }
    }
}
