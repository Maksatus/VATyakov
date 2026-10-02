using UnityEditor;

namespace VATyakov.Editor
{
    internal sealed class VatProfileContext
    {
        public readonly SerializedObject SerializedObject;
        public readonly VatProfileModel Model = new();

        public VatBakeProfile Profile => (VatBakeProfile)SerializedObject.targetObject;

        public VatProfileContext(SerializedObject serializedObject)
        {
            SerializedObject = serializedObject;
        }

        public void Refresh()
        {
            SerializedObject.Update();
            Model.Changed.Call();
        }
    }
}
