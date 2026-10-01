using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace VATyakov.Editor
{
    static class VatIndexBuffer
    {
        public static void Set(Mesh mesh, VatSourceSubMesh[] subMeshes, int vertexCount, MeshUpdateFlags flags)
        {
            var indices = Concat(subMeshes);
            if (vertexCount > ushort.MaxValue + 1)
                Set32(mesh, indices, flags);
            else
                Set16(mesh, indices, flags);
        }

        static void Set32(Mesh mesh, int[] indices, MeshUpdateFlags flags)
        {
            mesh.SetIndexBufferParams(indices.Length, IndexFormat.UInt32);
            mesh.SetIndexBufferData(indices, 0, 0, indices.Length, flags);
        }

        static void Set16(Mesh mesh, int[] indices, MeshUpdateFlags flags)
        {
            mesh.SetIndexBufferParams(indices.Length, IndexFormat.UInt16);
            mesh.SetIndexBufferData(Array.ConvertAll(indices, i => (ushort)i), 0, 0, indices.Length, flags);
        }

        static int[] Concat(VatSourceSubMesh[] subMeshes)
        {
            int count = 0;
            foreach (var subMesh in subMeshes)
                count += subMesh.Indices.Length;

            var indices = new int[count];
            int at = 0;
            foreach (var subMesh in subMeshes)
            {
                Array.Copy(subMesh.Indices, 0, indices, at, subMesh.Indices.Length);
                at += subMesh.Indices.Length;
            }
            return indices;
        }
    }
}
