using UnityEditor;

namespace VATyakov.Editor
{
    static class VatFoldoutHeader
    {
        public static bool Draw(string key, string title)
        {
            bool expanded = SessionState.GetBool(key, true);
            bool next = EditorGUILayout.BeginFoldoutHeaderGroup(expanded, title);
            EditorGUILayout.EndFoldoutHeaderGroup();
            if (next != expanded)
                SessionState.SetBool(key, next);
            if (next)
                EditorGUILayout.Space(2);
            return next;
        }
    }
}
