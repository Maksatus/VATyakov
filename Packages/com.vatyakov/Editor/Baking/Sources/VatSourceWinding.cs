using System;
using UnityEngine;

namespace VATyakov.Editor
{
    internal static class VatSourceWinding
    {
        public static VatSourceMesh Reversed(VatSourceMesh mesh)
        {
            return new VatSourceMesh(mesh.Name, mesh.VertexCount, mesh.Uv0, Array.ConvertAll(mesh.SubMeshes, Reversed));
        }

        private static VatSourceSubMesh Reversed(VatSourceSubMesh subMesh)
        {
            var corners = Corners(subMesh.Topology);
            var indices = (int[])subMesh.Indices.Clone();
            for (var start = 0; corners > 0 && start + corners <= indices.Length; start += corners)
            {
                Array.Reverse(indices, start + 1, corners - 1);
            }

            return new VatSourceSubMesh(indices, subMesh.Topology);
        }

        private static int Corners(MeshTopology topology)
        {
            return topology switch
            {
                MeshTopology.Triangles => 3,
                MeshTopology.Quads => 4,
                _ => 0,
            };
        }
    }
}
