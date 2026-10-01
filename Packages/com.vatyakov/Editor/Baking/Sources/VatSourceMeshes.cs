using System.Collections.Generic;
using UnityEngine;

namespace VATyakov.Editor
{
    static class VatSourceMeshes
    {
        // Vertices one after another, sub-meshes in order; UV is null only when no part has it.
        public static VatSourceMesh Combine(string name, VatSourceMesh[] parts)
        {
            int vertexCount = 0;
            var subMeshes = new List<VatSourceSubMesh>();
            foreach (var part in parts)
            {
                foreach (var subMesh in part.SubMeshes)
                    subMeshes.Add(Shift(subMesh, vertexCount));
                vertexCount += part.VertexCount;
            }
            return new VatSourceMesh(name, vertexCount, CombineUv(parts, vertexCount), subMeshes.ToArray());
        }

        static VatSourceSubMesh Shift(VatSourceSubMesh subMesh, int offset)
        {
            var indices = new int[subMesh.Indices.Length];
            for (int i = 0; i < indices.Length; i++)
                indices[i] = subMesh.Indices[i] + offset;
            return new VatSourceSubMesh(indices, subMesh.Topology);
        }

        static Vector2[] CombineUv(VatSourceMesh[] parts, int vertexCount)
        {
            if (System.Array.TrueForAll(parts, part => part.Uv0 == null))
                return null;
            var uv = new Vector2[vertexCount];
            int offset = 0;
            foreach (var part in parts)
            {
                part.Uv0?.CopyTo(uv, offset);
                offset += part.VertexCount;
            }
            return uv;
        }
    }
}
