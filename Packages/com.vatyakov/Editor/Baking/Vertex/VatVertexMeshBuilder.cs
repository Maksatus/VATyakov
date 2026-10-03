using UnityEngine;
using UnityEngine.Rendering;

namespace VATyakov.Editor
{
    internal static class VatVertexMeshBuilder
    {
        private const MeshUpdateFlags Flags = MeshUpdateFlags.DontRecalculateBounds;

        public static Mesh Build(string name, VatRestPose rest, VatSourceMesh source, VatSubMeshes subMeshes, Bounds bounds)
        {
            var mesh = new Mesh { name = name };
            SetVertices(mesh, rest, source.Uv0);
            VatIndexBuffer.Set(mesh, source.SubMeshes, source.VertexCount, Flags);
            subMeshes.Apply(mesh, source.SubMeshes, Flags);
            mesh.bounds = bounds;
            return mesh;
        }

        private static void SetVertices(Mesh mesh, VatRestPose rest, Vector2[] uv)
        {
            var count = rest.Positions.Length;
            mesh.SetVertexBufferParams(count, VatVertexFormat.Attributes);
            mesh.SetVertexBufferData(rest.Positions, 0, 0, count, 0, Flags);
            mesh.SetVertexBufferData(VatVertexStream1.Build(rest, uv), 0, 0, count, 1, Flags);
        }
    }
}
