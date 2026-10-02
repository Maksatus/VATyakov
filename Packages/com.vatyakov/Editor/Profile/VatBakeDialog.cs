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
            catch (VatBakeException exception)
            {
                EditorUtility.DisplayDialog("VAT", exception.Message, "OK");
            }
        }
    }
}
