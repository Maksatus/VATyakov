using UnityEngine;

namespace VATyakov.Editor
{
    internal sealed class VatFrame
    {
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
