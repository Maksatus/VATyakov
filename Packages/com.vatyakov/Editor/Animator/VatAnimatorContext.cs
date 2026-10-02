using UnityEditor;

namespace VATyakov.Editor
{
    internal sealed class VatAnimatorContext
    {
        public const string AssetPath = "_asset";
        public const string ClipPath = "_clip";

        public readonly SerializedObject SerializedObject;

        public VatAnimatorContext(SerializedObject serializedObject)
        {
            SerializedObject = serializedObject;
        }
    }
}
