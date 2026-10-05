using UnityEngine;
using UnityEngine.Rendering;

namespace VATyakov.Editor
{
    internal static class VatRigidMeshBuilder
    {
        private const MeshUpdateFlags Flags = MeshUpdateFlags.DontRecalculateBounds;

        public static Mesh Build(string name, VatBoneSkin skin, VatSubMeshes subMeshes, Bounds bounds)
        {
            var mesh = new Mesh { name = name };
            var source = skin.Source;
            var count = source.VertexCount;
            mesh.SetVertexBufferParams(count, VatBoneFormat.Attributes);
            mesh.SetVertexBufferData(VatRigidStream0.Build(skin), 0, 0, count, 0, Flags);
            mesh.SetVertexBufferData(VatVertexStream1.Build(skin.Rest, source.Uv0), 0, 0, count, 1, Flags);
            VatIndexBuffer.Set(mesh, source.SubMeshes, count, Flags);
            subMeshes.Apply(mesh, source.SubMeshes, Flags);
            mesh.bounds = bounds;
            return mesh;
        }
    }
}
