using VATyakov.Editor;
using UnityEngine;

namespace VATyakov.Tests
{
    // Bake pipeline without the asset writer: mesh and texture stay readable.
    sealed class VatInMemoryBake
    {
        public readonly VatLayout Layout;
        public readonly Mesh Mesh;
        public readonly Texture2D Position;
        public Vector3[] Rest { get; private set; }
        public Vector3[] RestNormals { get; private set; }

        public VatInMemoryBake(VatTestRig rig, float fps)
        {
            using var source = new SkinnedFrameSource(rig.Renderer, new[] { rig.Clip });
            Layout = VatLayout.ForVertex(source.Mesh.VertexCount, VatClipRequest.From(source.Clips, fps));
            var encoder = new VertexEncoder(Layout, source.Mesh);
            EncodeAll(source, encoder);
            Mesh = encoder.BuildMesh("InMemory");
            Position = encoder.BuildPositionTexture("InMemory");
        }

        public void Destroy()
        {
            Object.DestroyImmediate(Mesh);
            Object.DestroyImmediate(Position);
        }

        void EncodeAll(IVatFrameSource source, VertexEncoder encoder)
        {
            var frame = new VatFrame(source.Mesh.VertexCount);
            var clip = Layout.Clips[0];
            for (int k = 0; k < clip.FrameCount; k++)
            {
                source.Sample(0, VatMath.LoopFrameTime(k, clip.FrameCount, clip.Length), frame);
                Rest ??= (Vector3[])frame.Positions.Clone();
                RestNormals ??= (Vector3[])frame.Normals.Clone();
                encoder.AddFrame(0, k, frame);
            }
        }
    }
}
