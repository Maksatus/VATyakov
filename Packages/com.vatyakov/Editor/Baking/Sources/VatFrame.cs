using UnityEngine;

namespace VATyakov.Editor
{
    internal sealed class VatFrame
    {
        public static readonly Vector3 MissingNormal = Vector3.forward;
        public static readonly Vector4 MissingTangent = new(1f, 0f, 0f, 1f);

        public readonly Vector3[] Positions;
        public readonly Vector3[] Normals;
        public readonly Vector4[] Tangents;

        public VatFrame(int vertexCount)
        {
            Positions = new Vector3[vertexCount];
            Normals = new Vector3[vertexCount];
            Tangents = new Vector4[vertexCount];
        }
    }
}
