using UnityEngine;
using UnityEngine.Rendering;

namespace VATyakov.Editor
{
    // §1.7: explicit layout, vertices never reordered.
    static class VatVertexMeshBuilder
    {
        const MeshUpdateFlags Flags = MeshUpdateFlags.DontRecalculateBounds;

        public static Mesh Build(string name, VatRestPose rest, VatSourceMesh source, VatSubMeshes subMeshes, Bounds bounds)
        {
            var mesh = new Mesh { name = name };
            SetVertices(mesh, rest, source.Uv0);
            VatIndexBuffer.Set(mesh, source.SubMeshes, source.VertexCount, Flags);
            SetSubMeshes(mesh, source.SubMeshes, subMeshes);
            mesh.bounds = bounds;
            return mesh;
        }

        static void SetVertices(Mesh mesh, VatRestPose rest, Vector2[] uv)
        {
            int count = rest.Positions.Length;
            mesh.SetVertexBufferParams(count, VatVertexFormat.Attributes);
            mesh.SetVertexBufferData(rest.Positions, 0, 0, count, 0, Flags);
            mesh.SetVertexBufferData(VatVertexStream1.Build(rest, uv), 0, 0, count, 1, Flags);
        }

        static void SetSubMeshes(Mesh mesh, VatSourceSubMesh[] source, VatSubMeshes subMeshes)
        {
            mesh.subMeshCount = source.Length;
            int start = 0;
            for (int s = 0; s < source.Length; s++)
            {
                mesh.SetSubMesh(s, Descriptor(source[s], subMeshes, s, start), Flags);
                start += source[s].Indices.Length;
            }
        }

        // §1.1: baseVertex = 0 — D3D and Metal exclude it from SV_VertexID, Vulkan and GLES do not.
        static SubMeshDescriptor Descriptor(VatSourceSubMesh source, VatSubMeshes subMeshes, int index, int start) =>
            new SubMeshDescriptor(start, source.Indices.Length, source.Topology)
            {
                baseVertex = 0,
                bounds = subMeshes.Bounds(index),
                firstVertex = subMeshes.FirstVertex(index),
                vertexCount = subMeshes.VertexCount(index),
            };
    }
}
