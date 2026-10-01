using UnityEngine;

namespace VATyakov.Editor
{
    // Topology and UVs shared by all frames.
    sealed class VatSourceMesh
    {
        public readonly string Name;
        public readonly int VertexCount;
        public readonly Vector2[] Uv0;
        public readonly VatSourceSubMesh[] SubMeshes;

        public VatSourceMesh(string name, int vertexCount, Vector2[] uv0, VatSourceSubMesh[] subMeshes)
        {
            Name = name;
            VertexCount = vertexCount;
            Uv0 = uv0;
            SubMeshes = subMeshes;
        }

        public static VatSourceMesh Read(Mesh mesh)
        {
            var uv = mesh.uv;
            return new VatSourceMesh(mesh.name, mesh.vertexCount, uv.Length == mesh.vertexCount ? uv : null, ReadSubMeshes(mesh));
        }

        static VatSourceSubMesh[] ReadSubMeshes(Mesh mesh)
        {
            var subMeshes = new VatSourceSubMesh[mesh.subMeshCount];
            for (int i = 0; i < subMeshes.Length; i++)
                subMeshes[i] = new VatSourceSubMesh(mesh.GetIndices(i, true), mesh.GetTopology(i));
            return subMeshes;
        }
    }
}
