using UnityEditor;
using UnityEngine;

namespace VATyakov.Editor
{
    // Read-only object field: pings on click, never reassigns.
    static class VatObjectLinkField
    {
        public static void Draw(GUIContent label, Object target)
        {
            var rect = EditorGUI.PrefixLabel(EditorGUILayout.GetControlRect(), label);
            if (GUI.Button(rect, EditorGUIUtility.ObjectContent(target, target.GetType()), EditorStyles.objectField))
                EditorGUIUtility.PingObject(target);
        }
    }
}
