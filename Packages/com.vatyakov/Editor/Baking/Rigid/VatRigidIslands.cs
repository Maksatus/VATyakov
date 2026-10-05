using System.Collections.Generic;
using UnityEngine;

namespace VATyakov.Editor
{
    internal static class VatRigidIslands
    {
        public static int[][] Find(VatSourceMesh mesh)
        {
            var sets = new VatUnionFind(mesh.VertexCount);
            foreach (var subMesh in mesh.SubMeshes)
            {
                Join(sets, subMesh);
            }

            var roots = new Dictionary<int, List<int>>();
            var islands = new List<List<int>>();
            for (var vertex = 0; vertex < mesh.VertexCount; vertex++)
            {
                var root = sets.Find(vertex);
                if (!roots.TryGetValue(root, out var island))
                {
                    island = new List<int>();
                    roots.Add(root, island);
                    islands.Add(island);
                }

                island.Add(vertex);
            }

            return islands.ConvertAll(island => island.ToArray()).ToArray();
        }

        public static int PrimitiveSize(VatSourceSubMesh subMesh)
        {
            var size = subMesh.Topology switch
            {
                MeshTopology.Triangles => 3,
                MeshTopology.Quads => 4,
                MeshTopology.Lines => 2,
                MeshTopology.Points => 1,
                _ => subMesh.Indices.Length,
            };
            return Mathf.Max(size, 1);
        }

        private static void Join(VatUnionFind sets, VatSourceSubMesh subMesh)
        {
            var size = PrimitiveSize(subMesh);
            var indices = subMesh.Indices;
            for (var start = 0; start + size <= indices.Length; start += size)
            {
                for (var corner = 1; corner < size; corner++)
                {
                    sets.Union(indices[start], indices[start + corner]);
                }
            }
        }
    }
}
