using UnityEditor;

namespace VATyakov.Editor
{
    sealed class VatProfileContext
    {
        public readonly SerializedObject SerializedObject;
        public readonly VatProfileModel Model = new VatProfileModel();

        public VatProfileContext(SerializedObject serializedObject)
        {
            SerializedObject = serializedObject;
        }

        public VatBakeProfile Profile => (VatBakeProfile)SerializedObject.targetObject;

        public void Refresh()
        {
            SerializedObject.Update();
            Model.Changed.Call();
        }
    }
}
