using UnityEngine;
using UnityEngine.Rendering;

namespace VATyakov.Editor
{
    internal static class VatBoneMeshBuilder
    {
        private const MeshUpdateFlags Flags = MeshUpdateFlags.DontRecalculateBounds;

        public static Mesh Build(string name, VatBoneRig rig, VatSourceMesh source, VatSubMeshes subMeshes, Bounds bounds)
        {
            var mesh = new Mesh { name = name };
            var count = source.VertexCount;
            mesh.SetVertexBufferParams(count, VatBoneFormat.Attributes);
            mesh.SetVertexBufferData(VatBoneStream0.Build(rig), 0, 0, count, 0, Flags);
            mesh.SetVertexBufferData(VatVertexStream1.Build(rig.Rest, source.Uv0), 0, 0, count, 1, Flags);
            VatIndexBuffer.Set(mesh, source.SubMeshes, count, Flags);
            subMeshes.Apply(mesh, source.SubMeshes, Flags);
            mesh.bounds = bounds;
            return mesh;
        }
    }
}
