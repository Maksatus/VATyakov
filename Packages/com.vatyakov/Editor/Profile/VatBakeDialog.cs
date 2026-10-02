using System;
using UnityEditor;

namespace VATyakov.Editor
{
    internal static class VatBakeDialog
    {
        public static void Run(Action action)
        {
            try
            {
                action();
            }
            catch (VatBakeException e)
            {
                EditorUtility.DisplayDialog("VAT", e.Message, "OK");
            }
        }
    }
}
